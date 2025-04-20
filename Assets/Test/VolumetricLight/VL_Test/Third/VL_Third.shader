Shader "VLTest/Third"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _ShadowDepthTexture("ShadowDepthTexture", 2D) = "white" {}
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
            #include "Assets/Test/VolumetricLight/VL_Test/Third/VoxelLight.hlsl"

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

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _ShadowDepthTexture;
            

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _Samples;
            float _Attenuation;
            float _PhaseG;

            float _SpotlightNear;
            float _SpotlightFar;
            float _SpotlightFov;
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
            float GetDepth(float2 uv)
            {
                return tex2D(_CameraDepthTexture, uv).r;
            }
            float GetLinearDepthUseUV(float2 uv, float near, float far)
            {
                float2 ndc = uv * 2 - 1;
                float depth = tex2D(_CameraDepthTexture, uv).r; //depth pass 에서 (근)0 ~ 1(원) 으로 그려둠
                float4 posDepthVS = mul(UNITY_MATRIX_I_P, float4(ndc, depth, 1));
                posDepthVS = posDepthVS / posDepthVS.w;
                float depthVS = -posDepthVS.z;
                float linearDepth = (depthVS - near) / (far - near); //near = 0, far = 1
                return linearDepth;
            }
            float GetLinearDepthUseWSPos(float3 posWS, float near, float far)
            {
                float4 posVS = mul(unity_WorldToCamera, float4(posWS, 1));
                float linearDepth = (posVS.z - near) / (far - near); //near = 0, far = 1
                return linearDepth;
            }
            float4x4 GetProjectionMat10(float near, float far) //view.z 안뒤집어도 됨
            {
                float FminusN_RCP = 1 / (far - near);
                return float4x4(unity_CameraProjection[0][0], 0, 0, 0,
                    0, unity_CameraProjection[1][1], 0, 0,
                    0, 0, -near * FminusN_RCP, near * far * FminusN_RCP,
                    0, 0, 1, 0);
            }
            float4x4 GetInvProjectionMat10(float4x4 projmat)
            {
                float ARCP = 1 / projmat[0][0];
                float BRCP = 1 / projmat[1][1];
                float C = projmat[2][2];
                float DRCP = 1 / projmat[2][3];
                float ERCP = 1 / projmat[3][2];

                return float4x4(ARCP, 0, 0, 0,
                    0, BRCP, 0, 0,
                    0, 0, 0, ERCP,
                    0, 0, DRCP, -C * ERCP * DRCP);
            }

            float4x4 GetSpotLightProjectionMat10(float fov_degree, float near, float far)
            {
                float FminusN_RCP = 1 / (far - near);
                float t = tan(radians(fov_degree * 0.5f));
                float a = 1 / (2 * t);
                return float4x4(a, 0, 0.5f, 0,
                    0, a, 0.5f, 0,
                    0, 0, -near * FminusN_RCP, near * far * FminusN_RCP,
                    0, 0, 1, 0);
            }

            float4x4 GetSpotLightInvProjectionMat10(float4x4 spotlightProjmat)
            {
                float ARCP = 1 / spotlightProjmat[0][0];
                float C = spotlightProjmat[2][2];
                float DRCP = 1 / spotlightProjmat[2][3];
                float ERCP = 1 / spotlightProjmat[3][2];

                return float4x4(ARCP, 0, 0, -0.5f * ERCP * ARCP,
                    0, ARCP, 0, -0.5f * ERCP * ARCP,
                    0, 0, 0, ERCP,
                    0, 0, DRCP, -C * ERCP * DRCP);
            }

            //Henyey-Greenstein
            float PhaseFunction(float3 p2v, float l2p)
            {
                float _cos = dot(p2v, l2p);
                float squareG = _PhaseG * _PhaseG;
                float bottomRCP = 1 / (4 * PI * pow((1 + squareG - 2 * _PhaseG * _cos), 1.5f));
                return (1 - squareG)  * bottomRCP;
            }

            //UNITY_MATRIX_ 애들은 작동을 제대로 안함 아마 포스트프로세싱이라서 그렇지않을까...
            //unity_~ 얘들은 opengl 기준이라서 near = -1, far = 1임
            //내 depthTexture에는 near = 1, far = 0이 기록되어있음

            half4 frag(v2f i) : SV_Target
            { 

                float3 camPosWS = GetCameraPositionWS();
                float near = GetNear();
                float far = GetFar();
                float4x4 projMat = GetProjectionMat10(near, far);
                float4x4 invProjMat = GetInvProjectionMat10(projMat);
                float4x4 shadowProj = GetSpotLightProjectionMat10(_SpotlightFov, _SpotlightNear, _SpotlightFar);
                float4x4 shadowInvProj = GetSpotLightInvProjectionMat10(shadowProj);
                float4x4 invViewProjMat = mul(unity_CameraToWorld, invProjMat);
                float sampleRCP = 1 / _Samples;
                float PI_RCP = 1 / PI;

                half3 color = tex2D(_MainTex, i.uv).rgb;              
                float depth = tex2D(_CameraDepthTexture, i.uv).r; //near = 1, far = 0 //비선형

                float2 ndc = i.uv * 2 - 1;
                float4 posWS4 = mul(invViewProjMat, float4(ndc, depth, 1));
                float3 posWS = posWS4.xyz / posWS4.w;

                float3 raymarchPos = posWS;
                float raymarchDistance = length(raymarchPos - camPosWS) - near;
                float stepsize = raymarchDistance * sampleRCP;
                float3 stepDir = normalize(camPosWS - raymarchPos);

                int stepCount = 0;
                float moveDis = 0;
                float4 shadowCoord = float4(0, 0, 0, 0);
                float shadowmapDis = 0;

                float Tr = exp(-_Attenuation * raymarchDistance);
                color = color * Tr;
                for(int idx = 0; idx < _Samples; idx++)
                {
                    for(int lightIdx = 0;lightIdx < 8;lightIdx++)
                    {
                        VoxelLight vLight = GetVoxelLight(raymarchPos, lightIdx);
                        float shadow = 1;
                        float disAtt = vLight.distanceAttenuation;
                        float3 lightDir = vLight.direction;

                        float p2vDis = raymarchDistance - stepsize * idx;

                        float tr = exp(-_Attenuation * p2vDis);

                        float f = PhaseFunction(stepDir, -lightDir);
                        color += disAtt * shadow * tr * f * stepsize * vLight.color;
                        raymarchPos += stepsize * stepDir;

                    }
                    // for (int lightIdx = 0; lightIdx < additionalLightCount; lightIdx++)
                    // {
                    //     Light light = GetAdditionalLight(lightIdx, raymarchPos, half4(1, 1, 1, 1));
                    //     float shadow = light.shadowAttenuation;
                    //     float shadowTr = exp(-_Attenuation * length(_AdditionalLightsPosition[lightIdx] - raymarchPos));
                    //     shadow = shadow == 0 ? saturate(pow(length(_AdditionalLightsPosition[lightIdx] - raymarchPos) / 40, 2)) : shadow * shadowTr;
                    //     float lightAttenuation = light.distanceAttenuation;
                    //     float3 p2lDir = light.direction;
                    //     float p2vDis = raymarchDistance - stepsize * idx;

                    //     float tr = exp(-_Attenuation * p2vDis);

                    //     float f = PhaseFunction(stepDir, -p2lDir);
                    //     color += lightAttenuation * shadow * tr * f * stepsize * light.color;
                    //     raymarchPos += stepsize * stepDir;
                    // }
                }
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
