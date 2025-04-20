
struct VoxelLightData
{
    int type;
    int3 padding;
    float4 color;
    float range;
    float3 pos;
};

StructuredBuffer<VoxelLightData> _LightData;
StructuredBuffer<int> _CPUVoxelLight;
StructuredBuffer<uint> _GPUVoxelLight;

int3 _CamCPUVoxelGridPos;
int _CPUVoxelSize;
int4 _CPUVoxelAxisSize; //x = horizontal, y = vertical, z = hor / 2, w = ver / 2
int _CPUVoxelLightMax;

VoxelLightData GetLight(float3 worldPos, int idx)
{
    //������ġ�� cpu���� ��ġ ���ϰ� �ش���ġ�� cpu���� �ε��� ���ؼ� cpu�������� ���
    //cpu������ġ�� ������ġ�� gpu���� �ε��� ���ϰ�
    //gpu���������� cpu���� ������ ����Ʈ ���� ���
    //�ش� �� ����
    
    VoxelLightData vLightData = (VoxelLightData)0;
    
    int3 voxelGridPos = floor(worldPos / _CPUVoxelSize);
    int halfHor = _CPUVoxelAxisSize.z;
    int halfVer = _CPUVoxelAxisSize.w;
    int3 cur2CamGridPos = _CamCPUVoxelGridPos - voxelGridPos;
    int3 absCur2CamGridPos = abs(cur2CamGridPos);
    
    if(absCur2CamGridPos.x > halfHor || absCur2CamGridPos.y > halfVer || absCur2CamGridPos.z > halfHor)
    {
        return vLightData;
    }
    
    int3 remainder = voxelGridPos % _CPUVoxelAxisSize.xyx;
    remainder.x = remainder.x < 0 ? remainder.x + _CPUVoxelAxisSize.x : remainder.x;
    remainder.y = remainder.y < 0 ? remainder.y + _CPUVoxelAxisSize.y : remainder.y;
    remainder.z = remainder.z < 0 ? remainder.z + _CPUVoxelAxisSize.x : remainder.z;
        
    int cpuvoxelIdx = remainder.x + remainder.y * _CPUVoxelAxisSize.x + remainder.z * _CPUVoxelAxisSize.x * _CPUVoxelAxisSize.y;
    int cpuvoxelLightStartIdx = cpuvoxelIdx * _CPUVoxelLightMax;
        
    int3 gpuVoxelPos = floor(worldPos);
    int3 gpuVoxelIdx3 = gpuVoxelPos - voxelGridPos * _CPUVoxelSize;
    int gpuVoxelIdx = gpuVoxelIdx3.x + gpuVoxelIdx3.y * _CPUVoxelSize + gpuVoxelIdx3.z * _CPUVoxelSize * _CPUVoxelSize;
    gpuVoxelIdx /= 4; //4voxel => 4byte
        
    uint curGPUVoxelLight = _GPUVoxelLight[gpuVoxelIdx];
    bool isLight = (curGPUVoxelLight & (uint) (1 << (idx + 8 * (gpuVoxelIdx3.x % 4)))) > 0 ? true : false;
    
    if(isLight == false)
    {
        return vLightData;
    }
    
    int lightIdx = _CPUVoxelLight[cpuvoxelLightStartIdx + idx];
    return _LightData[lightIdx];
}

