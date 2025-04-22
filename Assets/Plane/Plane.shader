Shader "Terrain/Plane"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Ambient("Ambient", Range(0,1)) = 0
        _Diffuse("Diffuse", Range(0,1)) = 0

        _NoiseColor("NoiseColor", Color) = (1,1,1,1)
        _NoiseTex("NoiseTex", 2D) = "white"{}
        _NoiseBias("NoiseBias" , Range(0,1)) = 1
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque" 
            "Queue" = "Geometry"
        }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/Plane/PlaneHeight.hlsl"
        #pragma enable_d3d11_debug_symbols

        struct appdata
        {
            float4 posModel : POSITION;
        };

        struct v2f
        {
            float4 posCS : SV_POSITION;
            float3 posWS : TEXCOORD1;
            float2 uv : TEXCOORD0;
            float3 normal : NORMAL;
        };

        sampler2D _NoiseTex;

        CBUFFER_START(UnityPerMaterial)
        half4 _Color;
        float _Ambient;

        float4 _NoiseTex_ST;
        float4 _NoiseColor;
        float _NoiseBias;

        float4 _HeightMap_ST;
        float4 _NormalMap_ST;

        int _VertexCount;
        int _Quality;
        int _MeshSize;
        CBUFFER_END

        v2f vert(appdata i)
        {
            v2f o;
            float2 meshUV = saturate(i.posModel.xz / _MeshSize + 0.5f);
            float dTexUV = 1 / _VertexCount;
            float2 texUV = meshUV * (1 - dTexUV) + (dTexUV * 0.5f);
            o.uv = texUV;

            o.posWS = TransformObjectToWorld(i.posModel.xyz);
            o.posWS.y = GetHeight(o.posWS.xz, _Quality);

            //퀄리티 2이상부터는 uv 0.25~0.75 일때 높이를 낮춰야함  0 ~ 0.26 ~ 0.5 ~ 0.74 ~ 1 =>  0 ~ 0 ~ 1 ~ 0 ~ 0
            float2 temp = abs(meshUV- 0.5f); //0.5로 부터 uv거리  0.5 ~ 0.24 ~ 0 ~ 0.24 ~ 0.5
            float2 weight = temp - 0.24f; //0.26 ~ 0 ~ -0.24 ~ 0 ~ 0.26
            weight = saturate(-weight); //0 ~ 0 ~ 0.24 ~ 0 ~ 0
            weight *= 100 / (float)24; //0 ~ 0 ~ 1 ~ 0 ~ 0
            weight *= 10;
            o.posWS.y -= min(weight.x, weight.y) * (_Quality > 1 ? 1 : 0);
            o.posCS = TransformWorldToHClip(o.posWS);
            o.normal = GetNormal(o.posWS.xz, _Quality);
            return o;
        }
        ENDHLSL

        Pass
        {   
            Name "Plane_ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            half3 MainLightCalc(Light light, float3 normal, half3 albedo, half3 ambient)
            {
                float ndotl = saturate(dot(normal, light.direction));
                half3 diffuse = albedo * light.color * ndotl * light.shadowAttenuation;
                half3 ambientInShadow = albedo * light.color * ndotl * (1 - light.shadowAttenuation) * ambient;

                return diffuse + ambientInShadow;
            }
            half3 AdditionalLightCalc(Light light, float3 normal, half3 albedo)
            {                
                float ndotl = saturate(dot(normal, light.direction));
                half3 diffuse = albedo * light.color * ndotl * light.shadowAttenuation * light.distanceAttenuation;
                return diffuse;
            }
            half4 frag(v2f i) : SV_Target
            {

                float4 shadowCoord = TransformWorldToShadowCoord(i.posWS);
                half4 col = half4(0,0,0,1);
                float noiseValue = tex2D(_NoiseTex, i.posWS.xz * _NoiseTex_ST.xy + _NoiseTex_ST.zw).r;
                half3 albedo = noiseValue > _NoiseBias ? _NoiseColor : _Color.rgb;
                float3 normal = normalize(i.normal);
                Light mainLight = GetMainLight(shadowCoord);
                col.rgb += MainLightCalc(GetMainLight(shadowCoord), normal, albedo, _Ambient);

                float ndotl = saturate(dot(normal, mainLight.direction));

                int additionalLightCount = GetAdditionalLightsCount();
                for (int idx = 0; idx < additionalLightCount; idx++)
                {
                    Light light = GetAdditionalLight(idx, i.posWS, shadowCoord);
                    col.rgb += AdditionalLightCalc(light, normal, albedo);
                }
                col.rgb += _Ambient * albedo;
                return col;
            }

            ENDHLSL
        }

        Pass
        {

            Name "Plane_DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            ZWrite On
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            #pragma vertex vert
            #pragma fragment DepthonlyFrag

            half DepthonlyFrag(v2f i) : SV_Target
            {
                return i.posCS.z;
            }
            ENDHLSL
        }

        Pass
        {

            Name "Plane_DepthNormalOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }
            ZWrite On
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            #pragma vertex vert
            #pragma fragment DepthNormalsonlyFrag

            half4 DepthNormalsonlyFrag(v2f i) : SV_Target
            {
                half depth = i.posCS.z;
                float3 normal = normalize(i.normal);
                normal = normal * 0.5f + 0.5f; // -1~1 => 0~1
                return half4(normal, depth);
            }
            ENDHLSL
        }
      
        
    }
}
