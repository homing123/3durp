using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("DeferredFog")]
public class DeferredFogSetting : VolumeComponent, IPostProcessComponent
{
    //serializefield 안됨
    public ClampedFloatParameter m_Intensity = new ClampedFloatParameter(0, 0, 1);
    public FloatParameter m_NearDis = new FloatParameter(1);
    public FloatParameter m_FarDis = new FloatParameter(10);
    public FloatParameter m_Height = new FloatParameter(5);
    public ColorParameter m_FogColor = new ColorParameter(Color.white);

    float m_LastIntensity;
    float m_LastNearDis;
    float m_LastFarDis;
    float m_LastHeight;
    Color m_LastFogColor;
    public bool IsUpdate()
    {
        return m_LastHeight != m_Height.value || m_LastFarDis != m_FarDis.value || m_LastNearDis != m_NearDis.value || m_LastIntensity != m_Intensity.value || m_LastFogColor!= m_FogColor.value;
    }
    public void Update()
    {
        m_LastHeight = m_Height.value;
        m_LastFarDis = m_FarDis.value;
        m_LastNearDis = m_NearDis.value;
        m_LastIntensity = m_Intensity.value;
        m_LastFogColor = m_FogColor.value;
    }
    public bool IsActive()
    {
        return (m_Intensity.value > 0.0f) && active;
    }

    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }
}
