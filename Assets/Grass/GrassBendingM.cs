using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct BendingObject
{
    public Transform transform;
    public float radius;

    public BendingObject(Transform _transform, float _radius)
    {
        transform = _transform;
        radius = _radius;
    }
}
struct BendingBuffer
{
    public Vector3 pos;
    public float radius;
    public BendingBuffer(BendingObject obj)
    {
        pos = obj.transform.position;
        radius = obj.radius;
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
    const int BendingObjectMaxCount = 8;

    ComputeBuffer m_BendingBuffer;
    ComputeBuffer m_BendingTexBuffer;

    List<BendingObject> L_BendingObjs = new List<BendingObject>();

    [SerializeField] ComputeShader m_CSBending;
    [SerializeField] Material m_GrassMat;
    [SerializeField] float m_BendingRenderDis;
    public void AddBending(Transform tf, float radius)
    {
        if(L_BendingObjs.Count < BendingObjectMaxCount)
        {
            L_BendingObjs.Add(new BendingObject(tf, radius));
        }
    }

    private void Start()
    {
        InitCSBuffer();
    }

    void InitCSBuffer()
    {
        m_BendingTexBuffer = new ComputeBuffer(TexWidth * TexWidth, sizeof(float));
        m_BendingBuffer = new ComputeBuffer(BendingObjectMaxCount, sizeof(float) * 4);
        m_CSBending.SetBuffer(0, "_BendingTexBuffer", m_BendingTexBuffer);
        m_CSBending.SetBuffer(0, "_BendingBuffer", m_BendingBuffer);
        m_GrassMat.SetBuffer("_BendingTexBuffer", m_BendingTexBuffer);

    }
    void UpdateBendingTex()
    {
        List<BendingBuffer> l_buffer = new List<BendingBuffer>();
        for(int i=0;i<L_BendingObjs.Count;i++)
        {
            l_buffer.Add(new BendingBuffer(L_BendingObjs[i]));
        }
        m_BendingBuffer.SetData(l_buffer.ToArray());
        m_CSBending.SetInt("_BendingDataCount", l_buffer.Count);
        m_CSBending.SetVector("_CamPos", new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z));
        m_CSBending.SetFloat("_RenderDis", m_BendingRenderDis);
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
    int rate = 2;
    int curRate = 0;
    private void Update()
    {
        curRate++;
        if (curRate >= rate)
        {
            curRate = 0;
            UpdateBendingTex();
        }
    }
    private void OnDestroy()
    {
        m_BendingBuffer.Release();
        m_BendingTexBuffer.Release();
    }
}
