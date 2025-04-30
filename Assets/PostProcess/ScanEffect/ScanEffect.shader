Shader "PostProcessing/ScanEffect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _GridTex("GridTexture", 2D) = "white" {}
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
            sampler2D _GridTex;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half4 _GridColor;
            float _Range;
            float _TotalTime;
            float _LineWidth;
            float _CurTime;
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
            half4 frag(v2f i) : SV_Target
            {
                float3 camPosWS = GetCameraPositionWS();
                float near = GetNear();
                float far = GetFar();
                float4x4 projMat = GetProjectionMat10(near, far);
                float4x4 invProjMat = GetInvProjectionMat10(projMat);
                float2 ndc = i.uv * 2 - 1;
                half3 color = tex2D(_MainTex, i.uv).rgb;

                float depth = tex2D(_CameraDepthTexture, i.uv).r; //near = 1, far = 0 //비선형

                float4 posVS = mul(invProjMat, float4(ndc, depth, 1));
                posVS = posVS / posVS.w;
                float linearDepth = (posVS.z - near) / (far - near); //near = 0, far = 1

                float3 posWS = mul(unity_CameraToWorld, float4(posVS.xyz, 1)).xyz;

                float distanceXZ = length(camPosWS.xz - posWS.xz);

                float time01 = saturate(_CurTime / _TotalTime);
                float curTimeMaxDis = _Range * time01;
                float curTimeMinDis = curTimeMaxDis - _LineWidth;
                float curTimeGridMinDis = curTimeMaxDis - _LineWidth * 3;
                float colorWeight = curTimeMaxDis < distanceXZ ? 0 : saturate((distanceXZ - curTimeMinDis) / (curTimeMaxDis - curTimeMinDis));
                float gridWeight = curTimeMaxDis < distanceXZ ? 0 : saturate((distanceXZ - curTimeGridMinDis) / (curTimeMaxDis - curTimeGridMinDis));

                half gridValue = tex2D(_GridTex, posWS.xz * 0.2f).r < 0.7f ? 1 : 0;

                half3 gridColor = _GridColor * gridValue * gridWeight;
                half3 scanColor = colorWeight * _Color.a * _Color + gridColor;
                float totalAlpha = saturate(1 - (time01 - 0.5f) * 2); // 0.5 ~ 1 => 1 ~ 0
                color += scanColor * totalAlpha;


                return half4(color, 1);



                
            }
            ENDHLSL
        }
    }
}
