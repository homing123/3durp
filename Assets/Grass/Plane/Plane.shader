Shader "Plane/Grass"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Ambient("Ambient", Range(0,1)) = 0

        _NoiseColor("NoiseColor", Color) = (1,1,1,1)
        _NoiseTex("NoiseTex", 2D) = "white"{}
        _NoiseBias("NoiseBias" , Range(0,1)) = 1
    
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {   
            Name "Plane"
            Tags{
            "LightMode" = "UniversalForwardOnly"
                }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            struct appdata
            {
                float4 posModel : POSITION;
                float2 uv : TEXCOORD;
                float3 normal : NORMAL;
            };
            struct v2f
            {
                float4 posCS : SV_POSITION;
                float2 uv : TEXCOORD;
                float4 shadowCoord : TEXCOORD2;
                float3 normal : NORMAL;
            };
            sampler2D _NoiseTex;

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Ambient;

            float4 _NoiseTex_ST;
            float4 _NoiseColor;
            float _NoiseBias;
            CBUFFER_END

            v2f vert(appdata i)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(i.posModel.xyz);
                o.uv = i.uv;
                VertexPositionInputs vInputs = GetVertexPositionInputs(i.posModel.xyz);
                o.shadowCoord = GetShadowCoord(vInputs);
                o.normal = i.normal;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 col;
                col.a = 1;

                Light mainLight = GetMainLight(i.shadowCoord);
                float3 normal = normalize(i.normal);
                float ndotl = saturate(dot(normal, mainLight.direction));

                float noiseValue = tex2D(_NoiseTex, i.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw).r;
                half3 albedo = noiseValue > _NoiseBias ? _NoiseColor : _Color.rgb;
                half3 diffuse = albedo * mainLight.color * ndotl * mainLight.shadowAttenuation;
                half3 ambientInShadow = albedo * mainLight.color * ndotl * (1 - mainLight.shadowAttenuation) * _Ambient;
                col.rgb = diffuse + ambientInShadow;


                return col;
                // col = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);
                // return col;
            }

            ENDHLSL
        }

     
      
        
    }
}
