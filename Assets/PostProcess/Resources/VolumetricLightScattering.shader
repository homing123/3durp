Shader "PostProcessing/VLS"
{
    Properties
    { 
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ UNITY_UV_STARTS_AT_TOP


            struct appdata
            {
                float4 posOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 posCS : SV_POSITION;
            };
            sampler2D _CameraDepthTexture;
            sampler2D _MainTex;
            CBUFFER_START(UnityPerMaterial)
            float3 _LightPos;
            float _Density;
            float _Decay;
            float _Exposure;
            float _Weight;
            int _Samples;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(v.posOS.xyz);
                o.uv = v.uv;
                return o;
            }
            float GetFar()
            {
                return unity_CameraProjection[2][3] / (unity_CameraProjection[2][2] + 1);
            }
            float GetNear()
            {
                return unity_CameraProjection[2][3] / (unity_CameraProjection[2][2] - 1);
            }

            float Phase(float3 inDir, float3 outDir)
            {
                float cosAngle = dot(inDir, outDir) / (length(inDir) * length(outDir));
                float nom = 3.0f * (1.0f + cosAngle * cosAngle);
                float denom = 16.0f * 3.141592f;
                return nom / denom;
            }

            /*float4x4 mat1 = GetWorldToViewMatrix(); //not work
            float4x4 mat2 = UNITY_MATRIX_V; //not work
            float4x4 mat3 = unity_WorldToCamera; //work
            float4x4 tempmat = mat1 + mat2 + mat3;
            float4 temp = mul(tempmat, float4(1, 1, 1, 1));
            return half4(temp);*/

            //            half4 frag(v2f i) : SV_Target
            //            {   
            //                float near = GetNear();
            //                float far = GetFar();
            //                float3 camPosWS = GetCameraPositionWS();
            //#ifdef UNITY_UV_STARTS_AT_TOP
            //                i.uv.y = 1 - i.uv.y;
            //#endif
            //                float2 ndc = i.uv * 2 - 1;
            //                float depth = tex2D(_CameraDepthTexture, i.uv).r;
            //
            //                float4 posNearVS = mul(UNITY_MATRIX_I_P, float4(ndc, 1, 1));
            //                posNearVS = posNearVS / posNearVS.w;
            //                posNearVS.z = near;
            //                float4 posNearWS = mul(unity_CameraToWorld, posNearVS);
            //
            //                float3 rayEntry = posNearWS;
            //                float3 rayDir = normalize(rayEntry - camPosWS);
            //                float rayStepDis = length(_Scale) / _Samples;
            //                
            //                float4 posDepthVS = mul(UNITY_MATRIX_I_P, float4(ndc, depth, 1));
            //                posDepthVS = posDepthVS / posDepthVS.w;
            //                float depthVS = -posDepthVS.z;
            //                float linearDepth = (depthVS - near) / (far - near); //near = 0, far = 1
            //
            //                float3 L = float3(0, 0, 0);
            //                int steps = 0;
            //                for (int idx = 0; idx < _Samples; idx++)
            //                {
            //                    float3 rayPos = rayEntry + rayDir * rayStepDis * (idx + 1);
            //                    //rayCoord = (rayOrigin - _Position) / _Scale + 0.5f;
            //                    float3 curPosVS = mul(unity_WorldToCamera, float4(rayPos, 1));
            //                    float z = (curPosVS.z - near) / (far - near);
            //
            //                    if (z >= linearDepth) { break; }
            //                    float4 shadowCoord = TransformWorldToShadowCoord(rayPos);
            //                    Light mainLight = GetMainLight(shadowCoord);
            //                    float shadow = mainLight.shadowAttenuation; // shadowattenuation = 1 그림자 진거, 0 = 그림자 안진거
            //                    float3 lightDir = mainLight.direction;
            //                    float lightDis = 70 - rayStepDis * idx;
            //                    float viewDis = length(rayPos - camPosWS);
            //                    float3 viewDir = normalize(camPosWS - rayPos);
            //                    float L_in = exp(-lightDis * _TauScattering) * shadow * 0.3f / (4.0f * 3.141592f * lightDis * lightDis);
            //                    float3 L_i = L_in * _TauScattering * mainLight.color * Phase(lightDir, viewDir);
            //                    L += L_i * exp(-viewDis * _TauScattering) * rayStepDis;
            //                    steps++;
            //                    //if (rayCoord.x < 0.0f || rayCoord.x > 1.0f || rayCoord.y < 0.0f || rayCoord.y > 1.0f || rayCoord.z < 0.0f || rayCoord.z > 1.0f) { break; }
            //                }
            //                // sample the texture
            //
            //                return half4(L, 1);
            //            }


            half4 frag(v2f i) : SV_Target
            {
                half3 color = tex2D(_MainTex, i.uv).rgb;

                float3 camPosWS = GetCameraPositionWS();
                float3 lightPosWS = camPosWS + _LightPos; // 라이트 위치는 스카이박스에서의 위치이므로 카메라위치 더해줘야함
                float4 lightPosClip = mul(UNITY_MATRIX_VP, float4(lightPosWS, 1));
                float3 lightPosNDC = lightPosClip.xyz / lightPosClip.w;
                float2 lightPosUV = (lightPosNDC.xy + 1) * 0.5f;
                lightPosUV = float2(0.5f, 0.5f);

                float2 toLight = lightPosUV - i.uv;
                float2 toLightDir = normalize(toLight);
                float deltaDis = 1 / (float)_Samples * _Density;

               
                half illuminationDecay = 1.0f;

                float2 curPos = i.uv;
                [loop]
                for (int idx = 0; idx < _Samples; idx++)
                {
                    curPos += deltaDis * toLightDir;
                    if (curPos.x > 1 || curPos.x < 0 || curPos.y > 1 || curPos.y < 0) { break; }
                    half3 curColor = tex2D(_MainTex, curPos);
                    curColor *= illuminationDecay * _Weight;
                    color += curColor;
                    illuminationDecay *= _Decay;
                }
                return half4(color * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
