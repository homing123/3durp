Shader "PostProcessing/VLS"
{
    Properties
    { 
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #pragma enable_d3d11_debug_symbols
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ UNITY_UV_STARTS_AT_TOP


            struct appdata
            {
                float4 posOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 posCS : SV_POSITION;
            };
            sampler2D _CameraDepthTexture;
            sampler2D _MainTex;
            CBUFFER_START(UnityPerMaterial)
            float2 _MainTex_TexelSize;
            float3 _LightPos;
            float _Density;
            float _Decay;
            float _Scattering;
            float _Weight;
            int _Samples;
            float _TempValue;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(v.posOS.xyz);
                o.uv = v.uv;
                return o;
            }
            float GetFar()
            {
                return unity_CameraProjection[2][3] / (unity_CameraProjection[2][2] + 1);
            }
            float GetNear()
            {
                return unity_CameraProjection[2][3] / (unity_CameraProjection[2][2] - 1);
            }

            float Phase(float3 inDir, float3 outDir)
            {
                float cosAngle = dot(inDir, outDir) / (length(inDir) * length(outDir));
                float nom = 3.0f * (1.0f + cosAngle * cosAngle);
                float denom = 16.0f * 3.141592f;
                return nom / denom;
            }
            
            //https://discussions.unity.com/t/where-is-unity_cameratoworld-documented/794950/4
            /*float4x4 mat1 = GetWorldToViewMatrix(); //not work
            float4x4 mat2 = UNITY_MATRIX_V; //not work
            float4x4 mat3 = unity_WorldToCamera; //work
            float4x4 tempmat = mat1 + mat2 + mat3;
            float4 temp = mul(tempmat, float4(1, 1, 1, 1));
            return half4(temp);*/

            //            half4 frag(v2f i) : SV_Target
            //            {   
            //                float near = GetNear();
            //                float far = GetFar();
            //                float3 camPosWS = GetCameraPositionWS();
            //#ifdef UNITY_UV_STARTS_AT_TOP
            //                i.uv.y = 1 - i.uv.y;
            //#endif
            //                float2 ndc = i.uv * 2 - 1;
            //                float depth = tex2D(_CameraDepthTexture, i.uv).r;
            //
            //                float4 posNearVS = mul(UNITY_MATRIX_I_P, float4(ndc, 1, 1));
            //                posNearVS = posNearVS / posNearVS.w;
            //                posNearVS.z = near;
            //                float4 posNearWS = mul(unity_CameraToWorld, posNearVS);
            //
            //                float3 rayEntry = posNearWS;
            //                float3 rayDir = normalize(rayEntry - camPosWS);
            //                float rayStepDis = length(_Scale) / _Samples;
            //                
            //                float4 posDepthVS = mul(UNITY_MATRIX_I_P, float4(ndc, depth, 1));
            //                posDepthVS = posDepthVS / posDepthVS.w;
            //                float depthVS = -posDepthVS.z;
            //                float linearDepth = (depthVS - near) / (far - near); //near = 0, far = 1
            //
            //                float3 L = float3(0, 0, 0);
            //                int steps = 0;
            //                for (int idx = 0; idx < _Samples; idx++)
            //                {
            //                    float3 rayPos = rayEntry + rayDir * rayStepDis * (idx + 1);
            //                    //rayCoord = (rayOrigin - _Position) / _Scale + 0.5f;
            //                    float3 curPosVS = mul(unity_WorldToCamera, float4(rayPos, 1));
            //                    float z = (curPosVS.z - near) / (far - near);
            //
            //                    if (z >= linearDepth) { break; }
            //                    float4 shadowCoord = TransformWorldToShadowCoord(rayPos);
            //                    Light mainLight = GetMainLight(shadowCoord);
            //                    float shadow = mainLight.shadowAttenuation; // shadowattenuation = 1 그림자 진거, 0 = 그림자 안진거
            //                    float3 lightDir = mainLight.direction;
            //                    float lightDis = 70 - rayStepDis * idx;
            //                    float viewDis = length(rayPos - camPosWS);
            //                    float3 viewDir = normalize(camPosWS - rayPos);
            //                    float L_in = exp(-lightDis * _TauScattering) * shadow * 0.3f / (4.0f * 3.141592f * lightDis * lightDis);
            //                    float3 L_i = L_in * _TauScattering * mainLight.color * Phase(lightDir, viewDir);
            //                    L += L_i * exp(-viewDis * _TauScattering) * rayStepDis;
            //                    steps++;
            //                    //if (rayCoord.x < 0.0f || rayCoord.x > 1.0f || rayCoord.y < 0.0f || rayCoord.y > 1.0f || rayCoord.z < 0.0f || rayCoord.z > 1.0f) { break; }
            //                }
            //                // sample the texture
            //
            //                return half4(L, 1);
            //            }


            //uv 뒤집히는 플랫폼이면 뒤집힌 그대로 써야함 즉 1 - uv.y 안한값 넣어야함
            float GetLinearDepthUseUV(float2 uv, float near, float far)
            {
                float2 ndc = uv * 2 - 1;
                float depth = tex2D(_CameraDepthTexture, uv).r; //depth pass 에서 (근)0 ~ 1(원) 으로 그려둠
                float4 posDepthVS = mul(UNITY_MATRIX_I_P, float4(ndc, depth, 1));
                posDepthVS = posDepthVS / posDepthVS.w;
                float depthVS = -posDepthVS.z;
                float linearDepth = (depthVS - near) / (far - near); //near = 0, far = 1
                return linearDepth;
            }

            float GetLinearDepthUseWSPos(float3 posWS, float near, float far)
            {
                float4 posVS = mul(unity_WorldToCamera, float4(posWS, 1));
                float linearDepth = (posVS.z - near) / (far - near); //near = 0, far = 1
                return linearDepth;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 color = tex2D(_MainTex, i.uv).rgb;
                float4x4 matInvVP = mul(unity_CameraToWorld, unity_CameraInvProjection);
                float4x4 matVP = mul(unity_CameraProjection, unity_WorldToCamera);
                float3 camPosWS = GetCameraPositionWS();
                float near = GetNear();
                float far = GetFar();
                float3 lightPosWS = camPosWS + _LightPos; // 라이트 위치는 스카이박스에서의 위치이므로 카메라위치 더해줘야함
                float2 platformTexUV = i.uv;
#ifdef UNITY_UV_STARTS_AT_TOP
                platformTexUV.y = 1 - platformTexUV.y;
#endif
                float2 ndc = i.uv * 2 - 1;
                float4 posNearVS = mul(unity_CameraInvProjection, float4(ndc, -1, 1));
                posNearVS = posNearVS / posNearVS.w;  
                posNearVS.z = -posNearVS.z;
                float4 posNearWS = mul(unity_CameraToWorld, posNearVS);
                float3 rayEntry = posNearWS.xyz;

                float3 rayDir = normalize(rayEntry - camPosWS); //캠 -> near 인가 아니면 near to light인가...

                half illuminationDecay = 1.0f; 
                float3 rayPosWS = rayEntry;
                float curDepth = GetLinearDepthUseUV(i.uv, near, far);

                //density == 32, sample = 128 일때 y = 32 / (128 * 128) *(x*x)

                float lastDepth = 0;
                float a = _Density / (float)(128 * 128 * 128 * 128);
                [loop]
                for (int idx = 1; idx <= _Samples; idx++)
                {
                    rayPosWS = rayPosWS + a * idx* idx* idx * idx * rayDir;
                    float4 rayPosVS = mul(unity_WorldToCamera, float4(rayPosWS, 1));
                    rayPosVS.z = -rayPosVS.z;
                    float4 rayPosNDC = mul(unity_CameraProjection, float4(rayPosVS.xyz, 1));
                    rayPosNDC = rayPosNDC / rayPosNDC.w;

                    if (rayPosNDC.x > 1 || rayPosNDC.x < -1 || rayPosNDC.y > 1 || rayPosNDC.y < -1 || rayPosNDC.z < -1 || rayPosNDC.z > 1) { break; } //플랫폼별 근접, 원거리 값이 필요한데 모르니 일단 d3d11방법으로하자
                    float curPosWSDepth = GetLinearDepthUseWSPos(rayPosWS, near, far);
                    lastDepth = curPosWSDepth;
                    if (curPosWSDepth >= curDepth) { break; }
                    float4 shadowCoord = TransformWorldToShadowCoord(rayPosWS);

                    Light mainLight = GetMainLight(shadowCoord);
                    float shadow = mainLight.shadowAttenuation; // shadowattenuation = 0 그림자 진거, 1 = 그림자 안진거
                    float3 toLight = rayPosWS - _LightPos;
                    float toLightDis = length(toLight);
                    float3 toLightDir = toLight / toLightDis;      

                    //현재 맵에서
                    float lightDisScattering = exp(-0.0005f * toLightDis) * _Scattering; //태양으로 부터 거리에 따른 빛 세기
                    float ldotv = dot(toLightDir, -rayDir);
                    half3 ScatteringLightColor = mainLight.color;
                    half3 addColor = ScatteringLightColor / lightDisScattering * illuminationDecay * _Weight * shadow * pow(ldotv, _TempValue);
                    color += saturate(addColor);
                    illuminationDecay *= _Decay; //샘플이 진행됨에 따라 감소하는 빛 세기 즉 카메라와의 거리에 따른 빛 세기
                }
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
