using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class WorleyNoise : MonoBehaviour
{
    public enum E_WorleyBufferType
    {
        Color = 0,
        Float = 1
    }
    [Serializable]
    public struct WorleyOption
    {
        public int width;
        public int height;
        public Vector2Int offset;
        public int pointCount;
        public bool useSideGrid;
        public float distanceWeight;
        public int disIdx;

        public static bool operator ==(WorleyOption p1, WorleyOption p2)
        {
            return p1.width == p2.width && p1.height == p2.height && p1.offset == p2.offset &&
                 p1.pointCount == p2.pointCount && p1.useSideGrid == p2.useSideGrid && p1.distanceWeight == p2.distanceWeight &&
                  p1.disIdx == p2.disIdx;
        }

        public static bool operator !=(WorleyOption p1, WorleyOption p2)
        {
            return p1.width != p2.width || p1.height != p2.height || p1.offset != p2.offset ||
                  p1.pointCount != p2.pointCount || p1.useSideGrid != p2.useSideGrid || p1.distanceWeight != p2.distanceWeight ||
                   p1.disIdx != p2.disIdx;
        }

    }

    const int WorleyNoise_Thread_Width = 32;


    public static ComputeBuffer WorleyNoiseGPU(WorleyOption option, E_WorleyBufferType type)
    {
        ComputeBuffer buffer = null;
        ComputeShader cs = CSM.Ins.m_WorleyNoise;

        Vector2[] arr_Points = GetPoints(option);
        ComputeBuffer pointBuffer = new ComputeBuffer(arr_Points.Length, HMUtil.StructSize(typeof(Vector2Int)));
        pointBuffer.SetData(arr_Points);

        switch (type)
        {
            case E_WorleyBufferType.Color:
                buffer = new ComputeBuffer(option.width * option.height, HMUtil.StructSize(typeof(Color)));
                cs.SetBuffer((int)type, "_ColorBuffer", buffer);
                break;
            case E_WorleyBufferType.Float:
                buffer = new ComputeBuffer(option.width * option.height, sizeof(float));
                cs.SetBuffer((int)type, "_FloatBuffer", buffer);
                break;
        }

        cs.SetInts("_TexSize", new int[2] { option.width, option.height });
        cs.SetInt("_PointCount", arr_Points.Length);
        cs.SetFloat("_DistanceWeight", option.distanceWeight);
        cs.SetFloats("_GridPos", new float[2] { option.offset.x, option.offset.y });

        cs.SetBuffer((int)type, "_PointBuffer", pointBuffer);

        int threadGroupCount_x = option.width / WorleyNoise_Thread_Width + (option.width % WorleyNoise_Thread_Width == 0 ? 0 : 1);
        int threadGroupCount_y = option.height / WorleyNoise_Thread_Width + (option.height % WorleyNoise_Thread_Width == 0 ? 0 : 1);

        cs.Dispatch((int)type, threadGroupCount_x, threadGroupCount_y, 1);

        pointBuffer.Release();
        return buffer;
    }


    public static Color[] WorleyNoiseCPU(WorleyOption option)
    {
        Color[] arr_Colors = new Color[option.width * option.height];
        Vector2[] arr_Point = GetPoints(option);
        int pointCount = arr_Point.Length;
        int disIdx = option.disIdx > option.pointCount - 1 ? pointCount - 1 : option.disIdx;
        Vector2Int gridPos = option.offset * new Vector2Int(option.width, option.height);
        if (option.distanceWeight == 0)
        {
            for (int y = 0; y < option.height; y++)
            {
                for (int x = 0; x < option.width; x++)
                {
                    Vector2 texelPos = new Vector2(x, y) + gridPos;
                    int[] arr_idx = new int[pointCount];
                    float[] arr_dis = new float[pointCount];

                    int texelIdx = x + y * option.width;

                    for (int i = 0; i < pointCount; i++)
                    {
                        float curDis = Vector2.Distance(arr_Point[i], texelPos);
                        arr_idx[i] = i;
                        arr_dis[i] = curDis;
                    }
                    int[] arr_sortIdx = arr_dis.GetSortIdx();
                    arr_idx = arr_idx.SetIdxArray(arr_sortIdx);
                    arr_dis = arr_dis.SetIdxArray(arr_sortIdx);

                    float value = arr_idx[disIdx] / (float)(pointCount - 1);
                    arr_Colors[texelIdx] = new Color(value, value, value, 1);
                }
            }
        }
        else
        {
            for (int y = 0; y < option.height; y++)
            {
                for (int x = 0; x < option.width; x++)
                {
                    Vector2 texelPos = new Vector2(x, y) + gridPos;
                    int[] arr_idx = new int[pointCount];
                    float[] arr_dis = new float[pointCount];

                    int texelIdx = x + y * option.width;

                    for (int i = 0; i < pointCount; i++)
                    {
                        float curDis = Vector2.Distance(arr_Point[i], texelPos);
                        arr_idx[i] = i;
                        arr_dis[i] = curDis;
                    }
                    int[] arr_sortIdx = arr_dis.GetSortIdx();
                    arr_idx = arr_idx.SetIdxArray(arr_sortIdx);
                    arr_dis = arr_dis.SetIdxArray(arr_sortIdx);
                    float value = arr_dis[disIdx] * option.distanceWeight;
                    arr_Colors[texelIdx] = new Color(value, value, value, 1);
                }
            }
        }
        return arr_Colors;
    }




    static Vector2[] GetPoints(WorleyOption option)
    {
        Vector2[] arr_Points = null;
        Vector2Int gridSize = new Vector2Int(option.width, option.height);
        if (option.useSideGrid)
        {
            arr_Points = new Vector2[option.pointCount * 9];
            for (int i = 0; i < 9; i++)
            {
                Vector2Int curGridPos = option.offset * gridSize + HMUtil.GetSamplingPos9(i, gridSize);
                Vector2[] curPoints = GetPointsInGrid(option.width, option.height, curGridPos, option.pointCount);
                curPoints.CopyTo(arr_Points, i * option.pointCount);
            }
        }
        else
        {
            Vector2Int gridPos = option.offset * gridSize;
            arr_Points = GetPointsInGrid(option.width, option.height, gridPos, option.pointCount);
        }
        return arr_Points;
    }
    static Vector2[] GetPointsInGrid(int width, int height, Vector2Int gridPos, int pointCount)
    {
        Vector2[] arr_Point = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            Vector2 pointPosInNorm = new Vector2(HMUtil.Random_uint3Tofloat(gridPos.x, gridPos.y, 2 * i), HMUtil.Random_uint3Tofloat(gridPos.x, gridPos.y, 2 * i + 1));
            arr_Point[i] = new Vector2(width * pointPosInNorm.x, height * pointPosInNorm.y) + gridPos;
        }
        return arr_Point;
    }
}
