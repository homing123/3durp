Shader "Custom/SkyboxCubeMap"
{
    Properties
    {
        _CubeMap ("CubeMap", Cube) = "white" {}
        _Rotation("Rotation", Range(0,360)) = 0

        _SunSize("SunSize", Range(0, 10)) = 1
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
                float3 viewDirWS = i.posOS;
                float cosf = cos(_Rotation * PI / 180.f);
                float sinf = sin(_Rotation * PI / 180.f);
                float2x2 xzRotMat = float2x2(cosf, -sinf, sinf, cosf);
                viewDirWS.xz = mul(xzRotMat, viewDirWS.xz);
                half4 col = texCUBE(_CubeMap, viewDirWS);
                //col = half4(1, 1, 1, 1);
                return col;
            }
            ENDHLSL
        }
    }
}
