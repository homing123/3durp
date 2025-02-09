using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    public static readonly Vector2 GroundSize = new Vector2(10, 10); //x,y 같게 사용할건데 편의상 vt2로 만듬
    public static Ground Create(Vector2 pos, float dis, Mesh[] arr_MeshLOD)
    {
        Ground ground = Instantiate(Prefabs.Ins.G_Ground, MapMaker.Ins.transform).GetComponent<Ground>();
        ground.m_Pos = pos;
        ground.m_MeshesLOD = arr_MeshLOD;
        ground.SetMesh(dis);
        return ground;
    }
    Vector2 m_Pos;
    Material m_Mat;
    MeshFilter m_MeshFilter;
    MeshRenderer m_MeshRenderer;
    Mesh[] m_MeshesLOD;
    int m_CurMeshIdx = -1;

    private void Awake()
    {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_Mat = m_MeshRenderer.material;
        m_MeshFilter = GetComponent<MeshFilter>();
    }
    private void Start()
    {
        //피봇이 중심이라 0.5곱한값 더해줌
        transform.position = new Vector3(m_Pos.x, 0, m_Pos.y);
    }
    int GetMeshIdx(float dis)
    {
        if(dis > 30)
        {
            return 0;
        }
        else
        {
            return m_MeshesLOD.Length - 1;
        }
    }
    public void SetMesh(float dis)
    {
        int meshIdx = GetMeshIdx(dis);
        if(m_CurMeshIdx != meshIdx)
        {
            m_CurMeshIdx = meshIdx;
            m_MeshFilter.mesh = m_MeshesLOD[m_CurMeshIdx];
        }
    }
}
