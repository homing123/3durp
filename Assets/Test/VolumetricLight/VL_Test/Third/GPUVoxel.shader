Shader "Unlit/GPUVoxel"
{
    Properties
    {
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
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
                float4 color : TEXCOORD0; 
            };
            struct VoxelDrawInfo
            {
                float3 posWS;
                float padding;
                float4 color;
            };

            StructuredBuffer<VoxelDrawInfo> _VoxelDrawInfo;

            CBUFFER_START(UnityPerMaterial)
            int _FlatHeight; //-1 => not flat
            int _GPUVoxelAxisInCPU;
            int _CPUVoxelHor;
            int _CPUVoxelVer;
            CBUFFER_END

            int GetVoxelIdx(int id) 
            {
                int a = _GPUVoxelAxisInCPU * _GPUVoxelAxisInCPU * _CPUVoxelHor;
                int cpuquotient = id / a;
                int cpuremainder = id % a;

                int gpuquotient = cpuremainder / _GPUVoxelAxisInCPU;
                int gpuremainder = cpuremainder % _GPUVoxelAxisInCPU;

                int cpumul = _GPUVoxelAxisInCPU * _GPUVoxelAxisInCPU * _GPUVoxelAxisInCPU * _CPUVoxelHor * _CPUVoxelVer;
                int gpumul = _GPUVoxelAxisInCPU * _GPUVoxelAxisInCPU;

                int result = gpuquotient * gpumul + gpuremainder + cpuquotient * cpumul;

                int heightQuotient = _FlatHeight / _GPUVoxelAxisInCPU;
                int heightRemainder = _FlatHeight % _GPUVoxelAxisInCPU;

                result += heightRemainder * _GPUVoxelAxisInCPU + heightQuotient * _GPUVoxelAxisInCPU * _GPUVoxelAxisInCPU * _GPUVoxelAxisInCPU * _CPUVoxelHor;
                return result;
            }

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                int id = _FlatHeight == -1 ? instanceID : GetVoxelIdx(instanceID);
                float3 posWS = v.posOS + _VoxelDrawInfo[id].posWS;
                o.posCS = TransformWorldToHClip(posWS);
                o.color = _VoxelDrawInfo[id].color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(i.color);
            }
            ENDHLSL
        }
    }
}
