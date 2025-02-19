using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTextureObject : MonoBehaviour
{
    // Start is called before the first frame update
    Vector2Int m_Key;
    float m_Size;
    RenderTexture m_RT;
    void Start()
    {
        Material mat = GetComponent<Renderer>().material;
        GetComponent<Renderer>().material = new Material(mat);
        transform.localScale = Vector3.one * m_Size;
        transform.position = (new Vector3(m_Key.x, 0, m_Key.y) + new Vector3(0.5f, 0, 0.5f)) * m_Size;
        SetRenderTexture(m_RT);
        //TerrainMaker.DebugRenderTexturePixels(m_RT);
    }

    public static RenderTextureObject Create(Vector2Int Key, float size, RenderTexture rt)
    {
        RenderTextureObject rto = Instantiate(Prefabs.Ins.G_RenderTextureObject, MapMaker.Ins.transform).GetComponent<RenderTextureObject>();
        rto.m_Key = Key;
        rto.m_Size = size;
        rto.m_RT = rt;
        return rto;
    }
    public void SetRenderTexture(RenderTexture rt)
    {
        GetComponent<Renderer>().material.SetTexture("_MainTex", rt);
    }
}
