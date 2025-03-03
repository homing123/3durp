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
    RTHandle m_SourceDepth;
    RTHandle m_DestiColor;
    Material m_Mat;
    DeferredFogSetting m_Setting;


    public void Init()
    {
        m_Mat = new Material(Shader.Find(ShaderFindName));
        m_Setting = VolumeManager.instance.stack.GetComponent<DeferredFogSetting>();
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        m_Init = m_Mat != null && m_Setting != null;

        if (m_Init)
        {
            m_DestiColor = RTHandles.Alloc(
                Vector2.one,
                dimension: TextureDimension.Tex2D,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                useDynamicScale: true,
                name: RenderTargetName,
                wrapMode: TextureWrapMode.Clamp
                );
        }
    }



    public bool Setup(ScriptableRenderer renderer)
    {
        if(m_Init == false)
        {
            Init();
        }
        m_SourceColor = renderer.cameraColorTargetHandle;
        m_SourceDepth = renderer.cameraDepthTargetHandle;

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
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_Init == false || m_Active == false)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(CMDBufferName);

        if(m_Setting.IsUpdate())
        {

        }

        cmd.Blit(m_SourceColor, m_DestiColor, m_Mat, 0);
        cmd.Blit(m_DestiColor, m_SourceColor, m_Mat, 0);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        if (m_Init == true)
        {
            m_DestiColor.Release();
        }
    }
    ~DeferredFogPass()
    {
        Dispose();
    }
}
