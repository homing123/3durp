using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMaker : MonoBehaviour
{
    public struct ChunkTerrainData
    {
        public ComputeBuffer heightBuffer;
        public ComputeBuffer normalBuffer;
        public float[] arr_Height;
        public Vector3[] arr_Normal;
        public Mesh[] arr_MeshLOD;
        public Vector2Int HeightBufferSize;

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
        PerlinNoise.PerlinOption option = m_PerlinOption;
        option.offset = groundPos;
        option.scale = Ground.GroundSize.x / m_PerlinOption.width * 100;
        option.width = m_PerlinOption.width + 3; // +1 은 맵이 이어지기위해필요, +2은 heightMap -> normalMap reduce로 진행하기때문에 필요 => height가 4x4 면 normal은 2x2로 나옴
        option.height = m_PerlinOption.height + 3;

        data.heightBuffer = PerlinNoise.PerlinNoiseGPU(option, PerlinNoise.E_PerlinBufferType.Height);
        data.normalBuffer = NormalMapMaker.HeightMapToNormalMapGPU(option.width - 2, option.height - 2, data.heightBuffer, NormalMapMaker.E_NormalBufferType.FloatToVector3, Ground.GroundSize, NormalMapMaker.E_NormalSideType.Reduce);
        data.arr_Height = new float[option.width * option.height];
        data.arr_Normal = new Vector3[(option.width - 2) * (option.height - 2)];
        data.heightBuffer.GetData(data.arr_Height);
        data.normalBuffer.GetData(data.arr_Normal);
        data.arr_MeshLOD = new Mesh[1];
        data.HeightBufferSize = new Vector2Int(option.width, option.height);


        int[] AxisVertexCount = new int[1] { 21 };
         
        for(int i=0;i< data.arr_MeshLOD.Length;i++)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[AxisVertexCount[i] * AxisVertexCount[i]];
            Vector2[] uvs = new Vector2[AxisVertexCount[i] * AxisVertexCount[i]];
            Vector3[] normals = new Vector3[AxisVertexCount[i] * AxisVertexCount[i]];
            //-ground크기 / 2 ~ ground크기 / 2
            Vector3 startPos = new Vector3(-Ground.GroundSize.x * 0.5f, 0, -Ground.GroundSize.y * 0.5f);
            float dVertex = Ground.GroundSize.x / (AxisVertexCount[i] - 1);
            float dUV = 1.0f / (AxisVertexCount[i] - 1);
            for (int z = 0; z < AxisVertexCount[i];z++)
            {
                for(int x = 0; x < AxisVertexCount[i];x++)
                {
                    Vector2 curUV = new Vector2(dUV * x, dUV * z);
                    int heightMapIdx_x = Mathf.RoundToInt(curUV.x * (option.width - 2)) + 1;
                    heightMapIdx_x = heightMapIdx_x == option.width - 1 ? heightMapIdx_x - 1 : heightMapIdx_x;
                    int heightMapIdx_y = Mathf.RoundToInt(curUV.y * (option.height - 2)) + 1;
                    heightMapIdx_y = heightMapIdx_y == option.height - 1 ? heightMapIdx_y - 1 : heightMapIdx_y;
                    int heightMapIdx = heightMapIdx_x + heightMapIdx_y * option.width;
                    int normalMapIdx = (heightMapIdx_x - 1) + (heightMapIdx_y - 1) * (option.width - 2);
                    float curHeight = data.arr_Height[heightMapIdx];
                    Vector3 curPos = startPos + new Vector3(dVertex * x, curHeight, dVertex * z);
                    int curIdx = x + z * AxisVertexCount[i];
                    Vector3 curNormal = data.arr_Normal[normalMapIdx];
                    curNormal = new Vector3(curNormal.x, curNormal.z, curNormal.y); //z와 y 바꿔야함 tangent bitangent 할필요없음
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
