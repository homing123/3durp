Shader "PostProcessing/TemporalAA"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        { 
            "RenderType"="Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Cull Off
        ZWrite Off
        ZTest Always
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct appdata
        {
            float4 posOS :POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 posCS : SV_POSITION;
        };

        sampler2D _LastFrameSource;
        sampler2D _MainTex;
        CBUFFER_START(UnityPerMaterial)
        float _Weight;
        CBUFFER_END

        v2f vert(appdata v)
        {
            v2f o;
            o.uv = v.uv;
            o.posCS = TransformObjectToHClip(v.posOS.xyz);
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "TemporalAA"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag(v2f i) : SV_TARGET
            {
                half3 curColor = tex2D(_MainTex, i.uv).rgb;
                half3 lastColor = tex2D(_LastFrameSource, i.uv).rgb;
                half3 interpolate = lerp(curColor, lastColor, _Weight);
                return half4(interpolate, 1);
            }
            ENDHLSL
           
        }
    }
}
