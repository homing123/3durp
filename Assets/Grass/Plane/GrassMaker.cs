using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassMaker : MonoBehaviour
{
    private uint[] m_MeshData = new uint[5];
    ComputeBuffer m_MeshBuffer;
    Mesh m_GrassMesh;
    Bounds m_FieldBounds;
    struct GrassData
    {
        public Vector3 position;
    }
    public Vector3 m_Position;
    public int m_GrassAmount;
    public int m_Scale;
    public Material m_GrassMaterial;


    [SerializeField] ComputeShader m_CSGrassPoint;
    ComputeBuffer m_GrassBuffer;

    bool m_isInfoChanged = true;

    private void Start()
    {
        InitMesh();
        Update();
    }
    private void Update()
    {
        if(m_isInfoChanged == true)
        {
            InitCSBuffer();
            InitArgsBuffer();
            m_isInfoChanged = false;
        }

        DrawInstances();
    }

    void InitMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[4] {new Vector3(-0.5f, 0.5f, 0)
        , new Vector3 (0.5f, 0.5f, 0)
        , new Vector3 (-0.5f, -0.5f, 0)
        , new Vector3 (0.5f, -0.5f, 0) };
        mesh.SetIndices(new int[4] { 0, 1, 3, 2 }, MeshTopology.Quads, 0);
        mesh.uv = new Vector2[4] {
            new Vector2 (0,1), new Vector2 ( 1,1),new Vector2 (0,0) ,new Vector2 (1,0) };
        mesh.normals = new Vector3[4]
        {
            new Vector3(0,0,-1), new Vector3(0,0,-1), new Vector3(0,0,-1), new Vector3(0,0,-1)
        };

        m_GrassMesh = mesh;
    }
    void InitArgsBuffer()
    {
        if(m_MeshBuffer != null)
        {
            m_MeshBuffer.Release();
        }
        m_MeshBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        m_MeshData[0] = (uint)m_GrassMesh.GetIndexCount(0);
        m_MeshData[1] = (uint)0;
        m_MeshData[2] = (uint)m_GrassMesh.GetIndexStart(0);
        m_MeshData[3] = (uint)m_GrassMesh.GetBaseVertex(0);
        m_MeshData[4] = 0;

        m_MeshBuffer.SetData(m_MeshData);
    }


    void InitCSBuffer()
    {
        int grassAxisCount = m_GrassAmount * m_Scale;
        int threadCount = grassAxisCount >> 5;
        threadCount = threadCount + 1;
        threadCount = threadCount << 5;

        m_GrassBuffer = new ComputeBuffer(grassAxisCount * grassAxisCount, sizeof(float) * 3);

        m_CSGrassPoint.SetInt("_GrassAmount", m_GrassAmount);
        m_CSGrassPoint.SetInt("_Scale", m_Scale);
        m_CSGrassPoint.SetVector("_Position", m_Position);
        m_CSGrassPoint.SetBuffer(0, "GrassBuffer", m_GrassBuffer);
        m_CSGrassPoint.Dispatch(0, threadCount, threadCount, 1);


        m_FieldBounds = new Bounds(m_Position, new Vector3(m_Scale, 5, m_Scale));

        //GrassData[] arr_GrassBuffer = new GrassData[grassAxisCount * grassAxisCount];
        //m_GrassBuffer.GetData(arr_GrassBuffer);

        //for(int i=0;i< grassAxisCount * grassAxisCount; i++)
        //{
        //    Debug.Log(arr_GrassBuffer[i].position);
        //}
    }

    void DrawInstances()
    {
        Graphics.DrawMeshInstancedIndirect(m_GrassMesh, 0, m_GrassMaterial, m_FieldBounds, m_MeshBuffer);
    }
}
