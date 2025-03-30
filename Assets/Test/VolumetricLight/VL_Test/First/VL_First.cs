using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

public class VL_First : MonoBehaviour, I_VLSetting
{
    [SerializeField] Material m_Mat;
    [SerializeField]
    [Range(32, 256)] int m_Samples = 32;
    [SerializeField]
    [Range(0, 1)] float m_Attenuation = 0.1f;
    [SerializeField]
    [Min(0.001f)] float m_ShadowScatteringDistance = 8.0f;
    [SerializeField]
    [Range(0, 1)] float m_Add = 0.15f;
    [SerializeField]
    [Range(0, 1)] float m_Out = 0.02f;

    int m_LastSamples;
    float m_LastShadowScatteringDistance;
    float m_LastAdd;
    float m_LastOut;

    public bool IsUpdate()
    {
        return m_LastSamples != m_Samples || m_LastShadowScatteringDistance != m_ShadowScatteringDistance || m_LastAdd != m_Add || m_LastOut != m_Out;
    }
    public void Update()
    {
        m_Mat.SetFloat("_Samples", (float)m_Samples);
        m_Mat.SetFloat("_ShadowScatteringDistance", m_ShadowScatteringDistance);
        m_Mat.SetFloat("_Add", m_Add);
        m_Mat.SetFloat("_Out", m_Out);
        m_LastSamples = m_Samples;
        m_LastShadowScatteringDistance = m_ShadowScatteringDistance;
        m_LastAdd = m_Add;
        m_LastOut = m_Out;
    }
    public bool IsActive()
    {
        return enabled;
    }

    public Material GetMat()
    {
        return m_Mat;
    }
    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }

}