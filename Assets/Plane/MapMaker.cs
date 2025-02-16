using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GrassMaker;
using static TerrainMaker;
using System;
using System.Net;

public class Chunk
{
    public static readonly Vector2 ChunkSize = new Vector2(10, 10); //x,y 같게 사용할건데 편의상 vt2로 만듬

    public Vector2Int m_Key;
    public GrassMaker.ChunkGrassData m_GrassData;
}
public class MapMaker : MonoBehaviour
{

    [SerializeField] bool m_Update;

    Dictionary<E_TerrainQuality, Dictionary<Vector2Int, TerrainMaker.TerrainData>> D_TerrainData = new Dictionary<E_TerrainQuality, Dictionary<Vector2Int, TerrainMaker.TerrainData>>();

    public static MapMaker Ins;
    [SerializeField][Range(1, 4)] float m_GroundSizeMul;

    Vector2Int m_CurCenterChunkIdxCoord;
    Dictionary<Vector2Int, Chunk> D_Chunk = new Dictionary<Vector2Int, Chunk>();
    [SerializeField] E_TerrainQuality m_RenterTextureQuality;
    [SerializeField] RenderTextureObject m_RenderTextureObj;
    List<RenderTextureObject> L_Objs = new List<RenderTextureObject>();
    private void Awake()
    {
        Ins = this;
    }
    private void Start()
    {
        MapInit();
    }

    public TerrainMaker.TerrainData GetTerrainData(E_TerrainQuality quality, Vector2Int key)
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
    [ContextMenu("RenderTexture")]
    public void CreateRenderTexture()
    {
        for(int i=0;i<L_Objs.Count;i++)
        {
            Destroy(L_Objs[i].gameObject);
            L_Objs.RemoveAt(i);
            i--;
        }

        foreach (Vector2Int key in D_TerrainData[m_RenterTextureQuality].Keys)
        {
            L_Objs.Add(RenderTextureObject.Create(key, m_RenterTextureQuality, D_TerrainData[m_RenterTextureQuality][key].normalBuffer));
        }
    }

    private void Update()
    {
        if(m_Update)
        {
            foreach (Chunk value in D_Chunk.Values)
            {
                value.m_GrassData.Release();
            }
            foreach (E_TerrainQuality quality in Enum.GetValues(typeof(E_TerrainQuality)))
            {
                foreach (TerrainMaker.TerrainData data in D_TerrainData[quality].Values)
                {
                    data.heightBuffer.Release();
                    data.normalBuffer.Release();
                }
            }
            MapInit();
        }
        Vector2Int chunkMoveDisIdxCoord = ChunkMoveCheck();
        if (chunkMoveDisIdxCoord != Vector2Int.zero)
        {
            ChunkMove(chunkMoveDisIdxCoord);
        }
        //DrawGrass();
    }
    private void OnDestroy()
    {
        foreach (Chunk value in D_Chunk.Values)
        {
            value.m_GrassData.Release();
        }
        foreach(E_TerrainQuality quality in Enum.GetValues(typeof(E_TerrainQuality)))
        {
            foreach(TerrainMaker.TerrainData data in D_TerrainData[quality].Values)
            {
                data.heightBuffer.Release();
                data.normalBuffer.Release();
            }
        }
    }

    void DrawGrass()
    {
        List<Chunk> l_GrassRenderChunk = new List<Chunk>();
        foreach (Vector2Int key in D_Chunk.Keys)
        {
            Rect rect = new Rect((key.x - 0.5f) * Chunk.ChunkSize.x, (key.y - 0.5f) * Chunk.ChunkSize.y, Chunk.ChunkSize.x, Chunk.ChunkSize.y);
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
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Chunk.ChunkSize.x), Mathf.FloorToInt(camPosXZ.y / Chunk.ChunkSize.y)); //중심이 되는 그리드 인덱스좌표
        m_CurCenterChunkIdxCoord = centerGridIdxCoord;

        foreach (E_TerrainQuality quality in Enum.GetValues(typeof(E_TerrainQuality)))
        {
            D_TerrainData[quality] = new Dictionary<Vector2Int, TerrainMaker.TerrainData>();
        }

