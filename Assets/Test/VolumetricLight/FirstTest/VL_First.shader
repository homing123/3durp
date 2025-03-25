Shader "VLTest/First"
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
            float _TAU;
            float3 _LightPos;
            float _PHI;
            float _Add;
            float _Out;
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
                float depth = tex2D(_CameraDepthTexture, uv).r; //depth pass ���� (��)0 ~ 1(��) ���� �׷���
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
            float4x4 GetProjectionMat10(float near, float far) //view.z �ȵ���� ��
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

             //float GetAdditionalLightDepthInWorldSpace(float3 worldPos, float4 shadowCoord, float near, float far)
             //{
             //    // �׸��� �ʿ��� ���� ���̰� ���ø�
             //    // _AdditionalLightsShadowmapTexture�� �׸��� �� �ؽ�ó
             //    float rawDepth = SAMPLE_TEXTURE2D(_AdditionalLightsShadowmapTexture, sampler_AdditionalLightsShadowmapTexture, shadowCoord.xy).r;
    
             //    // ���� ���̰��� 0-1 �������� ���� �� ���� ���̷� ��ȯ
             //    // �� �κ��� URP�� ��Ȯ�� ������ ���� �޶��� �� ����
             //    // �Ϲ����� ����ȭ ���� ���:
             //    float linearDepth = (near * far) / (far - rawDepth * (far - near));
    
             //    // ���� ���� �������� (0�� �ε����� �߰� ����)
             //    float3 lightDir = _AdditionalLightsPosition[0].xyz - worldPos;
             //    float lightDistance = length(lightDir);
             //    lightDir = normalize(lightDir);
    
             //    // ���� ���̸� ���� ���������� ���� ���� �Ÿ��� ��ȯ
             //    // �� �κ��� ���� Ÿ�԰� ���� ��Ŀ� ���� �޶��� �� ����
             //    float worldSpaceDepth = linearDepth * lightDistance / shadowCoord.w;
    
             //    return worldSpaceDepth;
             //}

            //UNITY_MATRIX_ �ֵ��� �۵��� ����� ���� �Ƹ� ����Ʈ���μ����̶� �׷���������...
            //unity_~ ����� opengl �����̶� near = -1, far = 1��
            //�� depthTexture���� near = 1, far = 0�� ��ϵǾ�����

            half4 frag(v2f i) : SV_Target
            {

                 half3 color = tex2D(_ShadowDepthTexture, i.uv).r;
                return half4(color, 1);
//                float3 camPosWS = GetCameraPositionWS();
//                float near = GetNear();
//                float far = GetFar();
//                float4x4 projMat = GetProjectionMat10(near, far);
//                float4x4 invProjMat = GetInvProjectionMat10(projMat);
//                float sampleRCP = 1 / _Samples;
//                float2 ndc = i.uv * 2 - 1;
//                half3 color = tex2D(_MainTex, i.uv).rgb;
//
//                float depth = tex2D(_CameraDepthTexture, i.uv).r; //near = 1, far = 0 //����
//
//                float4 posVS = mul(invProjMat, float4(ndc, depth, 1));
//                posVS = posVS / posVS.w;
//                float linearDepth = (posVS.z - near) / (far - near); //near = 0, far = 1
//
//                float3 posWS = mul(unity_CameraToWorld, float4(posVS.xyz, 1)).xyz;
//                half curshadow = AdditionalLightRealtimeShadow(0, posWS);
//
////#if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
////#if !USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
////                float4 additionalLigthShadowCoord = mul(_AdditionalLightsWorldToShadow[0], float4(posWS, 1.0f));
////                additionalLigthShadowCoord.xyz = additionalLigthShadowCoord.xyz / additionalLigthShadowCoord.w;
////                //float shadowDepth = SAMPLE_TEXTURE2D_SHADOW(_AdditionalLightsShadowmapTexture, sampler_LinearClampCompare, additionalLigthShadowCoord.xyz).r;
////                float shadowDepth = tex2D(_ShadowDepth, additionalLigthShadowCoord.xy).r;
////                color.rgb = shadowDepth;
////                return half4(color, 1);
////#endif
////#endif
////                return half4(1, 0, 0, 1);
//                
//
//                float3 raymarchPos = posWS;
//                float raymarchDistance = length(raymarchPos - camPosWS);
//                float stepsize = raymarchDistance * sampleRCP;
//                float3 stepDir = (camPosWS - raymarchPos) / raymarchDistance;
//
//                half3 additionalColor = half3(0, 0, 0);
//                int additionalLightCount = GetAdditionalLightsCount();
//                
//                float PI_RCP = 1 / PI;
//                int stepCount = 0;
//                float moveDis = 0;
//                for (float l = raymarchDistance; l > stepsize; l -= stepsize)
//                {
//                    Light light = GetAdditionalLight(0, raymarchPos, half4(1,1,1,1));
//                    float shadow = light.shadowAttenuation;
//
//                //     // �߰� ������ ��ġ�� ���
//                //     float3 lightPos = _AdditionalLightsPosition[0].xyz;
//                //     // ���� ��ġ���� ��������� �Ÿ� ���
//                //     float distanceToLight = length(lightPos - raymarchPos);
//
//                //     // �׸��� �ʿ��� ���ø��Ͽ� ����� ���̰� ���
//                //     float shadowMapDepth = SAMPLE_TEXTURE2D_SHADOW(_AdditionalLightsShadowmapTexture, sampler_AdditionalLightsShadowmapTexture, shadowCoord.xyz);
//                // color.rgb = shadowMapDepth;
//                // break;
//                //    // float linearDepth = (near * far) / (zFar - rawDepth * (zFar - zNear));
//                //     shadow = saturate(shadow + pow(moveDis / _TAU, _PHI));
//                    //shadow = 1;
//                    float lightDisAtt = light.distanceAttenuation;
//
//                    float3 toLightDir = normalize(raymarchPos - _LightPos);
//                    float ldotv = abs(dot(toLightDir, stepDir));
//                    
//                    float additionScattering = (ldotv * _Add) * stepsize * shadow * lightDisAtt;
//                    color = color * (1 - _Out* stepsize) + light.color * lightDisAtt * _Out * stepsize;
//
//                    
//                    color += light.color * additionScattering;
//                    raymarchPos+= stepDir * stepsize;
//                    moveDis+= stepsize;
//                }
//                // float temp = stepCount / _Samples;
//                // if(i.uv.y < 0.3f)
//                // {
//                //     return half4(0.5f,0.5f,0.5f, 1);
//                //     }
//                // return half4(temp, temp, temp, 1);
//
//                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
