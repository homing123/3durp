
using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GUILayout;

public class GrassMaker : MonoBehaviour
{

    public enum E_GrassFrustumCullingKernel
    {
        GrassDataCombine = 0,
        FrustumCulling = 1,
        PrefixSum = 2,
        GroupPrefixSum = 3,
        SetDrawedGrass = 4,
    }

    [Serializable]
    public struct GrassMakerOption
    {
        public Vector2 GridPos;
        public int GrassCountPerOne;
        public Vector2 GridSize;
        public float GrassRenderDis;
        public Vector2Int HeightBufferSize;
        public ComputeBuffer HeightBuffer;
        public ComputeBuffer NormalBuffer;
    }

    public struct ChunkGrassData
    {
        public ComputeBuffer ArgsBuffer;
        public ComputeBuffer DrawedBuffer;
        public ComputeBuffer GrassBuffer;
        public ComputeBuffer DrawedPrefixSumBuffer;
        public ComputeBuffer DrawedGroupSumBuffer;
        public ComputeBuffer DrawedGroupPrefixSumBuffer;
        public ComputeBuffer DrawedGrassBuffer;
        public Bounds FieldBound;
        public Material GrassMaterial;
        public GrassMakerOption Option;
        public int GroupXCount;
        public int GrassCount;

        public void Release()
        {
            ArgsBuffer.Release();
            DrawedBuffer.Release();
            GrassBuffer.Release();
            DrawedPrefixSumBuffer.Release();
            DrawedGroupSumBuffer.Release();
            DrawedGroupPrefixSumBuffer.Release();
            DrawedGrassBuffer.Release();
        }
    }

    struct GrassData
    {
        public Vector2 chunkUV;
        public Vector3 position;
    }

    private uint[] m_ArgsData = new uint[5];
    Mesh m_GrassMesh;    
    ComputeBuffer m_TotalChunkGrassBuffer;

    int m_LastTotalChunkGrassCount = 0;
    [SerializeField][Range(1, 64)] public int m_GrassCountPerOne;
    [SerializeField][Range(0.1f, 100)] public float m_GrassRenderDis;