        float lowQualityWidth = Chunk.ChunkSize.x * m_GroundSizeMul;
        Vector2 minXZWorld = new Vector2(camPosXZ.x - lowQualityWidth * 0.5f, camPosXZ.y - lowQualityWidth * 0.5f);
        Vector2 maxXZWorld = new Vector2(camPosXZ.x + lowQualityWidth * 0.5f, camPosXZ.y + lowQualityWidth * 0.5f);

        //foreach (E_TerrainQuality quality in Enum.GetValues(typeof(E_TerrainQuality)))
        //{
        //    Vector2 minXZKey = minXZWorld / (int)quality;
        //    Vector2 maxXZKey = maxXZWorld / (int)quality;
        //    Vector2Int minXZInt = new Vector2Int(Mathf.FloorToInt(minXZKey.x), Mathf.FloorToInt(minXZKey.y));
        //    Vector2Int maxXZInt = new Vector2Int(Mathf.FloorToInt(maxXZKey.x), Mathf.FloorToInt(maxXZKey.y));
        //    for (int y = minXZInt.y; y <= maxXZInt.y; y++)
        //    {
        //        for (int x = minXZInt.x; x <= maxXZInt.x; x++)
        //        {
        //            Vector2Int key = new Vector2Int(x, y);
        //            D_TerrainData[quality][key] = TerrainMaker.Ins.GetTerrainData(quality, key);
        //        }
        //    }

        //    if (quality == E_TerrainQuality.Ultra)
        //    {
        //        for (int y = minXZInt.y; y <= maxXZInt.y; y++)
        //        {
        //            for (int x = minXZInt.x; x <= maxXZInt.x; x++)
        //            {
        //                Vector2Int key = new Vector2Int(x, y);
        //                //D_Chunk[key] = CreateChunk(camPosXZ, key);
        //            }
        //        }
        //    }

        //    Debug.Log($"{quality} {minXZWorld} {maxXZWorld} {minXZKey} {maxXZKey} min: {minXZInt} max : {maxXZInt}");
        //}

        //E_TerrainQuality qual = E_TerrainQuality.Ultra;
        //Vector2Int ke = Vector2Int.zero;
        //D_TerrainData[qual][ke] = TerrainMaker.Ins.GetTerrainData(qual, ke);
        ////D_Chunk[ke] = CreateChunk(camPosXZ, ke);
        //foreach (Chunk value in D_Chunk.Values)
        //{
        //    Debug.Log($"청크 {value.m_Key}");
        //}
        //foreach (E_TerrainQuality quality in Enum.GetValues(typeof(E_TerrainQuality)))
        //{
        //    foreach (Vector2Int key in D_TerrainData[quality].Keys)
        //    {
        //        Debug.Log($"터레인 {quality} {key}");
        //    }
        //}
        CreateRenderTexture();
        Vector3 groundPos = new Vector3(camPosXZ.x, 0, camPosXZ.y);
        Ground.Create(groundPos, E_TerrainQuality.Ultra);
        //Ground.Create(groundPos, E_TerrainQuality.High);
        //Ground.Create(groundPos, E_TerrainQuality.Midium);
        //Ground.Create(groundPos, E_TerrainQuality.Low);


    }
    Vector2Int ChunkMoveCheck()
    {
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        Vector2Int centerGridIdxCoord = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / Chunk.ChunkSize.x), Mathf.FloorToInt(camPosXZ.y / Chunk.ChunkSize.y)); //중심이 되는 그리드 인덱스좌표
        if (m_CurCenterChunkIdxCoord != centerGridIdxCoord)
        {
            return centerGridIdxCoord - m_CurCenterChunkIdxCoord;
        }
        return Vector2Int.zero;
    }
    void ChunkMove(Vector2Int chunkMoveDisIdxCoord)
    {

    }
    Chunk CreateChunk(Vector2 curCamPos, Vector2Int key)
    {
        Chunk chunk = new Chunk();
        chunk.m_Key = key;
        GrassMaker.GrassMakerOption grassOption = new GrassMaker.GrassMakerOption();
        grassOption.GridCenterPos = (key + new Vector2(0.5f, 0.5f)) * Chunk.ChunkSize;
        grassOption.TerrainData = D_TerrainData[E_TerrainQuality.Ultra][key];
        chunk.m_GrassData = GrassMaker.GetChunkGrassData(grassOption);

        return chunk;
    }





}


//사각형 랜더범위만큼의 Ground 생성
//보이는곳만 Ground active on

//잔디맵 생성
//눈맵 생성
//바다 생성
//

