Shader "PP/GrayScale"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque", "RenderPipeline" = "UniversalPipeline"}
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

            #pragma vertex FullscreenVert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Amount;

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                half4 col = tex2D(_MainTex, uv);
                float3 grayscale = col.rgb * 0.33333f;
                col.rgb = lerp(col.rgb, grayscale, _Amount);
                return col;
            }
            ENDHLSL
        }
    }
}
