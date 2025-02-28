using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRenderPass : ScriptableRenderPass
{
    private Material m_Material;
    private BlurSettings m_BlurSettings;

    private RenderTargetIdentifier m_Source; //���̴� �����ϱ��� ȭ�� �ؽ���
    private RenderTargetHandle m_BlurTexHandle;
    private int m_BlurTexID;

    public bool Setup(ScriptableRenderer renderer)
    {
        m_Source = renderer.cameraColorTarget;
        m_BlurSettings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; //BeforeRenderingPostProcessing, URPPostProcessing, AfterRenderingPostProcessing ����

        if(m_BlurSettings != null && m_BlurSettings.IsActive())
        {
            m_Material = new Material(Shader.Find("PostProcessingP/Blur"));
            return true;
        }

        return false;
    }

    //�ӽø��ҽ�����
    //CommandBuffer = gpu�� �����ϴ� ��ɸ��
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

    //�����Ӵ� �ѹ��� �����
    //���̴��Ӽ� ����
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
