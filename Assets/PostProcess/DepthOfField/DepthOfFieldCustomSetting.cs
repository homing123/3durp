using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("DepthOfFieldCustom")]
public class DepthOfFieldCustomSetting : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter m_BlurIntensity = new ClampedFloatParameter(0, 0, 25);
    public ClampedFloatParameter m_FocusDepth = new ClampedFloatParameter(0, 0, 1);
    public ClampedFloatParameter m_FocusDepthSize = new ClampedFloatParameter(0, 0, 1);
    public MaterialParameter m_DofMat = new MaterialParameter(null);

    public bool IsActive()
    {
        return (m_BlurIntensity.value > 0.0f) && active;
    }

    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }

}

