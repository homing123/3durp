using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GrassMaker;
using static TerrainMaker;
using System;

public struct ChunkData
{
    public Vector2Int key; //min 값임
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
    public const FilterMode HeightFilter = FilterMode.Point;
    public const FilterMode NormalFilter = FilterMode.Point;
    public static MapMaker Ins;

    public const int ChunkSize = 16;
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

    List<Ground> L_Ground = new List<Ground>();
    List<RenderTexture> L_GroundHeightMap = new List<RenderTexture>();
    List<RenderTexture> L_GroundNormalMap = new List<RenderTexture>();
    public Vector2 GroundHeightMapTexWorldSize
    {
        get
        {
            float texWorldSize = TerrainMaker.Ins.m_MeshSize + (float)TerrainMaker.Ins.m_MeshSize / TerrainMaker.Ins.m_VertexWidth;
            return new Vector2(texWorldSize, texWorldSize * 2);
        }
    }
    public Vector2 GroundHeightMapCenterPos;
    private void Awake()
    {
        Ins = this;
    }
    private void Start()
    {
        MapInit();
        Shader.SetGlobalVector("_TexWorldSize", GroundHeightMapTexWorldSize);
        CamMove.ev_TerrainPosUpdate += Ev_CamMove;
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
        CamMove.ev_TerrainPosUpdate -= Ev_CamMove;

        for (int i = 0; i < TerrainCount; i++)
        {
            L_GroundHeightMap[i].Release();
            L_GroundNormalMap[i].Release();
        }
        L_GroundHeightMap.Clear();
        L_GroundNormalMap.Clear();

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
    void Ev_CamMove(Vector2 pos)
    {
        GroundHeightMapCenterPos = pos;
        for (int i = 1; i <= TerrainCount; i++)
        {
            RenderTexture heightMap = L_GroundHeightMap[i - 1];
            RenderTexture normalMap = L_GroundNormalMap[i - 1];
            SetHeightNormalMap(i, ref heightMap, ref normalMap, GroundHeightMapCenterPos);
        }
        Shader.SetGlobalVector("_TexCenterPosXZ", pos);       
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
        grassOption.chunkCenterPos = (key + new Vector2(0.5f, 0.5f)) * ChunkSize;
        chunk.grassData = GrassMaker.Ins.GetChunkGrassData(grassOption);
        D_ChunkData[key] = chunk;
    }

    void MapInit()
    {
        Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();
        Vector2Int curTerrainMeshGridKey = TerrainMaker.Ins.GetTerrainMeshGridKey(CamMove.Ins.transform.position.Vt2XZ());
        GroundHeightMapCenterPos.x = curTerrainMeshGridKey.x * TerrainMaker.Ins.TerrainMeshGridSize;
        GroundHeightMapCenterPos.y = curTerrainMeshGridKey.y * TerrainMaker.Ins.TerrainMeshGridSize;
        //create terrain
        for (int i = 1; i <= TerrainCount; i++)
        {
            D_TerrainData[i] = new Dictionary<Vector2Int, TerrainData>();
            
            RenderTexture heightMap = new RenderTexture(TerrainMaker.Ins.m_VertexWidth, TerrainMaker.Ins.m_VertexWidth, 0, RenderTextureFormat.RFloat);
            RenderTexture normalMap = new RenderTexture(TerrainMaker.Ins.m_VertexWidth, TerrainMaker.Ins.m_VertexWidth, 0, RenderTextureFormat.ARGBFloat);
            heightMap.enableRandomWrite = true;
            normalMap.enableRandomWrite = true;
            heightMap.filterMode = FilterMode.Bilinear;
            normalMap.filterMode = FilterMode.Bilinear;
            SetHeightNormalMap(i, ref heightMap, ref normalMap, GroundHeightMapCenterPos);

            L_GroundHeightMap.Add(heightMap);
            L_GroundNormalMap.Add(normalMap);

            Shader.SetGlobalTexture("_HeightMap_" + i, heightMap);
            Shader.SetGlobalTexture("_NormalMap_" + i, normalMap);

            Ground ground = Ground.Create(curTerrainMeshGridKey, i);
            L_Ground.Add(ground);
        }

        //create chunk data
        m_CurChunkKey = new Vector2Int(Mathf.FloorToInt(camPosXZ.x / ChunkSize), Mathf.FloorToInt(camPosXZ.y / ChunkSize)); //현재 카메라위치가 속한 청크의 키
        int chunkWidth = m_RenderChunkDis * 2 + 1;
        Vector2Int chunkKeyMin = m_CurChunkKey - new Vector2Int(m_RenderChunkDis, m_RenderChunkDis);
        for (int y = 0; y < chunkWidth; y++)
        {
            for (int x = 0; x < chunkWidth; x++)
            {
                Vector2Int curChunkKey = new Vector2Int(x, y) + chunkKeyMin;
                CreateChunkData(curChunkKey);
            }
        }
    }
    void SetHeightNormalMap(int quality, ref RenderTexture heightMap, ref RenderTexture normalMap, Vector2 curPos)
    {
        int groundSize = TerrainMaker.Ins.m_MeshSize * (int)quality;
        int mergeTexWidth = TerrainMaker.Ins.m_VertexWidth;
        int dataTexWidth = TerrainMaker.Ins.m_TexWidth;
        Vector2 curSize = new Vector2(groundSize, groundSize);

        Vector2 minWorld = curPos - curSize * 0.5f; //메쉬의 min, max 월드좌표
        Vector2 maxWorld = curPos + curSize * 0.5f;
        Vector2 minGrid = minWorld / curSize; //메쉬 min, max 월드좌표를 그리드 좌표로 변환
        Vector2 maxGrid = maxWorld / curSize;
        Vector2Int minKey = new Vector2Int(Mathf.FloorToInt(minGrid.x), Mathf.FloorToInt(minGrid.y)); //그리드 좌표를 키값좌표로 변환 
        Vector2Int maxKey = new Vector2Int(Mathf.FloorToInt(maxGrid.x), Mathf.FloorToInt(maxGrid.y));
        Vector2Int dataSize = new Vector2Int(maxKey.x - minKey.x + 1, maxKey.y - minKey.y + 1);
        TerrainData[] arr_Data = new TerrainData[dataSize.x * dataSize.y];
        //Debug.Log($"데이터 사이즈 : {dataSize}");

        int idx = 0;
        for (int y = minKey.y; y <= maxKey.y; y++)
        {
            for (int x = minKey.x; x <= maxKey.x; x++)
            {
                Vector2Int curKey = new Vector2Int(x, y);
                arr_Data[idx] = GetTerrainData(quality, curKey);
                m_CSTextureMerge.SetTexture(0, "_HeightMap" + idx, arr_Data[idx].heightTexture);
                m_CSTextureMerge.SetTexture(0, "_NormalMap" + idx, arr_Data[idx].normalTexture);
                idx++;
            }
        }
        Vector2 heightMapMinWorld = minKey * groundSize; //arr_data안의 높이맵의 범위 min
        Vector2 heightMapMaxWorld = (maxKey + new Vector2Int(1, 1)) * groundSize; //arr_data안의 높이맵의 범위 max
        float duv = 1 / (float)dataTexWidth;
        Vector2 uvMin = (minWorld - heightMapMinWorld) / (groundSize / (float)dataTexWidth) * duv + duv * new Vector2(0.5f, 0.5f);
        Vector2 uvMax = (maxWorld - heightMapMinWorld) / (groundSize / (float)dataTexWidth) * duv + duv * new Vector2(0.5f, 0.5f);
        //Debug.Log($"{heightMapMinWorld} {heightMapMaxWorld} {minWorld} {maxWorld} {minGrid} {maxGrid} {minKey} {maxKey} {uvMin} {uvMax} {duv} {m_Quality}");

        m_CSTextureMerge.SetInts("_MergeTexSize", new int[2] { mergeTexWidth, mergeTexWidth });
        m_CSTextureMerge.SetInts("_DataTexSize", new int[2] { dataTexWidth, dataTexWidth });
        m_CSTextureMerge.SetInts("_DataTexCount", new int[2] { dataSize.x, dataSize.y });
        m_CSTextureMerge.SetFloats("_UVMin", new float[2] { uvMin.x, uvMin.y });
        m_CSTextureMerge.SetFloats("_UVMax", new float[2] { uvMax.x, uvMax.y });
        m_CSTextureMerge.SetTexture(0, "_HeightMergeMap", heightMap);
        m_CSTextureMerge.SetTexture(0, "_NormalMergeMap", normalMap);

        int groupx = mergeTexWidth / Thread_Width + (mergeTexWidth % Thread_Width == 0 ? 0 : 1);
        m_CSTextureMerge.Dispatch(0, groupx, groupx, 1);
    }

    ChunkData[] GetDrawGrassChunks()
    {
        List<ChunkData> l_GrassRenderChunk = new List<ChunkData>();
        foreach (Vector2Int key in D_ChunkData.Keys)
        {
            Vector2 rectMin = key * ChunkSize;
            Rect rect = new Rect(rectMin.x, rectMin.y, ChunkSize, ChunkSize);
            Vector2[] vertex = new Vector2[4];
            
            //xz평면 4꼭지점
            vertex[0] = new Vector2(rect.xMin, rect.yMin);
            vertex[1] = new Vector2(rect.xMin, rect.yMax);
            vertex[2] = new Vector2(rect.xMax, rect.yMin);
            vertex[3] = new Vector2(rect.xMax, rect.yMax);

            Vector2 camPosXZ = Camera.main.transform.position.Vt2XZ();

            float grassRenderDisSquare = GrassMaker.Ins.m_GrassRenderDis * GrassMaker.Ins.m_GrassRenderDis;
            bool isDrawed = false;
            for (int i = 0; i < 4; i++)
            {
                float curVertexDisSquare = Mathf.Pow(vertex[i].x - camPosXZ.x, 2) + Mathf.Pow(vertex[i].y - camPosXZ.y, 2);
                //4꼭지점중 하나라도 잔디렌더거리안에 들어온다면  그림
                if (curVertexDisSquare < grassRenderDisSquare)
                {
                    isDrawed = true;
                    break;
                }
            }

            if (isDrawed)
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

    public void SetGroundHeightTexture(ComputeShader cs, int bufferIdx, string bufferName, int quality)
    {
        cs.SetTexture(bufferIdx, bufferName, L_GroundHeightMap[quality - 1]);
    }
    public void SetGroundNormalTexture(ComputeShader cs, int bufferIdx, string bufferName, int quality)
    {
        cs.SetTexture(bufferIdx, bufferName, L_GroundNormalMap[quality - 1]);
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

        int qualitySize = 1 << (m_RenterTextureQuality - 1);
        float size = (float)TerrainMaker.Ins.m_MeshSize * qualitySize;
        float dVertex = (float)TerrainMaker.Ins.m_MeshSize / TerrainMaker.Ins.m_TexWidth * qualitySize;

        foreach (Vector2Int key in D_TerrainData[m_RenterTextureQuality].Keys)
        {
            Vector2 pos = new Vector2(key.x + 0.5f, key.y + 0.5f) * size - new Vector2(dVertex, dVertex) * 0.5f;
            Debug.Log(key + " " + pos);
            L_Objs.Add(RenderTextureObject.Create(pos, size, D_TerrainData[m_RenterTextureQuality][key].heightTexture));
        }

    }
    [ContextMenu("TerrainNormal")]
    public void CreateTerrainNormalTexture()
    {
        for (int i = 0; i < L_Objs.Count; i++)
        {
            Destroy(L_Objs[i].gameObject);
            L_Objs.RemoveAt(i);
            i--;
        }

        int qualitySize = 1 << (m_RenterTextureQuality - 1);
        float size = (float)TerrainMaker.Ins.m_MeshSize * qualitySize;
        float dVertex = (float)TerrainMaker.Ins.m_MeshSize / TerrainMaker.Ins.m_TexWidth * qualitySize;

        foreach (Vector2Int key in D_TerrainData[m_RenterTextureQuality].Keys)
        {
            Vector2 pos = new Vector2(key.x + 0.5f, key.y + 0.5f) * size - new Vector2(dVertex, dVertex) * 0.5f; ;

            L_Objs.Add(RenderTextureObject.Create(pos, size, D_TerrainData[m_RenterTextureQuality][key].normalTexture));
        }

    }


}


//사각형 랜더범위만큼의 Ground 생성
//보이는곳만 Ground active on

//잔디맵 생성
//눈맵 생성
//바다 생성
//

