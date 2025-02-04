using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMaker : MonoBehaviour
{
    public struct ChunkTerrainData
    {
        public ComputeBuffer heightBuffer;
        public float[] arr_Height;
        public Mesh[] arr_MeshLOD;

    }
    [SerializeField] PerlinNoise.PerlinOption m_PerlinOption;

    public static TerrainMaker Ins;
    private void Awake()
    {
        Ins = this;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public ChunkTerrainData GetChunkTerrainData(Vector2 groundPos)
    {
        //scale = 10, width = 256, offset = 0 이면 0~25.6 까지임 2560 * 0.01f = 25.6f
        //ground크기를 width만큼으로 쓰기위해 scale값을 자동으로 조절하자
        ChunkTerrainData data = new ChunkTerrainData();
        m_PerlinOption.offset = groundPos;
        m_PerlinOption.scale = Ground.GroundWidth / m_PerlinOption.width * 100;
        data.heightBuffer = PerlinNoise.PerlinNoiseGPU(m_PerlinOption, PerlinNoise.E_PerlinBufferType.Height);
        data.arr_Height = new float[m_PerlinOption.width * m_PerlinOption.height];
        data.heightBuffer.GetData(data.arr_Height);
        data.arr_MeshLOD = new Mesh[2];

        int[] AxisVertexCount = new int[2] { 21, 81 };
        
        for(int i=0;i< data.arr_MeshLOD.Length;i++)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[AxisVertexCount[i] * AxisVertexCount[i]];
            Vector2[] uvs = new Vector2[AxisVertexCount[i] * AxisVertexCount[i]];
            Vector3[] normals = new Vector3[AxisVertexCount[i] * AxisVertexCount[i]];
            //-ground크기 / 2 ~ ground크기 / 2
            Vector3 startPos = new Vector3(-Ground.GroundWidth * 0.5f, 0, -Ground.GroundWidth * 0.5f);
            float dVertex = Ground.GroundWidth / (AxisVertexCount[i] - 1);
            float dUV = 1.0f / (AxisVertexCount[i] - 1);
            for (int z = 0; z < AxisVertexCount[i];z++)
            {
                for(int x = 0; x < AxisVertexCount[i];x++)
                {
                    Vector2 curUV = new Vector2(dUV * x, dUV * z);
                    int heightMapIdx_x = Mathf.RoundToInt(curUV.x * m_PerlinOption.width);
                    heightMapIdx_x = heightMapIdx_x == m_PerlinOption.width ? heightMapIdx_x - 1 : heightMapIdx_x;
                    int heightMapIdx_y = Mathf.RoundToInt(curUV.y * m_PerlinOption.height);
                    heightMapIdx_y = heightMapIdx_y == m_PerlinOption.height ? heightMapIdx_y - 1 : heightMapIdx_y;
                    int heightMapIdx = heightMapIdx_x + heightMapIdx_y * m_PerlinOption.width;
                    float curHeight = data.arr_Height[heightMapIdx];
                    Vector3 curPos = startPos + new Vector3(dVertex * x, curHeight, dVertex * z);
                    int curIdx = x + z * AxisVertexCount[i];
                    Vector3 curNormal = new Vector3(0, 1, 0);
                    vertices[curIdx] = curPos;
                    uvs[curIdx] = curUV;
                    normals[curIdx] = curNormal;
                }
            }
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;

            int[] indices = new int[(AxisVertexCount[i] - 1) * (AxisVertexCount[i] - 1) * 2 * 3];
            for (int z = 0; z < AxisVertexCount[i] - 1; z++)
            {
                for (int x = 0; x < AxisVertexCount[i] - 1; x++)
                {
                    int curIndexIdx = x + z * (AxisVertexCount[i] - 1);
                    int curVertexIdx = x + z * AxisVertexCount[i];

                    indices[curIndexIdx * 6 + 0] = curVertexIdx;
                    indices[curIndexIdx * 6 + 1] = curVertexIdx + AxisVertexCount[i];
                    indices[curIndexIdx * 6 + 2] = curVertexIdx + 1; 
                    indices[curIndexIdx * 6 + 3] = curVertexIdx + 1; 
                    indices[curIndexIdx * 6 + 4] = curVertexIdx + AxisVertexCount[i];
                    indices[curIndexIdx * 6 + 5] = curVertexIdx + 1 + AxisVertexCount[i];

                }
            }
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            data.arr_MeshLOD[i] = mesh;
        }
        return data;
    }
}
