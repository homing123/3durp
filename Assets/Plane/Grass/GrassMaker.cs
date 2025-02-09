
using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GUILayout;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;

public class GrassMaker : MonoBehaviour
{

    public enum E_GrassFrustumCullingKernel
    {
        CombineGrassBuffer = 0,
        FrustumCulling = 1,
        PrefixSum = 2,
        GroupPrefixSum = 3,
        SetDrawedGrass = 4,
    }

    [Serializable]
    public struct GrassMakerOption
    {
        public Vector2 GridPos;
        public Vector2Int HeightBufferSize;
        public ComputeBuffer HeightBuffer;
        public ComputeBuffer NormalBuffer;
    }

    public struct ChunkGrassData
    {
      
        public ComputeBuffer GrassBuffer;
        public GrassMakerOption Option;
        public int GrassCount;

        public void Release()
        {
            GrassBuffer.Release();
        }
    }

    struct GrassData
    {
        public Vector2 chunkUV;
        public Vector3 position;
    }

    private uint[] m_ArgsData = new uint[5];
    Mesh m_GrassMesh;
    const int GrassBufferCount = 25;
    ComputeBuffer m_TotalChunkGrassBuffer;
    ComputeBuffer m_DrawedBuffer;
    ComputeBuffer m_DrawedPrefixSumBuffer;
    ComputeBuffer m_DrawedGroupSumBuffer;
    ComputeBuffer m_DrawedGroupPrefixSumBuffer;
    ComputeBuffer m_DrawedGrassBuffer;

    ComputeBuffer m_ArgsBuffer;
    ComputeBuffer m_GrassCountBuffer;
    [SerializeField] Material m_GrassMaterial;

    ComputeBuffer[] arr_BlankBuffer;

