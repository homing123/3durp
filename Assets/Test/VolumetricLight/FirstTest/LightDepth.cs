using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
public class LightDepth : MonoBehaviour
{
    public Material m_Mat;

    public static LightDepth Ins;

    private void Awake()
    {
        Ins = this;
    }
}
public class LightDepthPass : ScriptableRenderPass
{
    public const string RenderTargetName = "LightDepth";
    public const string CMDBufferName = "LightDepth";

    bool m_Init = false;
    Material m_Mat;
    RenderTexture m_ShadowDepth;
    public void Init()
    {
        if (LightDepth.Ins == null)
        {
            return;
        }
        m_Mat = LightDepth.Ins.m_Mat;

        renderPassEvent = RenderPassEvent.AfterRenderingShadows;
        m_Init = m_Mat != null;
    }



    public bool Setup(ScriptableRenderer renderer)
    {
        if (m_Init == false)
        {
            Init();
        }

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
                Debug.Log("LightDepth must has depth");
                return false;
            }
        }

        return m_Init;
    }
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {

        if (m_Init == false)
        {
            return;
        }
        if(m_ShadowDepth == null)
        {
            m_ShadowDepth = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
            m_ShadowDepth.filterMode = FilterMode.Point;
            m_ShadowDepth.name = "ShadowDepthTexture";

        }

        base.Configure(cmd, cameraTextureDescriptor);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_Init == false)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(CMDBufferName);

        cmd.Blit(BuiltinRenderTextureType.CurrentActive, m_ShadowDepth, m_Mat);
        cmd.SetGlobalTexture("_ShadowDepthTexture", m_ShadowDepth);

        //TerrainMaker.DebugRenderTexturePixels(m_ShadowDepth);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        if(m_Init == false)
        {
            return;
        }
        if (m_ShadowDepth == null)
        {
            m_ShadowDepth.Release();
        }
        base.FrameCleanup(cmd);
    }
    public void Dispose()
    {
        if (m_Init == true)
        {
            //m_DestiColor.Release();
        }
    }
    ~LightDepthPass()
    {
        Dispose();
    }
}
