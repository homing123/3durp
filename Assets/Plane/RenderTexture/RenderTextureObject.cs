using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTextureObject : MonoBehaviour
{
    // Start is called before the first frame update
    Vector2Int m_Key;
    int m_Quality;
    RenderTexture m_RT;
    void Start()
    {
        Material mat = GetComponent<Renderer>().material;
        GetComponent<Renderer>().material = new Material(mat);
        transform.localScale = Vector3.one * TerrainMaker.Ins.m_MeshSize * m_Quality;
        transform.position = new Vector3(m_Key.x * TerrainMaker.Ins.m_MeshSize * m_Quality, 0, m_Key.y* TerrainMaker.Ins.m_MeshSize * m_Quality) + new Vector3(TerrainMaker.Ins.m_MeshSize * 0.5f,0, TerrainMaker.Ins.m_MeshSize * 0.5f) * m_Quality;
        SetRenderTexture(m_RT);
    }

    public static RenderTextureObject Create(Vector2Int Key, int quality, RenderTexture rt)
    {
        RenderTextureObject rto = Instantiate(Prefabs.Ins.G_RenderTextureObject, MapMaker.Ins.transform).GetComponent<RenderTextureObject>();
        rto.m_Key = Key;
        rto.m_Quality = quality;
        rto.m_RT = rt;
        return rto;
    }
    public void SetRenderTexture(RenderTexture rt)
    {
        GetComponent<Renderer>().material.SetTexture("_MainTex", rt);
    }
}
