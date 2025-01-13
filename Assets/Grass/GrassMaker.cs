using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class GrassMaker : MonoBehaviour
{
    struct LogData
    {
        public int ia;
        public int ib;
        public int ic;
        public int id;
        public int ie;
    }
    ComputeBuffer m_LogBuffer;
    private uint[] m_ArgsData = new uint[5];
    ComputeBuffer m_ArgsBuffer;
    Mesh m_GrassMesh;
    Bounds m_FieldBounds;

    struct GrassData
    {
        public Vector2 chunkUV;
        public Vector3 position;
    }
    public Vector3 m_Position;
    public int m_GrassCountPerOne;
    public int m_Scale;
    public Material m_GrassMaterial;

    public float m_RenderDis;
    public Texture2D m_NoiseTexture;
    [SerializeField] float m_RandomPosMul;

    [SerializeField] ComputeShader m_CSGrassPoint;
    [SerializeField] ComputeShader m_CSFrustumCulling;
    ComputeBuffer m_DrawedBuffer;
    ComputeBuffer m_GrassBuffer;
    ComputeBuffer m_DrawedPrefixSumBuffer;
    ComputeBuffer m_DrawedGroupSumBuffer;
    ComputeBuffer m_DrawedGroupPrefixSumBuffer;
    ComputeBuffer m_DrawedIdxBuffer;
    ComputeBuffer m_DrawedGrassBuffer;

    bool m_isInfoChanged = true;
    const int ThreadMax = 512;
    const int GroupMaxX = 512;


    int m_GrassAxisCount;
    int m_GrassCount;
    int m_GroupX;

    private void Start()
    {
        InitMaterial();
        InitMesh();
        Update();
    }
    private void OnDestroy()
    {
        Release();
    }
    private void Update()
    {
        if(m_isInfoChanged == true)
        {
            InitArgsBuffer();
            InitCSBuffer();
            InitMaterialBuffer();
            m_isInfoChanged = false;
        }
        FrustumCull();
        DrawInstances();
    }
    void InitMaterial()
    {
        //m_GrassMaterial = new Material(m_GrassMaterial);
    }
    void InitMaterialBuffer()
    {
        m_GrassMaterial.SetBuffer("_GrassData", m_DrawedGrassBuffer);
    }
    void InitMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[4] {new Vector3(-0.5f, 1f, 0)
        , new Vector3 (0.5f, 1f, 0)
        , new Vector3 (-0.5f, 0f, 0)
        , new Vector3 (0.5f, 0f, 0) };
        mesh.SetIndices(new int[6] { 0, 1, 2, 1,3,2 }, MeshTopology.Triangles, 0);
        mesh.uv = new Vector2[4] {
            new Vector2 (0,1), new Vector2 ( 1,1),new Vector2 (0,0) ,new Vector2 (1,0) };
        mesh.normals = new Vector3[4]
        {
            new Vector3(0,0,-1), new Vector3(0,0,-1), new Vector3(0,0,-1), new Vector3(0,0,-1)
        };
        mesh.name = "GrassMesh";
        m_GrassMesh = mesh;
    }
    void InitArgsBuffer()
    {
        m_ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        m_ArgsData[0] = (uint)m_GrassMesh.GetIndexCount(0);
        m_ArgsData[1] = 0; //잔디 갯수인데 computeshader 에서 구해서 넣음
        m_ArgsData[2] = (uint)m_GrassMesh.GetIndexStart(0);
        m_ArgsData[3] = (uint)m_GrassMesh.GetBaseVertex(0);
        m_ArgsData[4] = 0;

        Debug.Log(m_GrassCount + "개 생성");
        m_ArgsBuffer.SetData(m_ArgsData);
    }


    void InitCSBuffer()
    {
        m_GrassAxisCount = m_GrassCountPerOne * m_Scale;
        m_GrassCount = m_GrassAxisCount * m_GrassAxisCount;
        if(m_GrassCount > ThreadMax * GroupMaxX)
        {
            Debug.Log("TooLarge");
            return;
            //int remain = m_GrassCount % (ThreadMax * GroupMaxX);
            //int diminish = m_GrassCount / (ThreadMax * GroupMaxX);
            //m_GroupY = remain > 0 ? diminish + 1 : diminish;
            ////GrassPosThreadCountInGroup * 512 개를 넘어가면 비효율적인 로직임 일단 임시로 이렇게 해둠 25만개는 안넘길듯ㅋㅋ
            //m_GroupX = 512;
        }
        else
        {
            int remain = m_GrassCount & ThreadMax;
            int diminish = m_GrassCount / ThreadMax;
            m_GroupX = remain > 0 ? diminish + 1 : diminish;
            m_GroupX = m_GroupX == 0 ? 1 : m_GroupX;
        }
        //Debug.Log(m_GroupX + " " + m_GroupY);
        int structSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(GrassData));

        m_GrassBuffer = new ComputeBuffer(m_GrassCount, structSize);
        m_LogBuffer = new ComputeBuffer(1, sizeof(int) * 5);
        m_DrawedBuffer = new ComputeBuffer(m_GrassCount, sizeof(int));
        m_DrawedPrefixSumBuffer = new ComputeBuffer(m_GrassCount, sizeof(int));
        m_DrawedGroupSumBuffer = new ComputeBuffer(GroupMaxX, sizeof(int));
        m_DrawedGroupPrefixSumBuffer = new ComputeBuffer(GroupMaxX, sizeof(int));
        m_DrawedIdxBuffer = new ComputeBuffer(m_GrassCount, sizeof(int));
        m_DrawedGrassBuffer = new ComputeBuffer(m_GrassCount, structSize);

        m_CSGrassPoint.SetTexture(0, "_NoiseTex", m_NoiseTexture);
        m_CSGrassPoint.SetInt("_GrassAmount", m_GrassCountPerOne);
        m_CSGrassPoint.SetInt("_Scale", m_Scale);
        m_CSGrassPoint.SetVector("_Position", m_Position);
        m_CSGrassPoint.SetBuffer(0, "_GrassBuffer", m_GrassBuffer);
        m_CSGrassPoint.SetFloats("_RandomPosMul", m_RandomPosMul);
        m_CSGrassPoint.Dispatch(0, m_GroupX, 1, 1);

        m_CSFrustumCulling.SetInt("_GroupCount", m_GroupX);

        m_CSFrustumCulling.SetBuffer(0, "_GrassBuffer", m_GrassBuffer);
        m_CSFrustumCulling.SetBuffer(0, "_DrawedBuffer", m_DrawedBuffer);
        m_CSFrustumCulling.SetBuffer(1, "_DrawedBuffer", m_DrawedBuffer);
        m_CSFrustumCulling.SetBuffer(1, "_DrawedPrefixSumBuffer", m_DrawedPrefixSumBuffer);
        m_CSFrustumCulling.SetBuffer(1, "_DrawedGroupSumBuffer", m_DrawedGroupSumBuffer);
        m_CSFrustumCulling.SetBuffer(2, "_DrawedGroupPrefixSumBuffer", m_DrawedGroupPrefixSumBuffer);
        m_CSFrustumCulling.SetBuffer(2, "_DrawedGroupSumBuffer", m_DrawedGroupSumBuffer);
        m_CSFrustumCulling.SetBuffer(2, "_MeshArgsBuffer", m_ArgsBuffer);
        m_CSFrustumCulling.SetBuffer(3, "_DrawedIdxBuffer", m_DrawedIdxBuffer);
        m_CSFrustumCulling.SetBuffer(3, "_DrawedGroupPrefixSumBuffer", m_DrawedGroupPrefixSumBuffer);
        m_CSFrustumCulling.SetBuffer(3, "_DrawedPrefixSumBuffer", m_DrawedPrefixSumBuffer);
        m_CSFrustumCulling.SetBuffer(3, "_GrassBuffer", m_GrassBuffer);
        m_CSFrustumCulling.SetBuffer(3, "_DrawedGrassBuffer", m_DrawedGrassBuffer);

        m_FieldBounds = new Bounds(m_Position, new Vector3(m_Scale, 5, m_Scale));
    }
    void FrustumCull()
    {
        Matrix4x4 p = Camera.main.projectionMatrix;
        Matrix4x4 v = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = p * v;

        m_CSFrustumCulling.SetInt("_GrassCount", m_GrassCount);
        m_CSFrustumCulling.SetVector("_CamPos", Camera.main.transform.position);
        m_CSFrustumCulling.SetFloat("_RenderDis", m_RenderDis);
        m_CSFrustumCulling.SetMatrix("_MatVP", VP);

        //Cull
        m_CSFrustumCulling.Dispatch(0, m_GroupX, 1, 1);

        //PrefixSum
        m_CSFrustumCulling.Dispatch(1, m_GroupX, 1, 1);

        //GroupPrefixSum
        m_CSFrustumCulling.Dispatch(2, 1, 1, 1);

        //GetDrawedIdx
        m_CSFrustumCulling.Dispatch(3, m_GroupX, 1, 1);
        
        return;
        //log
        int[] arr_drawed = new int[m_GrassCount];
        m_DrawedBuffer.GetData(arr_drawed);
        int count = 0;
        for (int i = 0; i < m_GrassCount; i++)
        {
            if (arr_drawed[i] == 1)
            {
                count += 1;
            }
        }
        //Debug.Log(count);

        int[] arr_draedsum = new int[m_GrassCount];
        m_DrawedPrefixSumBuffer.GetData(arr_draedsum);
        int idx = 0;
        int beforeValue = 0;
        int prefixSumTotalValue = 0;
        for (idx = 0; idx < m_GrassCount; idx++)
        {
            if (arr_draedsum[idx] == 0 && beforeValue != 0)
            {
                prefixSumTotalValue += beforeValue;
                beforeValue = 0;
            }
            beforeValue = arr_draedsum[idx];
        }

        //Debug.Log($"갯수 : {count}, prefixsum : {prefixSumTotalValue}");
        int groupSum = 0;
        int[] arr_drawedGroupSum = new int[m_GroupX];
        m_DrawedGroupSumBuffer.GetData(arr_drawedGroupSum);
        for(int i=0;i< m_GroupX; i++)
        {
            groupSum += arr_drawedGroupSum[i];
        }

        int[] arr_drawedGroupPrefixSum = new int[m_GroupX];
        m_DrawedGroupPrefixSumBuffer.GetData(arr_drawedGroupPrefixSum);
        int groupPrefixSumLastValue = 0;
        groupPrefixSumLastValue = arr_drawedGroupPrefixSum[m_GroupX - 1];

        int[] arr_drawedIdx = new int[m_GrassCount];
        m_DrawedIdxBuffer.GetData(arr_drawedIdx);
        int curValue = -1;
        for(int i=0;i< count; i++)
        {
            if (arr_drawed[arr_drawedIdx[i]] == 0 && count > 0 && arr_drawedIdx[i] != 0)
            {
                Debug.Log($"error isdrawed : {arr_drawed[arr_drawedIdx[i]]} idx : {arr_drawedIdx[i]} i : {i}");
                break;
            }
            if (arr_drawedIdx[i] <= curValue)
            {
                Debug.Log("error");
                break;
            }
            else
            {
                curValue = arr_drawedIdx[i];
            }       
        }

        int[] args = new int[5];
        m_ArgsBuffer.GetData(args);
        Debug.Log($"{args[0]} {args[1]} {args[2]} {args[3]} {args[4]}");

        Debug.Log($"갯수 : {count}, prefixsum {prefixSumTotalValue}, prefixGroupSum {groupSum}, prefixGroupSumLastValue {groupPrefixSumLastValue}");



        //for (int i = 0; i < m_GroupX; i++)
        //{
        //    Debug.Log(m_GroupX + " " + i + " " + arr_drawedGroupPrefixSum[i]);
        //}

        //LogData[] log = new LogData[1];
        //m_LogBuffer.GetData(log);
        //Debug.Log($"Log {log[0].ia}, {log[0].ib}, {log[0].ic}, {log[0].id}, {log[0].ie}");

        //Vector3 testObjPos = m_TestObj.transform.position;
        //Vector4 testWorldPos = new Vector4(testObjPos.x, testObjPos.y, testObjPos.z, 1);


        //Vector4 posView = v * testWorldPos;
        //Vector4 posClip = VP * testWorldPos;
        //Vector3 posNDC;
        //posNDC.x = posClip.x / -posClip.w;
        //posNDC.y = posClip.y / -posClip.w;
        //posNDC.z = -posClip.w;

        //Debug.Log($"view : {posView} posClip : {posClip} posNDC : {posNDC}");
    }

    void DrawInstances()
    {
        Graphics.DrawMeshInstancedIndirect(m_GrassMesh, 0, m_GrassMaterial, m_FieldBounds, m_ArgsBuffer);
    }

    void Release()
    {
        m_GrassBuffer.Release();
        m_ArgsBuffer.Release();
        m_DrawedBuffer.Release();
        m_DrawedPrefixSumBuffer.Release();
        m_DrawedGroupSumBuffer.Release();
        m_DrawedGroupPrefixSumBuffer.Release();
        m_DrawedIdxBuffer.Release();
        m_DrawedGrassBuffer.Release();

        m_LogBuffer.Release();
    }
}
