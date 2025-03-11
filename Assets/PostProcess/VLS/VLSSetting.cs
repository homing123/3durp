using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("VLS")]
public class VLSSetting : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter m_Decay = new ClampedFloatParameter(0, 0, 1);
    public ClampedFloatParameter m_Scattering = new ClampedFloatParameter(0, 0, 1);
    public ClampedFloatParameter m_Weight = new ClampedFloatParameter(0, 0, 1);
    public FloatParameter m_Density = new FloatParameter(30);
    public IntParameter m_Samples = new IntParameter(32);
    public FloatParameter m_TempValue = new FloatParameter(10);

    float m_LastDecay;
    float m_LastScattering;
    float m_LastWeight;
    float m_LastDensity;
    int m_LastSamples;
    float m_LastTempValue;

    public bool IsUpdate()
    {
        return m_LastDecay != m_Decay.value || m_LastScattering != m_Scattering.value || m_LastWeight != m_Weight.value || m_LastDensity != m_Density.value || m_LastSamples != m_Samples.value || m_LastTempValue!= m_TempValue.value;
    }
    public void Update()
    {
        m_LastDecay = m_Decay.value;
        m_LastScattering = m_Scattering.value;
        m_LastWeight = m_Weight.value;
        m_LastDensity = m_Density.value;
        m_LastSamples = m_Samples.value;
        m_LastTempValue = m_TempValue.value;
    }
    public bool IsActive()
    {
        return (m_Samples.value > 0) && active;
    }

    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }

}
