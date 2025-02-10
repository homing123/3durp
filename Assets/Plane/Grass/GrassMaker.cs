
using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GUILayout;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor;

public class GrassMaker : MonoBehaviour
{

    public enum E_GrassFrustumCullingKernel
    {
        CombineGrassBuffer = 0,
        FrustumCulling = 1,
        PrefixSum = 2,
        GroupXPrefixSum = 3,
        GroupYPrefixSum = 4,
        SetDrawedGrass = 5,
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
    ComputeBuffer m_DrawedGroupXSumBuffer;
    ComputeBuffer m_DrawedGroupXPrefixSumBuffer;

    ComputeBuffer m_DrawedGroupYSumBuffer;
    ComputeBuffer m_DrawedGroupYPrefixSumBuffer;
    ComputeBuffer m_DrawedGrassBuffer;

    ComputeBuffer m_ArgsBuffer;
    ComputeBuffer m_GrassCountBuffer;
    [SerializeField] Material m_GrassMaterial;

    ComputeBuffer[] arr_BlankBuffer;

    int m_LastTotalChunkGrassCount = 0;
    int m_LastGroupCount = 0;
    int m_LastGroupYCount = 0;
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
            m_DrawedGroupXSumBuffer.Release();
            m_DrawedGroupXPrefixSumBuffer.Release();
        }
        if (m_LastGroupYCount > 0)
        {
            m_DrawedGroupYSumBuffer.Release();
            m_DrawedGroupYPrefixSumBuffer.Release();
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
        //청크갯수제한최대 32개다 청크 절두체컬링으로 안보이는청크 다짤라야겠따.

        if(arr_Chunk.Length > GrassBufferCount)
        {
            Debug.Log($"Draw grass chunk so large {arr_Chunk.Length} : {GrassBufferCount}");
            return;
        }
        for (int i = 0; i < arr_Chunk.Length; i++)
        {
            Vector2 rectMin = (arr_Chunk[i].m_Key - new Vector2(0.5f, 0.5f)) * Ground.GroundSize;
            Rect rect = new Rect(rectMin, Ground.GroundSize);
            bool temp = Camera.main.FrustumCulling(rect, 0);
            Debug.Log(i + " " + temp);
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
        int kernelGroupX = 1;
        int kernelGroupY = 1;
        if (groupCount > CullingGroupXMax)
        {
            kernelGroupX = CullingGroupXMax;
            kernelGroupY = groupCount / CullingGroupXMax + (groupCount % CullingGroupXMax == 0 ? 0 : 1);
            //Debug.Log($"Draw grass groupCaount too large {groupCount} : {CullingGroupXMax}");
            //return;
        }
        else
        {
            kernelGroupX = groupCount;
        }

        //Debug.Log($"잔디갯수 : {totalGrassCount}, 그룹갯수 : {groupCount}, 커널x : {kernelGroupX}, 커널y : {kernelGroupY}");

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
                Ins.m_DrawedGroupXSumBuffer.Release();
                Ins.m_DrawedGroupXPrefixSumBuffer.Release();
            }
            Ins.m_DrawedGroupXSumBuffer = new ComputeBuffer(groupCount, sizeof(int)); //그룹별 합을 배열로만든 버퍼
            Ins.m_DrawedGroupXPrefixSumBuffer = new ComputeBuffer(groupCount, sizeof(int));//그룹별 합을 누적합 한 버퍼
            Ins.m_LastGroupCount = groupCount;
        }
        if(Ins.m_LastGroupYCount < kernelGroupY)
        {
            if (Ins.m_LastGroupYCount > 0)
            {
                Ins.m_DrawedGroupYSumBuffer.Release();
                Ins.m_DrawedGroupYPrefixSumBuffer.Release();
            }
            Ins.m_DrawedGroupYSumBuffer = new ComputeBuffer(kernelGroupY, sizeof(int)); //그룹별 합을 배열로만든 버퍼
            Ins.m_DrawedGroupYPrefixSumBuffer = new ComputeBuffer(kernelGroupY, sizeof(int));//그룹별 합을 누적합 한 버퍼
            Ins.m_LastGroupYCount = kernelGroupY;
        }

