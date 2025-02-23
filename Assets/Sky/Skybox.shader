Shader "Custom/SkyboxCubeMap"
{
    Properties
    {
        _CubeMap ("CubeMap", Cube) = "white" {}
        _Rotation("Rotation", Range(0,360)) = 0

        _GradationDayColor("GradationDayColor", Color) = (0,0,0,0)
        _GradationNightColor("GradationNightColor", Color) = (0,0,0,0)
        _GradationIntensity("GradationIntensity", Range(0,1)) = 0
        _GradationHeight("GradationHeight", float) = 0

        [HDR]_SunColor("SunColor", Color) = (0,0,0,1)
        _SunSize("SunSize", Range(0,0.5)) = 0.3

        [HDR]_MoonColor("MoonColor", Color) = (0,0,0,1)
        _MoonSize("MoonSize", Range(0,0.5)) = 0.25

        _DayColor("DayColor", Color) = (1,1,1,1)
        _NightColor("NightColor", Color) = (0.15, 0.05, 0.3, 1)

        _DayTime("DayTime", Range(0,1)) = 0
        _SunPos("SunPos", Vector)=(2,2,2,0)
        _MoonPos("MoonPos", Vector) = (-2,-2,-2,0)

    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Background" 
            "RenderQueue" = "Background"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define CLOUD_RED_THRESHOLD_MAX 0.06f
            #define CLOUD_RED_THRESHOLD_MIN 0.05f
            #define GRADATION_HEIGHT_MIN -0.1f
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 posOS : POSITION;
            };

            struct v2f
            {
                float4 posCS : SV_POSITION;
                float3 posOS : TEXCOORD0;
            };

            samplerCUBE _CubeMap;
            CBUFFER_START(UnityPerMaterial)
            float _Rotation;
            half3 _GradationDayColor;
            half3 _GradationNightColor;
            float _GradationIntensity;
            float _GradationHeight;
            half4 _SunColor;
            float _SunSize;
            half4 _MoonColor;
            float _MoonSize;
            half3 _DayColor;
            half3 _NightColor;
            
            float3 _SunPos;
            float3 _MoonPos;
            float _DayTime;
            float _NightIntensity;
            float _SunriseIntensity;
            CBUFFER_END
            v2f vert (appdata v)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(v.posOS.xyz);
                o.posOS = v.posOS.xyz;
                return o;
            }

            half3 ColorLerp(half3 col1, half3 col2, float value)
            {
                return col1 * (1 - value) + col2 * value;
            }

            half4 frag(v2f i) : SV_Target
            {
                // sample the texture
                //float3 camPos = GetCameraPositionWS();
                float3 viewDir = normalize(i.posOS);
                float3 cubeUVW = viewDir;

                float DegToRad = PI / 180.f;

                //sunDensity
                float3 sunDistancePoint = max(0, dot(_SunPos, viewDir)) * viewDir;
                float sunDis = length(sunDistancePoint - _SunPos); //�¾� �߽����� ���� �Ÿ�
                float sunFadeStartSize = _SunSize * 0.9f;
                float sunDensity = saturate(1 - (sunDis - sunFadeStartSize) / (_SunSize - sunFadeStartSize));
                sunDensity = smoothstep(0,1,sunDensity);

                //sunriseDensity
                float3 sunDir = normalize(_SunPos);
                float3 VTS = sunDir - viewDir;
                float sunriseDensity = 1 - saturate(sqrt(VTS.x * VTS.x * 0.15f + VTS.y * VTS.y + VTS.z * VTS.z * 0.15f));
                sunriseDensity = smoothstep(0,1,sunriseDensity);

                //moonDensity
                float3 moonDistancePoint = max(0, dot(_MoonPos, viewDir)) * viewDir;
                float moonDis = length(moonDistancePoint - _MoonPos); //�� �߽����� ���� �Ÿ�
                float moonFadeStartSize = _MoonSize * 0.9f;
                float moonDensity = saturate(1 - (moonDis - moonFadeStartSize) / (_MoonSize - moonFadeStartSize));
                moonDensity = smoothstep(0,1,moonDensity);

                //gradationDensity
                float gradationDensity = smoothstep(0,1,saturate((cubeUVW.y - _GradationHeight) / (GRADATION_HEIGHT_MIN - _GradationHeight)));

                //cloudDensity
                float skyRotation = _Rotation + _Time.y * 0.5f;
                float cosf = cos(skyRotation * DegToRad);
                float sinf = sin(skyRotation * DegToRad);
                float2x2 xzRotMat = float2x2(cosf, -sinf, sinf, cosf);
                cubeUVW.xz = mul(xzRotMat, cubeUVW.xz);
                half3 cloudColor = texCUBE(_CubeMap, cubeUVW);
                float cloudDensity = saturate((cloudColor.r - CLOUD_RED_THRESHOLD_MIN) / (CLOUD_RED_THRESHOLD_MAX - CLOUD_RED_THRESHOLD_MIN));
                
                //colorMix
                half3 finalColor = cloudColor;
                half3 dayNightColor = ColorLerp(_DayColor, _NightColor, _NightIntensity);
                finalColor = finalColor * dayNightColor;
                half3 gradationColor = ColorLerp(_GradationDayColor, _GradationNightColor, _NightIntensity);
                finalColor = ColorLerp(finalColor, gradationColor, gradationDensity * _GradationIntensity);
                sunDensity = (1 - cloudDensity) * sunDensity;
                moonDensity = (1 - cloudDensity) * moonDensity;
                finalColor = ColorLerp(finalColor, _SunColor, sunDensity);
                finalColor = ColorLerp(finalColor, _MoonColor, moonDensity);
                finalColor.r += _SunriseIntensity * sunriseDensity;
                finalColor.g -= _SunriseIntensity * sunriseDensity * 0.2f;
                finalColor.b -= _SunriseIntensity * sunriseDensity * 0.8f;

                                
                // //�¾� ��ġ�� �ð��� ���� �ϸ� ���� sunrise Intensity
                // float sunRiseIntensityMulAtTime = _DayTime < 0.2f || _DayTime > 0.9f ? 0.3f : 1;
                // float sunHeight = sunPos.y;
                // float sunRiseIntensity = abs(0.2f - sunHeight);
                // sunRiseIntensity = (1 - saturate(sunRiseIntensity / 0.6f)) * sunRiseIntensityMulAtTime;

               
                
                // cloudDensity = moonDis > _MoonSize && sunDis > _SunSize ? 1 : cloudDensity; //��, �¾� ���� ���̸� ��������ġ 1
                // gradationDensity *= _GradationColor.a;
                // cloudColor = cloudColor * (1 - gradationDensity) + _GradationColor * gradationDensity;

                // //������ �¾��� ������ �¾簡��ġ 0
                // sunDensity = cloudDensity == 1 ? 0 : sunDensity;
                // moonDensity = cloudDensity == 1 ? 0 : moonDensity;

                // //cloudColor mix sunColor use sunDensity
                // half3 finalColor = sunColor * sunDensity + cloudColor * (1 - sunDensity);
                // finalColor = moonColor * moonDensity + finalColor * (1 - moonDensity);

                // finalColor.r += sunRiseIntensity * sunriseDensity;
                // finalColor.g -= sunRiseIntensity * sunriseDensity * 0.2f;
                // finalColor.b -= sunRiseIntensity * sunriseDensity * 0.8f;

                // half3 nightColor = finalColor * half3(0.15f, 0.05f, 0.3f);
                // float nightColorIntensity = 1 - saturate((sunHeight - (-0.2f)) / (0.4f - (-0.2f)));
                // nightColorIntensity *= 1 - moonDensity;

                // finalColor = finalColor * (1 - nightColorIntensity) + nightColor * nightColorIntensity;
                //��ü�� �ر���, �ޱ���, ��������, ��汸�� ������ ���������� �ʿ���
                //�������� �ð����� ������
                //����,����� �ð������� ����ȭ�� ������, ��ħ�ð��뿡 ���ϰ� ����ð��뿡 ���ϰ� 
                //�¾���ġ ������� �������� ���ϸ��
                //�㿡�� �������� �����ϰ� ���̰� �� ���̵��� �ϸ��
                //���� uv�� �׸���� ������ ���׸���ȿ� ���� �ϳ��� �����ϰ� �� �� ����ϸ� �ȴ�. 
                //�̸� ť��ʿ��� �����غ���
                //�׸��� computebuffer�� cpu���� ����ؼ� �־�� �׸��庰 �� ��ġ, ��, �� ��ġ, ��������
                //
                return half4(finalColor,1);
            }
            ENDHLSL
        }
    }
}
