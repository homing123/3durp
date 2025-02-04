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
        //0,0을 시작으로 하고 GroundWidth를 크기로 하는 그리드를 카메라위치를 중심으로 생성
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Ground.GroundWidth), Mathf.FloorToInt(camPosXZ.y / Ground.GroundWidth)); //중심이 되는 그리드 인덱스좌표
        m_CurCenterChunkIdxCoord = centerGridIdxCoord;

        //현재그리드 제외하고 중심에서 바깥으로 렌더거리만큼 그리기 위해 필요한 그리드 갯수
        //3일경우 생성그리드는 7 x 7 크기가 된다
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
        Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Ground.GroundWidth), Mathf.FloorToInt(camPosXZ.y / Ground.GroundWidth)); //중심이 되는 그리드 인덱스좌표
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


//사각형 랜더범위만큼의 Ground 생성
//보이는곳만 Ground active on

//잔디맵 생성
//눈맵 생성
//바다 생성
//

