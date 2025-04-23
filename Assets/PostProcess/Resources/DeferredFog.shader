Shader "PostProcessing/DeferredFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags 
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"    
        }
        Cull Off
        ZWrite Off
        ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _NearDis;
            float _FarDis;
            float _Height;
            float _Intensity;
            half3 _FogColor;
            CBUFFER_END


            float GetFar()
            {
                return unity_CameraProjection[2][3] / (unity_CameraProjection[2][2] + 1);
            }
            float GetNear()
            {
                return unity_CameraProjection[2][3] / (unity_CameraProjection[2][2] - 1);
            }
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }
           

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 col = half4(0,0,0,1);
                half3 color = tex2D(_MainTex, i.uv).rgb;
                float depth = tex2D(_CameraDepthTexture, i.uv).r;
                float3 posCS = float3(i.uv * 2 - 1, depth);

                float4 posVS = mul(UNITY_MATRIX_I_P, float4(posCS,1));
                posVS = posVS / posVS.w;
                posVS.yz *= -1;
                float4 posWS = mul(unity_CameraToWorld, posVS);
                float3 camPosWS = GetCameraPositionWS();

                float camFar = GetFar();
                float farDis = _FarDis >= camFar ? camFar : _FarDis;

                float dis = length(posWS.xyz - camPosWS);
                float disFactor = saturate((dis - _NearDis) / (farDis - _NearDis));
                disFactor = disFactor * disFactor;
                float disFogFactor = disFactor;

                float heightFogFactor = saturate(pow(1.1f, _Height - posWS.y));
                float fog = disFogFactor * heightFogFactor * _Intensity;
                col.rgb = _FogColor * fog + color * (1 - fog);
                // apply fog
                return col;
            }
            ENDHLSL
        }
    }
}