    int m_LastTotalChunkGrassCount = 0;
    int m_LastGroupCount = 0;
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
            m_DrawedBuffer.Release();
            m_DrawedPrefixSumBuffer.Release();
            m_DrawedGrassBuffer.Release();
        }
        if (m_LastGroupCount > 0)
        {
            m_DrawedGroupSumBuffer.Release();
            m_DrawedGroupPrefixSumBuffer.Release();
        }
        m_ArgsBuffer.Release();
        m_GrassCountBuffer.Release();
        for(int i=0;i<GrassBufferCount;i++)
        {
            arr_BlankBuffer[i].Release();
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
        m_ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_ArgsBuffer.SetData(m_ArgsData);

        arr_BlankBuffer = new ComputeBuffer[GrassBufferCount];
        m_GrassCountBuffer = new ComputeBuffer(GrassBufferCount, sizeof(int));
        for (int i=0;i<arr_BlankBuffer.Length;i++)
        {
            arr_BlankBuffer[i] = new ComputeBuffer(1, 4);
        }
        m_GrassMaterial.SetBuffer("_GrassBuffer", m_DrawedGrassBuffer);

    }

    public static void DrawGrass(MapMaker.Chunk[] arr_Chunk)
    {
        if(arr_Chunk.Length > GrassBufferCount)
        {
            Debug.Log($"Draw grass chunk so large {arr_Chunk.Length} : {GrassBufferCount}");
            return;
        }
        int totalGrassCount = 0;
        int[] grassCount = new int[GrassBufferCount];
        int[] grassPrefixSum = new int[arr_Chunk.Length];
        for (int i = 0; i < arr_Chunk.Length; i++)
        {
            grassCount[i] = arr_Chunk[i].m_GrassData.GrassCount;
            grassPrefixSum[i] = totalGrassCount;
            totalGrassCount += arr_Chunk[i].m_GrassData.GrassCount;
        }


        int groupCount = totalGrassCount / CullingThreadMax + (totalGrassCount % CullingThreadMax == 0 ? 0 : 1);
        if (groupCount > CullingGroupXMax)
        {
            Debug.Log($"Draw grass groupCaount too large {groupCount} : {CullingGroupXMax}");
            return;
        }

        int structSize = HMUtil.StructSize(typeof(GrassData));
       
        
        if (Ins.m_LastTotalChunkGrassCount < totalGrassCount)
        {
            if (Ins.m_LastTotalChunkGrassCount > 0)
            {
                Ins.m_TotalChunkGrassBuffer.Release();
                Ins.m_DrawedBuffer.Release();
                Ins.m_DrawedPrefixSumBuffer.Release();
                Ins.m_DrawedGrassBuffer.Release();
            }
            Ins.m_TotalChunkGrassBuffer = new ComputeBuffer(totalGrassCount, structSize);
            Ins.m_DrawedBuffer = new ComputeBuffer(totalGrassCount, sizeof(int)); //그릴지말지 bool값
            Ins.m_DrawedPrefixSumBuffer = new ComputeBuffer(totalGrassCount, sizeof(int)); //각 그룹단위에서의 누적합 버퍼
            Ins.m_DrawedGrassBuffer = new ComputeBuffer(totalGrassCount, structSize); //컬링 후 그려지는 grassbuffer 만 모아둔 버퍼
            Ins.m_GrassMaterial.SetBuffer("_GrassBuffer", Ins.m_DrawedGrassBuffer);
            Ins.m_LastTotalChunkGrassCount = totalGrassCount;
        }
        if(Ins.m_LastGroupCount < groupCount)
        {
            if(Ins.m_LastGroupCount > 0)
            {
                Ins.m_DrawedGroupSumBuffer.Release();
                Ins.m_DrawedGroupPrefixSumBuffer.Release();
            }
            Ins.m_DrawedGroupSumBuffer = new ComputeBuffer(groupCount, sizeof(int)); //그룹별 합을 배열로만든 버퍼
            Ins.m_DrawedGroupPrefixSumBuffer = new ComputeBuffer(groupCount, sizeof(int));//그룹별 합을 누적합 한 버퍼
            Ins.m_LastGroupCount = groupCount;
        }

        ComputeShader CSFrustumCulling = CSM.Ins.m_GrassFrustumCulling;
        Vector2 min = arr_Chunk[0].m_Key;
        Vector2 max = arr_Chunk[0].m_Key;
        for (int i = 0; i < arr_Chunk.Length; i++)
        {
            CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.CombineGrassBuffer, "_GrassBuffer" + i, arr_Chunk[i].m_GrassData.GrassBuffer);
            if (arr_Chunk[i].m_Key.x < min.x)
            {
                min.x = arr_Chunk[i].m_Key.x;
            }
            if (arr_Chunk[i].m_Key.y < min.y)
            {
                min.y = arr_Chunk[i].m_Key.y;
            }
            if (arr_Chunk[i].m_Key.x > max.x)
            {
                max.x = arr_Chunk[i].m_Key.x;
            }
            if (arr_Chunk[i].m_Key.y > max.y)
            {
                max.y = arr_Chunk[i].m_Key.y;
            }
        }
        for(int i = arr_Chunk.Length;i<GrassBufferCount;i++)
        {
            CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.CombineGrassBuffer, "_GrassBuffer" + i, Ins.arr_BlankBuffer[i]);
        }
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.CombineGrassBuffer, "_GrassBuffer", Ins.m_TotalChunkGrassBuffer);
        Ins.m_GrassCountBuffer.SetData(grassCount);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.CombineGrassBuffer, "_GrassCountBuffer", Ins.m_GrassCountBuffer);
        CSFrustumCulling.SetInt("_TotalGrassCount", totalGrassCount);

        Matrix4x4 p = Camera.main.projectionMatrix;
        Matrix4x4 v = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = p * v;

        CSFrustumCulling.SetVector("_CamPos", Camera.main.transform.position);
        CSFrustumCulling.SetFloat("_RenderDis", Ins.m_GrassRenderDis);
        CSFrustumCulling.SetMatrix("_MatVP", VP);

        CSFrustumCulling.SetInt("_GroupCount", groupCount);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.FrustumCulling, "_GrassBuffer", Ins.m_TotalChunkGrassBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.FrustumCulling, "_DrawedBuffer", Ins.m_DrawedBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.PrefixSum, "_DrawedBuffer", Ins.m_DrawedBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.PrefixSum, "_DrawedPrefixSumBuffer", Ins.m_DrawedPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.PrefixSum, "_DrawedGroupSumBuffer", Ins.m_DrawedGroupSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupPrefixSum, "_DrawedGroupPrefixSumBuffer", Ins.m_DrawedGroupPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupPrefixSum, "_DrawedGroupSumBuffer", Ins.m_DrawedGroupSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupPrefixSum, "_MeshArgsBuffer", Ins.m_ArgsBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_DrawedGroupPrefixSumBuffer", Ins.m_DrawedGroupPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_DrawedPrefixSumBuffer", Ins.m_DrawedPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_GrassBuffer", Ins.m_TotalChunkGrassBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_DrawedGrassBuffer", Ins.m_DrawedGrassBuffer);
        
        //CombineBuffer
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.CombineGrassBuffer, groupCount, 1, 1);

        //Cull
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.FrustumCulling, groupCount, 1, 1);

        //PrefixSum
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.PrefixSum, groupCount, 1, 1);

        //GroupPrefixSum
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.GroupPrefixSum, 1, 1, 1);

        //GetDrawedIdx
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.SetDrawedGrass, groupCount, 1, 1);


        Vector2 keyCenter = min + (max - min) * 0.5f;
        Vector2 keySize = max - min;

        Vector3 boundCenter = new Vector3(keyCenter.x * Ground.GroundSize.x, 0, keyCenter.y * Ground.GroundSize.y);
        Vector3 boundSize = new Vector3(keySize.x * Ground.GroundSize.x, 20, keySize.y * Ground.GroundSize.y);
        Bounds FieldBound = new Bounds(boundCenter, boundSize);

        Graphics.DrawMeshInstancedIndirect(Ins.m_GrassMesh, 0, Ins.m_GrassMaterial, FieldBound, Ins.m_ArgsBuffer);
    }


    public static ChunkGrassData GetChunkGrassData(GrassMakerOption option)
    {
        ComputeShader CSGrassPosition = CSM.Ins.m_GrassPosition;

        ChunkGrassData data = new ChunkGrassData();
        data.Option = option;
        int grassHorizonCount = Mathf.FloorToInt(Ins.m_GrassCountPerOne * Ground.GroundSize.x);
        int grassVerticalCount = Mathf.FloorToInt(Ins.m_GrassCountPerOne * Ground.GroundSize.y);
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
        CSGrassPosition.SetFloats("_GridPos", new float[2] { option.GridPos.x - Ground.GroundSize.x * 0.5f, option.GridPos.y - Ground.GroundSize.y * 0.5f });
        CSGrassPosition.SetFloats("_GridSize", new float[2] { Ground.GroundSize.x, Ground.GroundSize.y });
        CSGrassPosition.Dispatch(0, perlinKernel_x, perlinKernel_y, 1);

        int groupCount = grassCount / CullingThreadMax + (grassCount % CullingThreadMax == 0 ? 0 : 1);
        if(groupCount > 512)
        {
            Debug.Log($"Group too large {groupCount}");
        }
        data.GrassCount = grassCount;

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
