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
    void MeshInit()
    {
        m_Mesh = new Mesh();
        int verticesCount = m_VertexWidth * m_VertexWidth;
        Vector3[] vertices = new Vector3[verticesCount]; //버텍스는 기본적으로 넣어줘야한다고하네

        int indicesCount = (m_VertexWidth - 1) * (m_VertexWidth - 1) * 2 * 3; // *2 는 한 네모칸에 삼각형은 2개씩 붙어있음, *3은 삼각형하나에 점3개
        int[] indices = new int[indicesCount];
     
        //-ground크기 / 2 ~ ground크기 / 2
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
        //scale = 10, width = 256, offset = 0 이면 0~25.6 까지임 2560 * 0.01f = 25.6f
        //ground크기를 width만큼으로 쓰기위해 scale값을 자동으로 조절하자
        Vector2 d = Chunk.ChunkSize / m_TexWidth;
        TerrainData data = new TerrainData();
        Vector2 offset = key * (int)quality * Chunk.ChunkSize;// - d * key * (int)quality;
        float scale = Chunk.ChunkSize.x / m_TexWidth * 100 * (int)quality;

        data.heightBuffer = new RenderTexture(m_TexWidth, m_TexWidth, 0, RenderTextureFormat.RFloat);
        data.heightBuffer.enableRandomWrite = true;
        data.heightBuffer.filterMode = FilterMode.Point;
        data.heightBuffer.wrapMode = TextureWrapMode.Clamp;
        data.normalBuffer = new RenderTexture(m_TexWidth, m_TexWidth, 0, RenderTextureFormat.ARGB32);
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
        m_CS.SetTexture((int)E_TerrainMakerKernel.HeightMap, "_NormalMap", data.normalBuffer);

        m_CS.SetTexture((int)E_TerrainMakerKernel.NormalMap, "_NormalMap", data.normalBuffer);
        m_CS.SetTexture((int)E_TerrainMakerKernel.NormalMap, "_HeightMapForNormalMap", m_HeightMapForNormalMap);
        m_CS.SetTexture((int)E_TerrainMakerKernel.NormalMap, "_HeightMap", data.heightBuffer);


        int heightForNormalMapGroupCountx = curHeightMapForNormalWidth / Kernel_Width + (curHeightMapForNormalWidth % Kernel_Width == 0 ? 0 : 1);
        int heightForNormalMapGroupCounty = curHeightMapForNormalWidth / Kernel_Width + (curHeightMapForNormalWidth % Kernel_Width == 0 ? 0 : 1);
        int normalMapGroupCountx = m_TexWidth / Kernel_Width + (m_TexWidth % Kernel_Width == 0 ? 0 : 1);
        int normalMapGroupCounty = m_TexWidth / Kernel_Width + (m_TexWidth % Kernel_Width == 0 ? 0 : 1);
        //Debug.Log(quality + " " + key + " " + offset+" " + heightMapGroupCountx+" " + heightMapGroupCounty+" " + scale);

        m_CS.Dispatch((int)E_TerrainMakerKernel.HeightMap, heightForNormalMapGroupCountx, heightForNormalMapGroupCounty, 1);
        //m_CS.Dispatch((int)E_TerrainMakerKernel.NormalMap, normalMapGroupCountx, normalMapGroupCounty, 1);

        //RenderTexture.active = data.normalBuffer;  // RenderTexture 활성화

        //Texture2D tex = new Texture2D(data.normalBuffer.width, data.normalBuffer.height, TextureFormat.ARGB32, false);
        //tex.ReadPixels(new Rect(0, 0, data.normalBuffer.width, data.normalBuffer.height), 0, 0); // 데이터를 읽어옴
        //tex.Apply();

        //Color pixelColor = tex.GetPixel(0,0);
        //Debug.Log($"0,0 Pixel Color: {pixelColor}");

        //RenderTexture.active = null;  // 원래 상태로 복구


        return data;

    }
    //public ChunkTerrainData GetChunkTerrainData(Vector2 groundPos)
    //{
    //    //scale = 10, width = 256, offset = 0 이면 0~25.6 까지임 2560 * 0.01f = 25.6f
    //    //ground크기를 width만큼으로 쓰기위해 scale값을 자동으로 조절하자
    //    ChunkTerrainData data = new ChunkTerrainData();
    //    PerlinNoise.PerlinOption option = m_PerlinOption;
    //    option.offset = groundPos;
    //    option.scale = Chunk.ChunkSize.x / m_PerlinOption.width * 100;
    //    option.width = m_PerlinOption.width + 3; // +1 은 맵이 이어지기위해필요, +2은 heightMap -> normalMap reduce로 진행하기때문에 필요 => height가 4x4 면 normal은 2x2로 나옴
    //    option.height = m_PerlinOption.height + 3;

    //    data.heightBuffer = PerlinNoise.PerlinNoiseGPU(option, PerlinNoise.E_PerlinBufferType.Height);
    //    data.normalBuffer = NormalMapMaker.HeightMapToNormalMapGPU(option.width - 2, option.height - 2, data.heightBuffer, NormalMapMaker.E_NormalBufferType.FloatToVector3, Chunk.ChunkSize, NormalMapMaker.E_NormalSideType.Reduce);
    //    data.arr_Height = new float[option.width * option.height];
    //    data.arr_Normal = new Vector3[(option.width - 2) * (option.height - 2)];
    //    data.heightBuffer.GetData(data.arr_Height);
    //    data.normalBuffer.GetData(data.arr_Normal);
    //    data.arr_MeshLOD = new Mesh[1];
    //    data.HeightBufferSize = new Vector2Int(option.width, option.height);


    //    int[] AxisVertexCount = new int[1] { 21 };
         
    //    for(int i=0;i< data.arr_MeshLOD.Length;i++)
    //    {
    //        Mesh mesh = new Mesh();
    //        Vector3[] vertices = new Vector3[AxisVertexCount[i] * AxisVertexCount[i]];
    //        Vector2[] uvs = new Vector2[AxisVertexCount[i] * AxisVertexCount[i]];
    //        Vector3[] normals = new Vector3[AxisVertexCount[i] * AxisVertexCount[i]];
    //        //-ground크기 / 2 ~ ground크기 / 2
    //        Vector3 startPos = new Vector3(-Chunk.ChunkSize.x * 0.5f, 0, -Chunk.ChunkSize.y * 0.5f);
    //        float dVertex = Chunk.ChunkSize.x / (AxisVertexCount[i] - 1);
    //        float dUV = 1.0f / (AxisVertexCount[i] - 1);
    //        for (int z = 0; z < AxisVertexCount[i];z++)
    //        {
    //            for(int x = 0; x < AxisVertexCount[i];x++)
    //            {
    //                Vector2 curUV = new Vector2(dUV * x, dUV * z);
    //                int heightMapIdx_x = Mathf.RoundToInt(curUV.x * (option.width - 2)) + 1;
    //                heightMapIdx_x = heightMapIdx_x == option.width - 1 ? heightMapIdx_x - 1 : heightMapIdx_x;
    //                int heightMapIdx_y = Mathf.RoundToInt(curUV.y * (option.height - 2)) + 1;
    //                heightMapIdx_y = heightMapIdx_y == option.height - 1 ? heightMapIdx_y - 1 : heightMapIdx_y;
    //                int heightMapIdx = heightMapIdx_x + heightMapIdx_y * option.width;
    //                int normalMapIdx = (heightMapIdx_x - 1) + (heightMapIdx_y - 1) * (option.width - 2);
    //                float curHeight = data.arr_Height[heightMapIdx];
    //                Vector3 curPos = startPos + new Vector3(dVertex * x, curHeight, dVertex * z);
    //                int curIdx = x + z * AxisVertexCount[i];
    //                Vector3 curNormal = data.arr_Normal[normalMapIdx];
    //                curNormal = new Vector3(curNormal.x, curNormal.z, curNormal.y); //z와 y 바꿔야함 tangent bitangent 할필요없음
    //                vertices[curIdx] = curPos;
    //                uvs[curIdx] = curUV;
    //                normals[curIdx] = curNormal;
    //            }
    //        }
    //        mesh.vertices = vertices;
    //        mesh.uv = uvs;
    //        mesh.normals = normals;

    //        int[] indices = new int[(AxisVertexCount[i] - 1) * (AxisVertexCount[i] - 1) * 2 * 3];
    //        for (int z = 0; z < AxisVertexCount[i] - 1; z++)
    //        {
    //            for (int x = 0; x < AxisVertexCount[i] - 1; x++)
    //            {
    //                int curIndexIdx = x + z * (AxisVertexCount[i] - 1);
    //                int curVertexIdx = x + z * AxisVertexCount[i];

    //                indices[curIndexIdx * 6 + 0] = curVertexIdx;
    //                indices[curIndexIdx * 6 + 1] = curVertexIdx + AxisVertexCount[i];
    //                indices[curIndexIdx * 6 + 2] = curVertexIdx + 1; 
    //                indices[curIndexIdx * 6 + 3] = curVertexIdx + 1; 
    //                indices[curIndexIdx * 6 + 4] = curVertexIdx + AxisVertexCount[i];
    //                indices[curIndexIdx * 6 + 5] = curVertexIdx + 1 + AxisVertexCount[i];

    //            }
    //        }
    //        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

    //        data.arr_MeshLOD[i] = mesh;
    //    }
    //    return data;
    //}
}
