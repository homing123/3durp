using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    public enum InputMode
    { 
        Character,
        Camera
    }
    public static InputMode m_InputMode = InputMode.Character;

    public void ChangeInputMode(InputMode inputmode)
    {
        m_InputMode = inputmode;
    }
}


public class Util
{
    public static float GetAngleDis(float a, float b)
    {
        float angle = b - a;
        if(angle < -180)
        {
            return angle + 360;
        }
        if( angle > 180)
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
public static class UtilEx
{
    public static Vector2 RadianToUnitVector2(this float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }
}