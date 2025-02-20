Shader "Custom/SkyboxCubeMap"
{
    Properties
    {
        _CubeMap ("CubeMap", Cube) = "white" {}
        _Rotation("Rotation", Range(0,360)) = 0

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
            #define CLOUD_RED_THRESHOLD_MIN 0.059999f
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

                float timeToRad = _DayTime * PI * 2;
                float sunRotAxisX = 45 * DegToRad;
                float3 sunPos = float3(cos(timeToRad), sin(timeToRad), 0) * 2;
                float cosAxisX = cos(sunRotAxisX);
                float sinAxisX = sin(sunRotAxisX);
                float3x3 sunAxisXRotMat = float3x3(1, 0, 0,
                    0, cosAxisX, -sinAxisX,
                    0, sinAxisX, cosAxisX);
                sunPos = mul(sunAxisXRotMat, sunPos);
                float3 sunDistancePoint = max(0, dot(sunPos, viewDir)) * viewDir;
                float sunDis = length(sunDistancePoint - sunPos);
                half3 sunColor = _SunColor;
                float sunShiningIntensity = saturate(_SunShiningRange + _SunSize - sunDis);
                sunShiningIntensity = saturate(smoothstep(0, 1, sunShiningIntensity) * _SunShiningIntensity);
                float sunFadeStartSize = _SunSize * 0.9f;
                
                float sunDensity = saturate(1 - (sunDis - sunFadeStartSize) / (_SunSize - sunFadeStartSize));

                float cosf = cos(_Rotation * DegToRad);
                float sinf = sin(_Rotation * DegToRad);
                float2x2 xzRotMat = float2x2(cosf, -sinf, sinf, cosf);
                cubeUVW.xz = mul(xzRotMat, cubeUVW.xz);
                half3 cloudColor = texCUBE(_CubeMap, cubeUVW);
                float cloudDensity = saturate((cloudColor.r - CLOUD_RED_THRESHOLD_MIN) / (CLOUD_RED_THRESHOLD_MAX - CLOUD_RED_THRESHOLD_MIN));
                cloudDensity = sunDis > _SunSize ? 1 : cloudDensity; //태양범위 밖이면 구름가중치 1
                half3 cloudAndSunShining = cloudColor * (1 - sunShiningIntensity) + sunColor * sunShiningIntensity;

                sunDensity = cloudDensity == 1 ? 0 : sunDensity;
                
                half3 finalColor = sunColor * sunDensity + cloudAndSunShining * (1 - sunDensity);
               
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
