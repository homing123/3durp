using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpPaddingTest : MonoBehaviour
{
    [SerializeField] ComputeShader m_CS;
    ComputeBuffer[] arr_CBuffer;
    ComputeBuffer[] arr_CBufferPadding;
    [SerializeField] bool m_UsePadding;
    const int NvidiaWarpCount = 32;

    float[][] arr_Data;
    int[] arr_ArrayLength;
    int m_BufferCount = 8;
    void Start()
    {
        arr_Data = new float[m_BufferCount][];
        for(int i=0;i< m_BufferCount; i++)
        {
            int randomCount = Random.Range(5000, 10000);
            for(int j=0;j<randomCount;j++)
            {
                arr_Data[i][j] = Random.Range(0.0f, 1.0f);
            }
            arr_ArrayLength[i] = randomCount;
        }


        for(int i=0;i<m_BufferCount;i++)
        {
            arr_CBuffer[i] = new ComputeBuffer(arr_ArrayLength[i], sizeof(float));
            arr_CBuffer[i].SetData(arr_Data[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
