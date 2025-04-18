using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
        VoxelInit = 1,
        VoxelUpdate = 2,
        VoxelDrawInfoSetting = 3,
    }
    public enum E_DrawGizmoVoxelKind
    {
        CPU,
        CPU_XZFlat,
        GPU,
        GPU_XZFlat
    }
    //���� = ���� ����Ʈ��
    //GridPos = �����׸���ȭ �Ͽ� 1������ 1�϶��� ��ġ
    //������ġ = ���������� �ּҰ�
    //����ũ�� 8 => GridPos 1,0,-1 => ������ġ (8, 0, -8), �������� (8 ~ 16, 0 ~ 8, -8 ~ 0)

    public const int CPUVoxelLightMax = 8; //����1ĭ�� �ִ� ����Ʈ ����
    public const int CPUVoxelSize = 8; //cpu ���� ������� ũ��
    public const int CPUVoxelHalfSize = CPUVoxelSize / 2;
    public const int CPUVoxelHorizontalCount = 7; //cpu ���� xz�� ����
    public const int CPUVoxelVerticalCount = 3; //cpu ���� y�� ����
    public const int GPUVoxelSize = 1; //gpu ���� ������� ũ��
    public const int GPUVoxelAxisCount = 8; //gpu ���� �� ���� ���� ex 32 = 32 * 32 * 32 = 1 cpu ����
    public const int MaxLightCount = 128; //�ִ� ����Ʈ ����

    public const int CPUVoxelCount = CPUVoxelHorizontalCount * CPUVoxelHorizontalCount * CPUVoxelVerticalCount; //cpu ���� ��ü ����
    public const int GPUVoxelCount = CPUVoxelSize * CPUVoxelSize * CPUVoxelSize * CPUVoxelCount; //gpu ���� ��ü ����
    public const int GPUVoxelDataCount = GPUVoxelCount >> 2;


    [SerializeField] ComputeShader m_CSVoxelLight;
    [SerializeField] E_DrawGizmoVoxelKind m_DrawGizmoVoxelKind;
    [SerializeField] [Range(0, CPUVoxelVerticalCount - 1)] int m_DrawGizmoCPUYIdx;
    [SerializeField] [Range(0, CPUVoxelVerticalCount * CPUVoxelSize - 1)] int m_DrawGizmoGPUYIdx;
    [SerializeField] bool m_DrawGizmoOnlyLightVoxel;

    Light[] m_Lights; //�� ���� ��� ����Ʈ
    LightData[] m_LightDataCPU; //gpu�� �ѱ� ����Ʈ ���� = to gpu > m_LightDataGPU
    ComputeBuffer m_LightDataGPU;

    Vector3Int m_CurCPUVoxelGridPos; //���� �迭 �� ���� ī�޶� ���� ������ �ε���
    Vector3Int m_PreCPUVoexlGridPos; //������������ ī�޶� ���� ������ �ε��� (�����̵� �̺�Ʈ ��)
    int[,] m_CPUVoxelLightCPU; //cpu ���� ���� ����Ʈ �ε��� [voxel idx, 0 ~ VoxelLightMax - 1] = to gpu > m_CPUVoxelLightGPU
    ComputeBuffer m_CPUVoxelLightGPU;
    ComputeBuffer m_GPUVoxelLight; //1 Voxel = 1 byte, {(3,y,0), (2,y,0), (1,y,0), (0,y,0)}


    ComputeBuffer m_CheckCPUVoxels; //������ �ʿ��� ����
    ComputeBuffer m_InitCPUVoxels; //�ʱ�ȭ�� �ʿ��� ����

    uint[] m_CPUVoxelUpdateBitmaskCPU;
    ComputeBuffer m_CPUVoxelUpdateBitmaskGPU; //������Ʈ�� �ʿ��� ����Ʈ�ε��� ��Ʈ����ũ�� ǥ�� cpu������ 4byte (1byte�൵ ������ cpu�����̴� ó������)

    ComputeBuffer m_GPUVoxelDrawInfo; //GPU���� �׸������� �ʿ��� ����
    Mesh m_VoxelMesh;
    [SerializeField] Material m_VoxelMat;
    uint[] m_ArgsData = new uint[5];
    ComputeBuffer m_ArgsBuffer;
    List<int> m_MoveVoxel = new List<int>(); //�����ӿ� ���� ���� �����ֱ����� �ӽ÷δ�Ƶ� ����
    float m_MoveVoxelGizmoTime;
    
    [SerializeField] bool m_DrawMoveVoxel;
    private void Awake()
    {
        m_Lights = new Light[MaxLightCount];
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
        m_CPUVoxelUpdateBitmaskCPU = new uint[CPUVoxelCount];
        m_CPUVoxelUpdateBitmaskGPU = new ComputeBuffer(CPUVoxelCount, sizeof(int));

        m_CPUVoxelLightGPU = new ComputeBuffer(m_CPUVoxelLightCPU.Length, sizeof(int));
        m_CPUVoxelLightGPU.SetData(m_CPUVoxelLightCPU);
        m_CheckCPUVoxels = new ComputeBuffer(CPUVoxelCount, sizeof(int));
        m_InitCPUVoxels = new ComputeBuffer(CPUVoxelCount, sizeof(int));
        m_GPUVoxelLight = new ComputeBuffer(GPUVoxelDataCount, sizeof(int)); //4byte = 4Voxel
        m_GPUVoxelDrawInfo = new ComputeBuffer(GPUVoxelCount, HMUtil.StructSize(typeof(VoxelDrawInfo)));

        int[] arr_zero = new int[GPUVoxelDataCount];
        Array.Fill(arr_zero, 0);
        m_GPUVoxelLight.SetData(arr_zero);


        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_LightData", m_LightDataGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_CheckCPUVoxels", m_CheckCPUVoxels);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_CPUVoxelLight", m_CPUVoxelLightGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelAllLight, "_GPUVoxelLight", m_GPUVoxelLight);

        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelInit, "_InitCPUVoxels", m_InitCPUVoxels);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelInit, "_GPUVoxelLight", m_GPUVoxelLight);

        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelUpdate, "_LightData", m_LightDataGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelUpdate, "_CheckCPUVoxels", m_CheckCPUVoxels);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelUpdate, "_CPUVoxelLight", m_CPUVoxelLightGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelUpdate, "_GPUVoxelLight", m_GPUVoxelLight);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelUpdate, "_CPUVoxelUpdateBitmask", m_CPUVoxelUpdateBitmaskGPU);


        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelDrawInfoSetting, "_LightData", m_LightDataGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelDrawInfoSetting, "_CPUVoxelLight", m_CPUVoxelLightGPU);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelDrawInfoSetting, "_GPUVoxelLight", m_GPUVoxelLight);
        m_CSVoxelLight.SetBuffer((int)E_VoxelLightKernel.VoxelDrawInfoSetting, "_VoxelDrawInfo", m_GPUVoxelDrawInfo);

        m_CSVoxelLight.SetInts("_CurCPUVoxelGridPos", new int[3] { m_CurCPUVoxelGridPos.x, m_CurCPUVoxelGridPos.y, m_CurCPUVoxelGridPos.z });

        m_VoxelMesh = new Mesh();
        Vector3[] vertices = new Vector3[8]
        {
    new Vector3(-0.5f,-0.5f,-0.5f),
    new Vector3( 0.5f,-0.5f,-0.5f),
    new Vector3( 0.5f,-0.5f, 0.5f),
    new Vector3(-0.5f,-0.5f, 0.5f),
    new Vector3(-0.5f, 0.5f,-0.5f),
    new Vector3( 0.5f, 0.5f,-0.5f),
    new Vector3( 0.5f, 0.5f, 0.5f),
    new Vector3(-0.5f, 0.5f, 0.5f),
        };
        int[] lines = {
        0, 1, 1, 2, 2, 3, 3, 0, // bottom
        4, 5, 5, 6, 6, 7, 7, 4, // top
        0, 4, 1, 5, 2, 6, 3, 7  // sides
    };

        m_VoxelMesh.vertices = vertices;
        m_VoxelMesh.SetIndices(lines, MeshTopology.Lines, 0);

        m_ArgsData[0] = (uint)m_VoxelMesh.GetIndexCount(0);
        m_ArgsData[1] = GPUVoxelCount; //����
        m_ArgsData[2] = (uint)m_VoxelMesh.GetIndexStart(0);
        m_ArgsData[3] = (uint)m_VoxelMesh.GetBaseVertex(0);
        m_ArgsData[4] = 0;
        m_ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_ArgsBuffer.SetData(m_ArgsData);

        m_VoxelMat.SetBuffer("_VoxelDrawInfo", m_GPUVoxelDrawInfo);
        m_VoxelMat.SetInt("_GPUVoxelAxisInCPU", CPUVoxelSize);
        m_VoxelMat.SetInt("_CPUVoxelHor", CPUVoxelHorizontalCount);
        m_VoxelMat.SetInt("_CPUVoxelVer", CPUVoxelVerticalCount);
        m_VoxelMat.SetInt("_FlatHeight", m_DrawGizmoVoxelKind == E_DrawGizmoVoxelKind.GPU_XZFlat ? m_DrawGizmoGPUYIdx : -1);
    }
    private void Start()
    {
        LightDataInit();
        VoxelInit2();
        DrawInfoSetting();
    }
    private void Update()
    {
        VoxelUpdate();
        if (m_DrawMoveVoxel && m_MoveVoxelGizmoTime > 0)
        {
            return;
        }
        m_VoxelMat.SetInt("_FlatHeight", m_DrawGizmoVoxelKind == E_DrawGizmoVoxelKind.GPU_XZFlat ? m_DrawGizmoGPUYIdx : -1);

        switch (m_DrawGizmoVoxelKind)
        {
            case E_DrawGizmoVoxelKind.GPU:
            case E_DrawGizmoVoxelKind.GPU_XZFlat:
                DrawGPUVoxel();
                break;
        }
    }

    private void OnDestroy()
    {
        m_LightDataGPU.Release();
        m_CPUVoxelLightGPU.Release();
        m_GPUVoxelLight.Release();
        m_CheckCPUVoxels.Release();
        m_InitCPUVoxels.Release();
        m_CPUVoxelUpdateBitmaskGPU.Release();
        m_GPUVoxelDrawInfo.Release();
        m_ArgsBuffer.Release();
    }
    #region InitData
    void LightDataInit()
    {
        Light[] arr_Light = FindObjectsByType<Light>(FindObjectsSortMode.None);
        int lightIdx = 0;
        for (int i = 0; i < arr_Light.Length; i++)
        {
            if (arr_Light[i].type != LightType.Directional)
            {
                m_Lights[lightIdx] = arr_Light[i];
                m_LightDataCPU[lightIdx] = new LightData(m_Lights[lightIdx]);
                lightIdx++;
            }
        }

        m_LightDataGPU.SetData(m_LightDataCPU);
    }

    void VoxelInit()
    {
        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        List<int> l_CheckList = new List<int>();

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
                    bool addCheckList = false;
                    for (int i = 0; i < MaxLightCount; i++)
                    {
                        if (m_Lights[i] == null)
                        {
                            continue;
                        }
                        bool islight = CheckLight(boxCenter, CPUVoxelHalfSize, m_LightDataCPU[i]);
                        if (islight)
                        {
                            addCheckList = true;
                            if (voxellightIdx >= CPUVoxelLightMax)
                            {
                                Debug.Log($"lights so many {x}, {y}, {z} : {curVoxelGridPos}");
                                break;
                            }
                            m_CPUVoxelLightCPU[voxelIdx, voxellightIdx++] = i;
                        }
                    }
                    if(addCheckList)
                    {
                        l_CheckList.Add(voxelIdx);
                    }
                }
            }
        }

        int[] arr_CheckList = l_CheckList.ToArray();
        int ChecklistCount = arr_CheckList.Length;
        if (ChecklistCount == 0)
        {
            Debug.Log("CheckListCount is zero");
            return;
        }

        m_CheckCPUVoxels.SetData(arr_CheckList);
        m_CPUVoxelLightGPU.SetData(m_CPUVoxelLightCPU);
        m_CSVoxelLight.Dispatch((int)E_VoxelLightKernel.VoxelAllLight, ChecklistCount, 1, 1);
    }
    void VoxelInit2()
    {
        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        List<int> l_CheckList = new List<int>();

        for (int z = 0; z < CPUVoxelHorizontalCount; z++)
        {
            for (int y = 0; y < CPUVoxelVerticalCount; y++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    uint lightBitmask = 0;
                    Vector3Int curVoxelGridPos = m_CurCPUVoxelGridPos + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    Vector3 boxCenter = curVoxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(curVoxelGridPos);
                    int voxellightIdx = 0;
                    bool addCheckList = false;
                    for (int i = 0; i < MaxLightCount; i++)
                    {
                        if (m_Lights[i] == null)
                        {
                            continue;
                        }
                        bool islight = CheckLight(boxCenter, CPUVoxelHalfSize, m_LightDataCPU[i]);
                        if (islight)
                        {
                            if (voxellightIdx >= CPUVoxelLightMax)
                            {
                                Debug.Log($"lights so many {x}, {y}, {z} : {curVoxelGridPos}");
                                break;
                            }
                            lightBitmask = lightBitmask | (((uint)1) << voxellightIdx);
                            addCheckList = true;
                            m_CPUVoxelLightCPU[voxelIdx, voxellightIdx++] = i;
                        }
                    }
                    if (addCheckList)
                    {
                        l_CheckList.Add(voxelIdx);
                    }

                    m_CPUVoxelUpdateBitmaskCPU[voxelIdx] = lightBitmask;
                }
            }
        }

        int[] arr_CheckList = l_CheckList.ToArray();
        int ChecklistCount = arr_CheckList.Length;
        if (ChecklistCount == 0)
        {
            Debug.Log("CheckListCount is zero");
            return;
        }

        m_CheckCPUVoxels.SetData(arr_CheckList);
        m_CPUVoxelLightGPU.SetData(m_CPUVoxelLightCPU);
        m_CPUVoxelUpdateBitmaskGPU.SetData(m_CPUVoxelUpdateBitmaskCPU);
        m_CSVoxelLight.Dispatch((int)E_VoxelLightKernel.VoxelUpdate, ChecklistCount, 1, 1);
    }

    #endregion
    #region VoxelUpdate
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
            //nĭ ������ ��� ���� �� �ִ밪������� n���� ����.
            //�ּҰ�or�ִ밪 idx = centerRemainder + moveDisSign * halfRange
            //��ó���� �������������� �ε������� ������ �־����

            int curChangedIdx = centerRemainder + moveDisSign * halfRange - moveDisSign * i;
            curChangedIdx = curChangedIdx % axisCount;
            curChangedIdx = curChangedIdx < 0 ? curChangedIdx + axisCount : curChangedIdx;
            result[i] = curChangedIdx;
        }
        return result;
    }
    //���� �����ӿ� ���� ��ġ�� �ٲ� ������
    int[] GetGridMoveChangedVoxel(int[] axisx, int[] axisy, int[] axisz)
    {
        //cpuvoxel ť��� x, y, z ������ �ε��̵�
        bool[] isUseVoxel = new bool[CPUVoxelCount];

        //x
        for (int i = 0; i < axisx.Length; i++)
        {
            int xFlatCount = CPUVoxelHorizontalCount * CPUVoxelVerticalCount;
            int startIdx = axisx[i];
            for (int j = 0; j < xFlatCount; j++)
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

        List<int> l_CheckVoxelIdx = new List<int>();

        //������
        m_MoveVoxel.Clear();
        m_MoveVoxelGizmoTime = 1.0f;

        //�����̵��� ���� �����Ͱ����� �ʿ��� ��������Ʈ
        for (int i = 0; i < isUseVoxel.Length; i++)
        {
            if (isUseVoxel[i] == true)
            {
                m_MoveVoxel.Add(i);
                l_CheckVoxelIdx.Add(i);
            }
        }

        return l_CheckVoxelIdx.ToArray();

    }

    void VoxelUpdate()
    {
        List<int> l_RemoveLightIdx = new List<int>();
        List<int> l_AddorTransformationLightIdx = new List<int>();
        List<int> l_CheckVoxelIdx = new List<int>();
        List<int> l_InitVoxelIdx = new List<int>();

        //lightData Update
        for (int i = 0; i < MaxLightCount; i++)
        {

            if (m_Lights[i] == null && m_LightDataCPU[i].isOff() == false)
            {
                //�̹� �����ӿ� ���ŵ� ���
                l_RemoveLightIdx.Add(i);
                m_LightDataCPU[i].Reset();
            }
            else
            {
                if (m_Lights[i] == null)
                {
                    continue;
                }
                //����, �߰��� ���
                LightData curData = new LightData(m_Lights[i]);
                if (m_LightDataCPU[i] != curData)
                {
                    l_AddorTransformationLightIdx.Add(i);
                    m_LightDataCPU[i] = curData;
                }
            }
        }
        if (l_RemoveLightIdx.Count + l_AddorTransformationLightIdx.Count > 0)
        {
            m_LightDataGPU.SetData(m_LightDataCPU);
        }

        m_CurCPUVoxelGridPos = GetCPUVoexlGridPos(Camera.main.transform.position);
        if (m_PreCPUVoexlGridPos != m_CurCPUVoxelGridPos)
        {
            Vector3Int moveGridPos = m_CurCPUVoxelGridPos - m_PreCPUVoexlGridPos;
            //Debug.Log($"���� {moveGridPos}");
            int[] AxisXChangeIdx = GetCheckAxisIdx(CPUVoxelHorizontalCount, m_CurCPUVoxelGridPos.x, moveGridPos.x);
            int[] AxisYChangeIdx = GetCheckAxisIdx(CPUVoxelVerticalCount, m_CurCPUVoxelGridPos.y, moveGridPos.y);
            int[] AxisZChangeIdx = GetCheckAxisIdx(CPUVoxelHorizontalCount, m_CurCPUVoxelGridPos.z, moveGridPos.z);

            m_PreCPUVoexlGridPos = m_CurCPUVoxelGridPos;
            m_CSVoxelLight.SetInts("_CurCPUVoxelGridPos", new int[3] { m_CurCPUVoxelGridPos.x, m_CurCPUVoxelGridPos.y, m_CurCPUVoxelGridPos.z });

            //��ġ�� �ٲ� ������
            int[] arr_PosChangedVoxel = GetGridMoveChangedVoxel(AxisXChangeIdx, AxisYChangeIdx, AxisZChangeIdx);

            //��ġ�� �ٲ� ������ ������ ���� ���� ������ üũ��, ���� ������ init����
            int[] arr_lightIdx = new int[MaxLightCount];
            for (int i = 0; i < arr_PosChangedVoxel.Length; i++)
            {
                int curVoxelIdx = arr_PosChangedVoxel[i];
                Vector3Int curVoxelGridPos = GetVoxelGridPos(curVoxelIdx);
                Vector3 curVoxelCenter = curVoxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;

                int voxelLightIdx = 0;
                uint updateBitmask = 0;
                Array.Fill(arr_lightIdx, -1);
                for (int lightIdx = 0; lightIdx < MaxLightCount; lightIdx++)
                {
                    if (m_Lights[lightIdx] == null)
                    {
                        continue;
                    }
                    bool isLight = CheckLight(curVoxelCenter, CPUVoxelHalfSize, m_LightDataCPU[lightIdx]);
                    if (isLight)
                    {
                        if (voxelLightIdx >= CPUVoxelLightMax)
                        {
                            Debug.Log($"lights so many {curVoxelIdx} : {curVoxelGridPos}");
                            break;
                        }
                        arr_lightIdx[voxelLightIdx++] = lightIdx;
                    }
                }

                if (voxelLightIdx == 0)
                {
                    l_InitVoxelIdx.Add(curVoxelIdx);
                }
                else
                {
                    l_CheckVoxelIdx.Add(curVoxelIdx);
                }

                for (int j = 0; j < CPUVoxelLightMax; j++)
                {
                    //���� �� ���� -1�� �ƴѰ��� ������ -1�� �ƴ����� ���� �� -1�� �� ���� ������Ʈ ��Ʈ����ũ 1
                    if (arr_lightIdx[j] != -1 || (arr_lightIdx[j] == -1 && m_CPUVoxelLightCPU[curVoxelIdx, j] != -1))
                    {
                        updateBitmask = updateBitmask | ((uint)1 << j);
                    }
                    m_CPUVoxelLightCPU[curVoxelIdx, j] = arr_lightIdx[j];               
                }
              
                m_CPUVoxelUpdateBitmaskCPU[curVoxelIdx] = updateBitmask;
            }
        }

        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;

        for (int z = 0; z < CPUVoxelHorizontalCount; z++)
        {
            for (int y = 0; y < CPUVoxelVerticalCount; y++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    uint lightBitmask = 0;
                    Vector3Int curVoxelGridPos = m_CurCPUVoxelGridPos + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    Vector3 boxCenter = curVoxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(curVoxelGridPos);
                    if(l_CheckVoxelIdx.Contains(voxelIdx) || l_InitVoxelIdx.Contains(voxelIdx))
                    {
                        //�����̵����� ���� ���������� üũ�Ǿ��ٸ� ó�� ����
                        continue;
                    }
                    bool addCheckList = false;
                    for (int i = 0; i < l_RemoveLightIdx.Count; i++)
                    {
                        LightData curLightData = m_LightDataCPU[l_RemoveLightIdx[i]];
                        //���ŵ� ��� ���� cpu ������ ���� ������� �����ؾ���
                        for (int cpuVoxelLightIdx = 0; cpuVoxelLightIdx < CPUVoxelLightMax; cpuVoxelLightIdx++)
                        {
                            if (m_CPUVoxelLightCPU[voxelIdx, cpuVoxelLightIdx] == l_RemoveLightIdx[i])
                            {
                                m_CPUVoxelLightCPU[voxelIdx, cpuVoxelLightIdx] = -1;
                                lightBitmask = lightBitmask | ((uint)1) << cpuVoxelLightIdx;
                                addCheckList = true;
                                break;
                            }
                        }
                    }

                    for (int i = 0; i < l_AddorTransformationLightIdx.Count; i++)
                    {
                        LightData curLightData = m_LightDataCPU[l_AddorTransformationLightIdx[i]];
                        //�߰�, ���� �� ��� cpu���� üũ����Ʈ �ٽ��ؾ���

                        bool islight = CheckLight(boxCenter, CPUVoxelHalfSize, curLightData);

                        if (islight)
                        {
                            //cpu������ ������ ������ �ְ� �־��ٸ� cpuvoxel������  �������ص���
                            bool hasLightIdx = false;
                            for (int cpuVoxelLightIdx = 0; cpuVoxelLightIdx < CPUVoxelLightMax; cpuVoxelLightIdx++)
                            {
                                if (m_CPUVoxelLightCPU[voxelIdx, cpuVoxelLightIdx] == l_AddorTransformationLightIdx[i])
                                {
                                    hasLightIdx = true;
                                    lightBitmask = lightBitmask | ((uint)1) << cpuVoxelLightIdx;
                                    addCheckList = true;
                                    break;
                                }
                            }
                            if (hasLightIdx == false)
                            {
                                //������ ������ ���ְ� �־��ٸ� ��ĭ ã�Ƽ� �����ʿ�
                                for (int cpuVoxelLightIdx = 0; cpuVoxelLightIdx < CPUVoxelLightMax; cpuVoxelLightIdx++)
                                {
                                    if (m_CPUVoxelLightCPU[voxelIdx, cpuVoxelLightIdx] == -1)
                                    {
                                        m_CPUVoxelLightCPU[voxelIdx, cpuVoxelLightIdx] = l_AddorTransformationLightIdx[i];
                                        lightBitmask = lightBitmask | ((uint)1) << cpuVoxelLightIdx;
                                        addCheckList = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //������ ������ ������ �ִٰ� ���������ӿ� ���ְԵ� ��� �����������
                            for (int cpuVoxelLightIdx = 0; cpuVoxelLightIdx < CPUVoxelLightMax; cpuVoxelLightIdx++)
                            {
                                if (m_CPUVoxelLightCPU[voxelIdx, cpuVoxelLightIdx] == l_AddorTransformationLightIdx[i])
                                {
                                    m_CPUVoxelLightCPU[voxelIdx, cpuVoxelLightIdx] = -1;
                                    lightBitmask = lightBitmask | ((uint)1) << cpuVoxelLightIdx;
                                    addCheckList = true;
                                    break;
                                }
                            }
                        }

                    }

                    if (addCheckList)
                    {
                        l_CheckVoxelIdx.Add(voxelIdx);
                        m_CPUVoxelUpdateBitmaskCPU[voxelIdx] = lightBitmask;
                    }
                }
            }
        }

        if(l_InitVoxelIdx.Count + l_CheckVoxelIdx.Count > 0)
        {
            m_CPUVoxelLightGPU.SetData(m_CPUVoxelLightCPU);
        }
        if (l_InitVoxelIdx.Count > 0)
        {
            m_InitCPUVoxels.SetData(l_InitVoxelIdx.ToArray());
            m_CSVoxelLight.Dispatch((int)E_VoxelLightKernel.VoxelInit, l_InitVoxelIdx.Count, 1, 1);
        }

        if (l_CheckVoxelIdx.Count > 0)
        {
            m_CheckCPUVoxels.SetData(l_CheckVoxelIdx.ToArray());
            m_CPUVoxelUpdateBitmaskGPU.SetData(m_CPUVoxelUpdateBitmaskCPU);
            m_CSVoxelLight.Dispatch((int)E_VoxelLightKernel.VoxelUpdate, l_CheckVoxelIdx.Count, 1, 1);
        }

        if (l_InitVoxelIdx.Count + l_CheckVoxelIdx.Count > 0)
        {
            DrawInfoSetting();
        }
    }

    #endregion
    #region Light
    //����Ʈ������Ʈ ���� ��, ����׸��忡�� ��Ʈ����ũ�� �̿��� �������� �����԰� ���ÿ� �Ѵ� ������Ʈ�� ����Ǵ� ���� ����ǵ��� ��
    //cpu���� ������Ʈ ����� ���� ������ gpu���� �ѹ��� ó���ǵ��� �ؾ���
    
    public void AddLight(Light light)
    {
        for (int i = 0; i < MaxLightCount; i++)
        {
            if (m_Lights[i] == null)
            {
                m_Lights[i] = light;
                break;
            }
        }
    }
    public void RemoveLight(Light light)
    {
        for (int i = 0; i < MaxLightCount; i++)
        {
            if (m_Lights[i] == light)
            {
                m_Lights[i] = null;
                break;
            }
        }
    }
    #endregion
    #region Common
    bool CheckLight(Vector3 boxCenter, float boxhalfSize, in LightData light)
    {
        switch((LightType)light.Type)
        {
            case LightType.Point:
                float extensionBoxhalfSize = boxhalfSize + light.Range;
                Vector3 lightPos = light.Pos;
                Vector3 b2l = lightPos - boxCenter; //box to light
                Vector3 abs_b2l = new Vector3(MathF.Abs(b2l.x), MathF.Abs(b2l.y), MathF.Abs(b2l.z));
                //������ Ȯ�� �ڽ� �ȿ� ������ ������ �ƿ�
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
                Vector3 nearBoxPoint = tempSign * boxhalfSize + boxCenter; //���� ���� ����� �ڽ��� ����
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
    #endregion
    #region Log
    [ContextMenu("LogLightData")]
    public void LogLightData()
    {
        Debug.Log($"LogLightData");

        for (int i = 0; i < MaxLightCount; i++)
        {
            if (m_Lights[i] != null)
            {
                m_LightDataCPU[i].Log(i);
            }
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

    [ContextMenu("LogCPUVoxelBitmask")]
    public void LogCPUVoxelBitmask()
    {
        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        float halfvoxelSize = CPUVoxelSize * 0.5f;
        Debug.Log($"LogCPUVoxelBitmask m_CurCPUVoxelGridPos : {m_CurCPUVoxelGridPos}");
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
                    uint curBitmask = m_CPUVoxelUpdateBitmaskCPU[voxelIdx];
                    string binary = Convert.ToString(curBitmask, 2).PadLeft(32, '0');
                    Debug.Log($"voxelPos : {voxelPos}, voxelCenter : {voxelCenter}, voxelIdx {voxelIdx}, bitmask: {binary}");
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
                                //4���� gpu������ 4byte �� 1byte�� ������
                                for (int i = 0; i < 4; i++)
                                {
                                    for (int j = 0; j < CPUVoxelLightMax; j++)
                                    {
                                        bool isLight = (curGPUVoxelLight & po2) != 0;
                                        if(isLight)
                                        {
                                            if(m_CPUVoxelLightCPU[voxelIdx, j] == -1)
                                            {
                                                Debug.Log("�������ִ�");
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

    #endregion
    #region Gizmo
    void DrawCPUVoxelGizmo()
    {
        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        int worldMinVoxelGridPos = m_CurCPUVoxelGridPos.y - minusVer;
        for (int z = 0; z < CPUVoxelHorizontalCount; z++)
        {
            for (int y = 0; y < CPUVoxelVerticalCount; y++)
            {
                for (int x = 0; x < CPUVoxelHorizontalCount; x++)
                {
                    Vector3Int curVoxelGridPos = m_CurCPUVoxelGridPos + new Vector3Int(x - minusHor, y - minusVer, z - minusHor);
                    if(m_DrawGizmoVoxelKind == E_DrawGizmoVoxelKind.CPU_XZFlat)
                    {
                        //������� �ּҰ�~�ִ밪�� 0 ~ CPUVoxelVerticalCount - 1�� ��Ī
                        if(curVoxelGridPos.y - worldMinVoxelGridPos != m_DrawGizmoCPUYIdx)
                        {
                            continue;
                        }
                    }
                    Vector3 voxelCenter = curVoxelGridPos * CPUVoxelSize + Vector3.one * 0.5f * CPUVoxelSize;
                    int voxelIdx = GetVoxelIdx(curVoxelGridPos);
                    int lightCount = 0;
                    Color color = Color.black;
                    for (int i = 0; i < CPUVoxelLightMax; i++)
                    {
                        if (m_CPUVoxelLightCPU[voxelIdx, i] != -1)
                        {
                            lightCount++;
                            color += m_LightDataCPU[m_CPUVoxelLightCPU[voxelIdx, i]].Color;
                        }
                    }
                    Gizmos.color = color;
                    if(m_DrawGizmoOnlyLightVoxel == false || (m_DrawGizmoOnlyLightVoxel && lightCount > 0))
                    {
                        Gizmos.DrawWireCube(voxelCenter, Vector3.one * CPUVoxelSize);
                    }
                }
            }
        }
    }
    void DrawInfoSetting()
    {
        m_CSVoxelLight.Dispatch((int)E_VoxelLightKernel.VoxelDrawInfoSetting, GPUVoxelDataCount, 1, 1);
    }
    void DrawGPUVoxel()
    {
        Vector3 boundCenter = m_CurCPUVoxelGridPos * CPUVoxelSize + CPUVoxelSize * 0.5f * Vector3.one;
        Vector3 boundSize = new Vector3(CPUVoxelHorizontalCount, CPUVoxelVerticalCount, CPUVoxelHorizontalCount) * CPUVoxelSize;
        Bounds fieldBound = new Bounds(boundCenter, boundSize);

        if(m_DrawGizmoVoxelKind==E_DrawGizmoVoxelKind.GPU_XZFlat)
        {
            m_ArgsData[1] = GPUVoxelAxisCount * GPUVoxelAxisCount* CPUVoxelHorizontalCount * CPUVoxelHorizontalCount; //����
            m_ArgsBuffer.SetData(m_ArgsData);
        }
        else
        {
            m_ArgsData[1] = GPUVoxelCount; //����
            m_ArgsBuffer.SetData(m_ArgsData);
        }
        Graphics.DrawMeshInstancedIndirect(m_VoxelMesh, 0, m_VoxelMat, fieldBound, m_ArgsBuffer);
    }
    void DrawGPUVoxelGizmo()
    {

        int minusHor = CPUVoxelHorizontalCount / 2;
        int minusVer = CPUVoxelVerticalCount / 2;
        int worldMinVoxelGridPos = m_CurCPUVoxelGridPos.y - minusVer;
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
                                //4���� gpu������ 4byte �� 1byte�� ������
                                for (int i = 0; i < 4; i++)
                                {
                                    Color color = new Color(0, 0, 0, 1);
                                    int lightCount = 0;

                                    for (int j = 0; j < CPUVoxelLightMax; j++)
                                    {
                                        bool isLight = (curGPUVoxelLight & po2) != 0;
                                        if (isLight)
                                        {
                                            color += m_LightDataCPU[m_CPUVoxelLightCPU[voxelIdx, j]].Color;
                                            lightCount++;
                                        }
                                        po2 = po2 << 1;
                                    }
                                    Vector3 gpuVoxelPos = voxelPos + new Vector3(gpux * 4 + i, gpuy, gpuz);

                                    if (m_DrawGizmoVoxelKind == E_DrawGizmoVoxelKind.GPU_XZFlat)
                                    {
                                        if (gpuVoxelPos.y - worldMinVoxelGridPos * CPUVoxelSize != m_DrawGizmoGPUYIdx)
                                        {
                                            continue;
                                        }                            
                                    }

                                
                                    Vector3 gpuVoxelCenter = gpuVoxelPos + Vector3.one * 0.5f * GPUVoxelSize;
                                    Gizmos.color = color;
                                    if (m_DrawGizmoOnlyLightVoxel == false || (m_DrawGizmoOnlyLightVoxel && lightCount > 0))
                                    {
                                        Gizmos.DrawWireCube(gpuVoxelCenter, Vector3.one * GPUVoxelSize);
                                    }
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

        if (m_DrawMoveVoxel && m_MoveVoxelGizmoTime > 0)
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
                DrawGPUVoxel();
                //DrawGPUVoxelGizmo();
                break;
        }

    }
    #endregion
}


public struct VoxelDrawInfo
{
    public Vector3 PosWS;
    public float padding;
    public Color color;
}


//�ϴ� ����Ʈ�� �غ���
public struct LightData
{
    public int Type;
    public Vector3Int padding;
    public Color Color;
    public float Range;
    public Vector3 Pos;

    public LightData(Light light)
    {
        Type = (int)light.type;
        Color = light.color;
        Pos = light.transform.position;
        Range = light.range;
        padding = Vector3Int.zero;
    }

    public bool isOff()
    {
        return Range == 0;
    }
    public void Reset()
    {
        Type = 0;
        Range = 0;
    }
    public bool isUpdate(Light light)
    {
        Vector3 pos = light.transform.position;
        float range = light.range;
        Color color = light.color;
        if (pos != Pos || range != Range || color != Color)
        {
            return true;
        }
        return false;
    }
    public static bool operator ==(LightData d1, LightData d2)
    {
        return d1.Type == d2.Type && d1.Range == d2.Range && d1.Pos == d2.Pos && d1.Color == d2.Color;
    }
    public static bool operator !=(LightData d1, LightData d2)
    {
        return !(d1 == d2);
    }
    public void Log(int idx)
    {
        Debug.Log($"IDX : {idx}, Type: {Type}, Range : {Range}, Pos : {Pos}");
    }
}