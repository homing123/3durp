
using UnityEngine.Rendering.Universal;

public class VLSFeature : ScriptableRendererFeature
{

    VLSPass m_Pass;

    public override void Create()
    {
        m_Pass = new VLSPass();
        name = VLSPass.ShaderFileName;
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
