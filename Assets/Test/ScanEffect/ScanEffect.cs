using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
public class ScanEffect : VolumeComponent, IPostProcessComponent
{
    public MaterialParameter m_Mat = new MaterialParameter(null);
    public ColorParameter m_Color = new ColorParameter(new Color(1, 1, 1, 1));
    public FloatParameter m_Range = new FloatParameter(10);
    public FloatParameter m_TotalTime = new FloatParameter(1);
    public FloatParameter m_LineWidth = new FloatParameter(1);
    public ColorParameter m_GridColor = new ColorParameter(new Color(0.3f, 0.3f, 0.3f, 1));
    public float m_CurTime = 0;
    bool m_Active = false;

    Color m_LastColor;
    float m_LastRange;
    float m_LastTotalTime;
    float m_LastLineWidth;
    Color m_LastGridColor;

    static ScanEffect ins;
    public static ScanEffect Ins
    {
        get
        {
            if(ins ==null)
            {
                ins = VolumeManager.instance.stack.GetComponent<ScanEffect>();
            }
            return ins;
        }
    }
    public void Scan()
    {
        Ins.m_CurTime = 0;
        Ins.m_Active = true;      
    }

    public bool IsUpdate()
    {
        return m_LastColor != m_Color.value || m_LastRange != m_Range.value || m_LastTotalTime != m_TotalTime.value || m_LastLineWidth != m_LineWidth.value || m_LastGridColor!= m_GridColor.value;
    }
    public void Update()
    {
        m_LastColor = m_Color.value;
        m_LastRange = m_Range.value;
        m_LastTotalTime = m_TotalTime.value;
        m_LastLineWidth = m_LineWidth.value;
        m_LastGridColor = m_GridColor.value;
    }
    public void TimeUpdate()
    {
        m_CurTime += Time.deltaTime;
        if(m_CurTime > m_TotalTime.value)
        {
            m_Active = false;
        }
    }
    public bool IsActive()
    {
        return m_Active;
    }

    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }

}
public class ScanEffectPass : ScriptableRenderPass
{
    public const string RenderTargetName = "ScanEffect";
    public const string CMDBufferName = "ScanEffect";

    bool m_Init = false;
    RTHandle m_SourceColor;
    Material m_Mat;
    int m_RTDestiNameID;
    ScanEffect m_Setting;
    public void Init()
    {
        m_Setting = ScanEffect.Ins;
        if (m_Setting == null)
        {
            return;
        }
        m_Mat = m_Setting.m_Mat.value;

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        m_Init = m_Mat != null && ScanEffect.Ins != null;
    }



    public bool Setup(ScriptableRenderer renderer)
    {
        if (m_Init == false)
        {
            Init();
        }
        m_SourceColor = renderer.cameraColorTargetHandle;

#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            m_Init = false;
            return false;
        }
#else
#endif
        if (m_Init)
        {
            if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
            {
                Debug.Log("ScanEffect must has depth");
                return false;
            }
        }

        return m_Init;
    }
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {

        if (m_Init == false || m_Setting.IsActive() == false)
        {
            return;
        }

        cmd.GetTemporaryRT(m_RTDestiNameID, cameraTextureDescriptor);
        base.Configure(cmd, cameraTextureDescriptor);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_Init == false || m_Setting.IsActive() == false)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(CMDBufferName);

        if (m_Setting.IsUpdate())
        {
            m_Setting.Update();
            m_Mat.SetColor("_Color", m_Setting.m_Color.value);
            m_Mat.SetFloat("_Range", m_Setting.m_Range.value);
            m_Mat.SetFloat("_TotalTime", m_Setting.m_TotalTime.value);
            m_Mat.SetFloat("_LineWidth", m_Setting.m_LineWidth.value);
            m_Mat.SetColor("_GridColor", m_Setting.m_GridColor.value);
        }
        m_Mat.SetFloat("_CurTime", m_Setting.m_CurTime);
        m_Setting.TimeUpdate();
        cmd.Blit(m_SourceColor, m_RTDestiNameID, m_Mat, 0);
        cmd.Blit(m_RTDestiNameID, m_SourceColor);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(m_RTDestiNameID);
        base.FrameCleanup(cmd);
    }
    public void Dispose()
    {
        if (m_Init == true)
        {
            //m_DestiColor.Release();
        }
    }
    ~ScanEffectPass()
    {
        Dispose();
    }
}
