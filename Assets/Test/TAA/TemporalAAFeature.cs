using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public class TemporalAACustomFeature : HMScriptableRenderFeature
{
    public const string Name = "TemporalAACustom";

    public override void Create()
    {
        if (HMUtil.IsPlay())
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
        bool m_Active;
        float m_LastWeight;

        Material m_Mat; //마테리얼 캐싱안해두니까 null오류가 가끔씩 생긴다.

        public TemporalAACustomPass(Material mat)
        {
            m_Mat = mat;
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
            TemporalAACustomSetting m_Setting = VolumeManager.instance.stack.GetComponent<TemporalAACustomSetting>();
            if(m_Setting == null)
            {
                return false;
            }
            bool isActive = m_Setting.IsActive();

            if (m_LastWeight != m_Setting.m_Weight.value)
            {
                m_LastWeight = m_Setting.m_Weight.value;
                m_Mat.SetFloat("_Weight", m_LastWeight);
            }
            if (isActive)
            {
                if (Camera.main.depthTextureMode != DepthTextureMode.Depth || Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
                {
                    Debug.Log("TemporalAACustom must has depth");
                    return false;
                }
            }
            return isActive;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_Active == false)
            {
                return;
            }
            m_Mat.SetTexture("_LastFrameSource", m_LastFrameSource);
            base.Configure(cmd, cameraTextureDescriptor);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            if (m_Active == false)
            {
                return;
            }
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
