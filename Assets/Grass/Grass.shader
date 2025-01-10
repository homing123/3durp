Shader "Grass/Grass"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "white" {}
        _GrassTex("GrassTex", 2D) = "white" {}
        _GrassColor("GrassColor", Color) = (1,1,1,1)
        _DryGrassColor("DryGrassColor", Color) = (1,1,1,1)
        _DryBias("DryBias", Range(0,1)) = 0
        _GrassSize("GrassSize", Range(0.01, 1)) = 0.2
        _GrassSizeRandomMul("GrassSizeRandomMul", Range(0, 10)) = 1

    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }

          

            
            Pass
            {
                Name "Grass"

                HLSLPROGRAM
                #pragma vertex vs
                #pragma fragment fs

                #define TESSMIN 1


                struct appdata
                {
                    float4 posModel : POSITION;
                    float2 uv : TEXCOORD0;
                    float3 normal : NORMAL;
                };
                struct VertexOut
                {
                    float3 posWorld : TEXCOORD1;
                    float2 uv : TEXCOORD0;
                    float4 posCS : SV_POSITION;
                    float4 option : TEXCOORD2;
                };
               
                //현재 noise 텍스쳐가 0.04 ~ 0.57 범위다
                // -0.07 * 2 = 0~1이 된다. saturate 필수
                //이거 처리 후에 평균값은 0.3정도 되는듯하다.
                float GetNoiseToNormRange(float noiseValue)
                {
                    return saturate((noiseValue - 0.07f) * 2);
                }

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                sampler2D _GrassTex;
                sampler2D _NoiseTex;
                CBUFFER_START(UnityPerMaterial)

                float _GrassSize;
                float _GrassSizeRandomMul;
                half4 _GrassColor;
                half4 _DryGrassColor;
                float _DryBias;

                float4 _GrassTex_ST;
                float4 _NoiseTex_ST;
                CBUFFER_END
                VertexOut vs(appdata v)
                {
                    VertexOut o;
                    o.posWorld.xyz = TransformObjectToWorld(v.posModel.xyz);
                    o.uv = v.uv;
                    o.posCS = TransformObjectToHClip(v.posModel.xyz);
                    o.option = float4(1, 1, 1, 1);
                    return o;
                }

                half4 fs(VertexOut i) : SV_Target
                {
                    half4 col;
                    half4 texColor = tex2D(_GrassTex, i.uv);
                    clip(texColor.a - 0.5f);
                    col = texColor;
                  

                    return col;
                }
                ENDHLSL
            }

        }
}


//float heightFactor = i.option.x;
//float randomValue = i.option.y;
//
//float dryColorFactor = saturate(heightFactor * 0.5f + randomValue * 0.5f);
//float4 dryColor = _DryGrassColor * dryColorFactor + texColor * (1 - dryColorFactor);
//
////randomValue >= _DryBias 일땐 0~0.5 = 검, 0.5~1 = 초 인데
////거기서 - randomValue * heightFactor 를 해주기 때문에 즉 크기비례해서 값을 낮추기 때문에 
////즉 클수록 검정쪽으로 빼주는게 큼
//if (randomValue - randomValue * heightFactor >= _DryBias)
//{
//    col.rgb = texColor.rgb;
//}
//else
//{
//    col.rgb = half4(0, heightFactor, 0, 1);
//    if (i.uv.y < 0.6)
//    {
//        col.rgb = texColor.rgb;
//    }
//    else
//    {
//        float uvFactorBias = (randomValue - 0.3f) * 0.2f;
//        float uvy = saturate((i.uv.y - 0.6f + uvFactorBias) * 2.5f);
//        col.rgb = dryColor * uvy + (1 - uvy) * texColor.rgb;
//    }
    /* heightFactor 가 높으면 높을수록 변색의 정도가 큼 변색의 정도는 변색 범위와 색깔 모두에 영향을 미침
     uv.y가 일정 이상부터 변색이 시작되며 색깔비중이 uv.y가 높을수록 진해짐*/
//}

/* [maxvertexcount(4)]
 void gs(point DomainOut i[1], uint primID : SV_PrimitiveID, inout TriangleStream<GeometryOut> outputStream)
 {
     float4 sizeST = float4(0.6,0.55, 0.123, 0.757);
     float4 posST = float4(5, 5, 0, 0);
     float4 randomST = float4(10, 10, 0, 0);
     float randomNoise = tex2Dlod(_NoiseTex, float4(i[0].uv * randomST.xy + randomST.zw, 0, 0)).r;
     float sizeNoise = tex2Dlod(_NoiseTex, float4(i[0].uv * sizeST.xy + sizeST.zw, 0, 0)).r;
     float posNoise = tex2Dlod(_NoiseTex, float4(i[0].uv * posST.xy + posST.zw, 0, 0)).r;

     randomNoise = GetNoiseToNormRange(randomNoise);
     sizeNoise = GetNoiseToNormRange(sizeNoise);
     float posNoiseValue = (GetNoiseToNormRange(posNoise) * 2 - 1) * 0.3f;

     float width = _GrassSize * (1 + sizeNoise * _GrassSizeRandomMul);
     float height = _GrassSize * (1 + sizeNoise * _GrassSizeRandomMul);

     float3 camPos = GetCameraPositionWS();
     float dis = length(i[0].posWorld - camPos);
     float3 randomPos = float3(posNoiseValue, posNoiseValue * 0.05f, posNoiseValue);
     float3 centerWS = i[0].posWorld + randomPos ;

     float3 front = normalize(camPos - centerWS);
     float3 up = float3(0, 1, 0);
     float3 right = cross(up, front);

     float4 option = float4(sizeNoise, randomNoise, 0, 0);
     GeometryOut o;

     o.posWorld = centerWS + right * width * 0.5f + up * height;
     o.posCS = TransformWorldToHClip(o.posWorld);
     o.uv = float2(0, 1);
     o.option = option;
     outputStream.Append(o);

     o.posWorld = centerWS + right * -width * 0.5f + up * height;
     o.posCS = TransformWorldToHClip(o.posWorld);
     o.uv = float2(1, 1);
     o.option = option;
     outputStream.Append(o);

     o.posWorld = centerWS + right * width * 0.5f;
     o.posCS = TransformWorldToHClip(o.posWorld);
     o.uv = float2(0, 0);
     o.option = option;
     outputStream.Append(o);

     o.posWorld = centerWS + right * -width * 0.5f;
     o.posCS = TransformWorldToHClip(o.posWorld);
     o.uv = float2(1, 0);
     o.option = option;
     outputStream.Append(o);

 }*/