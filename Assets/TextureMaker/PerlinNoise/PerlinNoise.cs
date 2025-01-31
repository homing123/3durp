using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PerlinNoise : MonoBehaviour
{
    static Dictionary<Vector2Int, Vector2> D_PerlinGradientVec = new Dictionary<Vector2Int, Vector2>();

    [System.Serializable]
    public struct PerlinOption
    {
        public bool useGPU;
        public float scale;
        public bool isChanged(PerlinOption option)
        {
            return scale != option.scale;
        }
    }
    public readonly static PerlinOption BasicOption = new PerlinOption();

    public static void CreatePerlinNoise2DBuffer(int width, int height, PerlinOption option, Color[] buffer)
    {
        SetPerlinNoise2DBuffer(width, height, option, buffer);
    }
    public static Color[] CreatePerlinNoise2DBuffer(int width, int height, PerlinOption option)
    {
        Color[] arr_color = new Color[width * height];
        SetPerlinNoise2DBuffer(width, height, option, arr_color);
        return arr_color;
    }
    public static Color[] CreatePerlinNoise2DBuffer(int width, int height)
    {
        Color[] arr_color = new Color[width * height];
        SetPerlinNoise2DBuffer(width, height, BasicOption, arr_color);
        return arr_color;
    }
    static void SetPerlinNoise2DBuffer(int width, int height, PerlinOption option, Color[] buffer)
    {
        if(option.useGPU)
        {

        }
        else
        {
            if(buffer.Length != width * height)
            {
                throw new System.Exception("Buffer size error");
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = x + width * y;
                    float value = PerlinNoise2D(x * 0.01f * option.scale, y * 0.01f * option.scale);
                    buffer[idx] = new Color(value, value, value, 1);
                }
            }

        }
    }

    public static float PerlinInterpolation(float left, float right, float t)
    {
        float ease = 6 * Mathf.Pow(t, 5) - 15 * Mathf.Pow(t, 4) + 10 * Mathf.Pow(t, 3);
        return left + (right - left) * ease;
    }
   
    public static float PerlinNoise2D(float x, float y)
    {
        Vector2 point = new Vector2(x, y);
        Vector2Int lbot = new Vector2Int((int)x, (int)y);
        Vector2Int ltop = new Vector2Int((int)x, (int)y + 1);
        Vector2Int rbot = new Vector2Int((int)x + 1, (int)y);
        Vector2Int rtop = new Vector2Int((int)x + 1, (int)y + 1);

        Vector2Int[] arr_gridPos = new Vector2Int[4] { lbot, ltop, rbot, rtop };
        for (int i = 0; i < 4; i++)
        {
            if (D_PerlinGradientVec.ContainsKey(arr_gridPos[i]) == false)
            {
                float noiseValue = HMUtil.Noise2D(arr_gridPos[i].x, arr_gridPos[i].y) * Mathf.PI * 2;
                D_PerlinGradientVec[arr_gridPos[i]] = noiseValue.RadianToUnitVector2();
            }
        }

        float[] arr_gridWeight = new float[4];

        for (int i = 0; i < 4; i++)
        {
            Vector2 gradientVec = D_PerlinGradientVec[arr_gridPos[i]];
            Vector2 distanceVec = point - arr_gridPos[i];
            arr_gridWeight[i] = Vector2.Dot(gradientVec, distanceVec); //거리벡터가 정규화 되지 않기때문에 -1~1이 아님
        }

        float yLeft = PerlinInterpolation(arr_gridWeight[0], arr_gridWeight[1], y - (int)y);
        float yRight = PerlinInterpolation(arr_gridWeight[2], arr_gridWeight[3], y - (int)y);

        float result = PerlinInterpolation(yLeft, yRight, x - (int)x);
        result = result * 0.5f + 0.5f;

        return result;
    }
}
