Shader "Cel/Cel_BothFaces_Transparent"
{
    Properties
    {
        _MainColor("Color", Color) = (1,1,1,1)
        _CelTex("Cel", 2D) = "white"{}
        _CelFactor("CelFactor", Range(-1,1)) = 0
        _Diffuse("Diffuse", Range(0,1)) = 0
        _Specular("Specular", Range(0,1)) = 0
        _Shiness("Shiness", Range(0, 50)) = 0
        _Ambient("Ambient", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            CBUFFER_START(UnityPerMaterial)
            sampler2D _CelTex;
            float4 _CelTex_ST;
            float4 _MainColor;
            float _CelFactor;
            float _Diffuse;
            float _Specular;
            float _Shiness;
            float _Ambient;
            CBUFFER_END


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                half4 color;
                half3 mainColor = _MainColor.rgb;
                Light L = GetMainLight();
                float ndotl = max(0, dot(L.direction, i.normal));
                
                half celDiffuse = tex2D(_CelTex, float2(ndotl * _CelTex_ST.x + _CelTex_ST.z, 0));
                half3 diffuse = celDiffuse * L.color * _Diffuse;

               /* float3 halfDir = normalize(L.direction + viewDir);
                float ndoth = max(0, dot(i.normal, halfDir));
                half celSpecular = tex2D(_CelTex, float2(ndoth * _CelTex_ST.x + _CelTex_ST.z, 0));
                float3 ldotv = max(0, dot(L.direction, viewDir));
                half3 specular = L.color * pow(celSpecular, _Shiness) * _Specular;*/


                float3 RDir = 2 * i.normal * dot(L.direction, i.normal) - L.direction;
                float rdotv = max(0, dot(RDir, viewDir));
                half celSpecular = tex2D(_CelTex, float2(rdotv * _CelTex_ST.x + _CelTex_ST.z, 0));
                half3 specular = pow(rdotv, _Shiness) * _Specular * L.color;
                half3 ambient = _Ambient * mainColor;

                half3 additionalLightColor;
                uint additionalLight = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(additionalLight)
                Light light = GetAdditionalLight(lightIndex, i.worldPos);
                half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                half3 lightDiffuseColor = LightingLambert(attenuatedLightColor, light.direction, i.normal);
                additionalLightColor += lightDiffuseColor;
                LIGHT_LOOP_END

                color = half4(ambient + diffuse + additionalLightColor + specular, 1);
                //color = half4(additionalLightColor, 1);
                return color;
            }
            ENDHLSL
        }
    }
}
