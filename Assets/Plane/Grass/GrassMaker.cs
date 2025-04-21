
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
        public Vector2 chunkCenterPos;
        public RenderTexture heightTexture;
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
    [SerializeField][Range(0.1f, 30)] public float m_GrassRenderDis;

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
        m_ArgsData[1] = 0; //�ܵ� �����ε� computeshader ���� ���ؼ� ����
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
    [SerializeField] Vector2Int m_TestKey;

    public void DrawGrass(ChunkData[] arr_Chunk)
    {
        List<ChunkData> l_DrawedChunk = new List<ChunkData>();
        for (int i = 0; i < arr_Chunk.Length; i++)
        {
            Vector2 rectMin = arr_Chunk[i].key * MapMaker.ChunkSize;
            Rect rect = new Rect(rectMin, new Vector2(MapMaker.ChunkSize, MapMaker.ChunkSize));
            bool isOut = Camera.main.FrustumCullingInWorld(new Vector3(rect.min.x,-10, rect.min.y), new Vector3(rect.max.x, 10, rect.max.y));

            if (isOut == false)
            {
                l_DrawedChunk.Add(arr_Chunk[i]);
            }
        }

        arr_Chunk = l_DrawedChunk.ToArray();

        //ûũ���������ִ� 32���� ûũ ����ü�ø����� �Ⱥ��̴�ûũ ��©��߰ڵ�.
        if (arr_Chunk.Length > GrassBufferCount)
        {
            Debug.Log($"Draw grass chunk so large {arr_Chunk.Length} : {GrassBufferCount}");
            return;
        }
        if(arr_Chunk.Length==0)
        {
            Debug.Log("Drawgrass chunk count is zero");
            return;
        }
        int totalGrassCount = 0;
        int[] grassCount = new int[GrassBufferCount];
        int[] grassPrefixSum = new int[arr_Chunk.Length];
        for (int i = 0; i < arr_Chunk.Length; i++)
        {
            grassCount[i] = arr_Chunk[i].grassData.grassCount;
            grassPrefixSum[i] = totalGrassCount;
            totalGrassCount += arr_Chunk[i].grassData.grassCount;
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

        //Debug.Log($"�ܵ𰹼� : {totalGrassCount}, �׷찹�� : {groupCount}, Ŀ��x : {kernelGroupX}, Ŀ��y : {kernelGroupY}");

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
            Ins.m_DrawedBuffer = new ComputeBuffer(totalGrassCount, sizeof(int)); //�׸������� bool��
            Ins.m_DrawedPrefixSumBuffer = new ComputeBuffer(totalGrassCount, sizeof(int)); //�� �׷���������� ������ ����
            Ins.m_DrawedGrassBuffer = new ComputeBuffer(totalGrassCount, structSize); //�ø� �� �׷����� grassbuffer �� ��Ƶ� ����
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
            Ins.m_DrawedGroupXSumBuffer = new ComputeBuffer(groupCount, sizeof(int)); //�׷캰 ���� �迭�θ��� ����
            Ins.m_DrawedGroupXPrefixSumBuffer = new ComputeBuffer(groupCount, sizeof(int));//�׷캰 ���� ������ �� ����
            Ins.m_LastGroupCount = groupCount;
        }
        if(Ins.m_LastGroupYCount < kernelGroupY)
        {
            if (Ins.m_LastGroupYCount > 0)
            {
                Ins.m_DrawedGroupYSumBuffer.Release();
                Ins.m_DrawedGroupYPrefixSumBuffer.Release();
            }
            Ins.m_DrawedGroupYSumBuffer = new ComputeBuffer(kernelGroupY, sizeof(int)); //�׷캰 ���� �迭�θ��� ����
            Ins.m_DrawedGroupYPrefixSumBuffer = new ComputeBuffer(kernelGroupY, sizeof(int));//�׷캰 ���� ������ �� ����
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

        Vector2 keyMin = arr_Chunk[0].key;
        Vector2 keyMax = arr_Chunk[0].key;
        for (int i = 0; i < arr_Chunk.Length; i++)
        {
            CSFrustumCulling.SetBuffer((int)E_GrassFrustumCullingKernel.CombineGrassBuffer, "_GrassBuffer" + i, arr_Chunk[i].grassData.grassBuffer);
            if (arr_Chunk[i].key.x < keyMin.x)
            {
                keyMin.x = arr_Chunk[i].key.x;
            }
            if (arr_Chunk[i].key.y < keyMin.y)
            {
                keyMin.y = arr_Chunk[i].key.y;
            }
            if (arr_Chunk[i].key.x > keyMax.x)
            {
                keyMax.x = arr_Chunk[i].key.x;
            }
            if (arr_Chunk[i].key.y > keyMax.y)
            {
                keyMax.y = arr_Chunk[i].key.y;
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

        //PrefixSum DrawedBuffer���� DrawedPrefixSumBuffer�� �׷������ ������ �׸��� DraweGroupSumBuffer�� �� �׷��� ������ �������� ����
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.PrefixSum, kernelGroupX, kernelGroupY, 1);

        //GroupXPrefixSum
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.GroupXPrefixSum, kernelGroupY, 1, 1);

        //GroupYPrefixSum
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.GroupYPrefixSum, 1, 1, 1);

        //GetDrawedIdx
        CSFrustumCulling.Dispatch((int)E_GrassFrustumCullingKernel.SetDrawedGrass, kernelGroupX, kernelGroupY, 1);

        Vector3 boundMin = new Vector3(keyMin.x * MapMaker.ChunkSize, -10, keyMin.y * MapMaker.ChunkSize);
        Vector3 boundMax = new Vector3((keyMax.x + 1) * MapMaker.ChunkSize, 10, (keyMax.y + 1) * MapMaker.ChunkSize);

        Vector3 boundCenter = boundMin + (boundMax - boundMin) * 0.5f;
        Vector3 boundSize = boundMax - boundMin;
        Bounds FieldBound = new Bounds(boundCenter, boundSize);
        Graphics.DrawMeshInstancedIndirect(Ins.m_GrassMesh, 0, Ins.m_GrassMaterial, FieldBound, Ins.m_ArgsBuffer);
        return;
        int drawedCount = 0;
        int[] arr_Drawed = new int[totalGrassCount];
        int[] arr_GroupXSumCPU = new int[groupCount];
        int[] arr_GroupXPrefixCPU = new int[groupCount];
        Ins.m_DrawedBuffer.GetData(arr_Drawed);

        int groupIdx = 0;
        int prefixvalue = 0;

        for (int i = 0; i < totalGrassCount; i++)
        {
            if (i != 0 && i % CullingGroupXMax == 0)
            {
                arr_GroupXSumCPU[groupIdx] = prefixvalue;
                prefixvalue = 0;
                groupIdx++;
            }
            if (arr_Drawed[i] == 1)
            {
                drawedCount++;
                prefixvalue++;
            }

            if (i == totalGrassCount - 1)
            {
                arr_GroupXSumCPU[groupIdx] = prefixvalue;
            }
        }
        int[] arr_GroupYSumCPU = new int[kernelGroupY];
        int[] arr_GroupYPrefixCPU = new int[kernelGroupY];
        prefixvalue = 0;
        int groupYIdx = 0;
        for (int i = 0; i < groupCount; i++)
        {
            if (i != 0 && i % CullingGroupXMax == 0)
            {
                arr_GroupYSumCPU[groupYIdx] = prefixvalue;
                prefixvalue = 0;
                groupYIdx++;
            }
            prefixvalue += arr_GroupXSumCPU[i];
            arr_GroupXPrefixCPU[i] = prefixvalue;

            if (i == groupCount - 1)
            {
                arr_GroupYSumCPU[groupYIdx] = prefixvalue;
            }
        }

        prefixvalue = 0;
        for (int i = 0; i < kernelGroupY; i++)
        {
            prefixvalue += arr_GroupYSumCPU[i];
            arr_GroupYPrefixCPU[i] = prefixvalue;
        }

        int[] arr_args = new int[5];
        Ins.m_ArgsBuffer.GetData(arr_args);
        //Debug.Log($"cpu drawed ���� : {drawedCount}");
        //Debug.Log($"gpu drawed ���� : {arr_args[1]}");
        int[] arr_GroupXSum = new int[groupCount];
        int[] arr_GroupXPrefix = new int[groupCount];
        Ins.m_DrawedGroupXSumBuffer.GetData(arr_GroupXSum);
        Ins.m_DrawedGroupXPrefixSumBuffer.GetData(arr_GroupXPrefix);
        for (int i = 0; i < groupCount; i++)
        {
            //Debug.Log($" {i} {arr_GroupXSum[i]} {arr_GroupXSumCPU[i]} {arr_GroupXPrefix[i]} {arr_GroupXPrefixCPU[i]}");

            if (arr_GroupXSum[i] != arr_GroupXSumCPU[i] || arr_GroupXPrefix[i] != arr_GroupXPrefixCPU[i])
            {
                Debug.Log($"�޶�� X {i} {arr_GroupXSum[i]} {arr_GroupXSumCPU[i]} {arr_GroupXPrefix[i]} {arr_GroupXPrefixCPU[i]}");
                break;
            }
        }

        int[] arr_GroupYSum = new int[kernelGroupY];
        int[] arr_GroupYPrefix = new int[kernelGroupY];
        Ins.m_DrawedGroupYSumBuffer.GetData(arr_GroupYSum);
        Ins.m_DrawedGroupYPrefixSumBuffer.GetData(arr_GroupYPrefix);
        for (int i = 0; i < kernelGroupY; i++)
        {
            //Debug.Log($" Y {i} {arr_GroupYSum[i]} {arr_GroupYSumCPU[i]} {arr_GroupYPrefix[i]} {arr_GroupYPrefixCPU[i]}");

            if (arr_GroupYSum[i] != arr_GroupYSumCPU[i] || arr_GroupYPrefix[i] != arr_GroupYPrefixCPU[i])
            {
                Debug.Log($"�޶�� Y {i} {arr_GroupYSum[i]} {arr_GroupYSumCPU[i]} {arr_GroupYPrefix[i]} {arr_GroupYPrefixCPU[i]}");
                break;
            }
        }

        GrassData[] arr_DrawedGrass = new GrassData[totalGrassCount];
        Ins.m_DrawedGrassBuffer.GetData(arr_DrawedGrass);
        int idx = 0;
        Debug.Log(arr_DrawedGrass[50000].position.x + " " + arr_DrawedGrass[50000].position.y);
        return;

        for (int i = 0; i < totalGrassCount; i++)
        {
            if ((int)arr_DrawedGrass[i].position.x != 0)
            {
                if ((int)arr_DrawedGrass[i].position.y != idx)
                {
                    //Debug.Log($" {i} �ٸ��ٳ׿� {(int)arr_DrawedGrass[i].position.y} {idx}");
                    break;
                }
                idx++;
            }
        }
    }


    public Chunk_GrassData GetChunkGrassData(GrassMakerOption option)
    {
        ComputeShader CSGrassPosition = CSM.Ins.m_GrassPosition;

        Chunk_GrassData data = new Chunk_GrassData();
        data.option = option;
        int grassHorizonCount = Mathf.FloorToInt(Ins.m_GrassCountPerOne * MapMaker.ChunkSize);
        int grassVerticalCount = Mathf.FloorToInt(Ins.m_GrassCountPerOne * MapMaker.ChunkSize);
        int grassCount = grassHorizonCount * grassVerticalCount;

        int perlinKernel_x = grassHorizonCount / GrassThreadWidth + (grassHorizonCount % GrassThreadWidth == 0 ? 0 : 1);
        int perlinKernel_y = grassVerticalCount / GrassThreadWidth + (grassVerticalCount % GrassThreadWidth == 0 ? 0 : 1);
        int structSize = HMUtil.StructSize(typeof(GrassData));


        Vector2Int heightMapSize = TerrainMaker.Ins.HeightMapSize;

        data.grassBuffer = new ComputeBuffer(grassCount, structSize); //�ø� �� grassbuffer

        CSGrassPosition.SetTexture(0, "_HeightMap", option.heightTexture);
        CSGrassPosition.SetBuffer(0, "_GrassBuffer", data.grassBuffer);
        CSGrassPosition.SetInts("_HeightBufferSize", new int[2] { heightMapSize.x, heightMapSize.y });
        CSGrassPosition.SetInt("_GrassHorizonCount", grassHorizonCount);
        CSGrassPosition.SetInt("_GrassVerticalCount", grassVerticalCount);
        Vector2 chunkMinPos = option.chunkCenterPos - Vector2.one * MapMaker.ChunkSize * 0.5f;
        CSGrassPosition.SetFloats("_GridPos", new float[2] { chunkMinPos.x, chunkMinPos.y });
        CSGrassPosition.SetFloats("_GridSize", new float[2] { MapMaker.ChunkSize, MapMaker.ChunkSize });
       
        CSGrassPosition.Dispatch(0, perlinKernel_x, perlinKernel_y, 1);       

        int groupCount = grassCount / CullingThreadMax + (grassCount % CullingThreadMax == 0 ? 0 : 1);
        if (groupCount > 512)
        {
            Debug.Log($"Group too large {groupCount}");
        }
        data.grassCount = grassCount;

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
