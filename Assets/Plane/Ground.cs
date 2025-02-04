using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    public const float GroundWidth = 10;
    public static Ground Create(Vector2 pos)
    {
        Ground ground = Instantiate(Prefabs.Ins.G_Ground, MapMaker.Ins.transform).GetComponent<Ground>();
        ground.m_Pos = pos;
        return ground;
    }
    Vector2 m_Pos;
    private void Start()
    {
        //««∫ø¿Ã ¡ﬂΩ…¿Ã∂Û 0.5∞ˆ«—∞™ ¥ı«ÿ¡‹
        transform.position = new Vector3(m_Pos.x + GroundWidth * 0.5f, 0, m_Pos.y + GroundWidth * 0.5f);
    }
}
