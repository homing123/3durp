using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorleyNoise : MonoBehaviour, I_TextureMaker
{
    [SerializeField] [Min(1)]int m_PointCount;
    [SerializeField] bool m_UseSideGrid;
    [SerializeField] [Range(0,0.2f)] float m_DistanceWeight;
    int m_LastPointCount;
    bool m_LastUseSideGrid;
    float m_LastDistanceWeight;

    public static WorleyNoise Ins;
    
    private void Awake()
    {
        Ins = this;
    }

    public bool isChangedOption()
    {
        return m_PointCount != m_LastPointCount || m_UseSideGrid != m_LastUseSideGrid || m_DistanceWeight!= m_LastDistanceWeight;
    }
    public void LastOptionUpdate()
    {
        m_LastPointCount = m_PointCount;
        m_LastUseSideGrid = m_UseSideGrid;
        m_LastDistanceWeight = m_DistanceWeight;
    }

    public Color[] GetColorBuffer(int width, int height)
    {
        Color[] arr_Colors = new Color[width * height];
        //���� �ؽ����� 8���� �ؽ��ĵ� �ִٰ� ������ �� ���� ������
        //�ؽ���ũ�⸸ŭ�� �������� �ϴ� �׸��带 ����� �ش� �׸���ȿ� ����ġ �迭�� �ִ� 2�����迭���°� ������
        //�� �ؼ����� n��°�� ����� ���� ��ġ, �Ÿ� ���� �ʿ��� �̸� �����ϴ°��� �ɼ����� �Ѱ�

        Vector2Int offset = new Vector2Int(71313, 51991);
        Vector2Int gridSize = new Vector2Int(width, height);
        Vector2[] arr_Point = null;
        int pointCount = 0;
        if(m_UseSideGrid)
        {
            pointCount = m_PointCount * 9;
            arr_Point = new Vector2[pointCount];
            for (int i = 0; i < 9; i++)
            {
                Vector2Int gridPos = offset + HMUtil.GetSamplingPos9(i, gridSize);
                for (int z = 0; z < m_PointCount; z++)
                {
                    Vector2 pointPosInNorm = new Vector2(HMUtil.Random_uint3Tofloat(gridPos.x, gridPos.y, 2 * z), HMUtil.Random_uint3Tofloat(gridPos.x, gridPos.y, 2 * z + 1));
                    arr_Point[z] = new Vector2(width * pointPosInNorm.x, height * pointPosInNorm.y);
                    Debug.Log(z + " " + arr_Point[z]);
                }
            }
        }
        else
        {
            pointCount = m_PointCount;
            arr_Point = new Vector2[pointCount];
            Vector2Int gridPos = offset;
            for (int z = 0; z < m_PointCount; z++)
            {
                Vector2 pointPosInNorm = new Vector2(HMUtil.Random_uint3Tofloat(gridPos.x, gridPos.y, 2 * z), HMUtil.Random_uint3Tofloat(gridPos.x, gridPos.y, 2 * z + 1));
                arr_Point[z] = new Vector2(width * pointPosInNorm.x, height * pointPosInNorm.y);
            }
        }

       

        if(m_DistanceWeight == 0)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 texelPos = new Vector2(x, y);
                    int texelIdx = x + y * width;
                    int nearIdx = 0;
                    float nearDis = Vector2.Distance(arr_Point[nearIdx], texelPos);
                    for (int i = 1; i < pointCount; i++)
                    {
                        float curDis = Vector2.Distance(arr_Point[i], texelPos);
                        if (curDis < nearDis)
                        {
                            nearDis = curDis;
                            nearIdx = i;
                        }
                    }

                    float value = nearIdx / (float)(pointCount - 1);
                    arr_Colors[texelIdx] = new Color(value, value, value, 1);
                }
            }
        }
        else
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 texelPos = new Vector2(x, y);
                    int texelIdx = x + y * width;
                    int nearIdx = 0;
                    float nearDis = Vector2.Distance(arr_Point[nearIdx], texelPos);
                    for (int i = 1; i < pointCount; i++)
                    {
                        float curDis = Vector2.Distance(arr_Point[i], texelPos);
                        if (curDis < nearDis)
                        {
                            nearDis = curDis;
                            nearIdx = i;
                        }
                    }

                    float value = nearDis * m_DistanceWeight;
                    arr_Colors[texelIdx] = new Color(value, value, value, 1);
                }
            }
        }

       

        LastOptionUpdate();


        return arr_Colors;
    }
}
