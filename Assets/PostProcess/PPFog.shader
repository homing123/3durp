Shader "PP/Fog"
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
            //#pragma vertex FullscreenVert
            //#pragma fragment frag
            //// make fog work
            //#pragma multi_compile_fog

            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

            //struct appdata
            //{
            //    float4 vertex : POSITION;
            //    float2 uv : TEXCOORD0;
            //};

            //struct v2f
            //{
            //    float2 uv : TEXCOORD0;
            //    UNITY_FOG_COORDS(1)
            //    float4 vertex : SV_POSITION;
            //};

            //sampler2D _MainTex;
            //float4 _MainTex_ST;

            //v2f vert (appdata v)
            //{
            //    v2f o;
            //    o.vertex = UnityObjectToClipPos(v.vertex);
            //    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            //    UNITY_TRANSFER_FOG(o,o.vertex);
            //    return o;
            //}

            //fixed4 frag (v2f i) : SV_Target
            //{
            //    // sample the texture
            //    fixed4 col = tex2D(_MainTex, i.uv);
            //    // apply fog
            //    UNITY_APPLY_FOG(i.fogCoord, col);
            //    return col;
            //}
            ENDPROGRAM
        }
    }
}
