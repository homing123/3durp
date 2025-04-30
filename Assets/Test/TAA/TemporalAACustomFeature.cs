using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public class TemporalAACustomFeature : HMScriptableRenderFeature
{
    public const string Name = "TemporalAACustom";

    public override void Create()
    {
        if (IsPlay())
        {
            m_Pass = new TemporalAACustomPass(m_Mat);
            name = Name;
        }
    }
   
    public class TemporalAACustomPass : HMScriptableRenderPass
    {
        RTHandle m_Source;
        RTHandle m_LastFrameSource;

        bool m_LastFrameSourceInit;
        float m_CurWeight;

        Material m_Mat;

        public TemporalAACustomPass(Material mat)
        {
            m_Mat = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            //RenderTextureDescriptor des = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32);
            m_LastFrameSource = RTHandles.Alloc(
                Vector2.one,
                dimension: TextureDimension.Tex2D,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                useDynamicScale: true,
                name: Name,
                wrapMode: TextureWrapMode.Clamp);
        }

        public override bool SetUp(ScriptableRenderer renderer)
        {
            TemporalAACustomSetting setting = VolumeManager.instance.stack.GetComponent<TemporalAACustomSetting>();
            m_Source = renderer.cameraColorTargetHandle;
            if (setting == null)
            {
                return false;
            }
            bool isActive = setting.IsActive();

            if (m_CurWeight != setting.m_Weight.value)
            {
                m_CurWeight = setting.m_Weight.value;
                m_Mat.SetFloat("_Weight", m_CurWeight);
            }
            if (isActive)
            {
                if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
                {
                    Debug.Log("TemporalAACustom must has depth");
                    return false;
                }
            }
            return isActive;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_Mat.SetTexture("_LastFrameSource", m_LastFrameSource);
            base.Configure(cmd, cameraTextureDescriptor);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_LastFrameSourceInit == false)
            {
                m_LastFrameSourceInit = true;
                CommandBuffer cmd = CommandBufferPool.Get(Name);
                cmd.Blit(m_Source, m_LastFrameSource);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
            else
            {
                CommandBuffer cmd = CommandBufferPool.Get(Name);
                cmd.Blit(m_Source, m_LastFrameSource, m_Mat, 0);
                cmd.Blit(m_Source, m_LastFrameSource);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
            throw new System.NotImplementedException();
        }
        public override void Dispose()
        {
            m_LastFrameSource.Release();
        }
    }


}
