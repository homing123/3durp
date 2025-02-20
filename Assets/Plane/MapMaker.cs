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
    public RenderTexture heightTexture;
    public RenderTexture normalTexture;
    public Chunk_GrassData grassData;
    public void Release()
    {
        heightTexture.Release();
        normalTexture.Release();
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
    [SerializeField] [Range(16, 512)] int m_ChunkTextureWIdth;
    [SerializeField] int m_RenterTextureQuality;
    [SerializeField] RenderTextureObject m_RenderTextureObj;
    [SerializeField][Range(1, 7)] int m_RenderChunkDis;
    [SerializeField] ComputeShader m_CSTextureMerge;
    public const int TerrainCount = 2;
    const int Thread_Width = 32;

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
        //GrassMaker.Ins.DrawGrass(GetDrawGrassChunks());

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
    public ChunkData GetChunkData(Vector2Int key)
    {
        if (D_ChunkData.ContainsKey(key) == false)
        {
            CreateChunkData(key);
        }
      
        return D_ChunkData[key];
    }
    public void CreateChunkData(Vector2Int key)
    {
        ChunkData chunk = new ChunkData();
        chunk.key = key;
        GrassMakerOption grassOption = new GrassMakerOption();
        chunk.heightTexture = new RenderTexture(m_ChunkTextureWIdth, m_ChunkTextureWIdth, 0, RenderTextureFormat.RFloat);
        chunk.normalTexture = new RenderTexture(m_ChunkTextureWIdth, m_ChunkTextureWIdth, 0, RenderTextureFormat.ARGBFloat);
        chunk.heightTexture.enableRandomWrite = true;
        chunk.normalTexture.enableRandomWrite = true;
        chunk.heightTexture.filterMode = FilterMode.Point;
        chunk.normalTexture.filterMode = FilterMode.Bilinear;
        chunk.heightTexture.wrapMode = TextureWrapMode.Mirror;
        chunk.normalTexture.wrapMode = TextureWrapMode.Mirror;
        MergeHeightNormalTexture(key, ref chunk.heightTexture, ref chunk.normalTexture);

        grassOption.chunkCenterPos = (key + new Vector2(0.5f, 0.5f)) * ChunkSize;
        grassOption.heightTexture = chunk.heightTexture;
        chunk.grassData = GrassMaker.Ins.GetChunkGrassData(grassOption);
        D_ChunkData[key] = chunk;
    }
    void MergeHeightNormalTexture(Vector2Int chunkKey,ref RenderTexture heightMergeTexture, ref RenderTexture normalMergeTexture)
    {
        int texWidth = m_ChunkTextureWIdth;
        Vector2 curSize = new Vector2(ChunkSize, ChunkSize);
        Vector2 curCenterPosXZ = chunkKey * ChunkSize + curSize * 0.5f;

        Vector2 minWorld = curCenterPosXZ - curSize * 0.5f;
        Vector2 maxWorld = curCenterPosXZ + curSize * 0.5f;
        Vector2 minTerrainGrid = minWorld / TerrainMaker.Ins.m_MeshSize;
        Vector2 maxTerrainGrid = maxWorld / TerrainMaker.Ins.m_MeshSize;
        Vector2 minChunkGrid = minWorld / ChunkSize;
        Vector2 maxChunkGrid = maxWorld / ChunkSize;
        Vector2Int minTerrainKey = new Vector2Int(Mathf.FloorToInt(minTerrainGrid.x), Mathf.FloorToInt(minTerrainGrid.y));
        Vector2Int maxTerrainKey = new Vector2Int(Mathf.FloorToInt(maxTerrainGrid.x), Mathf.FloorToInt(maxTerrainGrid.y)); 
        Vector2Int minChunkKey = new Vector2Int(Mathf.FloorToInt(minChunkGrid.x), Mathf.FloorToInt(minChunkGrid.y));
        Vector2Int maxChunkKey = new Vector2Int(Mathf.FloorToInt(maxChunkGrid.x), Mathf.FloorToInt(maxChunkGrid.y));
        Vector2Int dataSize = new Vector2Int(maxTerrainKey.x - minTerrainKey.x + 1, maxTerrainKey.y - minTerrainKey.y + 1);
        TerrainData[] arr_Data = new TerrainData[dataSize.x * dataSize.y];
        //Debug.Log($"chunk mergetextue {minWorld} {maxWorld} {minGrid} {maxGrid} {minKey} {maxKey}");

        int idx = 0;
        for (int y = minTerrainKey.y; y <= maxTerrainKey.y; y++)
        {
            for (int x = minTerrainKey.x; x <= maxTerrainKey.x; x++)
            {
                Vector2Int curKey = new Vector2Int(x, y);
                arr_Data[idx] = MapMaker.Ins.GetTerrainData(1, curKey);
                m_CSTextureMerge.SetTexture(0, "_HeightMap" + idx, arr_Data[idx].heightTexture);
                m_CSTextureMerge.SetTexture(0, "_NormalMap" + idx, arr_Data[idx].normalTexture);
                idx++;

            }
        }
        Vector2 heightMapMinWorld = minTerrainKey * TerrainMaker.Ins.m_MeshSize;
        Vector2 heightMapMaxWorld = (maxTerrainKey + new Vector2Int(1, 1)) * TerrainMaker.Ins.m_MeshSize;
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
        m_CSTextureMerge.SetTexture(0, "_HeightMergeMap", heightMergeTexture);
        m_CSTextureMerge.SetTexture(0, "_NormalMergeMap", normalMergeTexture);

        int groupx = texWidth / Thread_Width + (texWidth % Thread_Width == 0 ? 0 : 1);
        m_CSTextureMerge.Dispatch(0, groupx, groupx, 1);
    }
    void MapInit()
    {
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        Vector2Int curTerrainMeshGridKey = TerrainMaker.Ins.GetTerrainMeshGridKey(CamMove.Ins.transform.position.Vt2XZ());

        Vector3 groundPos = new Vector3(curTerrainMeshGridKey.x, 0, curTerrainMeshGridKey.y) * TerrainMaker.Ins.TerrainMeshGridSize;
        //create terrain
        for (int i = 1; i <= TerrainCount; i++)
        {
            D_TerrainData[i] = new Dictionary<Vector2Int, TerrainData>();
            Ground.Create(groundPos, i);
        }

        //create chunk data
        //m_CurChunkKey = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / ChunkSize), Mathf.FloorToInt(camPosXZ.y / ChunkSize)); //현재 카메라위치가 속한 청크의 키
        //int chunkWidth = m_RenderChunkDis * 2 + 1;
        //Vector2Int chunkKeyMin = m_CurChunkKey - new Vector2Int(m_RenderChunkDis, m_RenderChunkDis);
        //for (int y = 0; y < chunkWidth; y++)
        //{
        //    for (int x = 0; x < chunkWidth; x++)
        //    {
        //        Vector2Int curChunkKey = new Vector2Int(x, y) + chunkKeyMin;
        //        CreateChunkData(curChunkKey);
        //    }
        //}

        //CreateChunkData(m_CurChunkKey);

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
   




    [ContextMenu("ChunkHeight")]
    public void CreateChunkHeightTexture()
    {
        for (int i = 0; i < L_Objs.Count; i++)
        {
            Destroy(L_Objs[i].gameObject);
            L_Objs.RemoveAt(i);
            i--;
        }

        foreach (Vector2Int key in D_ChunkData.Keys)
        {
            L_Objs.Add(RenderTextureObject.Create(key, ChunkSize, D_ChunkData[key].heightTexture));
        }


    }
    [ContextMenu("TerrainHeight")]
    public void CreateTerrainHeightTexture()
    {
        for (int i = 0; i < L_Objs.Count; i++)
        {
            Destroy(L_Objs[i].gameObject);
            L_Objs.RemoveAt(i);
            i--;
        }

        foreach (Vector2Int key in D_TerrainData[m_RenterTextureQuality].Keys)
        {
            L_Objs.Add(RenderTextureObject.Create(key, m_RenterTextureQuality * TerrainMaker.Ins.m_MeshSize, D_TerrainData[m_RenterTextureQuality][key].heightTexture));
        }

    }


}


//사각형 랜더범위만큼의 Ground 생성
//보이는곳만 Ground active on

//잔디맵 생성
//눈맵 생성
//바다 생성
//

