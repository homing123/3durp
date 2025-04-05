using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelLight : MonoBehaviour
{
    //cpu voxel �� ������.
    //cpu voxel �ȿ� gpu voxel�� ������.

    //������ �ʿ��� �����͸� �Ҵ��Ѵ�.
    //������ ��� ĳ���ؾ���


    //ù ���
    //cpu������ ������ ���ϴ°����� cpu���� idx�� ���� idx�� ��� �迭ȭ �� gpu�� �ѱ�
    //computeshader : cpu���� idx �� ���� idx �� ���� �����͸� ������ ���� gpu���������� �������� ��� �� gpu������ ���� �����͸� ����


    //cpu �׸��� �̵�
    //last �׸��� ��ġ�� ���� �׸��� ��ġ�� �̵��� �׸��尪�� ���
    //x,z �� horizontalCount ���� ���Կ����̰ų� y�� verticalCount ���� ���Կ����̸� �̵��� ĭ�� ���
    //���ź��� ���� ���̱� ���� 0,1,2,3,4 ���� ���������� ��ĭ ������ ��� 5,6,2,3,4 �� �ǵ��� ����
    //���� ���� ���� ������ n���� ��� ������ ������ ��� ���������� �� ũ�� ������ ������
    //0, 1, 2, -2, -1 �� ������ = 0, 1, 2, -2, -1 �̰� �����ȯ(0���� ������ + 5) �ϸ� 0, 1, 2, 3, 4 ��
    //����� cpu�������� ���� ��� ���� ���� ���� ��� ��
    //computeshader ���


    //�������� ����
    //���� �����ӿ� ����� ������ ����� ���� idx������ �迭ȭ
    //cpu �������� ����� ������ ������ �� ��� �� computeshader �� ���

    public enum E_VoxelLightKernel
    {
        VoxelAllLight = 0,

    }


    public const int CPUVoxelLightMax = 32;
    public const int CPUVoxelSize = 32;
    public const int CPUVoxelHorizontalCount = 5;
    public const int CPUVoxelVerticalCount = 3;
    public const int GPUVoxelSize = 1;
    public const int GPUVoxelAxisCount = 32;
    public const int MaxLightCount = 128;

    public static readonly int CPUVoxelCount = CPUVoxelHorizontalCount * CPUVoxelHorizontalCount * CPUVoxelVerticalCount;
    public static readonly int GPUVoxelCount = CPUVoxelSize * CPUVoxelSize * CPUVoxelSize * CPUVoxelCount;


    [SerializeField] ComputeShader m_CSVoxelLight;

    List<Light> m_Lights = new List<Light>();
    LightData[] m_LightDataCPU;
    ComputeBuffer m_LightDataGPU;

    Vector3Int m_CurCPUVoxelGridIdx;
    Vector3Int m_PreCPUVoexlGridIdx;
    int[,] m_CPUVoxelLightCPU;
    ComputeBuffer m_CPUVoxelLightGPU;
    ComputeBuffer m_GPUVoxelLight; //1 Voxel = 1 byte, {(0,y,0), (1,y,0), (2,y,0), (3,y,0)}

    ComputeBuffer m_CheckListCPUVoxelLight;

    private void Awake()
    {
        m_LightDataCPU = new LightData[MaxLightCount];
        m_LightDataGPU = new ComputeBuffer(MaxLightCount, HMUtil.StructSize(typeof(LightData)));

        m_CurCPUVoxelGridIdx = GetCPUVoexlGridIdx(Camera.main.transform.position);
        m_PreCPUVoexlGridIdx = m_CurCPUVoxelGridIdx;
        m_CPUVoxelLightCPU = new int[CPUVoxelCount, CPUVoxelLightMax];
        for (int i = 0; i < CPUVoxelCount; i++)
        {
            for (int j = 0; j < CPUVoxelLightMax; j++)
            {
                m_CPUVoxelLightCPU[i, j] = -1;
            }
        }

        m_CPUVoxelLightGPU = new ComputeBuffer(m_CPUVoxelLightCPU.Length, sizeof(int));
        m_GPUVoxelLight = new ComputeBuffer(GPUVoxelCount >> 2, sizeof(int));

        m_CheckListCPUVoxelIdx = new int[CPUVoxelCount];
    }
    private void Start()
    {
        Light[] arr_Light = FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < arr_Light.Length; i++)
        {
            if (arr_Light[i].type != LightType.Directional)
            {
                m_Lights.Add(arr_Light[i]);
            }
        }
        LightDataInit();
        CPUVoxelInfoInit();
        GPUVoxelInfoInit();
    }

    void LightDataInit()
    {
        int idx = 0;
        for(int i=0;i<m_Lights.Count;i++)
        {
            if (m_Lights[i].intensity > 0 && m_Lights[i].range > 0 && m_Lights[i].gameObject.activeSelf)
            {
                m_LightDataCPU[idx] = new LightData(m_Lights[i]);
            }
        }

        m_LightDataGPU.SetData(m_LightDataCPU);
    }
    void CPUVoxelInfoInit()
    {
        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;

        for (int z = 0; z < CPUVoxelHorizontalCount; z++)
        {
            for (int y = 0; y < CPUVoxelVerticalCount; y++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    Vector3Int curVoxelGridIdx = m_CurCPUVoxelGridIdx + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    float halfvoxelSize = CPUVoxelSize * 0.5f;
                    Vector3 boxCenter = curVoxelGridIdx * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(m_CurCPUVoxelGridIdx);
                    int voxellightIdx = 0;
                    for (int i=0;i<m_Lights.Count;i++)
                    {
                        bool islight = CheckLight(boxCenter, halfvoxelSize, m_Lights[i]);
                        if(islight)
                        {
                            m_CPUVoxelLightCPU[voxelIdx, voxellightIdx++] = i;
                            if (voxellightIdx >= CPUVoxelLightMax)
                            {
                                Debug.Log($"lights so many {x}, {y}, {z} : {m_CurCPUVoxelGridIdx}");
                                break;
                            }
                        }
                    }
                }
            }
        }

        m_CPUVoxelLightGPU.SetData(m_CPUVoxelLightCPU);
    }
    void GPUVoxelInfoInit()
    {
        List<int> l_CheckList = new List<int>();
        for (int i = 0; i < CPUVoxelCount; i++)
        {
            for (int j = 0; j < CPUVoxelLightMax; j++)
            {
                if (m_CPUVoxelLightCPU[i, j] != -1)
                {
                    l_CheckList.Add(i);
                    break;
                }
            }
        }

        int[] arr_CheckList = l_CheckList.ToArray();
        m_CheckListCPUVoxelLight.SetData(arr_CheckList);
        int ChecklistCount = arr_CheckList.Length;

        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_LightData", m_LightDataGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_CheckListCPUVoxelLight", m_CheckListCPUVoxelLight);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_CPUVoxelLight", m_CPUVoxelLightGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_GPUVoxelLight", m_GPUVoxelLight);

        





    }
    bool CheckLight(Vector3 boxCenter, float boxhalfSize, Light light)
    {
        switch(light.type)
        {
            case LightType.Point:
                float extensionBoxhalfSize = boxhalfSize + light.range;
                Vector3 lightPos = light.transform.position;
                Vector3 b2l = lightPos - boxCenter; //box to light
                Vector3 abs_b2l = new Vector3(MathF.Abs(b2l.x), MathF.Abs(b2l.y), MathF.Abs(b2l.z));
                //������ Ȯ�� �ڽ� �ȿ� ������ ������ �ƿ�
                if (abs_b2l.x > extensionBoxhalfSize || abs_b2l.y > extensionBoxhalfSize || abs_b2l.z > extensionBoxhalfSize)
                {
                    return false;
                }
                Vector3 temp = new Vector3(abs_b2l.x > boxhalfSize ? 1 : 0, abs_b2l.y > boxhalfSize ? 1 : 0, abs_b2l.z > boxhalfSize ? 1 : 0);
                float total = temp.x + temp.y + temp.z;

                if(total < 2)
                {
                    return true;
                }

                Vector3 tempSign = new Vector3(temp.x * Mathf.Sign(b2l.x), temp.y * Mathf.Sign(b2l.y), temp.z * Mathf.Sign(b2l.z));
                Vector3 point = tempSign * boxhalfSize + boxCenter;
                point.x = tempSign.x == 0 ? lightPos.x : point.x;
                point.y = tempSign.y == 0 ? lightPos.y : point.y;
                point.z = tempSign.z == 0 ? lightPos.z : point.z;

                Vector3 p2l = lightPos - point; //point to light
                float disSquare = Vector3.Dot(p2l, p2l);
                return disSquare < light.range * light.range;
        }
        return false; 
    }

    Vector3Int GetCPUVoexlGridIdx(Vector3 pos)
    {
        Vector3 temp = pos / CPUVoxelSize;
        return new Vector3Int(Mathf.FloorToInt(temp.x), Mathf.FloorToInt(temp.y), Mathf.FloorToInt(temp.z));
    }

    int GetVoxelIdx(int x, int y, int z)
    {
        int _x = x % CPUVoxelHorizontalCount;
        int _y = y % CPUVoxelVerticalCount;
        int _z = z % CPUVoxelHorizontalCount;
        _x = _x < 0 ? _x + CPUVoxelHorizontalCount : _x;
        _y = _y < 0 ? _y + CPUVoxelVerticalCount : _y;
        _z = _z < 0 ? _z + CPUVoxelHorizontalCount : _z;
        return _x + _y * CPUVoxelHorizontalCount + _z * CPUVoxelHorizontalCount * CPUVoxelVerticalCount;
    }
    int GetVoxelIdx(Vector3Int idx)
    {
        int _x = idx.x % CPUVoxelHorizontalCount;
        int _y = idx.y % CPUVoxelVerticalCount;
        int _z = idx.z % CPUVoxelHorizontalCount;
        _x = _x < 0 ? _x + CPUVoxelHorizontalCount : _x;
        _y = _y < 0 ? _y + CPUVoxelVerticalCount : _y;
        _z = _z < 0 ? _z + CPUVoxelHorizontalCount : _z;
        return _x + _y * CPUVoxelHorizontalCount + _z * CPUVoxelHorizontalCount * CPUVoxelVerticalCount;
    }
}


