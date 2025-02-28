
using UnityEngine.Rendering.Universal;

public class BlurRendererFeature : ScriptableRendererFeature
{
    BlurRenderPass m_BlurRenderPass;
    public override void Create()
    {
        m_BlurRenderPass = new BlurRenderPass();
        name = "Blur";
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(m_BlurRenderPass.Setup(renderer))
        {
            renderer.EnqueuePass(m_BlurRenderPass);
        }
    }

    

  
}
