Shader "Custom/SkyboxCubeMap"
{
    Properties
    {
        _CubeMap ("CubeMap", Cube) = "white" {}
        _Rotation("Rotation", Range(0,360)) = 0

        _GradationColor("GradationColor", Color) = (0,0,0,0)
        _GradationHeight("GradationHeight", float) = 0

        _SunColor("SunColor", Color) = (0,0,0,1)
        _SunSize("SunSize", Range(0,0.5)) = 0.3
        _SunShiningRange("SunShiningRange", Range(0,2)) = 1
        _SunShiningIntensity("SunShiningIntensity", Range(0,1)) = 1
        _DayTime("DayTime", Range(0,1)) = 0
        _Temp("temp", Range(0,1))=0
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
            float _Rotation;
            half4 _GradationColor;
            float _GradationHeight;
            half4 _SunColor;
            float _SunSize;
            float _SunShiningRange;
            float _SunShiningIntensity;
            float _DayTime;
            float _Temp;

            v2f vert (appdata v)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(v.posOS.xyz);
                o.posOS = v.posOS.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // sample the texture
                //float3 camPos = GetCameraPositionWS();
                float3 viewDir = normalize(i.posOS);
                float3 cubeUVW = viewDir;

                float DegToRad = PI / 180.f;

                //sun
                float timeToRad = _DayTime * PI * 2;
                float sunRotAxisX = 45 * DegToRad;
                float3 sunDir = float3(cos(timeToRad), sin(timeToRad), 0);
                float cosAxisX = cos(sunRotAxisX);
                float sinAxisX = sin(sunRotAxisX);
                float3x3 sunAxisXRotMat = float3x3(1, 0, 0,
                    0, cosAxisX, -sinAxisX,
                    0, sinAxisX, cosAxisX);
                sunDir = mul(sunAxisXRotMat, sunDir);
                float3 sunPos = sunDir * 2;
                float3 sunDistancePoint = max(0, dot(sunPos, viewDir)) * viewDir;
                float sunDis = length(sunDistancePoint - sunPos); //태양 중심으로 부터 거리
                half3 sunColor = _SunColor;
                float sunFadeStartSize = _SunSize * 0.9f;
                float sunDensity = saturate(1 - (sunDis - sunFadeStartSize) / (_SunSize - sunFadeStartSize));
                sunDensity = smoothstep(0,1,sunDensity);
                float sunDirElevationAngle = atan2(sunDir.) //고도각
                float sunAzimuthAngle = atan2(sunDirElevationAngle.x) //방위각


                //sunrise
                float sunHeight = sunPos.y;
                float sunRiseIntensity = abs(0.2f - sunHeight);
                sunRiseIntensity = 1 - saturate(sunRiseIntensity / 0.6f);

                //sunshining
                float sunShiningMul = 1 - sunRiseIntensity;
                float sunShiningIntensity = saturate(_SunShiningRange + _SunSize - sunDis) * sunShiningMul;
                sunShiningIntensity = saturate(smoothstep(0, 1, sunShiningIntensity) * _SunShiningIntensity) * sunShiningMul;
                
                //cloud and background and gradation
                float gradationDensity = saturate((cubeUVW.y - _GradationHeight) / (GRADATION_HEIGHT_MIN - _GradationHeight));
                float skyRotation = _Rotation + _Time.y * 0.5f;
                float cosf = cos(skyRotation * DegToRad);
                float sinf = sin(skyRotation * DegToRad);
                float2x2 xzRotMat = float2x2(cosf, -sinf, sinf, cosf);
                cubeUVW.xz = mul(xzRotMat, cubeUVW.xz);
                half3 cloudColor = texCUBE(_CubeMap, cubeUVW);
                float cloudDensity = saturate((cloudColor.r - CLOUD_RED_THRESHOLD_MIN) / (CLOUD_RED_THRESHOLD_MAX - CLOUD_RED_THRESHOLD_MIN));
                cloudDensity = sunDis > _SunSize ? 1 : cloudDensity; //태양범위 밖이면 구름가중치 1
                gradationDensity *= _GradationColor.a;
                cloudColor = cloudColor * (1 - gradationDensity) + _GradationColor * gradationDensity;
                half3 cloudAndSunShining = cloudColor * (1 - sunShiningIntensity) + sunColor * sunShiningIntensity;

                //태양 범위 밖이면 태양 색깔 가중치 0
                sunDensity = cloudDensity == 1 ? 0 : sunDensity;

                //cloudAndSunShiningColor mix sunColor use sunDensity
                half3 finalColor = sunColor * sunDensity + cloudAndSunShining * (1 - sunDensity);
                finalColor.r = 2.0f * (1 - sunDis * 0.4f);
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
