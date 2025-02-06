
using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GUILayout;

public class GrassMaker : MonoBehaviour
{

    [Serializable]
    public struct GrassMakerOption
    {
        public Vector2 GridPos;
        public int GrassCountPerOne;
        public Vector2 GridSize;
        public float GrassRenderDis;
        public float RandomPosMul;
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
        public ComputeBuffer DrawedIdxBuffer;
        public ComputeBuffer DrawedGrassBuffer;
        public Bounds FieldBound;
        public Material GrassMaterial;

        public void Release()
        {
            ArgsBuffer.Release();
            DrawedBuffer.Release();
            GrassBuffer.Release();
            DrawedPrefixSumBuffer.Release();
            DrawedGroupSumBuffer.Release();
            DrawedGroupPrefixSumBuffer.Release();
            DrawedIdxBuffer.Release();
            DrawedGrassBuffer.Release();
        }
    }

    struct GrassData
    {
        public Vector2 chunkUV;
        public Vector3 position;
    }

    const int GrassThreadWidth = 32;


    public static ChunkGrassData GetChunkGrassData(GrassMakerOption option)
    {
        Init();
        ChunkGrassData data = new ChunkGrassData(); ;
        int grassHorizonCount = Mathf.FloorToInt(option.GrassCountPerOne * option.GridSize.x);
        int grassVerticalCount = Mathf.FloorToInt(option.GrassCountPerOne * option.GridSize.y);
        int grassCount = grassHorizonCount * grassVerticalCount;

        int perlinKernel_x = grassHorizonCount / GrassThreadWidth + (grassHorizonCount % GrassThreadWidth == 0 ? 0 : 1);
        int perlinKernel_y = grassVerticalCount / GrassThreadWidth + (grassVerticalCount % GrassThreadWidth == 0 ? 0 : 1);
        int groupCount = perlinKernel_x * perlinKernel_y;
        int structSize = HMUtil.StructSize(typeof(GrassData));

        data.ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        data.ArgsBuffer.SetData(ArgsData);

        data.DrawedBuffer = new ComputeBuffer(grassCount, sizeof(int)); //그릴지말지 bool값
        data.GrassBuffer = new ComputeBuffer(grassCount, structSize); //컬링 전 grassbuffer
        data.DrawedPrefixSumBuffer = new ComputeBuffer(grassCount, sizeof(int)); //그룹별 누적합 버퍼
        data.DrawedGroupSumBuffer = new ComputeBuffer(groupCount, sizeof(int)); //그룹별 합을 배열로만든 버퍼
        data.DrawedGroupPrefixSumBuffer = new ComputeBuffer(groupCount, sizeof(int));//그룹별 합을 누적합 한 버퍼
        data.DrawedIdxBuffer = new ComputeBuffer(grassCount, sizeof(int)); //컬링 후 그려지는 idx _DrawedBuffer 의 인덱스임
        data.DrawedGrassBuffer = new ComputeBuffer(grassCount, structSize); //컬링 후 그려지는 grassbuffer 만 모아둔 버퍼

        ComputeShader CSGrassPosition = CSM.Ins.m_GrassPosition;
        ComputeShader CSFrustumCulling = CSM.Ins.m_GrassFrustumCulling;

        CSGrassPosition.SetBuffer(0, "_HeightBuffer", option.HeightBuffer);
        CSGrassPosition.SetBuffer(0, "_GrassBuffer", data.GrassBuffer);
        CSGrassPosition.SetInts("_HeightBufferSize", new int[2] { option.HeightBufferSize.x, option.HeightBufferSize.y });
        CSGrassPosition.SetInt("_GrassHorizonCount", grassHorizonCount);
        CSGrassPosition.SetInt("_GrassVerticalCount", grassVerticalCount);
        CSGrassPosition.SetFloats("_GridPos", new float[2] { option.GridPos.x, option.GridPos.y });
        CSGrassPosition.SetFloats("_GridSize", new float[2] { option.GridSize.x, option.GridSize.y });
        CSGrassPosition.SetFloat("_RandomPosMul", option.RandomPosMul);
        CSGrassPosition.Dispatch(0, perlinKernel_x, perlinKernel_y, 1);

        data.GrassMaterial = new Material(Prefabs.Ins.M_Grass);
        data.GrassMaterial.SetBuffer("_GrassBuffer", data.GrassBuffer);
        //m_CSFrustumCulling.SetInt("_GroupCount", m_GroupX);

        //m_CSFrustumCulling.SetBuffer(0, "_GrassBuffer", m_GrassBuffer);
        //m_CSFrustumCulling.SetBuffer(0, "_DrawedBuffer", m_DrawedBuffer);
        //m_CSFrustumCulling.SetBuffer(1, "_DrawedBuffer", m_DrawedBuffer);
        //m_CSFrustumCulling.SetBuffer(1, "_DrawedPrefixSumBuffer", m_DrawedPrefixSumBuffer);
        //m_CSFrustumCulling.SetBuffer(1, "_DrawedGroupSumBuffer", m_DrawedGroupSumBuffer);
        //m_CSFrustumCulling.SetBuffer(2, "_DrawedGroupPrefixSumBuffer", m_DrawedGroupPrefixSumBuffer);
        //m_CSFrustumCulling.SetBuffer(2, "_DrawedGroupSumBuffer", m_DrawedGroupSumBuffer);
        //m_CSFrustumCulling.SetBuffer(2, "_MeshArgsBuffer", m_ArgsBuffer);
        //m_CSFrustumCulling.SetBuffer(3, "_DrawedIdxBuffer", m_DrawedIdxBuffer);
        //m_CSFrustumCulling.SetBuffer(3, "_DrawedGroupPrefixSumBuffer", m_DrawedGroupPrefixSumBuffer);
        //m_CSFrustumCulling.SetBuffer(3, "_DrawedPrefixSumBuffer", m_DrawedPrefixSumBuffer);
        //m_CSFrustumCulling.SetBuffer(3, "_GrassBuffer", m_GrassBuffer);
        //m_CSFrustumCulling.SetBuffer(3, "_DrawedGrassBuffer", m_DrawedGrassBuffer);


        Vector3 boundCenter = new Vector3(option.GridPos.x, 0, option.GridPos.y) + new Vector3(option.GridSize.x, 0, option.GridSize.y) * 0.5f;
        Vector3 boundSize = new Vector3(option.GridSize.x, 20, option.GridSize.y);
        data.FieldBound = new Bounds(boundCenter, boundSize);

        return data;
    }
    private static uint[] ArgsData = new uint[5];
    static Mesh GrassMesh;

    static bool isInit = false;
    static void Init()
    {
        if(isInit)
        {
            return;
        }
        isInit = true;
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[4] {new Vector3(-0.5f, 1f, 0)
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
        GrassMesh = mesh;
        ArgsData[0] = (uint)GrassMesh.GetIndexCount(0);
        ArgsData[1] = 500; //잔디 갯수인데 computeshader 에서 구해서 넣음
        ArgsData[2] = (uint)GrassMesh.GetIndexStart(0);
        ArgsData[3] = (uint)GrassMesh.GetBaseVertex(0);
        ArgsData[4] = 0;
    }

    public static void DrawGrass(ChunkGrassData data)
    {
        Graphics.DrawMeshInstancedIndirect(GrassMesh, 0, data.GrassMaterial, data.FieldBound, data.ArgsBuffer);

        //int[] temp = new int[5];

        //Debug.Log()
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
