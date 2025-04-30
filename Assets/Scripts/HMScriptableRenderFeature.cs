using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HMScriptableRenderFeature : ScriptableRendererFeature
{
    protected HMScriptableRenderPass m_Pass;
    public Material m_Mat;
    protected bool m_Setting;

    public override void Create()
    {

    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (IsPlay())
        {
            m_Setting = m_Pass.SetUp(renderer) && m_Mat!= null;
        }
        else
        {
            m_Setting = false;
        }
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (IsPlay() && m_Setting)
        {
            renderer.EnqueuePass(m_Pass);
        }
    }
    protected override void Dispose(bool disposing)
    {
        m_Pass?.Dispose();
        base.Dispose(disposing);
    }

    public static bool IsPlay()
    {
#if UNITY_EDITOR
        return Application.isPlaying;
#endif 
        return true;
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

