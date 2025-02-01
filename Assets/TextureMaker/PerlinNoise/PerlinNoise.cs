using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PerlinNoise : MonoBehaviour
{
    const int GridGradient_Thread_Width = 32;
    const int Perlin_Thread_Width = 32;
    const int Group_Max = 512;

    [SerializeField] bool m_UseGPU = true;
    [SerializeField][Min(0.01f)] float m_Scale = 7f;
    [SerializeField][Range(0.0f, 1.0f)] float m_GradientRadianMul;
    [SerializeField][Range(0.01f, 1.0f)] float m_Amplitude;
    [SerializeField][Min(0.1f)] float m_Frequency;
    [SerializeField][Min(0.1f)] float m_Persistence;
    [SerializeField][Range(1.5f, 2.5f)] float m_Lacunarity;
    [SerializeField][Range(1, 10)] int m_Octave;



    float LastScale;
    float LastGradientRadianMul;
    float LastAmplitude;
    float LastFrequency;
    float LastPersistence;
    float LastLacunarity;
    int LastOctaves;

    int m_Width;
    int m_Height;

    [SerializeField] ComputeShader m_CS;
    ComputeBuffer m_GradientVecBuffer;
    ComputeBuffer m_ColorBuffer;

    public static PerlinNoise Ins;
    private void Awake()
    {
        Ins = this;
    }

    private void Start()
    {
        
    }
    private void Update()
    {
        
    }

    void DispatchCS()
    {
        Vector2 offset = new Vector2(173191.511f, 177191.311f);
        float sizeMul = m_Frequency * Mathf.Pow(m_Lacunarity, m_Octave);
        Vector2 size = new Vector2(m_Width * m_Scale * 0.01f * sizeMul, m_Height * m_Scale * 0.01f * sizeMul);
        Vector2Int gridMin = new Vector2Int((int)offset.x, (int)offset.y);
        Vector2 max = offset + size;
        Vector2Int gridSize = new Vector2Int((int)max.x + 2 - gridMin.x, (int)max.y + 2 - gridMin.y);
        //Debug.Log("그리드 사이즈 : "+gridSize);
        //Debug.Log($"{size}, {gridMin}, {gridSize}, {offset}, {max}");
        m_GradientVecBuffer = new ComputeBuffer(gridSize.x * gridSize.y, sizeof(float) * 2);
        m_ColorBuffer = new ComputeBuffer(m_Width * m_Height, sizeof(float) * 4);
        m_CS.SetBuffer(0, "_GradientVecBuffer", m_GradientVecBuffer);
        m_CS.SetBuffer(1, "_GradientVecBuffer", m_GradientVecBuffer);
        m_CS.SetBuffer(1, "_ColorBuffer", m_ColorBuffer);

        int gradientKernel_x = gridSize.x / GridGradient_Thread_Width + (gridSize.x % GridGradient_Thread_Width == 0 ? 0 : 1);
        int gradientKernel_y = gridSize.y / GridGradient_Thread_Width + (gridSize.y % GridGradient_Thread_Width == 0 ? 0 : 1);

        int perlinKernel_x = m_Width / GridGradient_Thread_Width + (m_Width % GridGradient_Thread_Width == 0 ? 0 : 1);
        int perlinKernel_y = m_Height / GridGradient_Thread_Width + (m_Height % GridGradient_Thread_Width == 0 ? 0 : 1);

        //int2 _GridMin;
        //int2 _GridSize;
        //float _GradientRadianMul;

        //float2 _Offset;
        //int2 _TexSize;
        //float _Scale;
        //float _Amplitude;
        //float _Frequency;
        //float _Persistence;
        //float _Lacunarity;
        //int _Octaves;

        m_CS.SetInts("_GridMin", new int[2] { gridMin.x, gridMin.y });
        m_CS.SetInts("_GridSize", new int[2] { gridSize.x, gridSize.y });
        m_CS.SetFloat("_GradientRadianMul", m_GradientRadianMul);

        m_CS.SetVector("_Offset", offset);
        m_CS.SetInts("_TexSize", new int[2] { m_Width, m_Height });
        m_CS.SetFloat("_Scale", m_Scale);
        m_CS.SetFloat("_Amplitude", m_Amplitude);
        m_CS.SetFloat("_Frequency", m_Frequency);
        m_CS.SetFloat("_Persistence", m_Persistence);
        m_CS.SetFloat("_Lacunarity", m_Lacunarity);
        m_CS.SetInt("_Octaves", m_Octave);

        m_CS.Dispatch(0, gradientKernel_x, gradientKernel_y, 1);
        m_CS.Dispatch(1, perlinKernel_x, perlinKernel_y, 1);
        return;
        Vector2[] gradientVec = new Vector2[gridSize.x * gridSize.y];
        m_GradientVecBuffer.GetData(gradientVec);
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Debug.Log($"{x}, {y} : {gradientVec[x + y * gridSize.x]}");
            }
        }
        return;
        Color[] colors = new Color[m_Width * m_Height];
        m_ColorBuffer.GetData(colors);
        for (int y = 0; y < m_Height; y++)
        {
            for (int x = 0; x < m_Width; x++)
            {
                Debug.Log($"{x}, {y} : {colors[x + y * m_Width]}");
            }
        }

      

    }

    void ReleaseCSBuffer()
    {
        m_GradientVecBuffer.Release();
        m_ColorBuffer.Release();
    }
    public bool isChangedOption()
    {
        return m_Scale != LastScale || m_GradientRadianMul != LastGradientRadianMul ||
            m_Amplitude != LastAmplitude || m_Frequency != LastFrequency ||
            m_Persistence != LastPersistence || m_Lacunarity != LastLacunarity ||
            m_Octave != LastOctaves;
    }
    public void LastValueUpdate()
    {
        LastScale = m_Scale;
        LastGradientRadianMul = m_GradientRadianMul;
        LastAmplitude = m_Amplitude;
        LastFrequency = m_Frequency;
        LastPersistence = m_Persistence;
        LastLacunarity = m_Lacunarity;
        LastOctaves = m_Octave;
    }

    public void CreatePerlinNoise2DBuffer(int width, int height, Color[] buffer)
    {
        SetPerlinNoise2DBuffer(width, height, buffer);
    }
    public Color[] CreatePerlinNoise2DBuffer(int width, int height)
    {
        Color[] arr_color = new Color[width * height];
        SetPerlinNoise2DBuffer(width, height, arr_color);
        return arr_color;
    }
    void SetPerlinNoise2DBuffer(int width, int height, Color[] buffer)
    {
        m_Width = width;
        m_Height = height;
        if(m_UseGPU)
        {
            DispatchCS();
            m_ColorBuffer.GetData(buffer);
            ReleaseCSBuffer();
        }
        else
        {
            if(m_Octave > 3)
            {
                Debug.Log("cpu가 타고있어요... 불타고 있다고!!!");
                Debug.Log("옥타브 4이상은 gpu로");
            }
            Dictionary<Vector2Int, Vector2> D_PerlinGradientVec = new Dictionary<Vector2Int, Vector2>();

            if (buffer.Length != width * height)
            {
                throw new System.Exception("Buffer size error");
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = x + width * y;
                    float frequency = m_Frequency;
                    float amplitude = m_Amplitude;
                    float value = 0;
                    for (int i = 0; i < m_Octave; i++)
                    {
                        value += amplitude * PerlinNoise2D(x * 0.01f * frequency, y * 0.01f * frequency, D_PerlinGradientVec);
                        amplitude *= m_Persistence;
                        frequency *= m_Lacunarity;
                    }
                    value = value * 0.5f + 0.5f;
                    buffer[idx] = new Color(value, value, value, 1);
                }
            }
        }
    }

    public float PerlinInterpolation(float left, float right, float t)
    {
        float ease = 6 * Mathf.Pow(t, 5) - 15 * Mathf.Pow(t, 4) + 10 * Mathf.Pow(t, 3);
        return left + (right - left) * ease;
    }
   
    public float PerlinNoise2D(float x, float y, Dictionary<Vector2Int, Vector2> D_PerlinGradientVec)
    {
        x *= m_Scale;
        y *= m_Scale;
        Vector2 point = new Vector2(x, y);
        Vector2Int lbot = new Vector2Int((int)x, (int)y);
        Vector2Int ltop = new Vector2Int((int)x, (int)y + 1);
        Vector2Int rbot = new Vector2Int((int)x + 1, (int)y);
        Vector2Int rtop = new Vector2Int((int)x + 1, (int)y + 1);

        Vector2Int[] arr_gridPos = new Vector2Int[4] { lbot, ltop, rbot, rtop };
        for (int i = 0; i < 4; i++)
        {
            if (D_PerlinGradientVec.ContainsKey(arr_gridPos[i]) == false)
            {
                float noiseValue = HMUtil.Noise2D(arr_gridPos[i].x, arr_gridPos[i].y) * Mathf.PI * 2 * m_GradientRadianMul;
                D_PerlinGradientVec[arr_gridPos[i]] = noiseValue.RadianToUnitVector2();
            }
        }

        float[] arr_gridWeight = new float[4];

        for (int i = 0; i < 4; i++)
        {
            Vector2 gradientVec = D_PerlinGradientVec[arr_gridPos[i]];
            Vector2 distanceVec = point - arr_gridPos[i];
            arr_gridWeight[i] = Vector2.Dot(gradientVec, distanceVec); //거리벡터가 정규화 되지 않기때문에 -1~1이 아님
        }

        float yLeft = PerlinInterpolation(arr_gridWeight[0], arr_gridWeight[1], y - (int)y);
        float yRight = PerlinInterpolation(arr_gridWeight[2], arr_gridWeight[3], y - (int)y);

        float result = PerlinInterpolation(yLeft, yRight, x - (int)x);

        return result;
    }
}
