using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

public class VL_Fourth : MonoBehaviour, I_VLSetting
{
    [SerializeField] Material m_Mat;
    [SerializeField]
    [Range(32, 256)] int m_Samples = 32;
    [SerializeField]
    [Range(0, 1)] float m_Attenuation = 0.1f;
    [SerializeField]
    [Range(-0.9f, 0.9f)] float m_PhaseG;

    int m_LastSamples;
    float m_LastAttenuation;
    float m_LastPhaseG;

    public bool IsUpdate()
    {
        return m_LastSamples != m_Samples || m_LastAttenuation != m_Attenuation || m_LastPhaseG != m_PhaseG;
    }
    public void Update()
    {
        m_Mat.SetFloat("_Samples", (float)m_Samples);
        m_Mat.SetFloat("_Attenuation", m_Attenuation);
        m_Mat.SetFloat("_PhaseG", m_PhaseG);
        m_LastSamples = m_Samples;
        m_LastAttenuation = m_Attenuation;
        m_LastPhaseG = m_PhaseG;
    }
    public bool IsActive()
    {
        return VL_TestScene.Ins.m_Actives;
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