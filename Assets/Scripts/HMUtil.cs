using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UIElements;

public enum E_RandomType
{
    DRandomXORShift
}

public class HMUtil
{
    static Vector2Int[] SamplingGrid9 = new Vector2Int[9]
    {new Vector2Int(-1,1),new Vector2Int(0,1),new Vector2Int(1,1),
    new Vector2Int(-1,0),new Vector2Int(0,0),new Vector2Int(1,0),
    new Vector2Int(-1,-1),new Vector2Int(0,-1),new Vector2Int(1,-1)};

    public static Vector2Int GetSamplingPos9(int idx, Vector2Int gridSize)
    {
        return new Vector2Int(gridSize.x * SamplingGrid9[idx].x, gridSize.y * SamplingGrid9[idx].y);
    }
    public static uint DRandomXORShift(uint seed)
    {
        seed ^= seed >> 16;
        seed *= 0x85ebca6b;
        seed ^= seed >> 13;
        seed *= 0xc2b2ae35;
        seed ^= seed >> 16;
        return seed;
    }
    public static float Random_uint2Tofloat(int x, int y, E_RandomType type = E_RandomType.DRandomXORShift)
    {
        uint seed = (uint)x * 73856093u ^ (uint)y  * 19349663u + 104395301u;

        switch (type)
        {
            case E_RandomType.DRandomXORShift:
                return DRandomXORShift(seed) / (float)uint.MaxValue;
        }
        return 0;
    }
    public static float Vector2IntToSeed(Vector2Int vt2i)
    {
        uint seed = ((uint)vt2i.x * 73856093u) ^ ((uint)vt2i.y * 19349663u) + 104395301u;
        return seed;
    }
    public static float Random_uint3Tofloat(int x, int y, int z, E_RandomType type = E_RandomType.DRandomXORShift)
    {
        uint seed = (uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)z * 83492791u + 104395301u;

        switch (type)
        {
            case E_RandomType.DRandomXORShift:
                return DRandomXORShift(seed) / (float)uint.MaxValue;
        }
        return 0;
    }
    
    

    public static float GetAngleDis(float a, float b)
    {
        float angle = b - a;
        if (angle < -180)
        {
            return angle + 360;
        }
        if (angle > 180)
        {
            return angle - 360;
        }
        return angle;
    }
    public static int StructSize(System.Type type)
    {
        return System.Runtime.InteropServices.Marshal.SizeOf(type);
    }

    public static Vector3 DotProduct(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
    }
}
public static class HMUtilEx
{
    public static Vector2 RadianToUnitVector2(this float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }
    public static void Sort(this float[] arr)
    {
        int length = arr.Length;
        if(length > 1)
        {
            for (int i = 0; i < length; i++)
            {
                int smallIdx = i;
                for (int j = i + 1; j < length; j++)
                {
                    if (arr[smallIdx] > arr[j])
                    {
                        smallIdx = j;
                    }
                }
                float temp = arr[i];
                arr[i] = arr[smallIdx];
                arr[smallIdx] = temp;
            }
        }
    }
    public static int[] GetSortIdx(this float[] arr)
    {
        int length = arr.Length;
        int[] arr_idx = new int[length];
        if (length > 1)
        {
            float[] arr_Value = new float[length];

            for(int i=0;i<length;i++)
            {
                arr_Value[i] = arr[i];
            }
            for (int i = 0; i < length; i++)
            {
                int smallIdx = i;
                for (int j = i + 1; j < length; j++)
                {
                    if (arr_Value[smallIdx] > arr_Value[j])
                    {
                        smallIdx = j;
                    }
                }
                float temp = arr_Value[i];
                arr_Value[i] = arr_Value[smallIdx];
                arr_Value[smallIdx] = temp;
                arr_idx[i] = smallIdx;
            }
        }
        return arr_idx;
    }
    public static T[] SetIdxArray<T>(this T[] arr, int[] arr_idx)
    {
        if(arr.Length != arr_idx.Length)
        {
            throw new System.Exception($"SetIdxArray length error {arr.Length}, {arr_idx.Length}");
        }
        T[] arr_result = new T[arr.Length];
        
        for (int i=0;i<arr_idx.Length;i++)
        {
            arr_result[i] = arr[arr_idx[i]];
        }
        return arr_result;
    }
    public static Vector2 Vt2XZ(this Vector3 vt3)
    {
        return new Vector2(vt3.x, vt3.z);
    }
    public static Matrix4x4 GetVP(this Camera cam)
    {
        Matrix4x4 p = cam.projectionMatrix;
        Matrix4x4 v = cam.transform.worldToLocalMatrix;
        Matrix4x4 VP = p * v;
        return VP;
    }

