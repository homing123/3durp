using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

[System.Serializable, VolumeComponentMenu("Blur")]
public class BlurSettings : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Standard deviation (spread) of the blur, Grid size is approx. 3x larger.")]
    public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 15.0f);

    public bool IsActive()
    {
        return (strength.value > 0.0f) && active;
    }

    public bool IsTileCompatible()
    {
        //2023 ���� ���� �Ⱦ��ٴ��� return false �ص� �������
        return false;
    }
}
