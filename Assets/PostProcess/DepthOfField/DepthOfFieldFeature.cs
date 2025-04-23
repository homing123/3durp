using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DepthOfFieldFeature : ScriptableRendererFeature
{
    DepthOfFieldCustomPass m_Pass;
    public override void Create()
    {
        m_Pass = new DepthOfFieldCustomPass();
        name = DepthOfFieldCustomPass.Name;
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