    public static bool FrustumCullingInWorld(this Camera cam, Vector3 boxMin, Vector3 boxMax)
    {
        //절두체와 회전이없는 박스의 충돌체크
        Matrix4x4 v = cam.transform.worldToLocalMatrix;
        Matrix4x4 p = cam.projectionMatrix;
        float f = cam.farClipPlane;
        float n = cam.nearClipPlane;
        p.m32 = 1;
        p.m22 = f / (f - n);
        p.m23 = -f * n / (f - n);
        Matrix4x4 VP = p * v;
        //Debug.Log(v);
        //Debug.Log(p);
        //Debug.Log(VP);
        Vector4[] arr_row = new Vector4[4] { VP.GetRow(0), VP.GetRow(1), VP.GetRow(2), VP.GetRow(3)};
        //각 평면의 안쪽에 있을 조건
        //(Mr3 + Mr0) * V >= 0 //left
        //(Mr3 - Mr0) * V >= 0 //right
        //(Mr3 + Mr1) * V >= 0 //bottom
        //(Mr3 - Mr1) * V >= 0 //top
        //(Mr3 + Mr2) * V >= 0 //near
        //(Mr3 - Mr2) * V >= 0 //far

        (Vector3 normal, float d)[] arr_Plane = new (Vector3, float)[6];
        Vector4[] vt4 =  new Vector4[6];
        vt4[0] = -(arr_row[3] + arr_row[0]);
        vt4[1] = -(arr_row[3] - arr_row[0]);
        vt4[2] = -(arr_row[3] + arr_row[1]);
        vt4[3] = -(arr_row[3] - arr_row[1]);
        vt4[4] = -(arr_row[3] + arr_row[2]);
        vt4[5] = -(arr_row[3] - arr_row[2]);
        for(int i=0;i<6;i++)
        {
            Vector3 vt3 = new Vector3(vt4[i].x, vt4[i].y, vt4[i].z);
            float dis = vt3.magnitude;
            vt4[i] /= dis;
            arr_Plane[i] = (new Vector3(vt4[i].x, vt4[i].y, vt4[i].z), vt4[i].w);
        }

        bool isOut = false;
        for (int i=0;i<6;i++)
        {
            Vector3 nearPoint;
            nearPoint.x = arr_Plane[i].normal.x < 0 ? boxMax.x : boxMin.x;
            nearPoint.y = arr_Plane[i].normal.y < 0 ? boxMax.y : boxMin.y;
            nearPoint.z = arr_Plane[i].normal.z < 0 ? boxMax.z : boxMin.z;
            float dis = arr_Plane[i].normal.x * nearPoint.x + arr_Plane[i].normal.y * nearPoint.y + arr_Plane[i].normal.z * nearPoint.z + arr_Plane[i].d;
            if(dis > 0)
            {
                isOut = true;
                break;
            }
            //Debug.Log(i + " " + arr_Plane[i].normal + " " + arr_Plane[i].d + " " + nearPoint + " " + dis + " " + (dis <= 0));
        }
        //Debug.Log(boxMin + " " + boxMax + " " + isIn);
        return isOut;
    }
}