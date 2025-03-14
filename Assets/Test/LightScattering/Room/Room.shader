Shader "Room/Plane"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0)
        _Ambient("Ambient", Range(0,1)) = 0
        _Diffuse("Diffuse", Range(0,1)) = 0

        _MainTex("MainTex", 2D) = "white"{}

    }
        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #pragma enable_d3d11_debug_symbols

        struct appdata
        {
            float4 posModel : POSITION;
            float2 uv : TEXCOORD0;
            float3 normal : NORMAL;
        };

        struct v2f
        {
            float4 posCS : SV_POSITION;
            float3 posWS : TEXCOORD1;
            float2 uv : TEXCOORD0;
            float3 normal : NORMAL;
        };

        sampler2D _MainTex;

        CBUFFER_START(UnityPerMaterial)
        half4 _Color;
        float _Ambient;
        float _Diffuse;
        float4 _MainTex_ST;

        CBUFFER_END

        v2f vert(appdata i)
        {
            v2f o;
            o.uv = i.uv;
            o.posWS = TransformObjectToWorld(i.posModel.xyz);
            o.posCS = TransformWorldToHClip(o.posWS);
            o.normal = TransformObjectToWorldNormal(i.normal);
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
                //float4 test = float4(1,1,1,1);
                //float4 t1 = mul(UNITY_MATRIX_V, test);
                //float4 t2 = mul(UNITY_MATRIX_P, test);
                //float4 t3 = mul(unity_WorldToCamera, test);
                //float4 t4 = mul(unity_CameraProjection, test);
                //return t1 + t2 + t3 + t4;

                float4 shadowCoord = TransformWorldToShadowCoord(i.posWS);
                half4 col = half4(0,0,0,1);
                half3 albedo = (1 - tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw).rgb )* _Color;
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
