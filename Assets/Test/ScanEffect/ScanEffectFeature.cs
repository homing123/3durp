using UnityEngine.Rendering.Universal;

public class ScanEffectFeature : ScriptableRendererFeature
{
    ScanEffectPass m_Pass;

    public override void Create()
    {
        m_Pass = new ScanEffectPass();
        name = ScanEffectPass.CMDBufferName;
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