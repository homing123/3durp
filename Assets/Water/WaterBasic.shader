
Shader "Water/Basic"
{

    Properties
    {
        _Direction("Dir", Range(0, 3.1415926535)) = 0
        _Amplitude("Amplitude", Range(0.01, 10)) = 1
        _Frequency("Frequency", Range(0.01, 10)) = 0.5
        _Speed("Speed", Range(0, 10)) = 1
        _Height("Height", float) = 1
        _Steepness("Steepness", Range(1, 5)) = 1

        _Color("Color", Color) = (1,1,1,1)
        _Specular("Specular", Range(0,1)) = 1
        _Shiness("Shiness", Range(0,20)) = 1
        _Ambient("Ambient", Range(0,1)) = 1
        _Diffuse("Diffuse", Range(0,1)) = 1
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : NORMAL;
                float dx : TEXCOORD0;
            };

            struct Wave {
                float2 direction;
            };

            float _Direction;
            float _Amplitude;
            float _Frequency;
            float _Speed;
            float _Height;
            float _Steepness;
            half4 _Color;
            float _Specular;
            float _Shiness;
            float _Ambient;
            float _Diffuse;
            

            float3 CalcWave(float2 worldPos, float dirAngle)
            {
                float amp = _Amplitude;
                float freq = _Frequency;
                float speed = _Speed;
                float height = 0;
                float2 totaldw = 0;

                for (int i = 0; i < 5; i++)
                {
                    float2 vDir = float2 (cos(dirAngle), sin(dirAngle));

                    float vDirDotPos = dot(vDir, worldPos.xy);
                    float x = vDirDotPos * freq + _Time.y * speed;
                    float wave = amp * exp(_Steepness * sin(x));
                    float dx = _Steepness * wave * cos(x);
                    float2 dw = -dx * vDir; //도함수, 기울기
                    totaldw += dw;
                    height += wave;
                    freq *= 2.5;
                    amp *= 0.35f;
                    dirAngle += 0.5f;
                    speed *= 0.35f;
                }

                float3 result = float3(height, totaldw.x, totaldw.y);
                return result;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                float3 result = CalcWave(worldPos.xz, _Direction);
                worldPos.y += result.x;
                o.normal = normalize(float3(result.y, 1, result.z));
                o.worldPos = worldPos;
                o.dx = 0;
                o.vertex = TransformWorldToHClip(worldPos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            { 
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                half4 col;
                col.rgb = _Color.rgb;
                Light L = GetMainLight();

                float3 normal = i.normal;
                //float3 normal = normalize(float3(-i.dx, 1, 0));
                //normal.xz = float2(cos(_Direction) * normal.x, sin(_Direction) * normal.x);

                float ndotl = max(0, dot(L.direction, normal));
                float diffuse = ndotl * _Diffuse;

                float3 vHalf = normalize(L.direction + viewDir);
                float ndoth = saturate(dot(normal, vHalf));
                float specular = pow(ndoth, _Shiness) * _Specular;

                col.a = 1;
                col.rgb = _Color.rgb * _Ambient + _Color.rgb * (diffuse + specular) * L.color.rgb;
                return col;
            }

            
            ENDHLSL
        }
    }
}
