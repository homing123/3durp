Shader "Unlit/Normal"
{
    Properties
    {
        _NormalDis("NormalDistance", Range(0.1, 1)) = 0.5
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _NormalDis;
            CBUFFER_END
            struct appdata
            {
                float3 posModel : POSITION;
                float3 normal : NORMAL;
            };
            struct v2g
            {
                float4 posWorld : SV_POSITION;
                float3 normalWorld : NORMAL;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float3 color : COLOR;
            };


            v2g vert (appdata v)
            {
                v2g o;
                o.posWorld = float4(TransformObjectToWorld(v.posModel),1);
                o.normalWorld = TransformObjectToWorldNormal(v.normal.xzy);
                return o;
            }
            
            [maxvertexcount(2)]
            void geom(triangle v2g i[3], inout LineStream<g2f> stream)
            {
                g2f o;
                o.vertex = TransformWorldToHClip(i[0].posWorld);
                o.color = half3(1, 1, 0);
                stream.Append(o);
                float3 worldPos = i[0].posWorld + i[0].normalWorld * _NormalDis;
                o.vertex = TransformWorldToHClip(worldPos);
                o.color = half3(1, 0, 0);
                stream.Append(o);

            }


            half4 frag(g2f i) : SV_Target
            {
                // sample the texture
                half4 col = half4(i.color, 1);

                return col;
            }
            ENDHLSL
        }
    }
}
