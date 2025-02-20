using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TerrainMaker : MonoBehaviour
{
    public enum E_TerrainMakerKernel
    {
        HeightMap = 0,
        NormalMap = 1,
    }

    public Mesh m_Mesh { get; private set; }
    RenderTexture m_HeightMapForNormalMap;
    int m_LastHeightMapForNormalWidth = 0;
    [SerializeField] ComputeShader m_CSTerrainMaker;

    [Range(10, 100)] public int m_MeshSize;
    [SerializeField][Range(11, 101)] int m_VertexWidth;
    [SerializeField] [Min(0f)] float m_GradientRadianMul;
    [SerializeField] [Range(1, 8)] int m_TexWidthMul;
    public int m_TexWidth { get; private set; }
    [SerializeField] [Range(1, 10)] float m_Amplitude;
    [SerializeField] [Range(0.01f, 1)] float m_Freaquency;
    [SerializeField] [Range(0.01f, 1)] float m_Persistence;
    [SerializeField] [Range(1.5f, 2.5f)] float m_Lacunarity;
    [SerializeField] [Range(1, 9)] int m_Octave;

    public Vector2Int HeightMapSize
    {
        get
        {
            return new Vector2Int(m_TexWidth, m_TexWidth);
        }
    }
    public float TerrainMeshGridSize
    {
        get
        {
            return (m_MeshSize * (1 << (MapMaker.TerrainCount - 1))) / (float)(m_VertexWidth - 1);
        }
    }
    const int Kernel_Width = 32;

    public static TerrainMaker Ins;
    private void Awake()
    {
        Ins = this;
        m_TexWidth = (m_VertexWidth - 1) * m_TexWidthMul;
        MeshInit();
    }

    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
   
    public Vector2Int GetTerrainMeshGridKey(Vector2 vt2xz)
    {
        Vector2 quotient = vt2xz / TerrainMeshGridSize;
        return new Vector2Int(Mathf.FloorToInt(quotient.x), Mathf.FloorToInt(quotient.y));
    }
    void MeshInit()
    {
        m_Mesh = new Mesh();
        int verticesCount = m_VertexWidth * m_VertexWidth;
        Vector3[] vertices = new Vector3[verticesCount]; //버텍스는 기본적으로 넣어줘야한다고하네

        int indicesCount = (m_VertexWidth - 1) * (m_VertexWidth - 1) * 2 * 3; // *2 는 한 네모칸에 삼각형은 2개씩 붙어있음, *3은 삼각형하나에 점3개
        int[] indices = new int[indicesCount];
     
        //-ground크기 / 2 ~ ground크기 / 2
        Vector3 startPos = new Vector3(-m_MeshSize * 0.5f, 0, -m_MeshSize * 0.5f);
        float dVertex = m_MeshSize / (float)(m_VertexWidth - 1);
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
    public TerrainData GetTerrainData(int quality, Vector2Int key)
    {
        //scale = 10, width = 256, offset = 0 이면 0~25.6 까지임 2560 * 0.01f = 25.6f
        //ground크기를 width만큼으로 쓰기위해 scale값을 자동으로 조절하자
        float d = m_MeshSize / (float)m_TexWidth * quality;
        TerrainData data = new TerrainData();
        Vector2 offset = key * quality * m_MeshSize;// - d * key * (int)quality;
        float scale = m_MeshSize / (float)m_TexWidth * 100 * quality;

        data.heightTexture = new RenderTexture(m_TexWidth, m_TexWidth, 0, RenderTextureFormat.RFloat);
        data.heightTexture.enableRandomWrite = true;
        data.heightTexture.filterMode = FilterMode.Point;
        data.heightTexture.wrapMode = TextureWrapMode.Clamp;
        data.normalTexture = new RenderTexture(m_TexWidth, m_TexWidth, 0, RenderTextureFormat.ARGBFloat);
        data.normalTexture.filterMode = FilterMode.Bilinear;
        data.normalTexture.enableRandomWrite = true;

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


        m_CSTerrainMaker.SetFloat("_GradientRadianMul", m_GradientRadianMul);
        m_CSTerrainMaker.SetFloats("_Offset", new float[2] { offset.x, offset.y });
        m_CSTerrainMaker.SetInts("_TexSize", new int[2] {m_TexWidth, m_TexWidth});
        m_CSTerrainMaker.SetInts("_HeightForNormalTexSize", new int[2] { curHeightMapForNormalWidth , curHeightMapForNormalWidth });
        m_CSTerrainMaker.SetFloat("_Scale", scale);
        m_CSTerrainMaker.SetFloat("_Amplitude", m_Amplitude);
        m_CSTerrainMaker.SetFloat("_Frequency", m_Freaquency);
        m_CSTerrainMaker.SetFloat("_Persistence", m_Persistence);
        m_CSTerrainMaker.SetFloat("_Lacunarity", m_Lacunarity);
        m_CSTerrainMaker.SetInt("_Octaves", m_Octave);
        m_CSTerrainMaker.SetFloats("_D", new float[2] { d, d });
        m_CSTerrainMaker.SetTexture((int)E_TerrainMakerKernel.HeightMap, "_HeightMap", data.heightTexture);
        m_CSTerrainMaker.SetTexture((int)E_TerrainMakerKernel.HeightMap, "_HeightMapForNormalMap", m_HeightMapForNormalMap);
        m_CSTerrainMaker.SetTexture((int)E_TerrainMakerKernel.NormalMap, "_NormalMap", data.normalTexture);
        m_CSTerrainMaker.SetTexture((int)E_TerrainMakerKernel.NormalMap, "_HeightMapForNormalMap", m_HeightMapForNormalMap);

        int heightForNormalMapGroupCountx = curHeightMapForNormalWidth / Kernel_Width + (curHeightMapForNormalWidth % Kernel_Width == 0 ? 0 : 1);
        int heightForNormalMapGroupCounty = curHeightMapForNormalWidth / Kernel_Width + (curHeightMapForNormalWidth % Kernel_Width == 0 ? 0 : 1);
        int normalMapGroupCountx = m_TexWidth / Kernel_Width + (m_TexWidth % Kernel_Width == 0 ? 0 : 1);
        int normalMapGroupCounty = m_TexWidth / Kernel_Width + (m_TexWidth % Kernel_Width == 0 ? 0 : 1);
        //Debug.Log(quality + " " + key + " " + offset+" " + heightMapGroupCountx+" " + heightMapGroupCounty+" " + scale);

        m_CSTerrainMaker.Dispatch((int)E_TerrainMakerKernel.HeightMap, heightForNormalMapGroupCountx, heightForNormalMapGroupCounty, 1);
        m_CSTerrainMaker.Dispatch((int)E_TerrainMakerKernel.NormalMap, normalMapGroupCountx, normalMapGroupCounty, 1);

        return data;

    }

    public static void DebugRenderTexturePixels(RenderTexture rt)
    {
        // 현재 활성화된 RenderTexture를 저장
        RenderTexture currentRT = RenderTexture.active;

        // 임시 Texture2D 생성
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);

        try
        {
            // RenderTexture를 활성화
            RenderTexture.active = rt;

            // RenderTexture의 내용을 Texture2D로 읽어옴
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            // 픽셀 데이터 가져오기
            Color[] pixels = tex.GetPixels();

            // 픽셀 값 출력
            for (int y = 0; y < rt.height; y++)
            {
                for (int x = 0; x < rt.width; x++)
                {
                    int index = y * rt.width + x;
                    Color pixel = pixels[index];
                    
                    Debug.Log($"Pixel [{x}, {y}]: R={pixel.r:F3}, G={pixel.g:F3}, B={pixel.b:F3}, A={pixel.a:F3}");
                    
                }
            }
        }
        finally
        {
            // 정리
            RenderTexture.active = currentRT;
            Object.Destroy(tex);
        }
    }
}
