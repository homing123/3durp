Shader "Plane/Grass"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Ambient("Ambient", Range(0,1)) = 0
        _Diffuse("Diffuse", Range(0,1)) = 0

        _NoiseColor("NoiseColor", Color) = (1,1,1,1)
        _NoiseTex("NoiseTex", 2D) = "white"{}
        _NoiseBias("NoiseBias" , Range(0,1)) = 1
    
        _Skybox("Skybox", cube) = "white" {}
        _HeightMap("heightMap", 2D) = "white"{}
        _HighQualityHeightMap("highQualityHeightMap", 2D) = "white"{}
        _NormalMap("normalMap", 2D) = "white"{}

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {   
            Name "Plane"
            Tags{
            "LightMode" = "UniversalForwardOnly"
                }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            struct appdata
            {
                float4 posModel : POSITION;
            };
            struct v2f
            {
                float4 posCS : SV_POSITION;
                float3 posWS : TEXCOORD3;
                float2 uv : TEXCOORD0;
                float4 shadowCoord : TEXCOORD2;
                float fogFactor : TEXCOORD1;
                float3 normal : NORMAL;
            };
            sampler2D _NoiseTex;
            samplerCUBE _Skybox;

            sampler2D _HeightMap;
            sampler2D _HighQualityHeightMap;
            sampler2D _NormalMap;
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Ambient;

            float4 _NoiseTex_ST;
            float4 _NoiseColor;
            float _NoiseBias;

            float4 _HeightMap_ST;
            float4 _HighQualityHeightMap_ST;
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
                //o.posWS.y = tex2Dlod(_HeightMap, float4(o.uv * _HeightMap_ST.xy + _HeightMap_ST.zw, 0, 0)).r;
                float2 temp = abs(o.uv - 0.5f); //0.5로 부터 uv거리
                float2 weight = saturate(temp * -4 + 1) * 10; //0.5 ~ 0 => -1 ~ 1, 0.25 ~ 0 => 0 ~ 1
                //weight = 0;
                //o.posWS.y -= min(weight.x, weight.y) * (_Quality > 1 ? 1 : 0);
                o.posCS = TransformWorldToHClip(o.posWS);

                VertexPositionInputs vInputs = GetVertexPositionInputs(i.posModel.xyz);
                o.shadowCoord = GetShadowCoord(vInputs);
                o.normal = tex2Dlod(_NormalMap, float4(o.uv * _NormalMap_ST.xy + _NormalMap_ST.zw, 0, 0)).rbg;
                o.fogFactor = ComputeFogFactor(o.posCS.z);
                

                //only built in
                //UNITY_TRANSFER_FOG(o,o.posCS); //o안의 fog값에 필요한 변수들을 알아서 채워줌 설정에 따라 달라지기 때문에 매크로로 묶어둔거라고함

                return o;
            }

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
                return half4((normal.zzz * 0.5f + 0.5f) * 2 - 0.5f, 1);

                int additionalLightCount = GetAdditionalLightsCount();
                for (int idx = 0; idx < additionalLightCount; idx++)
                {
                    Light light = GetAdditionalLight(idx, i.posWS, shadowCoord);
                    col.rgb += AdditionalLightCalc(light, normal, albedo);
                }
                col.rgb += _Ambient * albedo;
               

               


                ////col.rgb = half3(i.fogFactor,0,0); //near = 1 far = 0 not linear 

                //float3 camPosWS = GetCameraPositionWS();
                //float3 viewDir = normalize(i.posWS - camPosWS);
                //float3 reflectVector = reflect(viewDir, normal);

                //float4 skyColor = texCUBE(_Skybox, reflectVector);
                ////col.rgb = skyColor.rgb;
                //col.rgb = col.rgb * i.fogFactor + (1 - i.fogFactor)* skyColor.rgb;
                //col.rgb = normal;

                return col;
            }

            ENDHLSL
        }

     
      
        
    }
}
