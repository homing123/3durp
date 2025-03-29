using UnityEngine.Rendering.Universal;

public class LightDepthFeature : ScriptableRendererFeature
{
    LightDepthPass m_Pass;

    public override void Create()
    {
        m_Pass = new LightDepthPass();
        name = LightDepthPass.CMDBufferName;
    }


    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        m_Pass.Setup(renderer);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_Pass);
    }

}