using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMaker : MonoBehaviour
{

    public class Chunk
    {
        public Vector2Int m_Key;
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
        DrawGrass();
    }

    void DrawGrass()
    {
        List<Chunk> l_GrassRenderChunk = new List<Chunk>();
        foreach (Vector2Int key in D_Chunk.Keys)
        {
            Rect rect = new Rect((key.x - 0.5f) * Ground.GroundSize.x, (key.y - 0.5f) * Ground.GroundSize.y, Ground.GroundSize.x, Ground.GroundSize.y);
            Vector2[] vertex = new Vector2[4];
            vertex[0] = new Vector2(rect.xMin, rect.yMin);
            vertex[1] = new Vector2(rect.xMin, rect.yMax);
            vertex[2] = new Vector2(rect.xMax, rect.yMin);
            vertex[3] = new Vector2(rect.xMax, rect.yMax);

            Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();

            float grassRenderDisSquare = GrassMaker.Ins.m_GrassRenderDis * GrassMaker.Ins.m_GrassRenderDis;
            bool isDrawed = false;
            for(int i=0;i<4;i++)
            {
                float curVertexDisSquare = Mathf.Pow(vertex[i].x - camPosXZ.x, 2) + Mathf.Pow(vertex[i].y - camPosXZ.y, 2);
                if(curVertexDisSquare < grassRenderDisSquare)
                {
                    isDrawed = true;
                    break;
                }
            }

            if(isDrawed)
            {
                l_GrassRenderChunk.Add(D_Chunk[key]);
            }
        }

        GrassMaker.DrawGrass(l_GrassRenderChunk.ToArray());


        
    }
    void MapInit()
    {
        //0,0�� �������� �ϰ� GroundWidth�� ũ��� �ϴ� �׸��带 ī�޶���ġ�� �߽����� ����
        //�� �׶���� �׸������� �߽����� �Ѵ� = �׸��� 0,0 => -0.5, -0.5 ~ 0.5, 0.5
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Ground.GroundSize.x), Mathf.FloorToInt(camPosXZ.y / Ground.GroundSize.y)); //�߽��� �Ǵ� �׸��� �ε�����ǥ
        m_CurCenterChunkIdxCoord = centerGridIdxCoord;

        //����׸��� �����ϰ� �߽ɿ��� �ٱ����� �����Ÿ���ŭ �׸��� ���� �ʿ��� �׸��� ����
        //3�ϰ�� �����׸���� 7 x 7 ũ�Ⱑ �ȴ�
        m_GridCountPerRenderDis = Mathf.CeilToInt((m_RenderDis) / Ground.GroundSize.x);

        Vector2Int minGridIdxCoord = new Vector2Int(centerGridIdxCoord.x - m_GridCountPerRenderDis, centerGridIdxCoord.y - m_GridCountPerRenderDis);

        int gridWidthCount = m_GridCountPerRenderDis * 2 + 1;
        for (int y = 0; y < gridWidthCount; y++)
        {
            for (int x = 0; x < gridWidthCount; x++)
            {
                Vector2Int curGridIdxCoord = new Vector2Int(minGridIdxCoord.x + x, minGridIdxCoord.y + y);
                Vector2 groundPos = curGridIdxCoord * Ground.GroundSize;
                D_Chunk[curGridIdxCoord] = CreateChunk(groundPos, camPosXZ, curGridIdxCoord);
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
        Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Ground.GroundSize.x), Mathf.FloorToInt(camPosXZ.y / Ground.GroundSize.y)); //�߽��� �Ǵ� �׸��� �ε�����ǥ
        if (m_CurCenterChunkIdxCoord != centerGridIdxCoord)
        {
            return centerGridIdxCoord - m_CurCenterChunkIdxCoord;
        }
        return Vector2Int.zero;
    }
    void ChunkMove(Vector2Int chunkMoveDisIdxCoord)
    {

    }
    Chunk CreateChunk(Vector2 groundPos, Vector2 curCamPos ,Vector2Int key)
    {
        Chunk chunk = new Chunk();
        chunk.m_Key = key;
        chunk.m_TerrainData = TerrainMaker.Ins.GetChunkTerrainData(groundPos);
        float dis = Vector2.Distance(curCamPos, groundPos);
        chunk.m_Ground = Ground.Create(groundPos, dis, chunk.m_TerrainData.arr_MeshLOD);

        GrassMaker.GrassMakerOption grassOption = new GrassMaker.GrassMakerOption();
        grassOption.GridPos = groundPos;
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

