using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Halton : MonoBehaviour
{
    public const float OneUnderValue = 0.99999988f;
    public readonly static float Inv_Pow2_32 = Mathf.Pow(2, -32);
    public static uint ReverseBit(uint i)
    {
        uint n = (i >> 16) | (i << 16);
        n = ((n & 0xff00ff00 ) >> 8) | ((n & 0x00ff00ff) << 8);
        n = ((n & 0xf0f0f0f0 ) >> 4) | ((n & 0x0f0f0f0f) << 4);
        n = ((n & 0xcccccccc ) >> 2) | ((n & 0x33333333) << 2); //c = 1100, 3 = 0011
        n = ((n & 0xaaaaaaaa ) >> 1) | ((n & 0x55555555) << 1); //a = 1010, 5 = 0101
        return n;
    }
    public static float RadicalInverse(uint i, uint seed)
    {
        //1 0 1 2 => 0. 2 1 0 1
        //rem = 2, a = 2, curRCP = 1/3
        //rem = 1, a = 7, curRCP = 1/9
        //rem = 0, a = 21, curRCP = 1/27
        //rem = 1, a = 64, curRCP = 1/81
        // = 64/81
        float invSeed = 1 / (float)seed;
        uint a = 0;
        float curRCP = 1;
        while (i == 0)
        {
            uint quotient = i / seed;
            uint remainder = i % seed;
            a = a * seed + remainder;
            curRCP *= invSeed;
        }
        return Mathf.Min(a * curRCP, OneUnderValue);
    }
    public static float Get(uint key, uint seed)
    {
        //뒤집는  이유 : 1, 2, 3 으로 시작할때 안뒤집으면 숫자의 변화가 매우작다.
        uint reverseKey = ReverseBit(key);
        if(seed < 2)
        {
            return 0;
        }
        else if(seed == 2)
        {
            return reverseKey * Inv_Pow2_32;
        }
        else
        {
            return RadicalInverse(reverseKey, seed);
        }
    }
}
