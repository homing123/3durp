using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTextureObject : MonoBehaviour
{
    // Start is called before the first frame update
    Vector2 m_Pos;
    float m_Size;
    RenderTexture m_RT;
    void Start()
    {
        Material mat = GetComponent<Renderer>().material;
        GetComponent<Renderer>().material = new Material(mat);
        transform.localScale = Vector3.one * m_Size;
        transform.position = new Vector3(m_Pos.x, 0, m_Pos.y);
        SetRenderTexture(m_RT);
        //Debug.Log(m_Key);
        //TerrainMaker.DebugRenderTexturePixels(m_RT);
    }

    public static RenderTextureObject Create(Vector2 pos, float size, RenderTexture rt)
    {
        RenderTextureObject rto = Instantiate(Prefabs.Ins.G_RenderTextureObject, MapMaker.Ins.transform).GetComponent<RenderTextureObject>();
        rto.m_Pos = pos;
        rto.m_Size = size;
        rto.m_RT = rt;
        return rto;
    }
    public void SetRenderTexture(RenderTexture rt)
    {
        GetComponent<Renderer>().material.SetTexture("_MainTex", rt);
    }
}
