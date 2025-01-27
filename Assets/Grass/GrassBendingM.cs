using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

//캠 그리드 좌표값을 구한다.
//캠 그리드 좌표값을 텍스쳐의 중앙으로 한다.
//단위값을 정한다 현재 0.1인데 줄일수 있도록
//캠 그리드값이 이전과 달라지면 그리드좌표 동기화를 진행
//셰이더에서도 캠그리드값과 월드좌표를 기준으로 uv를 써야함
public class GrassBendingM : MonoBehaviour
{
    public static GrassBendingM Ins;
    private void Awake()
    {
        Ins = this;
    }

    const int TexWidth = 512;
    const int BendingObjectMaxCount = 8;
    [SerializeField] float m_BendingRadius;
    ComputeBuffer m_BendingBuffer;
    ComputeBuffer m_BendingTexBuffer;
    ComputeBuffer m_BendingTexBuffer2;
    Vector3 m_BeforeCamPos;

    List<BendingObject> L_BendingObjs = new List<BendingObject>();

    [SerializeField] ComputeShader m_CSBending;
    [SerializeField] Material m_GrassMat;
    [SerializeField] float m_TexInterval;
    int m_TexBufferKind = 0;
    Vector2Int GetCamGridPos(Vector3 camPos)
    {
        //25.6에 256 해서 0.1단위로?
        //그럼 절반은 12.8임
        return new Vector2Int( (int)(camPos.x), (int)(camPos.z) );
    }
    public void AddBending(Transform tf, float radius)
    {
        if(L_BendingObjs.Count < BendingObjectMaxCount)
        {
            L_BendingObjs.Add(new BendingObject(tf, radius));
        }
    }

    private void Start()
    {
        m_BeforeCamPos = Camera.main.transform.position;
        InitCSBuffer();
    }

    void InitCSBuffer()
    {
        m_BendingTexBuffer = new ComputeBuffer(TexWidth * TexWidth, sizeof(float));
        m_BendingTexBuffer2 = new ComputeBuffer(TexWidth * TexWidth, sizeof(float));
        m_BendingBuffer = new ComputeBuffer(BendingObjectMaxCount, sizeof(float) * 4);
        m_CSBending.SetBuffer(0, "_BendingTexBuffer", m_BendingTexBuffer);
        m_CSBending.SetBuffer(0, "_BendingTexBuffer2", m_BendingTexBuffer2); 
        m_CSBending.SetBuffer(1, "_BendingTexBuffer", m_BendingTexBuffer);
        m_CSBending.SetBuffer(1, "_BendingTexBuffer2", m_BendingTexBuffer2);
        m_CSBending.SetBuffer(1, "_BendingBuffer", m_BendingBuffer);
        m_GrassMat.SetBuffer("_BendingTexBuffer", m_BendingTexBuffer);
        //ImageView.Ins.SetImageViewBuffer(m_BendingTexBuffer);
    }
    int test = 1;
    void UpdateBendingTex()
    {
        List<BendingBuffer> l_buffer = new List<BendingBuffer>();
        for(int i=0;i<L_BendingObjs.Count;i++)
        {
            l_buffer.Add(new BendingBuffer(L_BendingObjs[i]));
        }
        m_BendingBuffer.SetData(l_buffer.ToArray());
        m_CSBending.SetInt("_BendingDataCount", l_buffer.Count);

        m_CSBending.SetFloat("_TexInterval", m_TexInterval);
        Vector2Int curCamGridPos = GetCamGridPos(Camera.main.transform.position);
        Vector2Int beforeCamGridPos = GetCamGridPos(m_BeforeCamPos);
        m_BeforeCamPos = Camera.main.transform.position;
        m_CSBending.SetVector("_CamPos", Camera.main.transform.position);
        m_CSBending.SetInts("_CamGridPos", new int[2] { curCamGridPos.x, curCamGridPos.y });

        Vector2Int camGridMove = curCamGridPos - beforeCamGridPos;
        if(camGridMove.x != 0 || camGridMove.y != 0)
        {
            //Debug.Log($"캠 그리드 움직임 : {camGridMove})");
            m_CSBending.SetInts("_CamGridMove", new int[2] { camGridMove.x, camGridMove.y });
            m_CSBending.Dispatch(0, TexWidth, 1, 1);
        }
        else
        {
            m_CSBending.SetInts("_CamGridMove", new int[2] { 0,0 });
        }
        m_CSBending.Dispatch(1, TexWidth, 1, 1);

        m_GrassMat.SetFloat("_BendingTexInterval", m_TexInterval);
        m_GrassMat.SetInt("_CamGridPosx", curCamGridPos.x);
        m_GrassMat.SetInt("_CamGridPosz", curCamGridPos.y);
        

    }
    int rate = 2;
    int curRate = 0;
    private void Update()
    {
        L_BendingObjs[0]= new BendingObject(L_BendingObjs[0].transform, m_BendingRadius);
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
        m_BendingTexBuffer2.Release();
    }
}
