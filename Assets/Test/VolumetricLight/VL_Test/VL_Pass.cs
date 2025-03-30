using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public interface I_VLSetting
{
    public bool IsUpdate();
    public void Update();
    public bool IsActive();
    public Material GetMat();

}


public class VL_Pass : ScriptableRenderPass
{
    public const string RenderTargetName = "VL";
    public const string CMDBufferName = "VL";

    bool m_Active = false;
    bool m_Init = false;
    RTHandle m_SourceColor;
    Material m_Mat;
    I_VLSetting m_Setting;
    int m_RTDestiNameID;
    public void Init()
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

#if UNITY_EDITOR
        m_Init = VL_TestScene.Ins != null && Application.isPlaying;
#else
        m_Init = VL_TestScene.Ins != null;
#endif
    }



    public bool Setup(ScriptableRenderer renderer)
    {
        if (m_Init == false)
        {
            Init();
        }
        
        if(m_Init == false)
        {
            return false;
        }
        m_SourceColor = renderer.cameraColorTargetHandle;
        m_Setting = VL_TestScene.Ins.GetVLSetting();
        m_Mat = m_Setting.GetMat();
        m_Active = m_Setting.IsActive();

        if (m_Active && m_Init)
        {
            if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
            {
                Debug.Log("VL must has depth");
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
        }
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
    ~VL_Pass()
    {
        Dispose();
    }
}
