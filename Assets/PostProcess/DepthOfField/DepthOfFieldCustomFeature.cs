using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthOfFieldCustomFeature : HMScriptableRenderFeature
{
    public const string Name = "DepthOfFieldCustom";
    public override void Create()
    {
        if(IsPlay())
        {
            m_Pass = new DepthOfFieldCustomPass(m_Mat);
            name = Name;
        }
    }
    public class DepthOfFieldCustomPass : HMScriptableRenderPass
    {
        RTHandle m_Source;
        RTHandle m_RTDestBlurX; //temporary를 사용해서 더빠르나 쉐이더에서 settexture로 사용불가능
        RTHandle m_RTDestBlurY;
        RTHandle m_RTSourceCopy; //쉐이더에서 settexture로 사용가능

        float m_CurBlurIntensity;
        float m_CurFocusDepth;
        float m_CurFocusDepthSize;

        Material m_Mat;
        public DepthOfFieldCustomPass(Material mat)
        {
            m_Mat = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            m_RTDestBlurX = RTHandles.Alloc(
                    Vector2.one, // 스크린 크기의 100% (1.0, 1.0)
                    dimension: TextureDimension.Tex2D,
                    colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                    useDynamicScale: true, // 동적 스케일링 지원
                    name: Name,
                    wrapMode: TextureWrapMode.Clamp
                );
            m_RTDestBlurY = RTHandles.Alloc(
                Vector2.one, // 스크린 크기의 100% (1.0, 1.0)
                dimension: TextureDimension.Tex2D,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                useDynamicScale: true, // 동적 스케일링 지원
                name: Name,
                wrapMode: TextureWrapMode.Clamp
                );
            m_RTSourceCopy = RTHandles.Alloc(
                Vector2.one, // 스크린 크기의 100% (1.0, 1.0)
                dimension: TextureDimension.Tex2D,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                useDynamicScale: true, // 동적 스케일링 지원
                name: Name,
                wrapMode: TextureWrapMode.Clamp
                );
        }

        public override bool SetUp(ScriptableRenderer renderer)
        {
            DepthOfFieldCustomSetting setting = VolumeManager.instance.stack.GetComponent<DepthOfFieldCustomSetting>();
            if (setting == null)
            {
                return false;
            }
            bool isActive = setting.IsActive();

            if (m_CurBlurIntensity != setting.m_BlurIntensity.value || m_CurFocusDepth != setting.m_FocusDepth.value || m_CurFocusDepthSize != setting.m_FocusDepthSize.value)
            {
                m_CurBlurIntensity = setting.m_BlurIntensity.value;
                m_CurFocusDepth = setting.m_FocusDepth.value;
                m_CurFocusDepthSize = setting.m_FocusDepthSize.value;
                m_Mat.SetFloat("_Spread", m_CurBlurIntensity);
                m_Mat.SetFloat("_FocusDepth", m_CurFocusDepth);
                m_Mat.SetFloat("_FocusDepthSize", m_CurFocusDepthSize);
                int gridSize = Mathf.CeilToInt(m_CurBlurIntensity * 6);
                gridSize = gridSize < 3 ? 3 : gridSize;
                if (gridSize % 2 == 0)
                {
                    gridSize++;
                }
                m_Mat.SetFloat("_GridSize", gridSize);
            }
            if (isActive)
            {
                if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
                {
                    Debug.Log("DepthOfField must has depth");
                    return false;
                }
            }
            return isActive;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_Mat.SetTexture("_OriginTex", m_RTSourceCopy);
            base.Configure(cmd, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(Name);
            cmd.Blit(m_Source, m_RTSourceCopy);
            cmd.Blit(m_Source, m_RTDestBlurX, m_Mat, 0);
            cmd.Blit(m_RTDestBlurX, m_RTDestBlurY, m_Mat, 1);
            cmd.Blit(m_RTDestBlurY, m_Source, m_Mat, 2);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void Dispose()
        {
            m_RTDestBlurX.Release();
            m_RTDestBlurY.Release();
            m_RTSourceCopy.Release();         
        }
    }


}
