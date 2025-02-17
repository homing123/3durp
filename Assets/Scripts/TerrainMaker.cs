using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TerrainMaker : MonoBehaviour
{
    public enum E_TerrainQuality
    {
        Ultra = 1,
        High = 2,
        Midium = 4,
        Low = 8,
    }
    public enum E_TerrainMakerKernel
    {
        HeightMap = 0,
        NormalMap = 1,
    }

    public Mesh m_Mesh { get; private set; }
    RenderTexture m_HeightMapForNormalMap;
    int m_LastHeightMapForNormalWidth = 0;

    [SerializeField][Range(11, 101)] int m_VertexWidth;
    [SerializeField] ComputeShader m_CS;
    [SerializeField] [Min(0f)] float m_GradientRadianMul;
    [SerializeField] [Range(32, 1024)] public int m_TexWidth;
    [SerializeField] [Range(1, 10)] float m_Amplitude;
    [SerializeField] [Range(0.01f, 1)] float m_Freaquency;
    [SerializeField] [Range(0.01f, 1)] float m_Persistence;
    [SerializeField] [Range(1.5f, 2.5f)] float m_Lacunarity;
    [SerializeField] [Range(1, 9)] int m_Octave;

    const int Kernel_Width = 32;


  
    public struct TerrainData
    {
        public RenderTexture heightBuffer;
        public RenderTexture normalBuffer;

        public void Release()
        {
            heightBuffer.Release();
            normalBuffer.Release();
        }

    }
    [SerializeField] PerlinNoise.PerlinOption m_PerlinOption;

    public static TerrainMaker Ins;
    private void Awake()
    {
        Ins = this;
        MeshInit();
    }
    public Vector2Int GetHeightMapSize()
    {
        return new Vector2Int(m_TexWidth, m_TexWidth);
    }

    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public float GetTerrainMeshGridSize()
    {
        return (Chunk.ChunkSize.x * (int)TerrainMaker.E_TerrainQuality.Low) / (m_VertexWidth - 1);   }
    public Vector2Int GetTerrainMeshGridKey(Vector2 vt2xz)
    {
        float terrainMeshGridSize = (Chunk.ChunkSize.x * (int)TerrainMaker.E_TerrainQuality.Low) / (m_VertexWidth - 1);
        Vector2 quotient = vt2xz / terrainMeshGridSize;
        return new Vector2Int(Mathf.FloorToInt(quotient.x), Mathf.FloorToInt(quotient.y));
    }
    void MeshInit()
    {
        m_Mesh = new Mesh();
        int verticesCount = m_VertexWidth * m_VertexWidth;
        Vector3[] vertices = new Vector3[verticesCount]; //���ؽ��� �⺻������ �־�����Ѵٰ��ϳ�

        int indicesCount = (m_VertexWidth - 1) * (m_VertexWidth - 1) * 2 * 3; // *2 �� �� �׸�ĭ�� �ﰢ���� 2���� �پ�����, *3�� �ﰢ���ϳ��� ��3��
        int[] indices = new int[indicesCount];
     
        //-groundũ�� / 2 ~ groundũ�� / 2
        Vector3 startPos = new Vector3(-Chunk.ChunkSize.x * 0.5f, 0, -Chunk.ChunkSize.y * 0.5f);
        float dVertex = Chunk.ChunkSize.x / (m_VertexWidth - 1);
        for (int z = 0; z < m_VertexWidth; z++)
        {
            for (int x = 0; x < m_VertexWidth; x++)
            {
                Vector3 curPos = startPos + new Vector3(dVertex * x, 0, dVertex * z);
                int curIdx = x + z * m_VertexWidth;    
                vertices[curIdx] = curPos;

            }
        }
        m_Mesh.vertices = vertices;

        for (int z = 0; z < m_VertexWidth - 1; z++)
        {
            for (int x = 0; x < m_VertexWidth - 1; x++)
            {
                int curIndexIdx = x + z * (m_VertexWidth - 1);
                int curVertexIdx = x + z * m_VertexWidth;

                indices[curIndexIdx * 6 + 0] = curVertexIdx;
                indices[curIndexIdx * 6 + 1] = curVertexIdx + m_VertexWidth;
                indices[curIndexIdx * 6 + 2] = curVertexIdx + 1;
                indices[curIndexIdx * 6 + 3] = curVertexIdx + 1;
                indices[curIndexIdx * 6 + 4] = curVertexIdx + m_VertexWidth;
                indices[curIndexIdx * 6 + 5] = curVertexIdx + 1 + m_VertexWidth;
            }
        }
        m_Mesh.SetIndices(indices, MeshTopology.Triangles, 0);

    }
    public TerrainData GetTerrainData(E_TerrainQuality quality, Vector2Int key)
    {
        //scale = 10, width = 256, offset = 0 �̸� 0~25.6 ������ 2560 * 0.01f = 25.6f
        //groundũ�⸦ width��ŭ���� �������� scale���� �ڵ����� ��������
        Vector2 d = Chunk.ChunkSize / m_TexWidth * (int)quality;
        TerrainData data = new TerrainData();
        Vector2 offset = key * (int)quality * Chunk.ChunkSize;// - d * key * (int)quality;
        float scale = Chunk.ChunkSize.x / m_TexWidth * 100 * (int)quality;

        data.heightBuffer = new RenderTexture(m_TexWidth, m_TexWidth, 0, RenderTextureFormat.RFloat);
        data.heightBuffer.enableRandomWrite = true;
        data.heightBuffer.filterMode = FilterMode.Point;
        data.heightBuffer.wrapMode = TextureWrapMode.Clamp;
        data.normalBuffer = new RenderTexture(m_TexWidth, m_TexWidth, 0, RenderTextureFormat.ARGBFloat);
        data.normalBuffer.enableRandomWrite = true;

        int curHeightMapForNormalWidth = m_TexWidth + 2;
        if (m_LastHeightMapForNormalWidth < curHeightMapForNormalWidth)
        {
            if (m_LastHeightMapForNormalWidth > 0)
            {
                m_HeightMapForNormalMap.Release();
            }
            m_HeightMapForNormalMap = new RenderTexture(curHeightMapForNormalWidth, curHeightMapForNormalWidth, 0, RenderTextureFormat.RFloat);
            m_HeightMapForNormalMap.enableRandomWrite = true;
            m_LastHeightMapForNormalWidth = curHeightMapForNormalWidth;
        }

        m_CS.SetFloat("_GradientRadianMul", m_GradientRadianMul);
        m_CS.SetFloats("_Offset", new float[2] { offset.x, offset.y });
        m_CS.SetInts("_TexSize", new int[2] {m_TexWidth, m_TexWidth});
        m_CS.SetInts("_HeightForNormalTexSize", new int[2] { curHeightMapForNormalWidth , curHeightMapForNormalWidth });
        m_CS.SetFloat("_Scale", scale);
        m_CS.SetFloat("_Amplitude", m_Amplitude);
        m_CS.SetFloat("_Frequency", m_Freaquency);
        m_CS.SetFloat("_Persistence", m_Persistence);
        m_CS.SetFloat("_Lacunarity", m_Lacunarity);
        m_CS.SetInt("_Octaves", m_Octave);
        m_CS.SetFloats("_D", new float[2] { d.x, d.y });
        m_CS.SetTexture((int)E_TerrainMakerKernel.HeightMap, "_HeightMap", data.heightBuffer);
        m_CS.SetTexture((int)E_TerrainMakerKernel.HeightMap, "_HeightMapForNormalMap", m_HeightMapForNormalMap);

        m_CS.SetTexture((int)E_TerrainMakerKernel.NormalMap, "_NormalMap", data.normalBuffer);
        m_CS.SetTexture((int)E_TerrainMakerKernel.NormalMap, "_HeightMapForNormalMap", m_HeightMapForNormalMap);

        int heightForNormalMapGroupCountx = curHeightMapForNormalWidth / Kernel_Width + (curHeightMapForNormalWidth % Kernel_Width == 0 ? 0 : 1);
        int heightForNormalMapGroupCounty = curHeightMapForNormalWidth / Kernel_Width + (curHeightMapForNormalWidth % Kernel_Width == 0 ? 0 : 1);
        int normalMapGroupCountx = m_TexWidth / Kernel_Width + (m_TexWidth % Kernel_Width == 0 ? 0 : 1);
        int normalMapGroupCounty = m_TexWidth / Kernel_Width + (m_TexWidth % Kernel_Width == 0 ? 0 : 1);
        //Debug.Log(quality + " " + key + " " + offset+" " + heightMapGroupCountx+" " + heightMapGroupCounty+" " + scale);

        m_CS.Dispatch((int)E_TerrainMakerKernel.HeightMap, heightForNormalMapGroupCountx, heightForNormalMapGroupCounty, 1);
        m_CS.Dispatch((int)E_TerrainMakerKernel.NormalMap, normalMapGroupCountx, normalMapGroupCounty, 1);

        return data;

    }

    public void DebugRenderTexturePixels(RenderTexture rt)
    {
        // ���� Ȱ��ȭ�� RenderTexture�� ����
        RenderTexture currentRT = RenderTexture.active;

        // �ӽ� Texture2D ����
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);

        try
        {
            // RenderTexture�� Ȱ��ȭ
            RenderTexture.active = rt;

            // RenderTexture�� ������ Texture2D�� �о��
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            // �ȼ� ������ ��������
            Color[] pixels = tex.GetPixels();

            // �ȼ� �� ���
            for (int y = 0; y < rt.height; y++)
            {
                for (int x = 0; x < rt.width; x++)
                {
                    int index = y * rt.width + x;
                    Color pixel = pixels[index];
                    if (x == 0 && y == 0)
                    {
                        Debug.Log($"Pixel [{x}, {y}]: R={pixel.r:F3}, G={pixel.g:F3}, B={pixel.b:F3}, A={pixel.a:F3}");
                    }
                }
            }
        }
        finally
        {
            // ����
            RenderTexture.active = currentRT;
            Object.Destroy(tex);
        }
    }
}
