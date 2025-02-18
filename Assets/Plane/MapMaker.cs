using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GrassMaker;
using static TerrainMaker;
using System;
using System.Net;

public struct ChunkData
{
    public Vector2Int key;
    public Chunk_GrassData grassData;
    public void Release()
    {
        grassData.Release();
    }
}
public struct Chunk_GrassData
{

    public ComputeBuffer grassBuffer;
    public GrassMakerOption option;
    public int grassCount;

    public void Release()
    {
        grassBuffer.Release();
    }
}

public struct TerrainData
{
    public RenderTexture heightTexture;
    public RenderTexture normalTexture;

    public void Release()
    {
        heightTexture.Release();
        normalTexture.Release();
    }

}
public class MapMaker : MonoBehaviour
{
    public static MapMaker Ins;

    public const int ChunkSize = 16;
    [SerializeField] int m_RenterTextureQuality;
    [SerializeField] RenderTextureObject m_RenderTextureObj;
    public const int TerrainCount = 2;

    List<RenderTextureObject> L_Objs = new List<RenderTextureObject>();

    Dictionary<int, Dictionary<Vector2Int, TerrainData>> D_TerrainData = new Dictionary<int, Dictionary<Vector2Int, TerrainData>>();
    Dictionary<Vector2Int, ChunkData> D_ChunkData = new Dictionary<Vector2Int, ChunkData>();
    Vector2Int m_CurChunkKey;

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
        GrassMaker.Ins.DrawGrass(GetDrawGrassChunks());

    }
    private void OnDestroy()
    {
        foreach (ChunkData value in D_ChunkData.Values)
        {
            value.Release();
        }
        for (int i = 1; i <= TerrainCount; i++)
        {
            foreach (TerrainData data in D_TerrainData[i].Values)
            {
                data.Release();
            }
        }
    }
    public TerrainData GetTerrainData(int quality, Vector2Int key)
    {
        if (D_TerrainData[quality].ContainsKey(key))
        {
            return D_TerrainData[quality][key];
        }
        else
        {
            D_TerrainData[quality][key] = TerrainMaker.Ins.GetTerrainData(quality, key);

            return D_TerrainData[quality][key];
        }
    }
    public ChunkData GetChunkData(ref Vector2Int key)
    {
        if (D_ChunkData.ContainsKey(key) == false)
        {
            CreateChunkData(ref key);
        }
      
        return D_ChunkData[key];
    }
    public void CreateChunkData(ref Vector2Int key)
    {
        ChunkData chunk = new ChunkData();
        chunk.key = key;
        GrassMakerOption grassOption = new GrassMakerOption();
        grassOption.chunkCenterPos = (key + new Vector2(0.5f, 0.5f)) * ChunkSize;
        grassOption.terrainData = D_TerrainData[TerrainCount - 1][key];
        chunk.grassData = GrassMaker.Ins.GetChunkGrassData(grassOption);
    }

    void MapInit()
    {
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        m_CurChunkKey = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / ChunkSize), Mathf.FloorToInt(camPosXZ.y / ChunkSize)); //현재 카메라위치가 속한 청크의 키
        Vector3 groundPos = new Vector3(camPosXZ.x, 0, camPosXZ.y);

        for (int i = 1; i <= TerrainCount; i++)
        {
            D_TerrainData[i] = new Dictionary<Vector2Int, TerrainData>();
        }

        //float lowQualityWidth = ChunkSize * m_GroundSizeMul;
        //Vector2 minXZWorld = new Vector2(camPosXZ.x - lowQualityWidth * 0.5f, camPosXZ.y - lowQualityWidth * 0.5f);
        //Vector2 maxXZWorld = new Vector2(camPosXZ.x + lowQualityWidth * 0.5f, camPosXZ.y + lowQualityWidth * 0.5f);

        for (int i = 1; i <= TerrainCount;i++)
        {
            Ground.Create(groundPos, i);
        }


    }


    ChunkData[] GetDrawGrassChunks()
    {
        List<ChunkData> l_GrassRenderChunk = new List<ChunkData>();
        foreach (Vector2Int key in D_ChunkData.Keys)
        {
            Vector2 rectMin = new Vector2((key.x - 0.5f) * ChunkSize, (key.y - 0.5f) *ChunkSize);
            Rect rect = new Rect(rectMin.x, rectMin.y, ChunkSize, ChunkSize);
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
                l_GrassRenderChunk.Add(D_ChunkData[key]);
            }
        }

        return l_GrassRenderChunk.ToArray();  
    }
  
    Vector2Int ChunkMoveCheck()
    {
        //Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        //Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Chunk.ChunkSize.x), Mathf.FloorToInt(camPosXZ.y / Chunk.ChunkSize.y)); //중심이 되는 그리드 인덱스좌표
        //if (m_CurCenterChunkIdxCoord != centerGridIdxCoord)
        //{
        //    return centerGridIdxCoord - m_CurCenterChunkIdxCoord;
        //}
        return Vector2Int.zero;
    }
    void ChunkMove(Vector2Int chunkMoveDisIdxCoord)
    {

    }
   




    [ContextMenu("RenderTexture")]
    public void CreateRenderTexture()
    {
        for (int i = 0; i < L_Objs.Count; i++)
        {
            Destroy(L_Objs[i].gameObject);
            L_Objs.RemoveAt(i);
            i--;
        }

        foreach (Vector2Int key in D_TerrainData[m_RenterTextureQuality].Keys)
        {
            L_Objs.Add(RenderTextureObject.Create(key, m_RenterTextureQuality, D_TerrainData[m_RenterTextureQuality][key].heightTexture));
        }
    }



}


//사각형 랜더범위만큼의 Ground 생성
//보이는곳만 Ground active on

//잔디맵 생성
//눈맵 생성
//바다 생성
//

