Shader "BEKKOLOCO/URPToonShader"
{
    Properties
    {
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _HighlightColor ("Highlight Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.5,0.5,0.5,1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionTex ("Emission Texture", 2D) = "black" {}
        _EmissionStrength ("Emission Strength", Range(0, 2)) = 0
        
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
        _RampSmoothing ("Ramp Smoothing", Range(0.001, 1)) = 0.1
        _SpecularPower ("Specular Power", Range(0, 100)) = 10
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.5
        
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0, 10)) = 3
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
    
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
    
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl" // Add this for shadow support
    
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE // Enable shadow receiving
    
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_EmissionTex);
            SAMPLER(sampler_EmissionTex);
    
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NormalMap_ST;
                float4 _EmissionTex_ST;
                float4 _HighlightColor;
                float4 _ShadowColor;
                float _NormalStrength;
                float4 _EmissionColor;
                float _EmissionStrength;
                float _RampThreshold;
                float _RampSmoothing;
                float _SpecularPower;
                float _SpecularStrength;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
            CBUFFER_END
    
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
    
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6; // Add shadow coordinates
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
    
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
        
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
        
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS); // Compute shadow coordinates
        
                return output;
            }
    
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
        
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                half4 emissionTex = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, input.uv);
        
                normalMap = lerp(half3(0, 0, 1), normalMap, _NormalStrength);
                float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                half3 normalWS = normalize(mul(normalMap, TBN));
        
                Light mainLight = GetMainLight(input.shadowCoord); // Pass shadow coordinates to GetMainLight
                half3 lightDir = normalize(mainLight.direction);
                half3 viewDir = normalize(input.viewDirWS);
                half3 halfDir = normalize(lightDir + viewDir);
        
                half NdotL = saturate(dot(normalWS, lightDir));
                half lightIntensity = smoothstep(_RampThreshold - _RampSmoothing, _RampThreshold + _RampSmoothing, NdotL);
        
                half shadow = mainLight.shadowAttenuation; // Get shadow factor
                lightIntensity *= shadow; // Apply shadow to lighting
        
                half NdotH = saturate(dot(normalWS, halfDir));
                half specular = pow(NdotH, _SpecularPower) * _SpecularStrength;
        
                half NdotV = 1.0 - saturate(dot(normalWS, viewDir));
                half rimIntensity = pow(NdotV, _RimPower) * _RimStrength;
        
                half3 emission = _EmissionColor.rgb * emissionTex.rgb * _EmissionStrength;
        
                half4 baseColor = lerp(_ShadowColor, _HighlightColor, lightIntensity) * albedo;
                half4 litColor = baseColor + half4(specular.xxx, 0) * half4(mainLight.color, 1);
                half4 finalColor = litColor + (_RimColor * rimIntensity) + half4(emission, 0);
        
                return finalColor;
            }
            ENDHLSL
        }
        
        // Updated Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
    
            ZWrite On
            ZTest LEqual
    
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
    
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 posCS : SV_POSITION;
            };
            float3 _LightDirection;
            float4 _ShadowBias; // Provided by URP
    
            Varyings ShadowVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
        
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
        
                // Apply shadow bias manually
                float3 lightDir = normalize(_LightDirection);
                float bias = max(_ShadowBias.x, _ShadowBias.y * dot(normalWS, lightDir));
                positionWS += lightDir * bias;
        
                Varyings o;
                o.posCS = TransformWorldToHClip(positionWS);
                return o;
            }
    
            half4 ShadowFrag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}