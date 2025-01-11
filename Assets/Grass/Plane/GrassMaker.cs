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


    [SerializeField] ComputeShader m_CSGrassPoint;
    [SerializeField] ComputeShader m_CSFrustumCulling;
    ComputeBuffer m_DrawedBuffer;
    ComputeBuffer m_GrassBuffer;

    bool m_isInfoChanged = true;
    const int GrassPosThreadDimension = 32;
    const int GrassPosThreadCountInGroup = GrassPosThreadDimension * GrassPosThreadDimension;

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
        if(m_GrassCount > GrassPosThreadCountInGroup * 512)
        {
            int remain = m_GrassCount % (GrassPosThreadCountInGroup * 512);
            int diminish = m_GrassCount / (GrassPosThreadCountInGroup * 512);
            m_GroupY = remain > 0 ? diminish + 1 : diminish;
            //GrassPosThreadCountInGroup * 512 개를 넘어가면 비효율적인 로직임 일단 임시로 이렇게 해둠 50만개는 안넘길듯ㅋㅋ
            m_GroupX = 512;
        }
        else
        {
            int remain = m_GrassCount & GrassPosThreadCountInGroup;
            int diminish = m_GrassCount / GrassPosThreadCountInGroup;
            m_GroupY = 1;
            m_GroupX = remain > 0 ? diminish + 1 : diminish;
            m_GroupX = m_GroupX == 0 ? 1 : m_GroupX;
        }
        Debug.Log(m_GroupX + " " + m_GroupY);
        int structSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(GrassData));

        m_GrassBuffer = new ComputeBuffer(m_GrassCount, structSize);

        m_CSGrassPoint.SetInt("_GrassAmount", m_GrassCountPerOne);
        m_CSGrassPoint.SetInt("_Scale", m_Scale);
        m_CSGrassPoint.SetVector("_Position", m_Position);
        m_CSGrassPoint.SetBuffer(0, "_GrassBuffer", m_GrassBuffer);

        int logstSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(LogData));
        m_LogBuffer = new ComputeBuffer(m_GrassCount, logstSize);
        m_CSGrassPoint.SetBuffer(0, "_LogBuffer", m_LogBuffer);
        m_CSGrassPoint.Dispatch(0, m_GroupX, m_GroupY, 1);
       
        m_DrawedBuffer = new ComputeBuffer(m_GrassCount, sizeof(int));

        m_FieldBounds = new Bounds(m_Position, new Vector3(m_Scale, 5, m_Scale));

        //GrassData[] arr_GrassBuffer = new GrassData[m_GrassCount];
        //m_GrassBuffer.GetData(arr_GrassBuffer);

        //for (int i = 0; i < m_GrassCount; i++)
        //{
        //    Debug.Log(i+"번 "+arr_GrassBuffer[i].position);
        //}
        //LogData[] arr_log = new LogData[m_GrassCount];
        //m_LogBuffer.GetData(arr_log);
        //for (int i = 0; i < 1; i++)
        //{
          
        //        Debug.Log("로그" + i + "번째" + arr_log[i].ia + ", " + arr_log[i].ib + ", " + arr_log[i].ic + ", " + arr_log[i].id + ", " + arr_log[i].ie);
            
        //}
    }
    void FrustumCull()
    {
        m_CSFrustumCulling.SetInt("_GrassAmount", m_GrassCountPerOne);
        m_CSFrustumCulling.SetInt("_Scale", m_Scale);
        m_CSFrustumCulling.SetVector("_CamPos", Camera.main.transform.position);
        m_CSFrustumCulling.SetFloat("_RenderDis", m_RenderDis);

        Matrix4x4 p = Camera.main.projectionMatrix;
        Matrix4x4 v = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = p * v;
        m_CSFrustumCulling.SetMatrix("_MatVP", VP);
        m_CSFrustumCulling.SetBuffer(0, "_GrassBuffer", m_GrassBuffer);
        m_CSFrustumCulling.SetBuffer(0, "_DrawedBuffer", m_DrawedBuffer);
        m_CSFrustumCulling.Dispatch(0, m_GroupX, m_GroupY, 1);

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
        Debug.Log(count);
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
        m_LogBuffer.Release();
    }
}
