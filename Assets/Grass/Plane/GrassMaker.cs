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

        m_GrassBuffer = new ComputeBuffer(grassAxisCount, sizeof(float) * 3);

        m_CSGrassPoint.SetInt("_GrassAmount", m_GrassAmount);
        m_CSGrassPoint.SetInt("_Scale", m_Scale);
        m_CSGrassPoint.SetFloats("_GrassAmount", new float[3] { m_Position.x, m_Position.y, m_Position.z });
        m_CSGrassPoint.SetBuffer(0, "GrassBuffer", m_GrassBuffer);
        m_CSGrassPoint.Dispatch(0, threadCount, threadCount, 1);


        m_FieldBounds = new Bounds(m_Position, new Vector3(m_Scale, 5, m_Scale));
    }

    void DrawInstances()
    {
        Graphics.DrawMeshInstancedIndirect(m_GrassMesh, 0, m_GrassMaterial, m_FieldBounds, m_MeshBuffer);
    }
}
