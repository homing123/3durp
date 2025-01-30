using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMaker : MonoBehaviour
{
    enum E_RandomType
    {
        DRandomXORShift
    }

    uint DRandomXORShift(uint seed)
    {
        seed ^= seed >> 16;
        seed *= 0x85ebca6b;
        seed ^= seed >> 13;
        seed *= 0xc2b2ae35;
        seed ^= seed >> 16;
        return seed;
    }

    float Noise2D(int x, int y, E_RandomType type = E_RandomType.DRandomXORShift)
    {
        uint seed = (uint)x * 73856093u ^ (uint)y * 19349663u;

        switch(type)
        {
            case E_RandomType.DRandomXORShift:
                return DRandomXORShift(seed) / (float)uint.MaxValue;
        }
        return 0;
    }
    static Dictionary<Vector2Int, Vector2> D_PerlinGradientVec = new Dictionary<Vector2Int, Vector2>();
    float PerlinEaseCurve(float left, float right, float t)
    {
        float ease = 6 * Mathf.Pow(t, 5) - 15 * Mathf.Pow(t, 4) + 10 * Mathf.Pow(t, 3);
        return left + (right - left) * ease;
    }
    float PerlinNoise2D(float x, float y)
    {
        Vector2 point = new Vector2(x, y);
        int floorx = Mathf.FloorToInt(x);
        int floory = Mathf.FloorToInt(y);
        int ceilx = Mathf.CeilToInt(x);
        int ceily = Mathf.CeilToInt(y);

        Vector2Int lbot = new Vector2Int(floorx, floory);
        Vector2Int ltop = new Vector2Int(floorx, ceily);
        Vector2Int rbot = new Vector2Int(ceilx, floory);
        Vector2Int rtop = new Vector2Int(ceilx, ceily);

        Vector2Int[] arr_gridPos = new Vector2Int[4] { lbot, ltop, rbot, rtop };
        for (int i = 0; i < 4; i++)
        {
            if (D_PerlinGradientVec.ContainsKey(arr_gridPos[i]) == false)
            {
                D_PerlinGradientVec[arr_gridPos[i]] = Noise2D(arr_gridPos[i].x, arr_gridPos[i].y).RadianToUnitVector2();
            }
        }

        float[] arr_gridWeight = new float[4];

        for (int i = 0; i < 4; i++)
        {
            Vector2 gradientVec = D_PerlinGradientVec[arr_gridPos[i]];
            Vector2 distanceVec = point - arr_gridPos[i];
            arr_gridWeight[i] = Vector2.Dot(gradientVec, distanceVec); //거리벡터가 정규화 되지 않기때문에 -1~1이 아님
        }

        float yLeft = PerlinEaseCurve(arr_gridWeight[0], arr_gridWeight[1], y - (int)y);
        float yRight = PerlinEaseCurve(arr_gridWeight[2], arr_gridWeight[3], y - (int)y);

        float result = PerlinEaseCurve(yLeft, yRight, x - (int)x);

        return result;


    }
}
