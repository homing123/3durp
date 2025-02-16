using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    public static Ground Create(Vector2 pos, TerrainMaker.E_TerrainQuality quality)
    {
        Ground ground = Instantiate(Prefabs.Ins.G_Ground, MapMaker.Ins.transform).GetComponent<Ground>();
        ground.m_Pos = pos;
        ground.m_Quality = quality;
        return ground;
    }
    [SerializeField] ComputeShader m_CSTextureMerge;
    [SerializeField] int m_TexWidth;
    TerrainMaker.E_TerrainQuality m_Quality;
    Vector2 m_Pos;
    Material m_Mat;
    MeshFilter m_MeshFilter;
    MeshRenderer m_MeshRenderer;
    RenderTexture m_HeightBuffer;
    RenderTexture m_NormalBuffer;
    const int Thread_Width = 32;

    private void Awake()
    {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_Mat = m_MeshRenderer.material;
        m_MeshRenderer.material = new Material(m_Mat);
        m_Mat = m_MeshRenderer.material;
        m_MeshFilter = GetComponent<MeshFilter>();
    }
    
    private void Start()
    {
        m_MeshFilter.mesh = TerrainMaker.Ins.m_Mesh;
        transform.localScale = new Vector3((int)m_Quality, (int)m_Quality, (int)m_Quality);
        transform.position = new Vector3(m_Pos.x, 0, m_Pos.y);
        m_HeightBuffer = new RenderTexture(m_TexWidth, m_TexWidth, 0, RenderTextureFormat.RFloat);
        m_NormalBuffer = new RenderTexture(m_TexWidth, m_TexWidth, 0, RenderTextureFormat.ARGBFloat);
        m_HeightBuffer.enableRandomWrite = true;
        m_NormalBuffer.enableRandomWrite = true;
        m_Mat.SetInt("_Quality", (int)m_Quality);

        SetHeightBuffer();

    }
    private void OnDestroy()
    {
        m_HeightBuffer.Release();
        m_NormalBuffer.Release();
    }
    private void Update()
    {
        Vector2 camXZ = Camera.main.transform.position.Vt2XZ();
        transform.position = new Vector3(camXZ.x, 0, camXZ.y);
        SetHeightBuffer();
    }
    void SetHeightBuffer()
    {
        Vector2 curSize = Chunk.ChunkSize * (int)m_Quality;
        Vector2 curPosXZ = transform.position.Vt2XZ();

        Vector2 minWorld = curPosXZ - curSize * 0.5f;
        Vector2 maxWorld = curPosXZ + curSize * 0.5f;
        Vector2 minChunkGrid = minWorld / ((int)m_Quality * Chunk.ChunkSize.x);
        Vector2 maxChunkGrid = maxWorld / ((int)m_Quality * Chunk.ChunkSize.x);
        Vector2Int minKey = new Vector2Int(Mathf.FloorToInt(minChunkGrid.x), Mathf.FloorToInt(minChunkGrid.y));
        Vector2Int maxKey = new Vector2Int(Mathf.FloorToInt(maxChunkGrid.x), Mathf.FloorToInt(maxChunkGrid.y));
        //Debug.Log($"{m_Quality} {minWorld} {maxWorld} {minChunkGrid} {maxChunkGrid} {minKey} {maxKey}");
        Vector2Int dataSize = new Vector2Int(maxKey.x - minKey.x + 1, maxKey.y - minKey.y + 1);
        TerrainMaker.TerrainData[] arr_Data = new TerrainMaker.TerrainData[dataSize.x * dataSize.y];
        int idx = 0;
        for (int y=minKey.y; y<=maxKey.y; y++)
        {
            for(int x=minKey.x;x<=maxKey.x;x++)
            {
                Vector2Int curKey = new Vector2Int(x, y);
                arr_Data[idx] = MapMaker.Ins.GetTerrainData(m_Quality, curKey);
                m_CSTextureMerge.SetTexture(0, "_HeightMap" + idx, arr_Data[idx].heightBuffer);
                m_CSTextureMerge.SetTexture(0, "_NormalMap" + idx, arr_Data[idx].normalBuffer);
                idx++;

            }
        }
        Vector2 heightMapMinWorld = minKey * (int)m_Quality * Chunk.ChunkSize;
        Vector2 heightMapMaxWorld = (maxKey + new Vector2Int(1, 1)) * (int)m_Quality * Chunk.ChunkSize;
        Vector2 uvMin = (minWorld - heightMapMinWorld) / (heightMapMaxWorld - heightMapMinWorld);
        Vector2 uvMax = (maxWorld - heightMapMinWorld) / (heightMapMaxWorld - heightMapMinWorld);
        uvMin = uvMin * dataSize;
        uvMax = uvMax * dataSize;
        //Debug.Log($"{heightMapMinWorld} {heightMapMaxWorld} {minWorld} {maxWorld} {uvMin} {uvMax}");

        m_CSTextureMerge.SetInts("_MergeTexSize", new int[2] { m_TexWidth, m_TexWidth });
        m_CSTextureMerge.SetInts("_DataTexSize", new int[2] { TerrainMaker.Ins.m_TexWidth, TerrainMaker.Ins.m_TexWidth });
        m_CSTextureMerge.SetInts("_DataTexCount", new int[2] { dataSize.x, dataSize.y });
        m_CSTextureMerge.SetFloats("_UVMin", new float[2] { uvMin.x, uvMin.y });
        m_CSTextureMerge.SetFloats("_UVMax", new float[2] { uvMax.x, uvMax.y });
        m_CSTextureMerge.SetTexture(0, "_HeightMergeMap", m_HeightBuffer);
        m_CSTextureMerge.SetTexture(0, "_NormalMergeMap", m_NormalBuffer);
        m_Mat.SetTexture("_HeightMap", m_HeightBuffer);
        m_Mat.SetTexture("_NormalMap", m_NormalBuffer);


        int groupx = m_TexWidth / Thread_Width + (m_TexWidth % Thread_Width == 0 ? 0 : 1);
        m_CSTextureMerge.Dispatch(0, groupx, groupx, 1);
    }


}
