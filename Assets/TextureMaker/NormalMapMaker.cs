using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalMapMaker : MonoBehaviour
{
    const int NormalToHeight_Thread_Width = 32;
    public enum E_NormalSideType
    {
        Clamp = 0,
        Reduce = 1,
    }
    public enum E_NormalBufferType
    {
        FloatToVector3 = 0,
        FloatToColor = 1,
        ColorToVector3 = 2,
        ColorToColor = 3,
    }
    public static Color[] HeightMapToNormalMapCPU(int width, int height, Color[] heightMap, Vector2 worldSize)
    {
        Color[] normalMap = new Color[width * height];
        Vector2 d = new Vector2(worldSize.x / width, worldSize.y / height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int curIdx = x + y * width;
                float left = x == 0 ? heightMap[curIdx].r : heightMap[curIdx - 1].r;
                float right = x == width - 1 ? heightMap[curIdx].r : heightMap[curIdx + 1].r;
                float bottom = y == 0 ? heightMap[curIdx].r : heightMap[curIdx - width].r;
                float top = y == height - 1 ? heightMap[curIdx].r : heightMap[curIdx + width].r;
                Vector3 value = GetNormal(left, right, bottom, top, d);

                value = value * 0.5f + new Vector3(0.5f, 0.5f, 0.5f);
                normalMap[curIdx].r = value.x;
                normalMap[curIdx].g = value.y;
                normalMap[curIdx].b = value.z;
                normalMap[curIdx].a = 1;
            }
        }

        return normalMap;
    }
    public static ComputeBuffer HeightMapToNormalMapGPU(int width, int height, ComputeBuffer heightBuffer, E_NormalBufferType type, Vector2 worldSize, E_NormalSideType sideType)
    {
        ComputeBuffer normalBuffer = null;
        ComputeShader cs = CSM.Ins.m_HeightToNormal;
        Vector2 d = new Vector2(worldSize.x / width, worldSize.y / height);
        switch (type)
        {
            case E_NormalBufferType.FloatToVector3:
                normalBuffer = new ComputeBuffer(width * height, sizeof(float) * 3);
                cs.SetBuffer((int)type, "_HeightFloatBuffer", heightBuffer);
                cs.SetBuffer((int)type, "_NormalVector3Buffer", normalBuffer);
                break;
            case E_NormalBufferType.FloatToColor:
                normalBuffer = new ComputeBuffer(width * height, sizeof(float) * 4);
                cs.SetBuffer((int)type, "_HeightFloatBuffer", heightBuffer);
                cs.SetBuffer((int)type, "_NormalColorBuffer", normalBuffer);
                break;
            case E_NormalBufferType.ColorToVector3:
                normalBuffer = new ComputeBuffer(width * height, sizeof(float) * 3);
                cs.SetBuffer((int)type, "_HeightColorBuffer", heightBuffer);
                cs.SetBuffer((int)type, "_NormalVector3Buffer", normalBuffer);
                break;
            case E_NormalBufferType.ColorToColor:
                normalBuffer = new ComputeBuffer(width * height, sizeof(float) * 4);
                cs.SetBuffer((int)type, "_HeightColorBuffer", heightBuffer);
                cs.SetBuffer((int)type, "_NormalColorBuffer", normalBuffer);
                break;
        }

        cs.SetFloats("_D", new float[2] { d.x, d.y });
        cs.SetInts("_TexSize", new int[2] { width, height });
        cs.SetInt("_NormalSideType", (int)sideType);
        int threadGroupCount_x = width / NormalToHeight_Thread_Width + (width % NormalToHeight_Thread_Width == 0 ? 0 : 1);
        int threadGroupCount_y = height / NormalToHeight_Thread_Width + (height % NormalToHeight_Thread_Width == 0 ? 0 : 1);

        cs.Dispatch((int)type, threadGroupCount_x, threadGroupCount_y, 1);
        return normalBuffer;
    }
    public static Vector3 GetNormal(float left, float right, float bottom, float top, in Vector2 d)
    {
        Vector3 horizon = new Vector3(2 * d.x, 0, right - left);
        Vector3 vertical = new Vector3(0, 2 * d.y, top - bottom);
        return HMUtil.DotProduct(horizon, vertical).normalized;
    }
}