        ComputeShader CSFrustumCulling = CSM.Ins.m_GrassFrustumCulling;
        Matrix4x4 VP = Camera.main.GetVP();
        CSFrustumCulling.SetInt("_TotalGrassCount", totalGrassCount);
        CSFrustumCulling.SetVector("_CamPos", Camera.main.transform.position);
        CSFrustumCulling.SetFloat("_RenderDis", Ins.m_GrassRenderDis);
        CSFrustumCulling.SetMatrix("_MatVP", VP);
        CSFrustumCulling.SetInt("_GroupCount", groupCount);
        CSFrustumCulling.SetInt("_KernelGroupY", kernelGroupY);

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

        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.FrustumCulling, "_GrassBuffer", Ins.m_TotalChunkGrassBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.FrustumCulling, "_DrawedBuffer", Ins.m_DrawedBuffer);

        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.PrefixSum, "_DrawedBuffer", Ins.m_DrawedBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.PrefixSum, "_DrawedPrefixSumBuffer", Ins.m_DrawedPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.PrefixSum, "_DrawedGroupXSumBuffer", Ins.m_DrawedGroupXSumBuffer);

        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupXPrefixSum, "_DrawedGroupXSumBuffer", Ins.m_DrawedGroupXSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupXPrefixSum, "_DrawedGroupXPrefixSumBuffer", Ins.m_DrawedGroupXPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupXPrefixSum, "_DrawedGroupYSumBuffer", Ins.m_DrawedGroupYSumBuffer);

        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupYPrefixSum, "_DrawedGroupYSumBuffer", Ins.m_DrawedGroupYSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupYPrefixSum, "_DrawedGroupYPrefixSumBuffer", Ins.m_DrawedGroupYPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.GroupYPrefixSum, "_MeshArgsBuffer", Ins.m_ArgsBuffer);

        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_DrawedGroupXPrefixSumBuffer", Ins.m_DrawedGroupXPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_DrawedGroupYPrefixSumBuffer", Ins.m_DrawedGroupYPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_DrawedPrefixSumBuffer", Ins.m_DrawedPrefixSumBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_GrassBuffer", Ins.m_TotalChunkGrassBuffer);
        CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.SetDrawedGrass, "_DrawedGrassBuffer", Ins.m_DrawedGrassBuffer);
        
        //CombineBuffer
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.CombineGrassBuffer, kernelGroupX, kernelGroupY, 1);

        //Cull
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.FrustumCulling, kernelGroupX, kernelGroupY, 1);

        //PrefixSum DrawedBuffer값을 DrawedPrefixSumBuffer에 그룹단위로 누적합 그리고 DraweGroupSumBuffer에 각 그룹의 누적합 마지막값 대입
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.PrefixSum, kernelGroupX, kernelGroupY, 1);

        //GroupXPrefixSum
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.GroupXPrefixSum, kernelGroupY, 1, 1);

        //GroupYPrefixSum
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.GroupYPrefixSum, 1, 1, 1);

        //GetDrawedIdx
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.SetDrawedGrass, kernelGroupX, kernelGroupY, 1);

        Vector2 keyCenter = min + (max - min) * 0.5f;
        Vector2 keySize = max - min;

        Vector3 boundCenter = new Vector3(keyCenter.x * Ground.GroundSize.x, 0, keyCenter.y * Ground.GroundSize.y);
        Vector3 boundSize = new Vector3(keySize.x * Ground.GroundSize.x, 20, keySize.y * Ground.GroundSize.y);
        Bounds FieldBound = new Bounds(boundCenter, boundSize);

        Graphics.DrawMeshInstancedIndirect(Ins.m_GrassMesh, 0, Ins.m_GrassMaterial, FieldBound, Ins.m_ArgsBuffer);

        //int drawedCount = 0;
        //int[] arr_Drawed = new int[totalGrassCount];
        //int[] arr_GroupXSumCPU = new int[groupCount];
        //int[] arr_GroupXPrefixCPU = new int[groupCount];
        //Ins.m_DrawedBuffer.GetData(arr_Drawed);

        //int groupIdx = 0;
        //int prefixvalue = 0;

        //for (int i=0;i< totalGrassCount;i++)
        //{
        //    if (i != 0 && i % CullingGroupXMax == 0)
        //    {
        //        arr_GroupXSumCPU[groupIdx] = prefixvalue;
        //        prefixvalue = 0;
        //        groupIdx++;
        //    }
        //    if (arr_Drawed[i] == 1)
        //    {
        //        drawedCount++;
        //        prefixvalue++;
        //    }

        //    if (i == totalGrassCount - 1)
        //    {
        //        arr_GroupXSumCPU[groupIdx] = prefixvalue;
        //    }
        //}
        //int[] arr_GroupYSumCPU = new int[kernelGroupY];
        //int[] arr_GroupYPrefixCPU = new int[kernelGroupY];
        //prefixvalue = 0;
        //int groupYIdx = 0;
        //for (int i=0;i<groupCount;i++)
        //{
        //    if (i != 0 && i % CullingGroupXMax == 0 )
        //    {
        //        arr_GroupYSumCPU[groupYIdx] = prefixvalue;
        //        prefixvalue = 0;
        //        groupYIdx++;
        //    }
        //    prefixvalue += arr_GroupXSumCPU[i];
        //    arr_GroupXPrefixCPU[i] = prefixvalue;

        //    if (i == groupCount - 1)
        //    {
        //        arr_GroupYSumCPU[groupYIdx] = prefixvalue;
        //    }
        //}

        //prefixvalue = 0;
        //for (int i=0;i<kernelGroupY;i++)
        //{
        //    prefixvalue += arr_GroupYSumCPU[i];
        //    arr_GroupYPrefixCPU[i] = prefixvalue;
        //}

        //int[] arr_args = new int[5];
        //Ins.m_ArgsBuffer.GetData(arr_args);
        //Debug.Log($"cpu drawed 갯수 : {drawedCount}");
        //Debug.Log($"gpu drawed 갯수 : {arr_args[1]}");

        //int[] arr_GroupXSum = new int[groupCount];
        //int[] arr_GroupXPrefix = new int[groupCount];
        //Ins.m_DrawedGroupXSumBuffer.GetData(arr_GroupXSum);
        //Ins.m_DrawedGroupXPrefixSumBuffer.GetData(arr_GroupXPrefix);
        //for (int i = 0; i < groupCount; i++)
        //{
        //    //Debug.Log($" {i} {arr_GroupXSum[i]} {arr_GroupXSumCPU[i]} {arr_GroupXPrefix[i]} {arr_GroupXPrefixCPU[i]}");

        //    if (arr_GroupXSum[i] != arr_GroupXSumCPU[i] || arr_GroupXPrefix[i] != arr_GroupXPrefixCPU[i])
        //    {
        //        Debug.Log($"달라용 X {i} {arr_GroupXSum[i]} {arr_GroupXSumCPU[i]} {arr_GroupXPrefix[i]} {arr_GroupXPrefixCPU[i]}");
        //        break;
        //    }
        //}

        //int[] arr_GroupYSum = new int[kernelGroupY];
        //int[] arr_GroupYPrefix = new int[kernelGroupY];
        //Ins.m_DrawedGroupYSumBuffer.GetData(arr_GroupYSum);
        //Ins.m_DrawedGroupYPrefixSumBuffer.GetData(arr_GroupYPrefix);
        //for (int i = 0; i < kernelGroupY; i++)
        //{            
        //    //Debug.Log($" Y {i} {arr_GroupYSum[i]} {arr_GroupYSumCPU[i]} {arr_GroupYPrefix[i]} {arr_GroupYPrefixCPU[i]}");

        //    if (arr_GroupYSum[i] != arr_GroupYSumCPU[i] || arr_GroupYPrefix[i] != arr_GroupYPrefixCPU[i])
        //    {
        //        Debug.Log($"달라용 Y {i} {arr_GroupYSum[i]} {arr_GroupYSumCPU[i]} {arr_GroupYPrefix[i]} {arr_GroupYPrefixCPU[i]}");
        //        break;
        //    }
        //}

        //GrassData[] arr_DrawedGrass = new GrassData[totalGrassCount];
        //Ins.m_DrawedGrassBuffer.GetData(arr_DrawedGrass);
        //int idx = 0;
        //for(int i=0;i< totalGrassCount; i++)
        //{
        //    if ((int)arr_DrawedGrass[i].position.x != 0)
        //    {
        //        if((int)arr_DrawedGrass[i].position.y != idx)
        //        {
        //            Debug.Log($" {i} 다르다네요 {(int)arr_DrawedGrass[i].position.y} {idx}");
        //            break;
        //        }
        //        idx++;
        //    }
        //}
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
