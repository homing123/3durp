
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class ObjM : MonoBehaviour
{
    [SerializeField] GameObject m_Obj;
    [SerializeField] GameObject m_Obj2;
    [SerializeField] Vector3 m_Normal0;
    [SerializeField] Vector3 m_Normal1;
    [SerializeField] Vector3 m_Normal2;
    [SerializeField] Vector3 m_Normal3;

    [SerializeField] Vector3 m_Normal1_0;
    [SerializeField] Vector3 m_Normal1_1;
    [SerializeField] Vector3 m_Normal1_2;
    [SerializeField] Vector3 m_Normal1_3;
    [SerializeField] Vector3 m_Normal1_4;
    [SerializeField] Vector3 m_Normal1_5;
    [SerializeField] Vector3 m_Normal1_6;
    [SerializeField] Vector3 m_Normal1_7;
    [SerializeField] Vector3 m_Normal1_8;
    [SerializeField] Vector3 m_Normal1_9;
    [SerializeField] Vector3 m_Normal1_10;
    [SerializeField] Vector3 m_Normal1_11;
    [SerializeField] Vector3 m_Normal1_12;
    [SerializeField] Vector3 m_Normal1_13;
    [SerializeField] Vector3 m_Normal1_14;
    [SerializeField] Vector3 m_Normal1_15;

    private void Start()
    {


    }
    // Update is called once per frame
    void Update()
    {
       

       
            float m_MeshSize = 3;
            int m_VertexWidth = 4;
            int verticesCount = m_VertexWidth * m_VertexWidth;
            Vector3[] vertices = new Vector3[verticesCount]; //버텍스는 기본적으로 넣어줘야한다고하네
            Vector3[] normal = new Vector3[verticesCount]; //버텍스는 기본적으로 넣어줘야한다고하네
            Mesh mesh = new Mesh();
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
            mesh.vertices = vertices;
        //normal[0] = new Vector3(0.293f, -0.196f, 0.936f);
        //normal[1] = new Vector3(0.245f, -0.168f, 0.955f);
        //normal[2] = new Vector3(0.106f, -0.093f, 0.990f);
        //normal[3] = new Vector3(-0.001f, 0.025f, 1.000f);

        //normal[4] = new Vector3(0.276f, -0.194f, 0.941f);
        //normal[5] = new Vector3(0.263f, -0.253f, 0.931f);
        //normal[6] = new Vector3(0.208f, -0.245f, 0.947f);
        //normal[7] = new Vector3(0.124f, -0.143f, 0.982f);

        //normal[8] = new Vector3(0.177f, -0.027f, 0.984f);
        //normal[9] = new Vector3(0.198f, -0.150f, 0.969f);
        //normal[10] = new Vector3(0.221f, -0.234f, 0.947f);
        //normal[11] = new Vector3(0.184f, -0.235f, 0.955f);

        //normal[12] = new Vector3(0.041f, 0.235f, 0.971f);
        //normal[13] = new Vector3(0.062f, 0.136f, 0.989f);
        //normal[14] = new Vector3(0.127f, 0.022f, 0.992f);
        //normal[15] = new Vector3(0.141f, -0.061f, 0.988f);

      
        for (int i = 0; i < 16; i++)
            {
                normal[i] = normal[i].normalized;
            normal[i].x = 0;
            normal[i].z = 0;
            }
        normal[0] = m_Normal1_0;
        normal[1] = m_Normal1_1;
        normal[2] = m_Normal1_2;
        normal[3] = m_Normal1_3;
        normal[4] = m_Normal1_4;
        normal[5] = m_Normal1_5;
        normal[6] = m_Normal1_6;
        normal[7] = m_Normal1_7;
        normal[8] = m_Normal1_8;
        normal[9] = m_Normal1_9;
        normal[10] = m_Normal1_10;
        normal[11] = m_Normal1_11;
        normal[12] = m_Normal1_12;
        normal[13] = m_Normal1_13;
        normal[14] = m_Normal1_14;
        normal[15] = m_Normal1_15;
            mesh.normals = normal;


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

            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            m_Obj.GetComponent<MeshFilter>().mesh = mesh;



        temp();



    }
    void temp()
    {

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-0.5f, 0, -0.5f);
        vertices[1] = new Vector3(-0.5f, 0, 0.5f);
        vertices[2] = new Vector3(0.5f, 0, 0.5f);
        vertices[3] = new Vector3(0.5f, 0, -0.5f);
        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(0, 1);
        uvs[2] = new Vector2(1, 1);
        uvs[3] = new Vector2(1, 0);
        Vector3[] normal = new Vector3[4];
        normal[0] = m_Normal0;
        normal[1] = m_Normal1;
        normal[2] = m_Normal2;
        normal[3] = m_Normal3;
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normal;
        int[] indices = new int[6] { 0, 1, 3, 1, 2, 3 };
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        m_Obj2.GetComponent<MeshFilter>().mesh = mesh;
    }


}
