Shader "VLTest/First"
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

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _Samples;
            float _TAU;
            float3 _LightPos;
            float _PHI;
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
                float sampleRCP = 1 / _Samples;
                float2 ndc = i.uv * 2 - 1;
                half3 color = tex2D(_MainTex, i.uv).rgb;

                float depth = tex2D(_CameraDepthTexture, i.uv).r; //near = 1, far = 0 //비선형

                float4 posVS = mul(invProjMat, float4(ndc, depth, 1));
                posVS = posVS / posVS.w;
                float linearDepth = (posVS.z - near) / (far - near); //near = 0, far = 1

                float3 posWS = mul(unity_CameraToWorld, float4(posVS.xyz, 1)).xyz;
                float3 raymarchPos = posWS;
                float raymarchDistance = length(raymarchPos - camPosWS);
                float stepsize = raymarchDistance * sampleRCP;
                float3 stepDir = (camPosWS - raymarchPos) / raymarchDistance;

                half3 additionalColor = half3(0, 0, 0);
                int additionalLightCount = GetAdditionalLightsCount();
                
                float PI_RCP = 1 / PI;
                int stepCount = 0;
                for (float l = raymarchDistance; l > stepsize; l -= stepsize)
                {
                    float4 shadowCoord = TransformWorldToShadowCoord(raymarchPos);
                    Light light = GetAdditionalLight(0, raymarchPos, shadowCoord);
                    float shadow = light.shadowAttenuation;
                    float lightDis = length(raymarchPos - _LightPos);
                    float d = length(raymarchPos - camPosWS);
                    float dRCP = 1 / d;
                    
                    //float intens = _TAU * (shadow * (_PHI * 0.25f * PI_RCP) * dRCP * dRCP) * exp(-d * _TAU) * exp( -_TAU * lightDis) * stepsize;
                    float intens = _PHI * shadow * light.distanceAttenuation * stepsize;
                    half3 lightColor = light.color;
                    additionalColor += intens * lightColor;
                    raymarchPos += stepsize * stepDir;
                   
                    
                }
              /*  float temp = stepCount / _Samples * stepsize;
                temp = totalShadow / _Samples;*/
                //return half4(temp, temp, temp, 1);
                return half4(color + additionalColor * sampleRCP, 1);
            }
            ENDHLSL
        }
    }
}
