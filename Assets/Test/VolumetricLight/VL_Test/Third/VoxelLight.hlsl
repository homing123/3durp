
struct VoxelLightData
{
    int type;
    int3 padding;
    float4 color;
    float range;
    float3 pos;
};

struct VoxelLight
{
    half3 direction;
    half3 color;
    float distanceAttenuation;
};


StructuredBuffer<VoxelLightData> _LightData;
StructuredBuffer<int> _CPUVoxelLight;
StructuredBuffer<uint> _GPUVoxelLight;

int3 _CamCPUVoxelGridPos;
int _CPUVoxelSize;
int _CPUVoxelHorizontal;
int _CPUVoxelVertical;
int _CPUVoxelHalfHor;
int _CPUVoxelHalfVer;
int _CPUVoxelLightMax;


VoxelLight GetVoxelLight(float3 worldPos, int idx)
{
    //월드위치로 cpu복셀 위치 구하고 해당위치로 cpu복셀 인덱스 구해서 cpu복셀정보 얻고
    //cpu복셀위치와 현재위치로 gpu복셀 인덱스 구하고
    //gpu복셀정보와 cpu복셀 정보로 라이트 정보 얻고
    //해당 값 리턴
    
    VoxelLightData vLightData = (VoxelLightData)0;
    VoxelLight vLight = (VoxelLight) 0;
    
    int3 voxelGridPos = floor(worldPos / _CPUVoxelSize);
    int halfHor = _CPUVoxelHalfHor;
    int halfVer = _CPUVoxelHalfVer;
    int3 cur2CamGridPos = _CamCPUVoxelGridPos - voxelGridPos;
    int3 absCur2CamGridPos = abs(cur2CamGridPos);
    
    if(absCur2CamGridPos.x > halfHor || absCur2CamGridPos.y > halfVer || absCur2CamGridPos.z > halfHor)
    {
        return vLight;
    }
    
    int3 remainder = voxelGridPos % int3(_CPUVoxelHorizontal, _CPUVoxelVertical, _CPUVoxelHorizontal);
    remainder.x = remainder.x < 0 ? remainder.x + _CPUVoxelHorizontal : remainder.x;
    remainder.y = remainder.y < 0 ? remainder.y + _CPUVoxelVertical : remainder.y;
    remainder.z = remainder.z < 0 ? remainder.z + _CPUVoxelHorizontal : remainder.z;
        
    int cpuvoxelIdx = remainder.x + remainder.y * _CPUVoxelHorizontal + remainder.z * _CPUVoxelHorizontal * _CPUVoxelVertical;
    int cpuvoxelLightStartIdx = cpuvoxelIdx * _CPUVoxelLightMax;
        
    int3 gpuVoxelPos = floor(worldPos);
    int3 gpuVoxelIdx3 = gpuVoxelPos - voxelGridPos * _CPUVoxelSize;
    int gpuVoxelIdxInCPUIdx = cpuvoxelIdx * _CPUVoxelSize * _CPUVoxelSize * _CPUVoxelSize;
    int gpuVoxelIdxOnlyGPUIdx = gpuVoxelIdx3.x + gpuVoxelIdx3.y * _CPUVoxelSize + gpuVoxelIdx3.z * _CPUVoxelSize * _CPUVoxelSize;
    int gpuVoxelIdx = gpuVoxelIdxOnlyGPUIdx + gpuVoxelIdxInCPUIdx;
    gpuVoxelIdx /= 4; //4voxel => 4byte
        
    uint curGPUVoxelLight = _GPUVoxelLight[gpuVoxelIdx];
    int bitIdx = idx + 8 * (gpuVoxelIdx3.x % 4);
    bool isLight = (curGPUVoxelLight & (uint) (1 << bitIdx)) > 0 ? true : false;
    
    if(isLight == false)
    {
        return vLight;
    }
    
    int lightIdx = _CPUVoxelLight[cpuvoxelLightStartIdx + idx];
    vLightData = _LightData[lightIdx];
    
    float3 pixel2Light = vLightData.pos - worldPos;
    float disSquare = dot(pixel2Light, pixel2Light);
    disSquare = disSquare == 0 ? 0.0001f : disSquare;
    float3 direction = normalize(pixel2Light);

    float rcpRangeSquare = rcp(vLightData.range * vLightData.range);
    float factor = disSquare * rcpRangeSquare;
    float smoothFactor = saturate(1 - factor * factor);
    smoothFactor = smoothFactor * smoothFactor;

    float disAtt = rcp(disSquare) * smoothFactor;
    
    vLight.color = vLightData.color.rgb;
    vLight.direction = direction;
    vLight.distanceAttenuation = disAtt;
    return vLight;

}


