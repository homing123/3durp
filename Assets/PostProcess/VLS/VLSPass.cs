using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class VLSPass : ScriptableRenderPass
{
    public const string RenderTargetName = "VLSRT";
    public const string ShaderFileName = "VolumetricLightScattering";
    public const string ShaderFindName = "PostProcessing/VLS";
    public const string CMDBufferName = "VLS Post Process";

    bool m_Active = false;
    bool m_Init = false;
    RTHandle m_SourceColor;
    Material m_Mat;
    VLSSetting m_Setting;
    RenderTextureDescriptor m_RTDesc;
    int m_RTDestiNameID;
    public void Init()
    {
        m_Setting = VolumeManager.instance.stack.GetComponent<VLSSetting>();
        m_Mat = m_Setting.m_Mat.value;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        m_Init = m_Mat != null && m_Setting != null;
    }



    public bool Setup(ScriptableRenderer renderer)
    {
        if (m_Init == false)
        {
            Init();
        }
        m_SourceColor = renderer.cameraColorTargetHandle;

#if UNITY_EDITOR
        m_Active = m_Setting.IsActive() && Application.isPlaying;
#else
        m_Active = m_Setting.IsActive();
#endif
        if (m_Active && m_Init)
        {
            if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
            {
                Debug.Log("VLS must has depth");
                return false;
            }
        }

        return m_Init && m_Active;
    }
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        if (m_Init == false || m_Active == false)
        {
            return;
        }

        cmd.GetTemporaryRT(m_RTDestiNameID, cameraTextureDescriptor);
        base.Configure(cmd, cameraTextureDescriptor);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_Init == false || m_Active == false)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(CMDBufferName);

        if (m_Setting.IsUpdate())
        {
            m_Setting.Update();
            m_Mat.SetFloat("_Decay", m_Setting.m_Decay.value);
            m_Mat.SetFloat("_Scattering", m_Setting.m_Scattering.value);
            m_Mat.SetFloat("_Weight", m_Setting.m_Weight.value);
            m_Mat.SetFloat("_Density", m_Setting.m_Density.value);
            m_Mat.SetInteger("_Samples", m_Setting.m_Samples.value);
            m_Mat.SetFloat("_TempValue", m_Setting.m_TempValue.value);
        }
        m_Mat.SetVector("_LightPos", DayM.Ins.GetSunPos());

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
    ~VLSPass()
    {
        Dispose();
    }
}