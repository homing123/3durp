using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PerlinNoise : MonoBehaviour
{
    [SerializeField] bool m_UseGPU = true;
    [SerializeField][Min(0.01f)] float m_Scale = 7f;
    [SerializeField] float m_GradientRadianMul;
    [SerializeField][Min(0.01f)] float m_Amplitude;
    [SerializeField][Min(0.01f)] float m_Frequency;
    [SerializeField][Min(0.01f)] float m_Persistence;
    [SerializeField][Min(0.01f)] float m_Lacunarity;
    [SerializeField][Range(1, 10)] int m_Octave;

    float LastScale;
    float LastGradientRadianMul;
    float LastAmplitude;
    float LastFrequency;
    float LastPersistence;
    float LastLacunarity;
    int LastOctaves;

    [SerializeField] ComputeShader m_CS;
    [SerializeField] ComputeBuffer m_CSBuffer;

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
        if(m_UseGPU)
        {

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
                        value = PerlinNoise2D(x * 0.01f * frequency, y * 0.01f * frequency, D_PerlinGradientVec);
                        amplitude *= m_Persistence;
                        frequency *= m_Lacunarity;
                    }
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
        result = result * 0.5f + 0.5f;

        return result;
    }
}
