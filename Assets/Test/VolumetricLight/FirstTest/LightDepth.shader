Shader "Custom/LightDepth"
{
    Properties
    {
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
            };

            struct v2f
            {
                float4 posCS : SV_POSITION;
                float3 posWS : Texcoord1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(v.posOS.xyz);
                o.posWS = TransformObjectToWorld(v.posOS.xyz);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(2,1,0.5f,0.3f);
            }
            ENDHLSL
        }
    }
}
