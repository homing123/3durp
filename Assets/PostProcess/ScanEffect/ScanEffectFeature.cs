using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScanEffectFeature : HMScriptableRenderFeature
{
    public const string Name = "ScanEffect";
    public override void Create()
    {
        if (IsPlay())
        {
            m_Pass = new ScanEffectPass(m_Mat);
            name = Name;
        }
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (IsPlay() && ScanEffectM.Ins != null && ScanEffectM.Ins.IsActive())
        {
            m_Setting = m_Pass.SetUp(renderer) && m_Mat != null;
        }
        else
        {
            m_Setting = false;
        }
    }

    public class ScanEffectPass : HMScriptableRenderPass
    {

        RTHandle m_SourceColor;
        Material m_Mat;
        int m_RTDestiNameID;

        Color m_CurColor;
        float m_CurRange;
        float m_CurTotalTime;
        float m_CurLineWidth;
        Color m_CurGridColor;

        public ScanEffectPass(Material mat)
        {
            m_Mat = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override bool SetUp(ScriptableRenderer renderer)
        {
            ScanEffectM setting = ScanEffectM.Ins;
            if (setting == null)
            {
                return false;
            }
            bool isActive = setting.IsActive();
            m_SourceColor = renderer.cameraColorTargetHandle;
            if (m_CurColor != setting.m_Color || m_CurRange != setting.m_Range || m_CurTotalTime != setting.m_TotalTime || m_CurLineWidth != setting.m_LineWidth || m_CurGridColor != setting.m_GridColor)
            {
                m_CurColor = setting.m_Color;
                m_CurRange = setting.m_Range;
                m_CurTotalTime = setting.m_TotalTime;
                m_CurLineWidth = setting.m_LineWidth;
                m_CurGridColor = setting.m_GridColor;
                m_Mat.SetColor("_Color", m_CurColor);
                m_Mat.SetFloat("_Range", m_CurRange);
                m_Mat.SetFloat("_TotalTime", m_CurTotalTime);
                m_Mat.SetFloat("_LineWidth", m_CurLineWidth);
                m_Mat.SetColor("_GridColor", m_CurGridColor);
            }
            if (isActive)
            {
                if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
                {
                    Debug.Log("ScanEffect must has depth");
                    return false;
                }
            }
            return isActive;
        }
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(m_RTDestiNameID, cameraTextureDescriptor);
            base.Configure(cmd, cameraTextureDescriptor);
            m_Mat.SetFloat("_CurTime", ScanEffectM.Ins.m_CurTime);
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