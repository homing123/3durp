using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CSM : MonoBehaviour
{
    public static CSM Ins;
    private void Awake()
    {
        Ins = this;
    }
    public ComputeShader m_PerlinNoise;
    public ComputeShader m_WorleyNoise;
    public ComputeShader m_HeightToNormal;

    public ComputeShader m_GrassPosition;
    public ComputeShader m_GrassFrustumCulling;

    public ComputeShader m_TerrainMaker;

}
