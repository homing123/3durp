using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderTextureObject : MonoBehaviour
{
    // Start is called before the first frame update
    Vector2Int m_Key;
    TerrainMaker.E_TerrainQuality m_Quality;
    RenderTexture m_RT;
    void Start()
    {
        Material mat = GetComponent<Renderer>().material;
        GetComponent<Renderer>().material = new Material(mat);
        transform.localScale = (int)(m_Quality) * Chunk.ChunkSize;
        transform.position = new Vector3(m_Key.x * (int)(m_Quality) * Chunk.ChunkSize.x, 0, m_Key.y*(int)(m_Quality) * Chunk.ChunkSize.y) + new Vector3(Chunk.ChunkSize.x * 0.5f,0, Chunk.ChunkSize.y * 0.5f) * (int)(m_Quality);
        Debug.Log(m_Key + " " + (int)(m_Quality) + " " + Chunk.ChunkSize);
        SetRenderTexture(m_RT);
    }

    public static RenderTextureObject Create(Vector2Int Key, TerrainMaker.E_TerrainQuality quality, RenderTexture rt)
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
