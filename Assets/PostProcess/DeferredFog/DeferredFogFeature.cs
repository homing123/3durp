using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class DeferredFogFeature : HMScriptableRenderFeature
{
    public const string Name = "DeferredFog";
    public override void Create()
    {
        if (IsPlay())
        {
            m_Pass = new DeferredFogPass(m_Mat);
            name = Name;
        }
    }
    public class DeferredFogPass : HMScriptableRenderPass
    {
        RTHandle m_SourceColor;
        Material m_Mat;
        int m_RTDestiNameID;

        float m_CurIntensity;
        float m_CurNearDis;
        float m_CurFarDis;
        float m_CurHeight;
        Color m_CurFogColor;
        public DeferredFogPass(Material mat)
        {
            m_Mat = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override bool SetUp(ScriptableRenderer renderer)
        {
            DeferredFogSetting setting = VolumeManager.instance.stack.GetComponent<DeferredFogSetting>();
            if (setting == null)
            {
                return false;
            }
            bool isActive = setting.IsActive();
            m_SourceColor = renderer.cameraColorTargetHandle;
            if (m_CurHeight != setting.m_Height.value || m_CurFarDis != setting.m_FarDis.value || m_CurNearDis != setting.m_NearDis.value || m_CurIntensity != setting.m_Intensity.value || m_CurFogColor != setting.m_FogColor.value)
            {
                m_CurHeight = setting.m_Height.value;
                m_CurFarDis = setting.m_FarDis.value;
                m_CurNearDis = setting.m_NearDis.value;
                m_CurIntensity = setting.m_Intensity.value;
                m_CurFogColor = setting.m_FogColor.value;
                m_Mat.SetFloat("_NearDis", m_CurNearDis);
                m_Mat.SetFloat("_FarDis", m_CurFarDis);
                m_Mat.SetFloat("_Height", m_CurHeight);
                m_Mat.SetFloat("_Intensity", m_CurIntensity);
                m_Mat.SetColor("_FogColor", m_CurFogColor);
            }
            if (isActive)
            {
                if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
                {
                    Debug.Log("DeferredFog must has depth");
                    return false;
                }
            }
            return isActive;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(m_RTDestiNameID, cameraTextureDescriptor);
            base.Configure(cmd, cameraTextureDescriptor);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(Name);
            cmd.Blit(m_SourceColor, m_RTDestiNameID, m_Mat, 0);
            cmd.Blit(m_RTDestiNameID, m_SourceColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_RTDestiNameID);
            base.FrameCleanup(cmd);
        }
        public override void Dispose()
        {
           
        }
    }

}
