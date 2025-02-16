Shader "Plane/Grass"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Ambient("Ambient", Range(0,1)) = 0
        _Diffuse("Diffuse", Range(0,1)) = 0

        _NoiseColor("NoiseColor", Color) = (1,1,1,1)
        _NoiseTex("NoiseTex", 2D) = "white"{}
        _NoiseBias("NoiseBias" , Range(0,1)) = 1
    
        _Skybox("Skybox", cube) = "white" {}
        _HeightMap("heightMap", 2D) = "white"{}
        _HighQualityHeightMap("highQualityHeightMap", 2D) = "white"{}
        _NormalMap("normalMap", 2D) = "white"{}

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
            #pragma multi_compile_fog

            struct appdata
            {
                float4 posModel : POSITION;
            };
            struct v2f
            {
                float4 posCS : SV_POSITION;
                float3 posWS : TEXCOORD3;
                float2 uv : TEXCOORD0;
                float4 shadowCoord : TEXCOORD2;
                float fogFactor : TEXCOORD1;
                float3 normal : NORMAL;
            };
            sampler2D _NoiseTex;
            samplerCUBE _Skybox;

            sampler2D _HeightMap;
            sampler2D _HighQualityHeightMap;
            sampler2D _NormalMap;
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Ambient;

            float4 _NoiseTex_ST;
            float4 _NoiseColor;
            float _NoiseBias;

            float4 _HeightMap_ST;
            float4 _HighQualityHeightMap_ST;
            float4 _NormalMap_ST;

            int _Quality;
            CBUFFER_END

            v2f vert(appdata i)
            {
                v2f o;
                o.uv = float2((i.posModel.x + 5) / 10.f, (i.posModel.z + 5) / 10.f);
                o.posWS = TransformObjectToWorld(i.posModel.xyz);
                o.posWS.y = tex2Dlod(_HeightMap, float4(o.uv * _HeightMap_ST.xy + _HeightMap_ST.zw, 0, 0)).r;
                o.posWS.y += 0.4f / (log2(_Quality) + 1);
                o.posCS = TransformWorldToHClip(o.posWS);

                VertexPositionInputs vInputs = GetVertexPositionInputs(i.posModel.xyz);
                o.shadowCoord = GetShadowCoord(vInputs);
                o.normal = tex2Dlod(_NormalMap, float4(o.uv * _NormalMap_ST.xy + _NormalMap_ST.zw, 0, 0)).rbg;
                o.fogFactor = ComputeFogFactor(o.posCS.z);
                

                //only built in
                //UNITY_TRANSFER_FOG(o,o.posCS); //o���� fog���� �ʿ��� �������� �˾Ƽ� ä���� ������ ���� �޶����� ������ ��ũ�η� ����аŶ����

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 temp = i.uv - 0.5f;
                if(abs(temp.x) < 0.22f && abs(temp.y) < 0.22f  && _Quality > 1)
                {
                    clip(-1);
                }
                half4 col;
                col.a = 1;
                //col.r = tex2Dlod(_HeightMap, float4(i.uv * _HeightMap_ST.xy + _HeightMap_ST.zw, 0, 0)).r;
                //return col;
                Light mainLight = GetMainLight(i.shadowCoord);
                float3 normal = normalize(i.normal);
              
                float ndotl = saturate(dot(normal, mainLight.direction));

                float noiseValue = tex2D(_NoiseTex, i.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw).r;
                half3 albedo = noiseValue > _NoiseBias ? _NoiseColor : _Color.rgb;
                half3 diffuse = albedo * mainLight.color * ndotl * mainLight.shadowAttenuation;
                half3 ambientInShadow = albedo * mainLight.color * ndotl * (1 - mainLight.shadowAttenuation) * _Ambient;
                col.rgb = diffuse + ambientInShadow;


                ////col.rgb = half3(i.fogFactor,0,0); //near = 1 far = 0 not linear 

                //float3 camPosWS = GetCameraPositionWS();
                //float3 viewDir = normalize(i.posWS - camPosWS);
                //float3 reflectVector = reflect(viewDir, normal);

                //float4 skyColor = texCUBE(_Skybox, reflectVector);
                ////col.rgb = skyColor.rgb;
                //col.rgb = col.rgb * i.fogFactor + (1 - i.fogFactor)* skyColor.rgb;
                //col.rgb = normal;

                return col;
            }

            ENDHLSL
        }

     
      
        
    }
}
