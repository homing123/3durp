using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HMScriptableRenderFeature : ScriptableRendererFeature
{
    protected HMScriptableRenderPass m_Pass;
    public Material m_Mat;

    public override void Create()
    {
        //if (HMUtil.IsPlay())
        //{
        //    m_Pass = new HMScriptableRenderPass();
            
        //}
    }
    bool m_Active;
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (HMUtil.IsPlay())
        {
            m_Active = m_Pass.SetUp(renderer);
        }
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (HMUtil.IsPlay() && m_Active)
        {
            renderer.EnqueuePass(m_Pass);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (m_Pass != null)
        {
            m_Pass.Dispose();
        }
        base.Dispose(disposing);
    }
}
public abstract class HMScriptableRenderPass : ScriptableRenderPass
{
    public abstract bool SetUp(ScriptableRenderer renderer);
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        throw new System.NotImplementedException();
    }
    public abstract void Dispose();
}

