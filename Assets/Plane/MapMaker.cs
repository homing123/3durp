using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMaker : MonoBehaviour
{

    public class Chunk
    {
        public Ground m_Ground;
        public GrassMaker.ChunkGrassData m_GrassData;
        public TerrainMaker.ChunkTerrainData m_TerrainData;
        ~Chunk()
        {
            m_GrassData.Release();
            m_TerrainData.Release();
        }
    }

    public static MapMaker Ins;
    [SerializeField][Range(1, 300)] float m_RenderDis;
    [SerializeField] bool m_UpdateGrassMaterial;
    [SerializeField][Range(1, 64)] int m_GrassCountPerOne;
    [SerializeField][Range(0.1f, 100)] float m_GrassRenderDis;
    int m_GridCountPerRenderDis;

    Vector2Int m_CurCenterChunkIdxCoord;
    Dictionary<Vector2Int, Chunk> D_Chunk = new Dictionary<Vector2Int, Chunk>();

    private void Awake()
    {
        Ins = this;
    }
    private void Start()
    {
        MapInit();
    }
  

    private void Update()
    {
        Vector2Int chunkMoveDisIdxCoord = ChunkMoveCheck();
        if (chunkMoveDisIdxCoord != Vector2Int.zero)
        {
            ChunkMove(chunkMoveDisIdxCoord);
        }
        foreach(Vector2Int key in D_Chunk.Keys)
        {
            if(m_UpdateGrassMaterial)
            {
                D_Chunk[key].m_GrassData.GrassMaterial = new Material(Prefabs.Ins.M_Grass);
                D_Chunk[key].m_GrassData.GrassMaterial.SetBuffer("_GrassBuffer", D_Chunk[key].m_GrassData.DrawedGrassBuffer);
            }
            GrassMaker.DrawGrass(D_Chunk[key].m_GrassData);
        }
    }

    void MapInit()
    {
        //0,0�� �������� �ϰ� GroundWidth�� ũ��� �ϴ� �׸��带 ī�޶���ġ�� �߽����� ����
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Ground.GroundWidth), Mathf.FloorToInt(camPosXZ.y / Ground.GroundWidth)); //�߽��� �Ǵ� �׸��� �ε�����ǥ
        m_CurCenterChunkIdxCoord = centerGridIdxCoord;

        //����׸��� �����ϰ� �߽ɿ��� �ٱ����� �����Ÿ���ŭ �׸��� ���� �ʿ��� �׸��� ����
        //3�ϰ�� �����׸���� 7 x 7 ũ�Ⱑ �ȴ�
        m_GridCountPerRenderDis = Mathf.CeilToInt((m_RenderDis) / Ground.GroundWidth);

        Vector2Int minGridIdxCoord = new Vector2Int(centerGridIdxCoord.x - m_GridCountPerRenderDis, centerGridIdxCoord.y - m_GridCountPerRenderDis);

        int gridWidthCount = m_GridCountPerRenderDis * 2 + 1;
        for (int y = 0; y < gridWidthCount; y++)
        {
            for (int x = 0; x < gridWidthCount; x++)
            {
                Vector2Int curGridIdxCoord = new Vector2Int(minGridIdxCoord.x + x, minGridIdxCoord.y + y);
                Vector2 groundPos = new Vector2(curGridIdxCoord.x * Ground.GroundWidth, curGridIdxCoord.y * Ground.GroundWidth);
                D_Chunk[curGridIdxCoord] = CreateChunk(groundPos, camPosXZ);
            }
        }

        //D_Chunk[new Vector2Int(0, 0)] = CreateChunk(new Vector2(0, 0) * Ground.GroundWidth, camPosXZ);
        //D_Chunk[new Vector2Int(1, 0)] = CreateChunk(new Vector2(1, 0) * Ground.GroundWidth, camPosXZ);
        //D_Chunk[new Vector2Int(0, 1)] = CreateChunk(new Vector2(0, 1) * Ground.GroundWidth, camPosXZ);
        //D_Chunk[new Vector2Int(1, 1)] = CreateChunk(new Vector2(1, 1) * Ground.GroundWidth, camPosXZ);
    }
    Vector2Int ChunkMoveCheck()
    {
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Ground.GroundWidth), Mathf.FloorToInt(camPosXZ.y / Ground.GroundWidth)); //�߽��� �Ǵ� �׸��� �ε�����ǥ
        if (m_CurCenterChunkIdxCoord != centerGridIdxCoord)
        {
            return centerGridIdxCoord - m_CurCenterChunkIdxCoord;
        }
        return Vector2Int.zero;
    }
    void ChunkMove(Vector2Int chunkMoveDisIdxCoord)
    {

    }
    Chunk CreateChunk(Vector2 groundPos, Vector2 curCamPos)
    {
        Chunk chunk = new Chunk();
        chunk.m_TerrainData = TerrainMaker.Ins.GetChunkTerrainData(groundPos);
        float dis = Vector2.Distance(curCamPos, groundPos);
        chunk.m_Ground = Ground.Create(groundPos, dis, chunk.m_TerrainData.arr_MeshLOD);

        //public Vector2 GridPos;
        //public int GrassCountPerOne;
        //public Vector2 GridSize;
        //public float GrassRenderDis;
        //public float RandomPosMul;
        //public Vector2Int HeightBufferSize;
        //public ComputeBuffer HeightBuffer;
        //public ComputeBuffer NormalBuffer;

        GrassMaker.GrassMakerOption grassOption = new GrassMaker.GrassMakerOption();
        grassOption.GridPos = groundPos;
        grassOption.GrassCountPerOne = m_GrassCountPerOne;
        grassOption.GridSize = new Vector2(Ground.GroundWidth, Ground.GroundWidth);
        grassOption.GrassRenderDis = m_GrassRenderDis;
        grassOption.HeightBufferSize = chunk.m_TerrainData.HeightBufferSize;
        grassOption.HeightBuffer = chunk.m_TerrainData.heightBuffer;
        grassOption.NormalBuffer = chunk.m_TerrainData.normalBuffer;

        chunk.m_GrassData = GrassMaker.GetChunkGrassData(grassOption);

        return chunk;
    }





}


//�簢�� ����������ŭ�� Ground ����
//���̴°��� Ground active on

//�ܵ�� ����
//���� ����
//�ٴ� ����
//

