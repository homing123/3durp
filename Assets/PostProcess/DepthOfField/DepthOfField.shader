Shader "PostProcessing/DepthOfField"
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

        Cull Off
        ZWrite Off
        ZTest Always
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/PostProcess/HMMatrix.hlsl"
        #define E 2.71828f
        #pragma enable_d3d11_debug_symbols
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


        sampler2D _OriginTex;
        sampler2D _CameraDepthTexture;
        sampler2D _MainTex;
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _DOFIntensity;
        float _Spread;
        int _GridSize;
        float _Depth;
        CBUFFER_END

        float gaussian(int x)
        {
            float sigmaSqu = _Spread * _Spread;
            return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
        }

        v2f vert(appdata v)
        {
            v2f o;
            o.posCS = TransformObjectToHClip(v.posOS.xyz);
            o.uv = v.uv;
            return o;
        }

        ENDHLSL
            //책에서 텍스쳐캐싱덕에 가로세로 합치는 쪽이 더빠르댔는데 개뻥이넹..
        Pass
        {
            Name "DOF_Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blur

            float4 frag_blur(v2f i) : SV_Target
            {
                float3 color = float3(0,0,0);
                float gridSum = 0;
                int upper = (_GridSize - 1) / 2;
                int lower = -upper;
                for (int y = lower; y <= upper; y++)
                {
                    for (int x = lower; x <= upper; x++)
                    {
                        float centerDis = sqrt(x * x + y * y);
                        float gaussianValue = gaussian(centerDis);
                        gridSum += gaussianValue;
                        float2 uv = i.uv + _MainTex_TexelSize * float2(x, y);
                        color += gaussianValue * tex2D(_MainTex, uv).rgb;
                    }
                }
                color /= gridSum;
                return float4(color, 1);

            }
            ENDHLSL
        }

            Pass
            {
                Name "DOF"
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag_dof

                float4 frag_dof(v2f i) : SV_Target
                {
                    float3 camPosWS = GetCameraPositionWS();
                    float depth = tex2D(_CameraDepthTexture, i.uv).r; //near = 1, far = 0 //비선형
                    float2 ndc = i.uv * 2 - 1;
                    float4 posVS4 = mul(_InvProjMat10, float4(ndc, depth, 1));
                    float3 posVS = posVS4.xyz / posVS4.w;
                    float linearDepth = (posVS.z - _CamNear) / (_CamFar - _CamNear);

                    float3 originColor = tex2D(_OriginTex, i.uv).rgb;
                    float3 blurColor = tex2D(_MainTex, i.uv).rgb;

                    float depthDis = saturate(abs(_Depth - linearDepth) * _DOFIntensity);

                    float3 col = lerp(originColor, blurColor, depthDis);
                    return half4(col, 1);
                }
            ENDHLSL
        }
    }
}
