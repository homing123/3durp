sampler2D _HeightMap_1;
sampler2D _NormalMap_1;
sampler2D _HeightMap_2;
sampler2D _NormalMap_2;

float2 _TexWorldSize; //x = quality 1, y = quality 2 
float2 _TexCenterPosXZ;


float GetHeight(float2 worldXZ, int quality)
{
	float texWorldSize = _TexWorldSize[quality - 1];
	float2 texMin = _TexCenterPosXZ - texWorldSize * 0.5f;
	float2 texMax = _TexCenterPosXZ + texWorldSize * 0.5f;
	float2 uv = saturate((worldXZ - texMin) / (texMax - texMin));
	float height = 0;
	if (quality == 1)
	{
		height = tex2Dlod(_HeightMap_1, float4(uv, 0, 0)).r;
	}
	else
	{
		height = tex2Dlod(_HeightMap_2, float4(uv, 0, 0)).r;
	}
	return height;

}
float3 GetNormal(float2 worldXZ, int quality)
{
	float texWorldSize = _TexWorldSize[quality - 1];
	float2 texMin = _TexCenterPosXZ - texWorldSize * 0.5f;
	float2 texMax = _TexCenterPosXZ + texWorldSize * 0.5f;
	float2 uv = saturate((worldXZ - texMin) / (texMax - texMin));
	float3 normal = float3(0, 0, 0);
	if (quality == 1)
	{
		normal = tex2Dlod(_NormalMap_1, float4(uv, 0, 0)).rgb;
	}
	else
	{
		normal = tex2Dlod(_NormalMap_2, float4(uv, 0, 0)).rgb;
	}
	return normal;

}


float GetHeightAutoQuality(float2 worldXZ)
{
	float2 disXZ = abs(_TexCenterPosXZ - worldXZ);
	if (disXZ.x < _TexWorldSize.x && disXZ.y < _TexWorldSize.x)
	{
		return GetHeight(worldXZ, 1);
	}
	else
	{
		return GetHeight(worldXZ, 2);
	}
}

float3 GetNormalAutoQuality(float2 worldXZ)
{
	float2 disXZ = abs(_TexCenterPosXZ - worldXZ);
	if (disXZ.x < _TexWorldSize.x && disXZ.y < _TexWorldSize.x)
	{
		return GetNormal(worldXZ, 1);
	}
	else
	{
		return GetNormal(worldXZ, 2);
	}
}
