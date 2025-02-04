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
    }

    public static MapMaker Ins;
    [SerializeField][Range(1, 300)] float m_RenderDis;
    [SerializeField] Ground m_Ground;
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
                D_Chunk[curGridIdxCoord] = CreateChunk(groundPos);
            }
        }
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
    Chunk CreateChunk(Vector2 groundPos)
    {
        Chunk chunk = new Chunk();
        chunk.m_Ground = Ground.Create(groundPos);


        return chunk;
    }





}


//�簢�� ����������ŭ�� Ground ����
//���̴°��� Ground active on

//�ܵ�� ����
//���� ����
//�ٴ� ����
//

