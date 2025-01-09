Shader "Plane/Grass"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {   
            Name "Plane"
            Tags{
            "LightMode" = "UniversalForwardOnly"
                }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 posModel : POSITION;
                float2 uv : TEXCOORD;
            };
            struct v2f
            {
                float4 posCS : SV_POSITION;
                float2 uv : TEXCOORD;
            };
            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            CBUFFER_END

            v2f vert(appdata i)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(i.posModel.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 col;
                col = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);
                return col;
            }

            ENDHLSL
        }

     
      
        
    }
}
