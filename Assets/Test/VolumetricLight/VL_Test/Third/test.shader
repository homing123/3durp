// Shader "Unlit/FogHD"
// {
//     Properties
//     {
//         _Color("Color", Color)= (1,1,1,1)
//         _MaxDistance("Max distance", Float) = 100
//         _StepSize("Step size", Range(0.1, 20)) = 1
//         _DensityMultiplier("Density multiplier", Range(0, 10)) = 1
//         _NoiseOffset("Noise offset", Float) = 0
        
//         //Noise texture
//         _FogNoise("Fog noixe", 3D) = "white" {}
//         _NoiseTiling("Noise tiling", Float) = 1
//         _DensityThreshold("Desnity threshold", Range(0,1)) = 0.1
        
//         [HDR]_LightContribution("Light contributuion", Color) = (1,1,1,1)
//         _LightScattering("Light scattering", Range(0, 1)) = 0.2
        
//     }
//     SubShader
//     {
//         Tags
//         {
//             "RenderType"="Opaque"
//         }

//         Cull Off
//         ZWrite On
//         Pass
//         {
            
//             Name "ForwardLit"
//             Tags
//             {
//                 "LightMode" = "UniversalForward"
//             }
            
//             HLSLPROGRAM
//             #pragma vertex Vert
//             #pragma fragment frag
//             #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

//             //Additional lights
//             #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
//             #pragma multi_compile _ _ADDITIONAL_LIGHTS
//             #define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0
            
            
//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
//             #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
//             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
//             #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"


//             float4 _Color;
//             float _MaxDistance;
//             float _StepSize;
//             float _DensityMultiplier;
//             float _NoiseOffset;
            
//             TEXTURE3D(_FogNoise);
//             float _NoiseTiling;
//             float _DensityThreshold;

//             float4 _LightContribution;
//             float _LightScattering;

//             struct CustomLightingData {
//             // Position and orientation
//             float3 positionWS;
//             float3 normalWS;
//             float3 viewDirectionWS;
//             float4 shadowCoord;

//             // Surface attributes
//             float3 albedo;
//             float smoothness;
//             };
            

//             float henyey_greenstein(float angle, float scattering)
//             {
//                 return (1.0 - angle * angle) / (4.0 * PI * pow(1.0 + scattering * scattering - (2.0 * scattering) * angle, 1.5f));
//             }
            
//             float get_density(float3 worldPos)
//             {
//                 //Adding the 3d noise texture
//                 //Add time here for movment
//                 float4 noise = _FogNoise.SampleLevel(sampler_TrilinearRepeat, worldPos * 0.01 * _NoiseTiling, 0);
//                 float density = dot(noise, noise);
//                 density = saturate(density - _DensityThreshold) * _DensityMultiplier;
//                 return  density;
//             }

//             float3 MyLightingFunction(float3 rayDir, Light light, float density)
//             {
//                 light.color = light.color * _LightContribution.rgb * henyey_greenstein(dot(rayDir, light.direction), _LightScattering) * density * light.shadowAttenuation * _StepSize;
//                 return light.color;
//             }
            
//             float3 MyLightLoop(float3 color, InputData inputData, float density, float3 rayDir, float3 rayPos)
//             {
//                 float3 lighting = color;
                
//                 // Get the main light
//                 Light mainLight = GetMainLight(TransformWorldToShadowCoord(rayPos));
//                 lighting += MyLightingFunction(rayDir, mainLight, density);
                
//                 // Get additional lights
//                 #if defined(_ADDITIONAL_LIGHTS)

//                 // Additional light loop including directional lights. This block is specific to Forward+.
//                 #if USE_FORWARD_PLUS
//                 UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
//                 {
//                     Light additionalLight = GetAdditionalLight(lightIndex, rayPos, half4(1,1,1,1));
//                     lighting += MyLightingFunction(rayDir, additionalLight, density);
//                 }
//                 #endif
 
                
//                 // Additional light loop. The GetAdditionalLightsCount method always returns 0 in Forward+.
//                 uint pixelLightCount = GetAdditionalLightsCount();
//                 LIGHT_LOOP_BEGIN(pixelLightCount)
//                     Light additionalLight = GetAdditionalLight(lightIndex, rayPos, half4(1,1,1,1));
//                     lighting += MyLightingFunction(rayDir, additionalLight, density);
//                 LIGHT_LOOP_END
                
//                 #endif
                
//                 return lighting;
//             }
            
//             half4 frag (Varyings IN) : SV_Target
//             {
//                 //Assuming this is scene color
//                 float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                
//                 //IN texcoord being the screen space UV
//                 //We get the camras depth texture
//                 float4 depth = SampleSceneDepth(IN.texcoord);

//                 //We get world pos of the pixel using depth and camera uv and a matrix?
//                 float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

//                 //Start of raymarching ray
//                 float3 entryPoint = _WorldSpaceCameraPos;
//                 //Vector between the pixel and camera
//                 float3 viewDir = worldPos - _WorldSpaceCameraPos;
//                 //Length of that vector
//                 float viewLength = length(viewDir);
//                 //Normalize viewDir vector to get the direction of the ray
//                 float3 rayDir = normalize(viewDir);

//                 //Screencoordinates in pixel to calculate IGN noise map
//                 float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                
//                 float distLimit = min(viewLength, _MaxDistance);
//                 //To track how long we have travled along the ray
//                 float distTravelled = InterleavedGradientNoise(pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;
//                 //Track the acuumultaed transmittance
//                 float transmittance = 1;

//                 float4 fogCol = _Color;

//                 //Forward + rendering path for the light loop (Additional lights) https://docs.unity3d.com/6000.1/Documentation/Manual/urp/use-built-in-shader-methods-additional-lights-fplus.html 
//                 InputData inputData = (InputData)0;
//                 inputData.positionWS = worldPos;
//                 inputData.normalWS = rayDir;
//                 inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
//                 inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(inputData.positionWS);
                
//                 while (distTravelled < distLimit)
//                 {
//                     float3 rayPos = entryPoint + rayDir * distTravelled;
                    
//                     float density = get_density(rayPos);
//                     if (density > 0)
//                     {
//                         //Add light color to fog
//                         fogCol.rgb = MyLightLoop(fogCol, inputData, density, rayDir, rayPos);
                        
//                         //Light mainLight = GetMainLight(TransformWorldToShadowCoord(rayPos));
//                         //fogCol.rgb += mainLight.color * _LightContribution.rgb * henyey_greenstein(dot(rayDir, mainLight.direction), _LightScattering) * density * mainLight.shadowAttenuation * _StepSize;
//                         //Biers law
//                         transmittance *= exp(-density * _StepSize);
//                     }
//                     distTravelled += _StepSize;
//                 }

//                 return lerp(col, fogCol, 1.0 - saturate(transmittance));
//             }
            
            
//             ENDHLSL