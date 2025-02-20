using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    public static Ground Create(Vector2 pos, int quality)
    {
        Ground ground = Instantiate(Prefabs.Ins.G_Ground, MapMaker.Ins.transform).GetComponent<Ground>();
        ground.m_Pos = pos;
        ground.m_Quality = quality;
        return ground;
    }
    [SerializeField] ComputeShader m_CSTextureMerge;
    int m_Quality;
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
        int texWidth = TerrainMaker.Ins.m_TexWidth;

        transform.localScale = new Vector3(m_Quality, m_Quality, m_Quality);
        transform.position = new Vector3(m_Pos.x, 0, m_Pos.y);
        m_HeightBuffer = new RenderTexture(texWidth, texWidth, 0, RenderTextureFormat.RFloat);
        m_NormalBuffer = new RenderTexture(texWidth, texWidth, 0, RenderTextureFormat.ARGBFloat);
        m_HeightBuffer.enableRandomWrite = true;
        m_NormalBuffer.enableRandomWrite = true;
        m_HeightBuffer.filterMode = FilterMode.Point;
        m_NormalBuffer.filterMode = FilterMode.Bilinear;
        m_Mat.SetInt("_Quality", (int)m_Quality);
        m_Mat.SetInt("_MeshSize", TerrainMaker.Ins.m_MeshSize);

        SetHeightBuffer();
        CamMove.ev_TerrainPosUpdate += Move;
    }
    private void OnDestroy()
    {
        m_HeightBuffer.Release();
        m_NormalBuffer.Release();
        CamMove.ev_TerrainPosUpdate -= Move;
    }
    private void Update()
    {
        Vector2 camXZ = Camera.main.transform.position.Vt2XZ();
        //transform.position = new Vector3(camXZ.x, 0, camXZ.y);
        //SetHeightBuffer();
    }
    void Move(Vector2 pos)
    { //각자 그리드 크기에 맞춰서 이동해야함

        transform.position = new Vector3(pos.x, 0, pos.y);
        SetHeightBuffer();
    }
    void SetHeightBuffer()
    {
        int groundSize = TerrainMaker.Ins.m_MeshSize * (int)m_Quality;
        int texWidth = TerrainMaker.Ins.m_TexWidth;
        Vector2 curSize = new Vector2(groundSize, groundSize);
        Vector2 curPosXZ = transform.position.Vt2XZ();

        Vector2 minWorld = curPosXZ - curSize * 0.5f;
        Vector2 maxWorld = curPosXZ + curSize * 0.5f;
        Vector2 minGrid = minWorld / curSize;
        Vector2 maxGrid = maxWorld / curSize;
        Vector2Int minKey = new Vector2Int(Mathf.FloorToInt(minGrid.x), Mathf.FloorToInt(minGrid.y));
        Vector2Int maxKey = new Vector2Int(Mathf.FloorToInt(maxGrid.x), Mathf.FloorToInt(maxGrid.y));
        Vector2Int dataSize = new Vector2Int(maxKey.x - minKey.x + 1, maxKey.y - minKey.y + 1);
        TerrainData[] arr_Data = new TerrainData[dataSize.x * dataSize.y];
        //Debug.Log($"{minWorld} {maxWorld} {minGrid} {maxGrid} {minKey} {maxKey}");

        int idx = 0;
        for (int y=minKey.y; y<=maxKey.y; y++)
        {
            for(int x=minKey.x;x<=maxKey.x;x++)
            {
                Vector2Int curKey = new Vector2Int(x, y);
                arr_Data[idx] = MapMaker.Ins.GetTerrainData(m_Quality, curKey);
                m_CSTextureMerge.SetTexture(0, "_HeightMap" + idx, arr_Data[idx].heightTexture);
                m_CSTextureMerge.SetTexture(0, "_NormalMap" + idx, arr_Data[idx].normalTexture);
                idx++;
            }
        }
        Vector2 heightMapMinWorld = minKey * groundSize; //arr_data안의 높이맵의 범위 min
        Vector2 heightMapMaxWorld = (maxKey + new Vector2Int(1, 1)) * groundSize; //arr_data안의 높이맵의 범위 max
        Vector2 uvMin = (minWorld - heightMapMinWorld) / (heightMapMaxWorld - heightMapMinWorld);
        Vector2 uvMax = (maxWorld - heightMapMinWorld) / (heightMapMaxWorld - heightMapMinWorld);
        uvMin = uvMin * dataSize;
        uvMax = uvMax * dataSize;
        //Debug.Log($"{heightMapMinWorld} {heightMapMaxWorld} {minWorld} {maxWorld} {uvMin} {uvMax}");

        m_CSTextureMerge.SetInts("_MergeTexSize", new int[2] { texWidth, texWidth });
        m_CSTextureMerge.SetInts("_DataTexSize", new int[2] { TerrainMaker.Ins.m_TexWidth, TerrainMaker.Ins.m_TexWidth });
        m_CSTextureMerge.SetInts("_DataTexCount", new int[2] { dataSize.x, dataSize.y });
        m_CSTextureMerge.SetFloats("_UVMin", new float[2] { uvMin.x, uvMin.y });
        m_CSTextureMerge.SetFloats("_UVMax", new float[2] { uvMax.x, uvMax.y });
        m_CSTextureMerge.SetTexture(0, "_HeightMergeMap", m_HeightBuffer);
        m_CSTextureMerge.SetTexture(0, "_NormalMergeMap", m_NormalBuffer);
        m_Mat.SetTexture("_HeightMap", m_HeightBuffer);
        m_Mat.SetTexture("_NormalMap", m_NormalBuffer);


        int groupx = texWidth / Thread_Width + (texWidth % Thread_Width == 0 ? 0 : 1);
        m_CSTextureMerge.Dispatch(0, groupx, groupx, 1);
    }


}