    public static GrassMaker Ins;
    const int GrassThreadWidth = 32;
    const int CullingThreadMax = 512;
    const int CullingGroupXMax = 512;
    private void Awake()
    {
        Ins = this;
        Init();
    }
    private void OnDestroy()
    {
        if (m_LastTotalChunkGrassCount > 0)
        {
            m_TotalChunkGrassBuffer.Release();
        }
    }
    void Init()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[4]
        {new Vector3(-0.5f, 1f, 0)
        , new Vector3 (0.5f, 1f, 0)
        , new Vector3 (-0.5f, 0f, 0)
        , new Vector3 (0.5f, 0f, 0) };
        mesh.SetIndices(new int[6] { 0, 1, 2, 1, 3, 2 }, MeshTopology.Triangles, 0);
        mesh.uv = new Vector2[4] {
            new Vector2 (0,1), new Vector2 ( 1,1),new Vector2 (0,0) ,new Vector2 (1,0) };
        mesh.normals = new Vector3[4]
        {
            new Vector3(0,0,-1), new Vector3(0,0,-1), new Vector3(0,0,-1), new Vector3(0,0,-1)
        };
        mesh.name = "GrassMesh";
        m_GrassMesh = mesh;
        m_ArgsData[0] = (uint)m_GrassMesh.GetIndexCount(0);
        m_ArgsData[1] = 0; //잔디 갯수인데 computeshader 에서 구해서 넣음
        m_ArgsData[2] = (uint)m_GrassMesh.GetIndexStart(0);
        m_ArgsData[3] = (uint)m_GrassMesh.GetBaseVertex(0);
        m_ArgsData[4] = 0;
    }

    public static void DrawGrass(MapMaker.Chunk[] arr_Chunk)
    {
        int curTotalGrassCount = 0;
        int[] grassPrefixSum = new int[arr_Chunk.Length];
        for (int i = 0; i < arr_Chunk.Length; i++)
        {
            grassPrefixSum[i] = curTotalGrassCount;
            curTotalGrassCount += arr_Chunk[i].m_GrassData.GrassCount;
        }

        if (Ins.m_LastTotalChunkGrassCount < curTotalGrassCount)
        {
            if (Ins.m_LastTotalChunkGrassCount > 0)
            {
                Ins.m_TotalChunkGrassBuffer.Release();
            }
            Ins.m_TotalChunkGrassBuffer = new ComputeBuffer(curTotalGrassCount, HMUtil.StructSize(typeof(GrassData)));
        }

        //nvidia에서 32개단위로 워프가 진행되고 해당 워프갯수에 맞춰서 패딩을 넣어주면 된다.
        //이렇게 했을때 더 빠르다는걸 실험으로 증명 한 후 ppt에 써보자
        ComputeShader CSFrustumCulling = CSM.Ins.m_GrassFrustumCulling;
        for (int i = 0; i < arr_Chunk.Length; i++)
        {
            //Ins.m_TotalChunkGrassBuffer.SetData()
;            //CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GrassDataCombine, "_GrassBuffers", arr_Chunk[i].m_GrassData.GrassBuffer, 1);
        }
    }
    public static void DrawGrass(ChunkGrassData data)
    {
        ComputeShader CSFrustumCulling = CSM.Ins.m_GrassFrustumCulling;

        Matrix4x4 p = Camera.main.projectionMatrix;
        Matrix4x4 v = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = p * v;

        CSFrustumCulling.SetVector("_CamPos", Camera.main.transform.position);
        CSFrustumCulling.SetFloat("_RenderDis", data.Option.GrassRenderDis);
        CSFrustumCulling.SetMatrix("_MatVP", VP);

        CSFrustumCulling.SetInt("_GrassCount", data.GrassCount);
        CSFrustumCulling.SetInt("_GroupCount", data.GroupXCount);
        CSFrustumCulling.SetBuffer(0, "_GrassBuffer", data.GrassBuffer);
        CSFrustumCulling.SetBuffer(0, "_DrawedBuffer", data.DrawedBuffer);
        CSFrustumCulling.SetBuffer(1, "_DrawedBuffer", data.DrawedBuffer);
        CSFrustumCulling.SetBuffer(1, "_DrawedPrefixSumBuffer", data.DrawedPrefixSumBuffer);
        CSFrustumCulling.SetBuffer(1, "_DrawedGroupSumBuffer", data.DrawedGroupSumBuffer);
        CSFrustumCulling.SetBuffer(2, "_DrawedGroupPrefixSumBuffer", data.DrawedGroupPrefixSumBuffer);
        CSFrustumCulling.SetBuffer(2, "_DrawedGroupSumBuffer", data.DrawedGroupSumBuffer);
        CSFrustumCulling.SetBuffer(2, "_MeshArgsBuffer", data.ArgsBuffer);
        CSFrustumCulling.SetBuffer(3, "_DrawedGroupPrefixSumBuffer", data.DrawedGroupPrefixSumBuffer);
        CSFrustumCulling.SetBuffer(3, "_DrawedPrefixSumBuffer", data.DrawedPrefixSumBuffer);
        CSFrustumCulling.SetBuffer(3, "_GrassBuffer", data.GrassBuffer);
        CSFrustumCulling.SetBuffer(3, "_DrawedGrassBuffer", data.DrawedGrassBuffer);

        //Cull
        CSFrustumCulling.Dispatch(0, data.GroupXCount, 1, 1);

        //PrefixSum
        CSFrustumCulling.Dispatch(1, data.GroupXCount, 1, 1);

        //GroupPrefixSum
        CSFrustumCulling.Dispatch(2, 1, 1, 1);

        //GetDrawedIdx
        CSFrustumCulling.Dispatch(3, data.GroupXCount, 1, 1);

        Graphics.DrawMeshInstancedIndirect(Ins.m_GrassMesh, 0, data.GrassMaterial, data.FieldBound, data.ArgsBuffer);
    }


    public static ChunkGrassData GetChunkGrassData(GrassMakerOption option)
    {
        ComputeShader CSGrassPosition = CSM.Ins.m_GrassPosition;
        ComputeShader CSFrustumCulling = CSM.Ins.m_GrassFrustumCulling;

        ChunkGrassData data = new ChunkGrassData();
        data.Option = option;
        int grassHorizonCount = Mathf.FloorToInt(option.GrassCountPerOne * option.GridSize.x);
        int grassVerticalCount = Mathf.FloorToInt(option.GrassCountPerOne * option.GridSize.y);
        int grassCount = grassHorizonCount * grassVerticalCount;

        int perlinKernel_x = grassHorizonCount / GrassThreadWidth + (grassHorizonCount % GrassThreadWidth == 0 ? 0 : 1);
        int perlinKernel_y = grassVerticalCount / GrassThreadWidth + (grassVerticalCount % GrassThreadWidth == 0 ? 0 : 1);
        int structSize = HMUtil.StructSize(typeof(GrassData));



        data.GrassBuffer = new ComputeBuffer(grassCount, structSize); //컬링 전 grassbuffer

        CSGrassPosition.SetBuffer(0, "_HeightBuffer", option.HeightBuffer);
        CSGrassPosition.SetBuffer(0, "_GrassBuffer", data.GrassBuffer);
        CSGrassPosition.SetInts("_HeightBufferSize", new int[2] { option.HeightBufferSize.x, option.HeightBufferSize.y });
        CSGrassPosition.SetInt("_GrassHorizonCount", grassHorizonCount);
        CSGrassPosition.SetInt("_GrassVerticalCount", grassVerticalCount);
        CSGrassPosition.SetFloats("_GridPos", new float[2] { option.GridPos.x, option.GridPos.y });
        CSGrassPosition.SetFloats("_GridSize", new float[2] { option.GridSize.x, option.GridSize.y });
        CSGrassPosition.Dispatch(0, perlinKernel_x, perlinKernel_y, 1);

        int groupCount = grassCount / CullingThreadMax + (grassCount % CullingThreadMax == 0 ? 0 : 1);
        if(groupCount > 512)
        {
            Debug.Log($"Group too large {groupCount}");
        }
        data.GroupXCount = groupCount;
        data.GrassCount = grassCount;

        data.DrawedBuffer = new ComputeBuffer(grassCount, sizeof(int)); //그릴지말지 bool값
        data.DrawedPrefixSumBuffer = new ComputeBuffer(grassCount, sizeof(int)); //그룹별 누적합 버퍼
        data.DrawedGroupSumBuffer = new ComputeBuffer(groupCount, sizeof(int)); //그룹별 합을 배열로만든 버퍼
        data.DrawedGroupPrefixSumBuffer = new ComputeBuffer(groupCount, sizeof(int));//그룹별 합을 누적합 한 버퍼
        data.DrawedGrassBuffer = new ComputeBuffer(grassCount, structSize); //컬링 후 그려지는 grassbuffer 만 모아둔 버퍼
        data.ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        data.ArgsBuffer.SetData(GrassMaker.Ins.m_ArgsData);

        Vector3 boundCenter = new Vector3(option.GridPos.x, 0, option.GridPos.y) + new Vector3(option.GridSize.x, 0, option.GridSize.y) * 0.5f;
        Vector3 boundSize = new Vector3(option.GridSize.x, 20, option.GridSize.y);
        data.FieldBound = new Bounds(boundCenter, boundSize);

        data.GrassMaterial = new Material(Prefabs.Ins.M_Grass);
        data.GrassMaterial.SetBuffer("_GrassBuffer", data.DrawedGrassBuffer);

        return data;
    }



    //void FrustumCull()
    //{
    //    Matrix4x4 p = Camera.main.projectionMatrix;
    //    Matrix4x4 v = Camera.main.transform.worldToLocalMatrix;
    //    Matrix4x4 VP = p * v;

        //    m_CSFrustumCulling.SetInt("_GrassCount", m_GrassCount);
        //    m_CSFrustumCulling.SetVector("_CamPos", Camera.main.transform.position);
        //    m_CSFrustumCulling.SetFloat("_RenderDis", m_RenderDis);
        //    m_CSFrustumCulling.SetMatrix("_MatVP", VP);

        //    //Cull
        //    m_CSFrustumCulling.Dispatch(0, m_GroupX, 1, 1);

        //    //PrefixSum
        //    m_CSFrustumCulling.Dispatch(1, m_GroupX, 1, 1);

        //    //GroupPrefixSum
        //    m_CSFrustumCulling.Dispatch(2, 1, 1, 1);

        //    //GetDrawedIdx
        //    m_CSFrustumCulling.Dispatch(3, m_GroupX, 1, 1);
        //}




}