public struct CPUVoxelInfo
{
    public int[] arr_LightIdx;
    public CPUVoxelInfo(int length)
    {
        arr_LightIdx = new int[length];
        Array.Fill(arr_LightIdx, -1);
    }
    public void AddLightIdx(int idx)
    {
        for (int i = 0; i < 32; i++)
        {
            if (arr_LightIdx[i] == -1)
            {
                arr_LightIdx[i] = idx;
                break;
            }
        }
    }
    public bool ContainLIght()
    {
        for (int i = 0; i < 32; i++)
        {
            if (arr_LightIdx[i] == -1)
            {
                return true;
            }
        }
        return false;
    }

    public static int GetClassSize()
    {
        return 32 * sizeof(int);
    }
}
public struct GPUVoxelInfo
{
    //byte������ �ϰ� 4�׸��带 ���°� ����ȭ������ �Ŀ� ������ �ٸ����� �߰��� ��츦 ����� 4�׸��带 �������� �ϴ� 4byte���
    public int Light; //32bit to cpu voxelinfo array idx
}



//�ϴ� ����Ʈ�� �غ���
public struct LightData
{
    public float Range;
    public Vector3 Pos;

    public LightData(Light light)
    {
        Pos = light.transform.position;
        Range = light.range;
    }

    public bool isUpdate(Light light)
    {
        Vector3 pos = light.transform.position;
        float range = light.range;
        if (pos != Pos || range != Range)
        {
            return true;
        }
        return false;
    }
}