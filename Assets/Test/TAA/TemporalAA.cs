using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("TemporalAA")]
public class TemporalAACustomSetting : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter m_Weight = new ClampedFloatParameter(0.1f, 0, 1);

    public bool IsActive()
    {
        return m_Weight.value == 0 || m_Weight.value == 1;
    }
    public bool IsTileCompatible()
    {
        return false;
    }
}
