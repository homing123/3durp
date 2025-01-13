using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class BendingData
{
    public Vector2 pos;
    public float radius;
    public float time;
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
                m_BendingList[i].radius = data.radius > m_BendingList[i].radius ? data.radius : m_BendingList[i].radius;
                m_BendingList[i].time = data.time > m_BendingList[i].time ? data.time : m_BendingList[i].time;
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
    }
    void UpdateBendingTex()
    {
        m_CSBending.SetInt("_BendingDataCount", m_BendingList.Count);
        m_CSBending.SetVector("_CamPos", Camera.main.transform.position);
        m_CSBending.SetFloat("_RenderDis", m_BendingRenderDis);
        m_CSBending.SetFloat("_BendingPower", m_BendingPower);
        m_CSBending.Dispatch(0, TexWidth, 1, 1);

        m_GrassMat.SetBuffer("_BendingTexBuffer", m_BendingTexBuffer);
        m_GrassMat.SetFloat("_BendingRenderDis", m_BendingRenderDis);
    }
    void UpdateBendingDataList()
    {
        for (int i = 0; i < m_BendingList.Count; i++)
        {
            m_BendingList[i].time -= Time.deltaTime;
            if (m_BendingList[i].time <= 0)
            {
                m_BendingList.RemoveAt(i);
                i--;
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
