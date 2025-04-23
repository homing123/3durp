using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DeferredFogPass : ScriptableRenderPass
{
    public const string RenderTargetName = "DeferredFogRT";
    public const string ShaderFileName = "DeferredFog";
    public const string ShaderFindName = "PostProcessing/DeferredFog";
    public const string CMDBufferName = "DeferredFog Post Process";
    bool m_Active = false;
    bool m_Init = false;
    RTHandle m_SourceColor;
    RTHandle m_DestColor;
    Material m_Mat;
    DeferredFogSetting m_Setting;
    RenderTextureDescriptor m_RTDesc;
    int m_RTDestiNameID;


    public void Init()
    {
        m_Mat = new Material(Shader.Find(ShaderFindName));
        m_Setting = VolumeManager.instance.stack.GetComponent<DeferredFogSetting>();
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        m_Init = m_Mat != null && m_Setting != null;

        //if (m_Init)
        //{
        //    m_DestiColor = RTHandles.Alloc(
        //        Vector2.one,
        //        dimension: TextureDimension.Tex2D,
        //        colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
        //        useDynamicScale: true,
        //        name: RenderTargetName,
        //        wrapMode: TextureWrapMode.Clamp
        //        );
        //}
    }



    public bool Setup(ScriptableRenderer renderer)
    {
        if(m_Init == false)
        {
            Init();
        }
        m_SourceColor = renderer.cameraColorTargetHandle;

#if UNITY_EDITOR
        m_Active = m_Setting.IsActive() && Application.isPlaying;
#else
        m_Active = m_Setting.IsActive();
#endif
        if(m_Active && m_Init)
        {
            if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
            {
                Debug.Log("DeferredFog must has depth");
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

        if(m_Setting.IsUpdate())
        {
            m_Setting.Update();
            m_Mat.SetFloat("_NearDis", m_Setting.m_NearDis.value);
            m_Mat.SetFloat("_FarDis", m_Setting.m_FarDis.value);
            m_Mat.SetFloat("_Height", m_Setting.m_Height.value);
            m_Mat.SetFloat("_Intensity", m_Setting.m_Intensity.value);
            m_Mat.SetColor("_FogColor", m_Setting.m_FogColor.value);
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
    ~DeferredFogPass()
    {
        Dispose();
    }
}
