Shader "Test/Raymarching"
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 posCS : SV_POSITION;
                float3 posWS : Texcoord1;
            };

            CBUFFER_START(UnityPerMaterial)
            float3 _Position;
            float3 _Scale;
            int _Samples;
            CBUFFER_END


            v2f vert(appdata v)
            {
                v2f o;
                o.posWS = TransformObjectToWorld(v.posOS.xyz);
                o.posCS = TransformObjectToHClip(v.posOS.xyz);
                o.uv = v.uv;
                return o;
            }

            bool sphere(float3 coord)
            {
                return length(coord * 2 - 1) < 1;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 camPosWS = GetCameraPositionWS();
                float3 rayOrigin = i.posWS;

                float3 rayCoord = (rayOrigin - _Position) / _Scale + 0.5f; // Range : 0 ~ 1 in volume

                float3 rayDir = normalize(i.posWS - camPosWS);
                
                float rayStep = length(_Scale) / _Samples;

                bool hit = false;

                int idx = 0;
                for (int i = 0; i < _Samples; i++)
                {
                    idx = i;
                    hit = sphere(rayCoord);

                    if (hit) break;
                    
                    rayOrigin += rayDir * rayStep;

                    rayCoord = (rayOrigin - _Position) / _Scale + 0.5f;

                    if (rayCoord.x < 0.0f || rayCoord.x > 1.0f || rayCoord.y < 0.0f || rayCoord.y > 1.0f || rayCoord.z < 0.0f || rayCoord.z > 1.0f) { break; }
                }
                // sample the texture
                half3 color = hit ? half3(1, 1, 1) * (idx / (float)_Samples) : half3(0, 0, 0);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
