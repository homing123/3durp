using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class ObjM : MonoBehaviour
{
    [SerializeField] GameObject m_Prefab;

    int m_MaxObjCount = 1000;

    List<GameObject> m_ObjList = new List<GameObject>();
    [SerializeField] bool m_UseGPU;
    [SerializeField] ComputeShader m_CS;
    ComputeBuffer m_PositionBuffer;
    Vector3[] arr_Pos;

    const int ThreadCount = 32;

    private void Start()
    {
        m_PositionBuffer = new ComputeBuffer(m_MaxObjCount, sizeof(float) * 3);
        m_CS.SetBuffer(0, "_PositionBuffer", m_PositionBuffer);
        m_CS.SetInt("_MaxCount", m_MaxObjCount);
        arr_Pos = new Vector3[m_MaxObjCount];

        for(int i=0;i<m_MaxObjCount;i++)
        {
            GameObject go = Instantiate(m_Prefab);
            go.transform.position = new Vector3(0, 0, 0);
            m_ObjList.Add(go);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if(m_UseGPU)
        {
            //현재 오브젝트 위치 arr_Pos에 세팅
            for(int i=0;i<m_MaxObjCount;i++)
            {
                arr_Pos[i] = m_ObjList[i].transform.position;
            }

            //computebuffer 에 arr_Pos값 넘김
            m_PositionBuffer.SetData(arr_Pos);

            int kernelGroupX = m_MaxObjCount / ThreadCount + (m_MaxObjCount % ThreadCount == 0 ? 0 : 1);
            m_CS.SetFloats("_MoveVt3", new float[3] { 0, Time.deltaTime, 0 });

            //커널함수 실행
            m_CS.Dispatch(0, kernelGroupX, 1, 1);

            //움직인 결과버퍼 arr_Pos에 옮김
            m_PositionBuffer.GetData(arr_Pos);

            //arr_pos 위치로 오브젝트 이동
            for(int i=0;i<m_MaxObjCount;i++)
            {
                m_ObjList[i].transform.position = arr_Pos[i];
            }
        }
        else
        {
            for (int i = 0; i < m_ObjList.Count; i++)
            {
                m_ObjList[i].transform.position += new Vector3(0, Time.deltaTime, 0);
            }
        }


    }

   
}
