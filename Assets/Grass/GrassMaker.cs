using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class GrassMaker : MonoBehaviour
{
    //struct LogData
    //{
    //    public int ia;
    //    public int ib;
    //    public int ic;
    //    public int id;
    //    public int ie;
    //}
    //ComputeBuffer m_LogBuffer;
    private uint[] m_ArgsData = new uint[5];
    ComputeBuffer m_ArgsBuffer;
    Mesh m_GrassMesh;
    Bounds m_FieldBounds;

    public GameObject m_TestObj;
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


    [SerializeField] ComputeShader m_CSGrassPoint;
    [SerializeField] ComputeShader m_CSFrustumCulling;
    ComputeBuffer m_DrawedBuffer;
    ComputeBuffer m_GrassBuffer;
    ComputeBuffer m_DrawedSumBuffer;

    bool m_isInfoChanged = true;
    const int ThreadMax = 256;
    const int GroupMaxX = 512;


    int m_GrassAxisCount;
    int m_GrassCount;
    int m_GroupX;
    int m_GroupY;

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
            InitCSBuffer();
            InitArgsBuffer();
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
        m_GrassMaterial.SetBuffer("_GrassData", m_GrassBuffer);

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
        m_ArgsData[1] = (uint)m_GrassCount;
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
            int remain = m_GrassCount % (ThreadMax * GroupMaxX);
            int diminish = m_GrassCount / (ThreadMax * GroupMaxX);
            m_GroupY = remain > 0 ? diminish + 1 : diminish;
            //GrassPosThreadCountInGroup * 512 개를 넘어가면 비효율적인 로직임 일단 임시로 이렇게 해둠 25만개는 안넘길듯ㅋㅋ
            m_GroupX = 512;
        }
        else
        {
            int remain = m_GrassCount & ThreadMax;
            int diminish = m_GrassCount / ThreadMax;
            m_GroupY = 1;
            m_GroupX = remain > 0 ? diminish + 1 : diminish;
            m_GroupX = m_GroupX == 0 ? 1 : m_GroupX;
        }
        //Debug.Log(m_GroupX + " " + m_GroupY);
        int structSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(GrassData));

        m_GrassBuffer = new ComputeBuffer(m_GrassCount, structSize);

        m_CSGrassPoint.SetInt("_GrassAmount", m_GrassCountPerOne);
        m_CSGrassPoint.SetInt("_Scale", m_Scale);
        m_CSGrassPoint.SetVector("_Position", m_Position);
        m_CSGrassPoint.SetBuffer(0, "_GrassBuffer", m_GrassBuffer);
        m_CSGrassPoint.Dispatch(0, m_GroupX, m_GroupY, 1);
       
        m_DrawedBuffer = new ComputeBuffer(m_GrassCount, sizeof(int));
        m_DrawedSumBuffer = new ComputeBuffer(m_GrassCount, sizeof(int));
        m_FieldBounds = new Bounds(m_Position, new Vector3(m_Scale, 5, m_Scale));
    }
    void FrustumCull()
    {
        Matrix4x4 p = Camera.main.projectionMatrix;
        Matrix4x4 v = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = p * v;

        //Cull
        m_CSFrustumCulling.SetInt("_GrassAmount", m_GrassCountPerOne);
        m_CSFrustumCulling.SetInt("_Scale", m_Scale);
        m_CSFrustumCulling.SetVector("_CamPos", Camera.main.transform.position);
        m_CSFrustumCulling.SetFloat("_RenderDis", m_RenderDis);

        m_CSFrustumCulling.SetMatrix("_MatVP", VP);
        m_CSFrustumCulling.SetBuffer(0, "_GrassBuffer", m_GrassBuffer);
        m_CSFrustumCulling.SetBuffer(0, "_DrawedBuffer", m_DrawedBuffer);
        m_CSFrustumCulling.Dispatch(0, m_GroupX, m_GroupY, 1);

        //PrefixSum
        m_CSFrustumCulling.SetBuffer(1, "_DrawedBuffer", m_DrawedBuffer);
        m_CSFrustumCulling.SetBuffer(1, "_DrawedSumBuffer", m_DrawedSumBuffer);
        m_CSFrustumCulling.Dispatch(1, m_GroupX, m_GroupY, 1);
        m_GrassMaterial.SetBuffer("_DrawedBuffer", m_DrawedBuffer);






        int[] arr_drawed = new int[m_GrassCount];
        m_DrawedBuffer.GetData(arr_drawed);
        int count = 0;
        for(int i=0;i<m_GrassCount;i++)
        {
            if (arr_drawed[i] == 1)
            {
                count += 1;
            }
        }
        int[] arr_draedsum = new int[m_GrassCount];
        m_DrawedSumBuffer.GetData(arr_draedsum);
        int idx = 0;
        int beforeValue = 0;
        int prefixSumTotalValue = 0;
        for(idx = 0; idx < m_GrassCount; idx++)
        {
            if (arr_draedsum[idx] == 0 && beforeValue != 0) 
            {
                prefixSumTotalValue += beforeValue;
                beforeValue = 0;
            }
            beforeValue = arr_draedsum[idx];
        }
    
        Debug.Log($"갯수 : {count}, prefixsum : {prefixSumTotalValue}");
        
        //Debug.Log(count);

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
    }
}
