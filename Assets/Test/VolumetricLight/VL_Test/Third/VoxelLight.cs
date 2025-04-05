using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelLight : MonoBehaviour
{
    //cpu voxel 로 나눈다.
    //cpu voxel 안에 gpu voxel로 나눈다.

    //복셀에 필요한 데이터를 할당한다.
    //조명을 모두 캐싱해야함


    //첫 계산
    //cpu복셀에 조명이 속하는곳들을 cpu복셀 idx와 조명 idx를 모두 배열화 후 gpu로 넘김
    //computeshader : cpu복셀 idx 와 조명 idx 와 조명 데이터를 가지고 현재 gpu복셀에서의 조명영향을 계산 후 gpu복셀별 조명 데이터를 세팅


    //cpu 그리드 이동
    //last 그리드 위치와 현재 그리드 위치로 이동한 그리드값을 계산
    //x,z 가 horizontalCount 보다 적게움직이거나 y가 verticalCount 보다 적게움직이면 이동한 칸만 계산
    //갱신복셀 양을 줄이기 위해 0,1,2,3,4 에서 오른쪽으로 두칸 움직일 경우 5,6,2,3,4 가 되도록 적용
    //현재 축의 복셀 갯수가 n개일 경우 복셀의 순서는 양수 나머지연산 후 크기 순서로 지정됨
    //0, 1, 2, -2, -1 의 나머지 = 0, 1, 2, -2, -1 이고 양수전환(0보다 작으면 + 5) 하면 0, 1, 2, 3, 4 임
    //변경된 cpu복셀값에 대해 모든 조명에 대한 영향 계산 후
    //computeshader 계산


    //조명설정값 변경
    //현재 프레임에 변경된 설정이 변경된 조명 idx값들을 배열화
    //cpu 복셀에서 변경된 조명의 영향을 재 계산 후 computeshader 로 계산

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
                //반지름 확장 박스 안에 중점이 없으면 아웃
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
    //byte단위로 하고 4그리드를 묶는게 최적화되지만 후에 복셀에 다른값이 추가될 경우를 대비해 4그리드를 묶지말고 일단 4byte사용
    public int Light; //32bit to cpu voxelinfo array idx
}



//일단 포인트만 해보자
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