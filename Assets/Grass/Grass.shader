Shader "Grass/Grass"
{
    Properties
    {
        _NoiseTex("Noise", 2D) = "white" {}
        _PosTilingOffset("PosTilingOffset", Vector) = (0,0,0,0)
        _ScaleTilingOffset("ScaleTilingOffset", Vector) = (0,0,0,0)
        _DryTilingOffset("DryTilingOffset", Vector) = (0,0,0,0)
        _GrassTex("GrassTex", 2D) = "white" {}
        _GrassColor("GrassColor", Color) = (1,1,1,1)
        _DryGrassColor("DryGrassColor", Color) = (1,1,1,1)
        _DryBias("DryBias", Range(0,1)) = 0
        _GrassSize("GrassSize", Range(0.01, 1)) = 0.2
        _GrassSizeRandomMul("GrassSizeRandomMul", Range(0, 10)) = 1

        _WindATilingWrap("WindATilingWrap", Vector) = (0,0,0,0)
        _WindAFrequency("WindAFrequency", float) = 0
        _WindAIntensity("WindAIntensity", float) = 0
        _WindBTilingWrap("WindBTilingWrap", Vector) = (0,0,0,0)
        _WindBFrequency("WindBFrequency", float) = 0
        _WindBIntensity("WindBIntensity", float) = 0
        _WindCTilingWrap("WindCTilingWrap", Vector) = (0,0,0,0)
        _WindCFrequency("WindCFrequency", float) = 0
        _WindCIntensity("WindCIntensity", float) = 0

  

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
                    uint id : SV_INSTANCEID;
                };
                struct GrassData
                {
                    float2 chunkUV;
                    float3 position;
                };

                StructuredBuffer<GrassData> _GrassBuffer;

                StructuredBuffer<float> _BendingTexBuffer;
                //���� noise �ؽ��İ� 0.04 ~ 0.57 ������
                // -0.07 * 2 = 0~1�� �ȴ�. saturate �ʼ�
                //�̰� ó�� �Ŀ� ��հ��� 0.3���� �Ǵµ��ϴ�.
                float GetNoiseToNormRange(float noiseValue)
                {
                    return saturate((noiseValue - 0.07f) * 2);
                }

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                sampler2D _GrassTex;
                sampler2D _NoiseTex;
                CBUFFER_START(UnityPerMaterial)

                float4 _ScaleTilingOffset;
                float4 _DryTilingOffset;

                float _GrassSize;
                float _GrassSizeRandomMul;
                half4 _GrassColor;
                half4 _DryGrassColor;
                float _DryBias;

                float4 _GrassTex_ST;
                float4 _NoiseTex_ST;

                float4 _WindATilingWrap;
                float _WindAFrequency;
                float _WindAIntensity;
                float4 _WindBTilingWrap;
                float _WindBFrequency;
                float _WindBIntensity;
                float4 _WindCTilingWrap;
                float _WindCFrequency;
                float _WindCIntensity;

                float _BendingRenderDis;
                CBUFFER_END
                VertexOut vs(appdata v, uint instanceID : SV_INSTANCEID)
                {
                    float2 chunkUV = _GrassBuffer[instanceID].chunkUV;

                    float dryNoise = tex2Dlod(_NoiseTex, float4(chunkUV * _DryTilingOffset.xy + _DryTilingOffset.zw, 0, 0)).r;
                    float sizeNoise = tex2Dlod(_NoiseTex, float4(chunkUV * _ScaleTilingOffset.xy + _ScaleTilingOffset.zw, 0, 0)).r;

                    dryNoise = GetNoiseToNormRange(dryNoise);
                    sizeNoise = GetNoiseToNormRange(sizeNoise);

                    float width = _GrassSize * (1 + sizeNoise * _GrassSizeRandomMul);
                    float height = _GrassSize * (1 + sizeNoise * _GrassSizeRandomMul);

                    VertexOut o;
                    float3 pivotPosWS = _GrassBuffer[instanceID].position;
                    float3 camPosWS = GetCameraPositionWS(); 

                    float3 bill_front = normalize(pivotPosWS - camPosWS);
                    float3 bill_up = float3(0, 1, 0);
                    float3 bill_right = normalize(cross(bill_up, bill_front));

                    float3 posWS = pivotPosWS + v.posModel.x * bill_right * width + v.posModel.y * bill_up * height;

                    //wind
                    float wind = 0;
                    wind += (sin(_Time.y * _WindAFrequency + dryNoise * _WindATilingWrap.x + dryNoise * _WindATilingWrap.y) * _WindATilingWrap.z + _WindATilingWrap.w) * _WindAIntensity;
                    wind += (sin(_Time.y * _WindBFrequency + dryNoise * _WindBTilingWrap.x + dryNoise * _WindBTilingWrap.y) * _WindBTilingWrap.z + _WindBTilingWrap.w) * _WindBIntensity;
                    wind += (sin(_Time.y * _WindCFrequency + dryNoise * _WindCTilingWrap.x + dryNoise * _WindCTilingWrap.y) * _WindCTilingWrap.z + _WindCTilingWrap.w) * _WindCIntensity;
                    wind *= v.uv.y;
                    float3 windOffset = bill_right * wind;
                    posWS += windOffset;

                    //bending
                    float2 bendingTexMinxz = camPosWS.xz - _BendingRenderDis;
                    float2 bendinguv = (posWS.xz - bendingTexMinxz) / (_BendingRenderDis * 2);
                    if (bendinguv.x >= 0 && bendinguv.x <= 1 && bendinguv.y >= 0 && bendinguv.y <= 1)
                    {
                        int2 bendingTexIdx = int2(bendinguv.x * 512, bendinguv.y * 512);
                        float bendingValue = _BendingTexBuffer[bendingTexIdx.x + 512 * bendingTexIdx.y];

                        posWS.y -= bendingValue;
                    }

                    o.posWorld = posWS;
                    o.uv = v.uv;
                    o.posCS = TransformWorldToHClip(o.posWorld);
                    o.option = float4(sizeNoise, dryNoise, 1, 1);
                    o.id = instanceID;
                    return o;
                }

                half4 fs(VertexOut i) : SV_Target
                {
                    half4 col;
                    half4 texColor = tex2D(_GrassTex, i.uv);
                    clip(texColor.a - 0.5f);      
                    
                    float heightFactor = i.option.x;
                    float dryNoise = i.option.y;
                    
                    float dryColorFactor = saturate(heightFactor * 0.5f + dryNoise * 0.5f);
                    float4 dryColor = _DryGrassColor * dryColorFactor + texColor * (1 - dryColorFactor);
                    
                    //randomValue >= _DryBias �϶� 0~0.5 = ��, 0.5~1 = �� �ε�
                    //�ű⼭ - randomValue * heightFactor �� ���ֱ� ������ �� ũ�����ؼ� ���� ���߱� ������ 
                    //�� Ŭ���� ���������� ���ִ°� ŭ
                    if (dryNoise - dryNoise * heightFactor >= _DryBias)
                    {
                       col.rgb = texColor.rgb;
                    }
                    else
                    {
                       col.rgb = half3(0, heightFactor, 0);
                       if (i.uv.y < 0.6)
                       {
                           col.rgb = texColor.rgb;
                       }
                       else
                       {
                           float uvFactorBias = (dryNoise - 0.3f) * 0.2f;
                           float uvy = saturate((i.uv.y - 0.6f + uvFactorBias) * 2.5f);
                           col.rgb = dryColor * uvy + (1 - uvy) * texColor.rgb;
                       }
                        /* heightFactor �� ������ �������� ������ ������ ŭ ������ ������ ���� ������ ���� ��ο� ������ ��ħ
                         uv.y�� ���� �̻���� ������ ���۵Ǹ� ��������� uv.y�� �������� ������*/
                    }
                    return col;
                }
                ENDHLSL
            }

        }
}



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