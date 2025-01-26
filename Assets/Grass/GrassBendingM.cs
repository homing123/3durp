using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct BendingData
{
    public Vector2 pos;
    public float radius;
    public float time;
    public BendingData(Vector2 _pos, float _radius, float _time)
    {
        pos = _pos;
        radius = _radius;
        time = _time;
    }
}
public class GrassBendingM : MonoBehaviour
{
    public static GrassBendingM Ins;
    private void Awake()
    {
        Ins = this;
    }

    const int TexWidth = 512;
    const int BendingDataMaxCount = 32;

    ComputeBuffer m_BendingBuffer;
    ComputeBuffer m_BendingTexBuffer;

    List<BendingData> m_BendingList = new List<BendingData>(BendingDataMaxCount);


    [SerializeField] ComputeShader m_CSBending;
    [SerializeField] Material m_GrassMat;
    [SerializeField] float m_BendingRenderDis;
    [SerializeField] float m_BendingPower;
    public void AddBending(BendingData data)
    {
        for(int i=0;i<m_BendingList.Count;i++)
        {
            if(data.pos == m_BendingList[i].pos)
            {
                float radius = data.radius > m_BendingList[i].radius ? data.radius : m_BendingList[i].radius;
                float time = data.time > m_BendingList[i].time ? data.time : m_BendingList[i].time;
                Vector2 pos = data.pos;
                m_BendingList[i] = new BendingData(pos, radius, time);
                return;
            }
        }
        if(m_BendingList.Count >= BendingDataMaxCount)
        {
            float timeMin = 1;
            int idx = 0;
            for (int i = 0; i < m_BendingList.Count; i++)
            {
                if (m_BendingList[i].time < timeMin)
                {
                    timeMin = m_BendingList[i].time;
                    idx = i;
                }
            }
            m_BendingList.RemoveAt(idx);
        }
        m_BendingList.Add(data);
    }

    private void Start()
    {
        InitCSBuffer();
    }

    void InitCSBuffer()
    {
        m_BendingTexBuffer = new ComputeBuffer(TexWidth * TexWidth, sizeof(float));
        m_BendingBuffer = new ComputeBuffer(BendingDataMaxCount, sizeof(float) * 4);
        m_CSBending.SetBuffer(0, "_BendingTexBuffer", m_BendingTexBuffer);
        m_CSBending.SetBuffer(0, "_BendingBuffer", m_BendingBuffer);
        m_GrassMat.SetBuffer("_BendingTexBuffer", m_BendingTexBuffer);

    }
    void UpdateBendingTex()
    {
        m_BendingBuffer.SetData(m_BendingList.ToArray());
        m_CSBending.SetInt("_BendingDataCount", m_BendingList.Count);
        m_CSBending.SetVector("_CamPos", new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z));
        m_CSBending.SetFloat("_RenderDis", m_BendingRenderDis);
        m_CSBending.SetFloat("_BendingPower", m_BendingPower);
        m_CSBending.Dispatch(0, TexWidth, 1, 1);

        m_GrassMat.SetFloat("_BendingRenderDis", m_BendingRenderDis);

        //float[] arr_tex = new float[512 * 512];
        //m_BendingTexBuffer.GetData(arr_tex);
        //for(int i=0;i<512 * 512;i++)
        //{
        //    if (arr_tex[i] > 0)
        //    {
        //        Debug.Log($"{i} {arr_tex[i]}");
        //    }
        //}
    }
    void UpdateBendingDataList()
    {
        for (int i = 0; i < m_BendingList.Count; i++)
        {
            float time = m_BendingList[i].time;
            time -= Time.deltaTime;
            if (time <= 0)
            {
                m_BendingList.RemoveAt(i);
                i--;
            }
            else
            {
                m_BendingList[i] = new BendingData(m_BendingList[i].pos, m_BendingList[i].radius, time);
            }

        }
    }
    private void Update()
    {
        UpdateBendingDataList();
        UpdateBendingTex();
    }
    private void OnDestroy()
    {
        m_BendingBuffer.Release();
        m_BendingTexBuffer.Release();
    }
}
