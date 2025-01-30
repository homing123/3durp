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
        _Ambient("Ambient", Range(0,1)) = 0
        _OcclusionHeightTop("OcclusionHeightTop", float) = 0
        _OcclusionHeightBottom("OcclusionHeightBottom", float) = 0
        _OcclusionStrength("OcclusionStrength", Range(0,1)) = 0

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
                    float4 shadowCoord : TEXCOORD3;

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
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
                #pragma multi_compile _ _ADDITIONAL_LIGHTS
                #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
                #pragma multi_compile _ _SHADOWS_SOFT
                
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
                float _Ambient;
                float _OcclusionHeightTop;
                float _OcclusionHeightBottom;
                float _OcclusionStrength;

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

                float _BendingTexInterval;
                int _CamGridPosx;
                int _CamGridPosz;
                CBUFFER_END
                VertexOut vs(appdata v, uint instanceID : SV_INSTANCEID)
                {
                    float2 chunkUV = _GrassBuffer[instanceID].chunkUV;

                    float dryNoise = tex2Dlod(_NoiseTex, float4(chunkUV * _DryTilingOffset.xy + _DryTilingOffset.zw, 0, 0)).r;
                    float sizeNoise = tex2Dlod(_NoiseTex, float4(chunkUV * _ScaleTilingOffset.xy + _ScaleTilingOffset.zw, 0, 0)).r;
                    float4 bendingTilingOffset = float4(20,20,0,0);
                    float bendingNoise = tex2Dlod(_NoiseTex, float4(chunkUV * bendingTilingOffset.xy + bendingTilingOffset.zw,0,0)).r;

                    dryNoise = GetNoiseToNormRange(dryNoise);
                    sizeNoise = GetNoiseToNormRange(sizeNoise);
                    bendingNoise = GetNoiseToNormRange(bendingNoise);

                    float width = _GrassSize * (1 + sizeNoise * _GrassSizeRandomMul);
                    float height = _GrassSize * (1 + sizeNoise * _GrassSizeRandomMul);

                    VertexOut o;
                    float3 pivotPosWS = _GrassBuffer[instanceID].position;
                    float3 camPosWS = GetCameraPositionWS(); 

                    float3 bill_front = normalize(pivotPosWS - camPosWS);
                    float3 bill_up = float3(0, 1, 0);
                    float3 bill_right = normalize(cross(bill_up, bill_front));

                    float3 posWS = pivotPosWS + v.posModel.x * bill_right * width + v.posModel.y * bill_up * height;

                    //bending
                    int bendingTexWidth = 512;
                    float2 bendingTexMinxz = int2(_CamGridPosx, _CamGridPosz) - bendingTexWidth * _BendingTexInterval * 0.5f;
                    float2 bendinguv = (posWS.xz - bendingTexMinxz) / (_BendingTexInterval * bendingTexWidth);
                    float bendingValue = 0;
                    if (bendinguv.x >= 0 && bendinguv.x <= 1 && bendinguv.y >= 0 && bendinguv.y <= 1)
                    {
                        int2 bendingTexIdx = int2(bendinguv.x * bendingTexWidth, bendinguv.y * bendingTexWidth);
                        bendingValue = _BendingTexBuffer[bendingTexIdx.x + bendingTexWidth * bendingTexIdx.y];

                        //���� ��ġ�� �õ�� ������ ������������ 1 = 90�� ȸ�� 0 = ȸ�� x = �̰� ��������
                        //0~1���� ������ �ٲ㼭 360���� Ŀ��ġ�°���
                        float radian = bendingNoise * 3.141592f * 2;
                        float2 bendingAddPos = float2(cos(radian), sin(radian)) * posWS.y;
                        posWS -= float3(bendingAddPos.x, posWS.y * 0.8f, bendingAddPos.y) * bendingValue;
                        
                        //posWS.y = posWS.y + posWS.y * bendingValue * 5;

                        //posWS.y -= bendingValue;
                    }

                    if (bendingValue == 0)
                    {
                        //wind
                        float wind = 0;
                        wind += (sin(_Time.y * _WindAFrequency + dryNoise * _WindATilingWrap.x + dryNoise * _WindATilingWrap.y) * _WindATilingWrap.z + _WindATilingWrap.w) * _WindAIntensity;
                        wind += (sin(_Time.y * _WindBFrequency + dryNoise * _WindBTilingWrap.x + dryNoise * _WindBTilingWrap.y) * _WindBTilingWrap.z + _WindBTilingWrap.w) * _WindBIntensity;
                        wind += (sin(_Time.y * _WindCFrequency + dryNoise * _WindCTilingWrap.x + dryNoise * _WindCTilingWrap.y) * _WindCTilingWrap.z + _WindCTilingWrap.w) * _WindCIntensity;
                        wind *= v.uv.y;
                        float3 windOffset = bill_right * wind;
                        posWS += windOffset;
                    }

                  

                    o.posWorld = posWS;
                    o.uv = v.uv;
                    o.posCS = TransformWorldToHClip(o.posWorld);
                    o.option = float4(sizeNoise, dryNoise, 1, 1);
                    o.id = instanceID;
                    o.shadowCoord = TransformWorldToShadowCoord(o.posWorld);
                    return o;
                }

                half4 fs(VertexOut i) : SV_Target
                {
                    half4 col = half4(0,0,0,1);
                    half4 texColor = tex2D(_GrassTex, i.uv);
                    clip(0.7 - (texColor.r + texColor.g + texColor.b));      
                    
                    float heightFactor = i.option.x;
                    float dryNoise = i.option.y;
                    
                    float dryColorFactor = saturate(heightFactor * 0.5f + dryNoise * 0.5f); //Ű�� Ŭ���� ���������� ����, �����ϰԵ� �����⶧���� �ݹ�
                    float4 dryColor = _DryGrassColor * dryColorFactor + texColor * (1 - dryColorFactor);
                    
                    //dryNoise >= _DryBias �϶� 0~0.5 = ��, 0.5~1 = �� �ε�
                    //�ű⼭ - dryNoise * heightFactor �� ���ֱ� ������ �� ũ�����ؼ� ���� ���߱� ������ 
                    //�� Ŭ���� ���������� ���ִ°� ŭ
                    
                    float dryFactor = 0;
                    dryFactor = dryNoise - dryNoise * heightFactor >= _DryBias ? 0 : 1;
                    dryFactor = dryFactor * (i.uv.y < 0.6 ? 0 : 1);
                    float uvFactorBias = (dryNoise - 0.3f) * 0.2f;
                    dryFactor = dryFactor * saturate((i.uv.y - 0.6f + uvFactorBias) * 2.5f);
                    half3 albedo = dryColor * dryFactor + (1 - dryFactor) * texColor.rgb;

                    float occ_bottom = _OcclusionHeightBottom + heightFactor * 0.025f; //�� ���� ���� �ٲ����
                    float occ_top = _OcclusionHeightTop + heightFactor * 0.05;
                    float occFactor = saturate((occ_top - i.posWorld.y) / (occ_top - occ_bottom));
                    albedo = albedo - albedo * occFactor * _OcclusionStrength;
                    Light mainLight = GetMainLight(i.shadowCoord);

                    float ndotl = saturate(dot(mainLight.direction, float3(0,1,0)));
                    half3 diffuse = albedo * ndotl * mainLight.shadowAttenuation;
                    half3 ambientInShadow = albedo * ndotl * _Ambient * (1 - mainLight.shadowAttenuation);

                    col.rgb = diffuse + ambientInShadow;     
                    return col;
                }
                ENDHLSL
            }

        }
}
               
                    // if (dryNoise - dryNoise * heightFactor >= _DryBias)
                    // {
                    //    col.rgb = texColor.rgb;
                    // }
                    // else
                    // {
                    //    if (i.uv.y < 0.6)
                    //    {
                    //        col.rgb = texColor.rgb;
                    //    }
                    //    else
                    //    {
                    //        float uvFactorBias = (dryNoise - 0.3f) * 0.2f;
                    //        float uvy = saturate((i.uv.y - 0.6f + uvFactorBias) * 2.5f);
                    //        col.rgb = dryColor * uvy + (1 - uvy) * texColor.rgb;
                    //    }
                    //     /* heightFactor �� ������ �������� ������ ������ ŭ ������ ������ ���� ������ ���� ��ο� ������ ��ħ
                    //      uv.y�� ���� �̻���� ������ ���۵Ǹ� ��������� uv.y�� �������� ������*/
                    // }


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