using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("DeferredFog")]
public class DeferredFogSetting : VolumeComponent, IPostProcessComponent
{
    //serializefield 안됨
    public ClampedFloatParameter m_Strength = new ClampedFloatParameter(0, 0, 1);
    public FloatParameter m_NearDis = new FloatParameter(1);
    public FloatParameter m_FarDis = new FloatParameter(10);

    float m_LastStrength;
    float m_LastNearDis;
    float m_LastFarDis;
    public bool IsUpdate()
    {
        return m_LastFarDis != m_FarDis.value || m_LastNearDis != m_NearDis.value || m_LastStrength != m_Strength.value;
    }
    public void Update()
    {
        m_LastFarDis = m_FarDis.value;
        m_LastNearDis = m_NearDis.value;
        m_LastStrength = m_Strength.value;
    }
    public bool IsActive()
    {
        return (m_Strength.value > 0.0f) && active;
    }

    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }
}
