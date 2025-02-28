using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRenderPass : ScriptableRenderPass
{
    private Material m_Material;
    private BlurSettings m_BlurSettings;

    private RenderTargetIdentifier m_Source; //셰이더 적용하기전 화면 텍스쳐
    private RenderTargetHandle m_BlurTexHandle;
    private int m_BlurTexID;

    public bool Setup(ScriptableRenderer renderer)
    {
        m_Source = renderer.cameraColorTarget;
        m_BlurSettings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; //BeforeRenderingPostProcessing, URPPostProcessing, AfterRenderingPostProcessing 순서

        if(m_BlurSettings != null && m_BlurSettings.IsActive())
        {
            m_Material = new Material(Shader.Find("PostProcessingP/Blur"));
            return true;
        }

        return false;
    }

    //임시리소스설정
    //CommandBuffer = gpu가 수행하는 명령목록
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        if(m_BlurSettings == null || !m_BlurSettings.IsActive())
        {
            return;
        }

        m_BlurTexID = Shader.PropertyToID("_BlurTex");
        m_BlurTexHandle = new RenderTargetHandle();
        m_BlurTexHandle.id = m_BlurTexID;
        cmd.GetTemporaryRT(m_BlurTexHandle.id, cameraTextureDescriptor);

        base.Configure(cmd, cameraTextureDescriptor);
    }

    //프레임당 한번씩 실행됨
    //셰이더속성 설정
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_BlurSettings == null || !m_BlurSettings.IsActive())
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get("Blur Post Process");

        //Set Blur effect propertties.
        int gridSize = Mathf.CeilToInt(m_BlurSettings.strength.value * 6.0f);
        
        if(gridSize % 2 == 0)
        {
            gridSize++;
        }

        m_Material.SetInteger("_GridSize", gridSize);
        m_Material.SetFloat("_Spread", m_BlurSettings.strength.value);

        //Execute effect using effect material with two passes.
        cmd.Blit(m_Source, m_BlurTexHandle.id, m_Material, 0);
        cmd.Blit(m_BlurTexHandle.id, m_Source, m_Material, 1);
        context.ExecuteCommandBuffer(cmd);

        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(m_BlurTexID);
    }
}
