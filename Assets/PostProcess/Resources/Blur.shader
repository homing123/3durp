Shader "PostProcessing/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Spread("Standard Deviation (Spread)", Float) = 0
        _GridSize("Grid Size", Integer) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline" //다른 파이프라인에서 셰이더가 사용되는것을 방지
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #define E 2.71828f
        sampler2D _MainTex;
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        uint _GridSize;
        float _Spread;
        CBUFFER_END

        float gaussian(int x)
        {
            float sigmaSqu = _Spread * _Spread;
            return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
        }
        struct appdata
        {
            float4 posOS : POSITION;
            float2 uv : TEXCOORD0;
        };
        struct v2f
        {
            float4 posCS : SV_POSITION;
            float2 uv :TEXCOORD0;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.posCS = TransformObjectToHClip(v.posOS.xyz);
            o.uv = v.uv;
            return o;
        }

        ENDHLSL

        Pass
        {
            Name "Horizontal"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_horizontal

            float4 frag_horizontal(v2f i) : SV_Target
            {
                float3 col = float3(0,0,0);
                float gridSum = 0;
                int upper = (_GridSize - 1) / 2;
                int lower = -upper;
                for (int x = lower; x <= upper; x++)
                {
                    float gaussianValue = gaussian(x);
                    gridSum += gaussianValue;
                    float2 uv = i.uv + float2(_MainTex_TexelSize.x * x, 0);
                    col += gaussianValue * tex2D(_MainTex, uv).xyz;
                }

                col /= gridSum; //가우시안 합이 정확히 1이 안될때를 방지
                return float4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Vertical"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_Vertical

            float4 frag_Vertical(v2f i) : SV_Target
            {
                float3 col = float3(0,0,0);
                float gridSum = 0;
                int upper = (_GridSize - 1) / 2;
                int lower = -upper;
                for (int y = lower; y <= upper; y++)
                {
                    float gaussianValue = gaussian(y);
                    gridSum += gaussianValue;
                    float2 uv = i.uv + float2(0, _MainTex_TexelSize.y * y);
                    col += gaussianValue * tex2D(_MainTex, uv).xyz;
                }

                col /= gridSum; //가우시안 합이 정확히 1이 안될때를 방지
                return float4(col, 1);
            }
            ENDHLSL
        }
    }
}
