using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BlurRendererFeature : ScriptableRendererFeature
{
    BlurRenderPass m_BlurRenderPass;
    public override void Create()
    {
        m_BlurRenderPass = new BlurRenderPass();
        name = BlurRenderPass.ShaderFileName;
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        m_BlurRenderPass.Setup(renderer);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_BlurRenderPass);
    }

    

  
}
