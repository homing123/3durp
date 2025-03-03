using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public class DeferredFogFeature : ScriptableRendererFeature
{
    DeferredFogPass m_Pass;

    public override void Create()
    {
        m_Pass = new DeferredFogPass();
        name = DeferredFogPass.ShaderFileName;
    }


    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        m_Pass.Setup(renderer);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_Pass);
    }
    
    // Start is called before the first frame update

}
