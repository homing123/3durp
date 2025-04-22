using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    public static Ground Create(Vector2Int terrainMeshGridKey, int quality)
    {
        Ground ground = Instantiate(Prefabs.Ins.G_Ground, MapMaker.Ins.transform).GetComponent<Ground>();
        Vector2 groundPos = new Vector2(terrainMeshGridKey.x, terrainMeshGridKey.y) * TerrainMaker.Ins.TerrainMeshGridSize;
        ground.m_Pos = groundPos;
        ground.m_Quality = quality;
        return ground;
    }
    [SerializeField] ComputeShader m_CSTextureMerge;
    int m_Quality;
    Vector2 m_Pos;
    Material m_Mat;
    MeshFilter m_MeshFilter;
    MeshRenderer m_MeshRenderer;
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
        int texWidth = TerrainMaker.Ins.m_VertexWidth;

        transform.localScale = new Vector3(m_Quality, m_Quality, m_Quality);
        transform.position = new Vector3(m_Pos.x, 0, m_Pos.y);

        m_Mat.SetInt("_Quality", (int)m_Quality);
        m_Mat.SetInt("_MeshSize", TerrainMaker.Ins.m_MeshSize);
        m_Mat.SetInt("_VertexCount", TerrainMaker.Ins.m_VertexWidth);

        CamMove.ev_TerrainPosUpdate += Move;
    }
    private void OnDestroy()
    {
        CamMove.ev_TerrainPosUpdate -= Move;
    }
    private void Update()
    {
        Vector2 camXZ = Camera.main.transform.position.Vt2XZ();
        //transform.position = new Vector3(camXZ.x, 0, camXZ.y);
        //SetHeightBuffer();
    }
    void Move(Vector2 pos)
    { 
        //각자 그리드 크기에 맞춰서 이동해야함
        transform.position = new Vector3(pos.x, 0, pos.y);
    }



    //RenderTextureObject m_RTO;
    //[ContextMenu("TerrainNormal")]
    //public void CreateTerrainNormalTexture()
    //{
    //    if (m_RTO != null)
    //    {
    //        Destroy(m_RTO.gameObject);
    //        m_RTO = null;
    //    }
    //    float dVertex = (float)TerrainMaker.Ins.m_MeshSize / TerrainMaker.Ins.m_TexWidth * m_Quality;
    //    float size = (TerrainMaker.Ins.m_MeshSize + dVertex) * m_Quality;

    //    Debug.Log(dVertex + " " + size);
    //    m_RTO = RenderTextureObject.Create(transform.position.Vt2XZ(), size, m_HeightBuffer);

    //}
    

}
