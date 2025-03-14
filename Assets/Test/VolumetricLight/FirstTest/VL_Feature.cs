using UnityEngine.Rendering.Universal;

public class VL_Feature : ScriptableRendererFeature
{
    VL_Pass m_Pass;

    public override void Create()
    {
        m_Pass = new VL_Pass();
        name = VL_Pass.CMDBufferName;
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