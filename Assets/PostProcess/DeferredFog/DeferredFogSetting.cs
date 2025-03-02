using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("DeferredFog")]
public class DeferredFogSetting : VolumeComponent, IPostProcessComponent
{
    [SerializeField]
    [Range(0, 1)] public float m_Strength;
    [SerializeField] public float m_NearDis;
    [SerializeField] public float m_FarDis;

    float m_LastStrength;
    float m_LastNearDis;
    float m_LastFarDis;
    public bool IsUpdate()
    {
        return m_LastFarDis != m_FarDis || m_LastNearDis != m_NearDis || m_LastStrength != m_Strength;
    }
    public bool IsActive()
    {
        return (m_Strength > 0.0f) && active;
    }

    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }
}
