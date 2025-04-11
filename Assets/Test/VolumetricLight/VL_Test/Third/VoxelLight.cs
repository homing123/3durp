using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
    public enum E_DrawGizmoVoxelKind
    {
        CPU,
        CPU_XZFlat,
        GPU,
        GPU_XZFlat
    }
    //복셀 = 복셀 라이트임
    //GridPos = 복셀그리드화 하여 1복셀당 1일때의 위치
    //복셀위치 = 복셀범위의 최소값
    //복셀크기 8 => GridPos 1,0,-1 => 복셀위치 (8, 0, -8), 복셀범위 (8 ~ 16, 0 ~ 8, -8 ~ 0)

    public const int CPUVoxelLightMax = 8; //복셀1칸에 최대 라이트 갯수
    public const int CPUVoxelSize = 8; //cpu 복셀 월드기준 크기
    public const int CPUVoxelHalfSize = CPUVoxelSize / 2;
    public const int CPUVoxelHorizontalCount = 7; //cpu 복셀 xz축 갯수
    public const int CPUVoxelVerticalCount = 3; //cpu 복셀 y축 갯수
    public const int GPUVoxelSize = 1; //gpu 복셀 월드기준 크기
    public const int GPUVoxelAxisCount = 8; //gpu 복셀 각 축의 갯수 ex 32 = 32 * 32 * 32 = 1 cpu 복셀
    public const int MaxLightCount = 128; //최대 라이트 갯수

    public const int CPUVoxelCount = CPUVoxelHorizontalCount * CPUVoxelHorizontalCount * CPUVoxelVerticalCount; //cpu 복셀 전체 갯수
    public const int GPUVoxelCount = CPUVoxelSize * CPUVoxelSize * CPUVoxelSize * CPUVoxelCount; //gpu 복셀 전체 갯수
    public const int GPUVoxelDataCount = GPUVoxelCount >> 2;


    [SerializeField] ComputeShader m_CSVoxelLight;
    [SerializeField] E_DrawGizmoVoxelKind m_DrawGizmoVoxelKind;
    [SerializeField] int m_DrawGizmoYGridPos;

    List<Light> m_Lights = new List<Light>(); //씬 안의 모든 라이트
    LightData[] m_LightDataCPU; //gpu로 넘길 라이트 정보 = to gpu > m_LightDataGPU
    ComputeBuffer m_LightDataGPU;

    Vector3Int m_CurCPUVoxelGridPos; //복셀 배열 중 현재 카메라가 속한 복셀의 인덱스
    Vector3Int m_PreCPUVoexlGridPos; //이전프레임의 카메라가 속한 복셀의 인덱스 (복셀이동 이벤트 용)
    int[,] m_CPUVoxelLightCPU; //cpu 복셀 안의 라이트 인덱스 [voxel idx, 0 ~ VoxelLightMax - 1] = to gpu > m_CPUVoxelLightGPU
    ComputeBuffer m_CPUVoxelLightGPU;
    ComputeBuffer m_GPUVoxelLight; //1 Voxel = 1 byte, {(3,y,0), (2,y,0), (1,y,0), (0,y,0)}


    ComputeBuffer m_CheckListCPUVoxelLight; //갱신이 필요한 복셀

    private void Awake()
    {
        m_LightDataCPU = new LightData[MaxLightCount];
        m_LightDataGPU = new ComputeBuffer(MaxLightCount, HMUtil.StructSize(typeof(LightData)));

        m_CurCPUVoxelGridPos = GetCPUVoexlGridPos(Camera.main.transform.position);
        m_PreCPUVoexlGridPos = m_CurCPUVoxelGridPos;
        m_CPUVoxelLightCPU = new int[CPUVoxelCount, CPUVoxelLightMax];
        for (int i = 0; i < CPUVoxelCount; i++)
        {
            for (int j = 0; j < CPUVoxelLightMax; j++)
            {
                m_CPUVoxelLightCPU[i, j] = -1;
            }
        }

        m_CPUVoxelLightGPU = new ComputeBuffer(m_CPUVoxelLightCPU.Length, sizeof(int));
        m_CPUVoxelLightGPU.SetData(m_CPUVoxelLightCPU);
        m_CheckListCPUVoxelLight = new ComputeBuffer(CPUVoxelCount, sizeof(int));
        m_GPUVoxelLight = new ComputeBuffer(GPUVoxelDataCount, sizeof(int)); //4byte = 4Voxel
        int[] arr_zero = new int[GPUVoxelDataCount];
        Array.Fill(arr_zero, 0);
        m_GPUVoxelLight.SetData(arr_zero);

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
    List<int> m_MoveVoxel = new List<int>(); //움직임에 의한 복셀 보여주기위해 임시로담아둘 공간
    float m_MoveVoxelGizmoTime;
    private void Update()
    {
        m_CurCPUVoxelGridPos = GetCPUVoexlGridPos(Camera.main.transform.position);
        if (m_PreCPUVoexlGridPos != m_CurCPUVoxelGridPos)
        {
            Vector3Int moveGridPos = m_CurCPUVoxelGridPos - m_PreCPUVoexlGridPos;
            Debug.Log($"무브 {moveGridPos}");
            int[] AxisXChangeIdx = GetCheckAxisIdx(CPUVoxelHorizontalCount, m_CurCPUVoxelGridPos.x, moveGridPos.x);
            int[] AxisYChangeIdx = GetCheckAxisIdx(CPUVoxelVerticalCount, m_CurCPUVoxelGridPos.y, moveGridPos.y);
            int[] AxisZChangeIdx = GetCheckAxisIdx(CPUVoxelHorizontalCount, m_CurCPUVoxelGridPos.z, moveGridPos.z);
            
            string temp = "";
            for(int i=0;i<AxisXChangeIdx.Length;i++)
            {
                temp += AxisXChangeIdx[i] + ", ";
            }
            //Debug.Log($"갱신 axis X : {temp}");
            temp = "";
            for (int i = 0; i < AxisYChangeIdx.Length; i++)
            {
                temp += AxisYChangeIdx[i] + ", ";
            }
            //Debug.Log($"갱신 axis Y : {temp}");
            temp = "";
            for (int i = 0; i < AxisZChangeIdx.Length; i++)
            {
                temp += AxisZChangeIdx[i] + ", ";
            }
            //Debug.Log($"갱신 axis Z : {temp}");
            m_PreCPUVoexlGridPos = m_CurCPUVoxelGridPos;

            int[] arr_CheckList = GetCheckListUseAxisIdx(AxisXChangeIdx, AxisYChangeIdx, AxisZChangeIdx);
            m_CheckListCPUVoxelLight.SetData(arr_CheckList);
            int ChecklistCount = arr_CheckList.Length;
            if (ChecklistCount == 0)
            {
                Debug.Log("CheckListCount is zero");
                return;
            }
            m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_CheckListCPUVoxelLight", m_CheckListCPUVoxelLight);
            m_CSVoxelLight.SetInt("_CheckListCount", ChecklistCount);
            m_CSVoxelLight.SetInts("_CurCPUVoxelGridPos", new int[3] { m_CurCPUVoxelGridPos.x, m_CurCPUVoxelGridPos.y, m_CurCPUVoxelGridPos.z });
            m_CSVoxelLight.Dispatch((int)E_VoxelLightKernel.VoxelAllLight, ChecklistCount, 1, 1);
        }
    }

    
    int[] GetCheckAxisIdx(int axisCount, int centerValue, int moveDis)
    {
        //ex axisCount = 5
        //center 9 -> 11 => centerValue = 11, moveDis = 2, (10, 11, 7, 8, 9) => (10, 11, 12, 13, 9), result = {2, 3}
        //center 9 -> 13 => centerValue = 11, moveDis = 2, (10, 11, 7, 8, 9) => (15, 11, 12, 13, 14), result = {0, 2, 3, 4}
        //center 9 -> 8 => centerValue = 11, moveDis = 2, (10, 11, 7, 8, 9) => (10, 6, 7, 8, 9), result = {1}

        int moveDisAbs = Mathf.Abs(moveDis);
        int moveDisSign = (int)Mathf.Sign(moveDis);

        int arrayLength = moveDisAbs > axisCount ? axisCount : moveDisAbs;
        int[] result = new int[arrayLength];

        int centerRemainder = centerValue % axisCount; //remainder = idx
        centerRemainder = centerRemainder < 0 ? centerRemainder + axisCount : centerRemainder;

        int halfRange = axisCount / 2;
        for (int i = 0; i < arrayLength; i++)
        {
            //n칸 증가일 경우 변경 후 최대값순서대로 n개가 변함.
            //최소값or최대값 idx = centerRemainder + moveDisSign * halfRange
            //후처리로 나머지연산으로 인덱스범위 안으로 넣어야함

            int curChangedIdx = centerRemainder + moveDisSign * halfRange - moveDisSign * i;
            curChangedIdx = curChangedIdx % axisCount;
            curChangedIdx = curChangedIdx < 0 ? curChangedIdx + axisCount : curChangedIdx;
            result[i] = curChangedIdx;
        }
        return result;
    }
    int[] GetCheckListUseAxisIdx(int[] axisx, int[] axisy, int[] axisz)
    {
        //cpuvoxel 큐브는 x, y, z 순서로 인덱싱됨
        bool[] isUseVoxel = new bool[CPUVoxelCount];

        //x
        for (int i = 0; i < axisx.Length; i++)
        {
            int xFlatCount = CPUVoxelHorizontalCount * CPUVoxelVerticalCount;
            int startIdx = axisx[i];
            for (int j = 0; j < xFlatCount;j++)
            {
                isUseVoxel[startIdx + CPUVoxelHorizontalCount * j] = true;
            }
        }

        //y
        for (int i = 0; i < axisy.Length; i++)
        {
            int curIdx = axisy[i] * CPUVoxelHorizontalCount;
            for (int z = 0; z < CPUVoxelHorizontalCount; z++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    isUseVoxel[curIdx + x + z * CPUVoxelHorizontalCount * CPUVoxelVerticalCount] = true;
                }
            }
        }

        //z
        for (int i = 0; i < axisz.Length; i++)
        {
            int curIdx = axisz[i] * CPUVoxelHorizontalCount * CPUVoxelVerticalCount;
            int zFlatCount = CPUVoxelHorizontalCount * CPUVoxelVerticalCount;
            for (int j = 0; j < zFlatCount; j++)
            {
                isUseVoxel[curIdx + j] = true;
            }
        }

        List<int> l_ChecklistIdx = new List<int>();

        m_MoveVoxel.Clear();
        m_MoveVoxelGizmoTime = 1.0f;
        //복셀이동에 의해 데이터갱신이 필요한 복셀리스트
        for (int i = 0; i < isUseVoxel.Length; i++)
        {
            if(isUseVoxel[i] == true)
            {
                m_MoveVoxel.Add(i);
                l_ChecklistIdx.Add(i);
            }
        }

        //복셀이동에 의한 갱신리스트의 복셀라이트 데이터 갱신
        //체크리스트에 빛이 없는 복셀 모두 제거
        for (int i = 0; i < l_ChecklistIdx.Count; i++)
        {
            Vector3Int curVoxelGridPos = GetVoxelGridPos(l_ChecklistIdx[i]);
            Vector3 curVoxelCenter = curVoxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;

            for(int j=0;j<CPUVoxelLightMax;j++)
            {
                m_CPUVoxelLightCPU[l_ChecklistIdx[i], j] = -1;
            }

            int voxelLightIdx = 0;
            bool hasLight = false;
            for (int lightIdx = 0; lightIdx < m_Lights.Count; lightIdx++)
            {
                bool isLight = CheckLight(curVoxelCenter, CPUVoxelHalfSize, m_LightDataCPU[lightIdx]);
                if(isLight)
                {
                    hasLight = true;
                    if (voxelLightIdx >= CPUVoxelLightMax)
                    {
                        Debug.Log($"lights so many {l_ChecklistIdx[i]} : {curVoxelGridPos}");
                        break;
                    }
                    m_CPUVoxelLightCPU[l_ChecklistIdx[i], voxelLightIdx++] = lightIdx;        
                }
            }
            if(hasLight== false)
            {
                l_ChecklistIdx.RemoveAt(i);
                i--;
            }
        }
        return l_ChecklistIdx.ToArray();
    }
    
    void LightDataInit()
    {
        for (int i = 0; i < m_Lights.Count; i++)
        {
            m_LightDataCPU[i] = new LightData(m_Lights[i]);      
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
                    Vector3Int curVoxelGridPos = m_CurCPUVoxelGridPos + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    Vector3 boxCenter = curVoxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(curVoxelGridPos);
                    int voxellightIdx = 0;
                    for (int i = 0; i < m_Lights.Count; i++)
                    {
                        bool islight = CheckLight(boxCenter, CPUVoxelHalfSize, m_LightDataCPU[i]);
                        if(islight)
                        {
                            if (voxellightIdx >= CPUVoxelLightMax)
                            {
                                Debug.Log($"lights so many {x}, {y}, {z} : {curVoxelGridPos}");
                                break;
                            }
                            m_CPUVoxelLightCPU[voxelIdx, voxellightIdx++] = i;                            
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
                    //Debug.Log($"체크리스트 : {i}");
                    break;
                }
            }
        }

        int[] arr_CheckList = l_CheckList.ToArray();
        m_CheckListCPUVoxelLight.SetData(arr_CheckList);
        int ChecklistCount = arr_CheckList.Length;
        if (ChecklistCount == 0)
        {
            Debug.Log("CheckListCount is zero");
            return;
        }
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_LightData", m_LightDataGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_CheckListCPUVoxelLight", m_CheckListCPUVoxelLight);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_CPUVoxelLight", m_CPUVoxelLightGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_GPUVoxelLight", m_GPUVoxelLight);
        m_CSVoxelLight.SetInt("_CheckListCount", ChecklistCount);
        m_CSVoxelLight.SetInts("_CurCPUVoxelGridPos", new int[3] { m_CurCPUVoxelGridPos.x, m_CurCPUVoxelGridPos.y, m_CurCPUVoxelGridPos.z });
        m_CSVoxelLight.Dispatch((int)E_VoxelLightKernel.VoxelAllLight, ChecklistCount, 1, 1);

    }
    bool CheckLight(Vector3 boxCenter, float boxhalfSize, in LightData light)
    {
        switch((LightType)light.Type)
        {
            case LightType.Point:
                float extensionBoxhalfSize = boxhalfSize + light.Range;
                Vector3 lightPos = light.Pos;
                Vector3 b2l = lightPos - boxCenter; //box to light
                Vector3 abs_b2l = new Vector3(MathF.Abs(b2l.x), MathF.Abs(b2l.y), MathF.Abs(b2l.z));
                //반지름 확장 박스 안에 중점이 없으면 아웃
                if (abs_b2l.x > extensionBoxhalfSize || abs_b2l.y > extensionBoxhalfSize || abs_b2l.z > extensionBoxhalfSize)
                {
                    return false;
                }
                Vector3Int temp = new Vector3Int(abs_b2l.x > boxhalfSize ? 1 : 0, abs_b2l.y > boxhalfSize ? 1 : 0, abs_b2l.z > boxhalfSize ? 1 : 0);
                int total = temp.x + temp.y + temp.z;

                if(total < 2)
                {
                    return true;
                }

                Vector3 tempSign = new Vector3(temp.x * Mathf.Sign(b2l.x), temp.y * Mathf.Sign(b2l.y), temp.z * Mathf.Sign(b2l.z));
                Vector3 nearBoxPoint = tempSign * boxhalfSize + boxCenter; //빛과 가장 가까운 박스의 지점
                nearBoxPoint.x = tempSign.x == 0 ? lightPos.x : nearBoxPoint.x;
                nearBoxPoint.y = tempSign.y == 0 ? lightPos.y : nearBoxPoint.y;
                nearBoxPoint.z = tempSign.z == 0 ? lightPos.z : nearBoxPoint.z;

                Vector3 p2l = lightPos - nearBoxPoint; //nearBoxPoint to light
                float disSquare = Vector3.Dot(p2l, p2l);
                return disSquare < light.Range * light.Range;
        }
        return false; 
    }

    Vector3Int GetCPUVoexlGridPos(Vector3 pos)
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
    Vector3Int VoxelIdxToIdx3(int voxelIdx)
    {
        int voxelIdx3_x = voxelIdx % CPUVoxelHorizontalCount;
        int voxelIdx3_y = (voxelIdx % (CPUVoxelVerticalCount * CPUVoxelHorizontalCount)) / CPUVoxelHorizontalCount;
        int voxelIdx3_z = voxelIdx / (CPUVoxelVerticalCount * CPUVoxelHorizontalCount);
        return new Vector3Int(voxelIdx3_x, voxelIdx3_y, voxelIdx3_z);
    }

    Vector3Int GetVoxelGridPos(int voxelIdx)
    {
        Vector3Int voxelIdx3 = VoxelIdxToIdx3(voxelIdx);
        Vector3Int remainder = m_CurCPUVoxelGridPos.Mod(new Vector3Int(CPUVoxelHorizontalCount, CPUVoxelVerticalCount, CPUVoxelHorizontalCount));
        remainder.x = remainder.x < 0 ? remainder.x + CPUVoxelHorizontalCount : remainder.x;
        remainder.y = remainder.y < 0 ? remainder.y + CPUVoxelVerticalCount : remainder.y;
        remainder.z = remainder.z < 0 ? remainder.z + CPUVoxelHorizontalCount : remainder.z;

        Vector3Int remainder2idx = voxelIdx3 - remainder;
        Vector3Int halfAxis = new Vector3Int(CPUVoxelHorizontalCount, CPUVoxelVerticalCount, CPUVoxelHorizontalCount) / 2;
        Vector3Int signValue = new Vector3Int((int)Mathf.Sign(remainder2idx.x), (int)Mathf.Sign(remainder2idx.y), (int)Mathf.Sign(remainder2idx.z));
        Vector3Int absValue = new Vector3Int(Mathf.Abs(remainder2idx.x), Mathf.Abs(remainder2idx.y), Mathf.Abs(remainder2idx.z));
        Vector3Int center2IdxValue = default;
        center2IdxValue.x = absValue.x > halfAxis.x ? remainder2idx.x - signValue.x * CPUVoxelHorizontalCount : remainder2idx.x;
        center2IdxValue.y = absValue.y > halfAxis.y ? remainder2idx.y - signValue.y * CPUVoxelVerticalCount : remainder2idx.y;
        center2IdxValue.z = absValue.z > halfAxis.z ? remainder2idx.z - signValue.z * CPUVoxelHorizontalCount : remainder2idx.z;

        Vector3Int result = m_CurCPUVoxelGridPos + center2IdxValue;
        //Debug.Log(Mathf.Sign(0));   
        //Debug.Log($"center : {m_CurCPUVoxelGridPos}, voxelIdx3 : {voxelIdx3}, remainder : {remainder}, r2idx : {remainder2idx}, half : {halfAxis}, sign : {signValue}, abs : {absValue}, center2idx : {center2IdxValue}, result : {result}");
        return m_CurCPUVoxelGridPos + center2IdxValue;
    }
    private void OnDestroy()
    {
        m_LightDataGPU.Release();
        m_CPUVoxelLightGPU.Release();
        m_GPUVoxelLight.Release();
        m_CheckListCPUVoxelLight.Release();
    }
    [ContextMenu("LogLightData")]
    public void LogLightData()
    {
        Debug.Log($"LogLightData");

        for (int i = 0; i < m_Lights.Count; i++)
        {
            m_LightDataCPU[i].Log();
        }
    }
    [ContextMenu("LogCPUVoxel")]
    public void LogCPUVoxel()
    {
        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        float halfvoxelSize = CPUVoxelSize * 0.5f;
        Debug.Log($"LogCPUVoxel m_CurCPUVoxelGridPos : {m_CurCPUVoxelGridPos}");

        for (int z = 0; z < CPUVoxelHorizontalCount; z++)
        {
            for (int y = 0; y < CPUVoxelVerticalCount; y++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    Vector3Int curVoxelGridPos = m_CurCPUVoxelGridPos + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    Vector3 voxelPos = curVoxelGridPos * CPUVoxelSize;
                    Vector3 voxelCenter = curVoxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(curVoxelGridPos);
                    int voxellightIdx = 0;
                    string msg = "";
                    for (int i = 0; i < CPUVoxelLightMax; i++)
                    {
                        msg += m_CPUVoxelLightCPU[voxelIdx, i] + ", ";
                    }
                    Debug.Log($"voxelPos : {voxelPos}, voxelCenter : {voxelCenter}, voxelIdx {voxelIdx}, msg: {msg}");
                }
            }
        }
    }
    [ContextMenu("LogGPUVoxel")]
    public void LogGPUVoxel()
    {
        Debug.Log($"LogGPUVoxel m_CurCPUVoxelGridPos : {m_CurCPUVoxelGridPos}");

        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        int[] GPUVoxelLight = new int[GPUVoxelDataCount];
        m_GPUVoxelLight.GetData(GPUVoxelLight);
        //for(int i=0;i<GPUVoxelDataCount;i++)
        //{
        //    Debug.Log(i+" " +GPUVoxelLight[i]);
        //}

        int GPUVoxelXAxisCount = (GPUVoxelAxisCount >> 2);
        int GPUVoxelDataCountInCPUVoxel = GPUVoxelXAxisCount * GPUVoxelAxisCount * GPUVoxelAxisCount;
        List<int> GPULightIdx = new List<int>();
        for (int z = 0; z < CPUVoxelHorizontalCount; z++)
        {
            for (int y = 0; y < CPUVoxelVerticalCount; y++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    Vector3Int curVoxelGridPos = m_CurCPUVoxelGridPos + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    Vector3 voxelPos = curVoxelGridPos * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(curVoxelGridPos);

                    for (int gpuz = 0; gpuz < GPUVoxelAxisCount; gpuz++)
                    {
                        for (int gpuy = 0; gpuy < GPUVoxelAxisCount; gpuy++)
                        {
                            for (int gpux = 0; gpux < GPUVoxelXAxisCount; gpux++)
                            {
                                int curGPUVoxelIdx = gpux + gpuy * GPUVoxelXAxisCount + gpuz * GPUVoxelXAxisCount * GPUVoxelAxisCount + voxelIdx * GPUVoxelDataCountInCPUVoxel;
                                int curGPUVoxelLight = GPUVoxelLight[curGPUVoxelIdx];
                                uint po2 = 1;
                                //4개의 gpu복셀이 4byte 에 1byte씩 차지중
                                for (int i = 0; i < 4; i++)
                                {
                                    for (int j = 0; j < CPUVoxelLightMax; j++)
                                    {
                                        bool isLight = (curGPUVoxelLight & po2) != 0;
                                        if(isLight)
                                        {
                                            if(m_CPUVoxelLightCPU[voxelIdx, j] == -1)
                                            {
                                                Debug.Log("문제가있다");
                                                string binary = Convert.ToString(curGPUVoxelLight, 2).PadLeft(32, '0');
                                                Debug.Log($"VoxelIdx : {voxelIdx}, curGPUVoxelIdx : {curGPUVoxelIdx}, gpuBinary : {binary}");
                                                return;
                                            }
                                            GPULightIdx.Add(m_CPUVoxelLightCPU[voxelIdx, j]);
                                        }
                                        po2 = po2 << 1;
                                    }
                                    Vector3 gpuVoxelPos = voxelPos + new Vector3(gpux * 4 + i, gpuy, gpuz);
                                    Vector3 gpuVoxelCenter = gpuVoxelPos + Vector3.one * 0.5f * GPUVoxelSize;
                                    string gpuVoxelmsg = "";
                                    if (GPULightIdx.Count > 0)
                                    {
                                        for (int j = 0; j < GPULightIdx.Count; j++)
                                        {
                                            gpuVoxelmsg += GPULightIdx[j] + ", ";
                                        }
                                        Debug.Log($"GPUVoxelPos : {gpuVoxelPos}, GPUVoxelCenter : {gpuVoxelCenter}, GPUVoxelmsg : {gpuVoxelmsg}, lightCount : {GPULightIdx.Count}");

                                        GPULightIdx.Clear();
                                    }
                                    else
                                    {
                                        gpuVoxelmsg += "Empty";
                                    }
                                    //Debug.Log($"GPUVoxelPos : {gpuVoxelPos}, GPUVoxelCenter : {gpuVoxelCenter}, GPUVoxelmsg : {gpuVoxelmsg}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    Color[] arr_Colors = new Color[8]
    {
        Color.black, Color.red, new Color(1,0.5f,0,1), Color.yellow, Color.green, Color.blue, new Color(0.5f, 0.5f, 1, 1), Color.magenta
    };
    void DrawCPUVoxelGizmo()
    {
        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        for (int z = 0; z < CPUVoxelHorizontalCount; z++)
        {
            for (int y = 0; y < CPUVoxelVerticalCount; y++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    Vector3Int curVoxelGridPos = m_CurCPUVoxelGridPos + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    if(m_DrawGizmoVoxelKind== E_DrawGizmoVoxelKind.CPU_XZFlat && curVoxelGridPos.y != m_DrawGizmoYGridPos)
                    {
                        continue;
                    }
                    Vector3 voxelCenter = curVoxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(curVoxelGridPos);
                    int lightCount = 0;
                    for (int i = 0; i < CPUVoxelLightMax; i++)
                    {
                        if (m_CPUVoxelLightCPU[voxelIdx, i] != -1)
                        {
                            lightCount++;
                        }
                    }
                    Gizmos.color = arr_Colors[lightCount];
                    Gizmos.DrawWireCube(voxelCenter, Vector3.one * CPUVoxelSize);
                }
            }
        }
    }
    void DrawGPUVoxelGizmo()
    {
        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        int[] GPUVoxelLight = new int[GPUVoxelDataCount];
        m_GPUVoxelLight.GetData(GPUVoxelLight);

        int GPUVoxelXAxisCount = (GPUVoxelAxisCount >> 2);
        int GPUVoxelDataCountInCPUVoxel = GPUVoxelXAxisCount * GPUVoxelAxisCount * GPUVoxelAxisCount;

        for (int z = 0; z < CPUVoxelHorizontalCount; z++)
        {
            for (int y = 0; y < CPUVoxelVerticalCount; y++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    Vector3Int curVoxelGridPos = m_CurCPUVoxelGridPos + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    Vector3 voxelPos = curVoxelGridPos * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(curVoxelGridPos);

                    for (int gpuz = 0; gpuz < GPUVoxelAxisCount; gpuz++)
                    {
                        for (int gpuy = 0; gpuy < GPUVoxelAxisCount; gpuy++)
                        {
                            for (int gpux = 0; gpux < GPUVoxelXAxisCount; gpux++)
                            {
                                int curGPUVoxelIdx = gpux + gpuy * GPUVoxelXAxisCount + gpuz * GPUVoxelXAxisCount * GPUVoxelAxisCount + voxelIdx * GPUVoxelDataCountInCPUVoxel;
                                int curGPUVoxelLight = GPUVoxelLight[curGPUVoxelIdx];
                                uint po2 = 1;
                                //4개의 gpu복셀이 4byte 에 1byte씩 차지중
                                for (int i = 0; i < 4; i++)
                                {
                                    int lightCount = 0;

                                    for (int j = 0; j < CPUVoxelLightMax; j++)
                                    {
                                        bool isLight = (curGPUVoxelLight & po2) != 0;
                                        if (isLight)
                                        {
                                            lightCount++;
                                        }
                                        po2 = po2 << 1;
                                    }
                                    Vector3 gpuVoxelPos = voxelPos + new Vector3(gpux * 4 + i, gpuy, gpuz);
                                    if(m_DrawGizmoVoxelKind == E_DrawGizmoVoxelKind.GPU_XZFlat && m_DrawGizmoYGridPos != gpuVoxelPos.y)
                                    {
                                        continue;
                                    }
                                    Vector3 gpuVoxelCenter = gpuVoxelPos + Vector3.one * 0.5f * GPUVoxelSize;
                                    Gizmos.color = arr_Colors[lightCount];
                                    Gizmos.DrawWireCube(gpuVoxelCenter, Vector3.one * GPUVoxelSize);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        if(Application.isPlaying == false)
        {
            return;
        }

        if (m_MoveVoxelGizmoTime > 0)
        {
            m_MoveVoxelGizmoTime -= Time.deltaTime;
            for (int i = 0; i < CPUVoxelCount; i++)
            {
                Vector3Int voxelGridPos = GetVoxelGridPos(i);
                Vector3 voxelCenter = voxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;

                if(m_MoveVoxel.Contains(i))
                {
                    Gizmos.color = Color.white;
                }
                else
                {
                    Gizmos.color = Color.black;
                }
                Gizmos.DrawWireCube(voxelCenter, Vector3.one * CPUVoxelSize);
            }
            return;
        }

        switch (m_DrawGizmoVoxelKind)
        {
            case E_DrawGizmoVoxelKind.CPU:          
            case E_DrawGizmoVoxelKind.CPU_XZFlat:
                DrawCPUVoxelGizmo();
                break;
            case E_DrawGizmoVoxelKind.GPU:
            case E_DrawGizmoVoxelKind.GPU_XZFlat:
                DrawGPUVoxelGizmo();
                break;
        }
        
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
    public int Type;
    public float Range;
    public Vector3 Pos;

    public LightData(Light light)
    {
        Type = (int)light.type;
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
    public void Log()
    {
        Debug.Log($"Type : {Type}, Range : {Range}, Pos : {Pos}");
    }
}