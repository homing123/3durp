Shader "Cel/Cel_BothFaces"
{
    Properties
    {
        _MainColor("Color", Color) = (1,1,1,1)
        _Specular("Specular", Color) = (1,1,1)
        _Ambient("Ambient", Color) = (1,1,1)
        _Shiness("Shiness", Range(0, 50)) = 0

        _MainTex("MainTex", 2D) = "white"{}
        _ToonTex("ToonTex", 2D) = "white"{}

       
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            CBUFFER_START(UnityPerMaterial)
             half4 _MainColor;
            half3 _Specular;
            half3 _Ambient;
            float _Shiness;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _ToonTex;
            float4 _ToonTex_ST;
            CBUFFER_END


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 color;
                color = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw).rgb;
            

            return half4(color, 1);

            }
            ENDHLSL
        }
    }
}
