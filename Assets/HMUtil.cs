using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        uint seed = (uint)x * 73856093u ^ (uint)y  * 19349663u + 104395301;

        switch (type)
        {
            case E_RandomType.DRandomXORShift:
                return DRandomXORShift(seed) / (float)uint.MaxValue;
        }
        return 0;
    }
    public static float Random_uint3Tofloat(int x, int y, int z, E_RandomType type = E_RandomType.DRandomXORShift)
    {
        uint seed = (uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)z * 83492791u + 104395301;

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
}
public static class HMUtilEx
{
    public static Vector2 RadianToUnitVector2(this float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }
}