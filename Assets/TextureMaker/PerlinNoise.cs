using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PerlinNoise : MonoBehaviour
{
    public enum E_PerlinBufferType
    {
        Color = 0,
        Height = 1
    }
    [Serializable]
    public struct PerlinOption
    {
        public int width;
        public int height;
        public Vector2 offset;
        public float scale;
        public float gradientRadianMul;
        public float amplitude;
        public float frequency;
        public float persistence;
        public float lacunarity;
        public int octave;

        public static bool operator ==(PerlinOption p1, PerlinOption p2)
        {
            return p1.width == p2.width && p1.height == p2.height && p1.offset == p2.offset &&
                 p1.scale == p2.scale && p1.gradientRadianMul == p2.gradientRadianMul && p1.amplitude == p2.amplitude &&
                  p1.frequency == p2.frequency && p1.persistence == p2.persistence && p1.lacunarity == p2.lacunarity && p1.octave == p2.octave;
        }

        public static bool operator !=(PerlinOption p1, PerlinOption p2)
        {
            return p1.width != p2.width || p1.height != p2.height || p1.offset != p2.offset ||
                  p1.scale != p2.scale || p1.gradientRadianMul != p2.gradientRadianMul || p1.amplitude != p2.amplitude ||
                   p1.frequency != p2.frequency || p1.persistence != p2.persistence || p1.lacunarity != p2.lacunarity || p1.octave != p2.octave;
        }
    }

    const int GridGradient_Thread_Width = 32;
    const int Perlin_Thread_Width = 32;
    const int Group_Max = 512;


    public static ComputeBuffer PerlinNoiseGPU(PerlinOption option, E_PerlinBufferType type)
    {
        ComputeBuffer buffer = null;
        ComputeShader cs = CSM.Ins.m_PerlinNoise;
        switch (type)
        {
            case E_PerlinBufferType.Color:
                buffer = new ComputeBuffer(option.width * option.height, HMUtil.StructSize(typeof(Color)));
                cs.SetBuffer((int)type, "_ColorBuffer", buffer);
                break;
            case E_PerlinBufferType.Height:
                buffer = new ComputeBuffer(option.width * option.height, sizeof(float));
                cs.SetBuffer((int)type, "_HeightBuffer", buffer);
                break;
        }

        int perlinKernel_x = option.width / GridGradient_Thread_Width + (option.width % GridGradient_Thread_Width == 0 ? 0 : 1);
        int perlinKernel_y = option.height / GridGradient_Thread_Width + (option.height % GridGradient_Thread_Width == 0 ? 0 : 1);

        cs.SetFloat("_GradientRadianMul", option.gradientRadianMul);
        cs.SetVector("_Offset", option.offset);
        cs.SetInts("_TexSize", new int[2] { option.width, option.height });
        cs.SetFloat("_Scale", option.scale);
        cs.SetFloat("_Amplitude", option.amplitude);
        cs.SetFloat("_Frequency", option.frequency);
        cs.SetFloat("_Persistence", option.persistence);
        cs.SetFloat("_Lacunarity", option.lacunarity);
        cs.SetInt("_Octaves", option.octave);

        cs.Dispatch((int)type, perlinKernel_x, perlinKernel_y, 1);

        return buffer;
    }

    public static Color[] PerlinNoiseCPU(PerlinOption option)
    {
        Color[] buffer = new Color[option.width * option.height];
        if(option.octave > 3)
        {
            Debug.Log("cpu가 타고있어요... 불타고 있다고!!!");
            Debug.Log("옥타브 4이상은 gpu로");
        }
        else
        {
            Dictionary<Vector2Int, Vector2> D_PerlinGradientVec = new Dictionary<Vector2Int, Vector2>();

            if (buffer.Length != option.width * option.height)
            {
                throw new System.Exception("Buffer size error");
            }
            for (int y = 0; y < option.height; y++)
            {
                for (int x = 0; x < option.width; x++)
                {
                    int idx = x + option.width * y;
                    float frequency = option.frequency;
                    float amplitude = option.amplitude;
                    float value = 0;
                    for (int i = 0; i < option.octave; i++)
                    {
                        value += amplitude * PerlinNoise2D((x * 0.01f * option.scale + option.offset.x) * frequency, (y * 0.01f * option.scale + option.offset.y) * frequency, D_PerlinGradientVec, option.gradientRadianMul);
                        amplitude *= option.persistence;
                        frequency *= option.lacunarity;
                    }
                    value = value * 0.5f + 0.5f;
                    buffer[idx] = new Color(value, value, value, 1);
                }
            }
        }
        return buffer;
    }

    public static float PerlinInterpolation(float left, float right, float t)
    {
        float ease = 6 * Mathf.Pow(t, 5) - 15 * Mathf.Pow(t, 4) + 10 * Mathf.Pow(t, 3);
        return left + (right - left) * ease;
    }
   
    public static float PerlinNoise2D(float x, float y, Dictionary<Vector2Int, Vector2> D_PerlinGradientVec, float gradientRadianMul)
    {
        Vector2 point = new Vector2(x, y);
        int i_x = Mathf.FloorToInt(x);
        int i_y = Mathf.FloorToInt(y);
        Vector2Int lbot = new Vector2Int(i_x, i_y);
        Vector2Int ltop = new Vector2Int(i_x, i_y + 1);
        Vector2Int rbot = new Vector2Int(i_x + 1, i_y);
        Vector2Int rtop = new Vector2Int(i_x + 1, i_y + 1);

        Vector2Int[] arr_gridPos = new Vector2Int[4] { lbot, ltop, rbot, rtop };
        for (int i = 0; i < 4; i++)
        {
            if (D_PerlinGradientVec.ContainsKey(arr_gridPos[i]) == false)
            {
                float noiseValue = HMUtil.Random_uint2Tofloat(arr_gridPos[i].x, arr_gridPos[i].y) * Mathf.PI * 2 * gradientRadianMul;
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

        float yLeft = PerlinInterpolation(arr_gridWeight[0], arr_gridWeight[1], y - i_y);
        float yRight = PerlinInterpolation(arr_gridWeight[2], arr_gridWeight[3], y - i_y);

        float result = PerlinInterpolation(yLeft, yRight, x - i_x);

        return result;
    }
}
