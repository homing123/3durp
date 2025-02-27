Shader "Unlit/testshader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };


            v2f vert (appdata v, uint vertIdx : SV_VertexID)
            {

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = v.normal.rbg;



                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //normal.zzz * 0.5f + 0.5f) * 2 - 0.5f) - 0.2f) * 0.5f, 1);
                // sample the texture
                fixed4 col = fixed4(0,0,0,1);
                float3 dirLight = normalize(float3(-1,0,1));
                float3 normal = normalize(i.normal);
                normal = i.normal;
                col.rgb = normal.zzz;
                //col.rgb = pow(normal.zzz,2.22f);
                
                /*col.rgb = (normal.zzz * 0.5f + 0.5f) *10 -3.72f;
                if (col.r < 0 || col.r > 1)
                {
                    col.r = 1;
                    col.gb = 0;
                }*/
                return col;
            }
            ENDCG
        }
    }
}
