using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("DeferredFog")]
public class DeferredFogSetting : VolumeComponent, IPostProcessComponent
{
    //serializefield �ȵ�
    public ClampedFloatParameter m_Intensity = new ClampedFloatParameter(0, 0, 1);
    public FloatParameter m_NearDis = new FloatParameter(1);
    public FloatParameter m_FarDis = new FloatParameter(10);
    public FloatParameter m_Height = new FloatParameter(5);
    public ColorParameter m_FogColor = new ColorParameter(Color.white);

    public bool IsActive()
    {
        return (m_Intensity.value > 0.0f) && active;
    }

    public bool IsTileCompatible()
    {
        //2023 ���� ���� �Ⱦ��ٴ��� return false �ص� �������
        return false;
    }
}
