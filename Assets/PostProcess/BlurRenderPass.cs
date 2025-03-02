using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRenderPass : ScriptableRenderPass
{

    bool m_isInit = false;
    bool m_isActive = false;
    public const string RenderTargetName = "BlurRT";
    public const string ShaderFileName = "Blur";
    public const string ShaderFindName = "PostProcessing/Blur";

    private Material m_Material;
    private BlurSettings m_BlurSettings;
    RTHandle m_BlurTexHandle;
    private RTHandle m_SourceHandle; //셰이더 적용하기전 화면 텍스쳐


    public void Init(RTHandle sourceHandle)
    {
        m_BlurSettings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
        m_Material = new Material(Shader.Find("PostProcessing/Blur"));
        m_BlurTexHandle = RTHandles.Alloc(
            Vector2.one, // 스크린 크기의 100% (1.0, 1.0)
            dimension: TextureDimension.Tex2D,
            colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
            useDynamicScale: true, // 동적 스케일링 지원
            name: RenderTargetName,
            wrapMode: TextureWrapMode.Clamp
        ); 
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        m_isInit = true;
    }

    public bool Setup(ScriptableRenderer renderer)
    {
        m_SourceHandle = renderer.cameraColorTargetHandle;
        if (m_isInit == false)
        {
            Init(m_SourceHandle);
        }
        m_isActive = m_BlurSettings != null && m_Material != null && m_BlurSettings.IsActive();
        return m_isInit;
    }

    //프레임당 한번씩 실행됨
    //셰이더속성 설정
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_isInit == false || m_isActive == false)
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
        cmd.Blit(m_SourceHandle, m_BlurTexHandle, m_Material, 0);
        cmd.Blit(m_BlurTexHandle, m_SourceHandle, m_Material, 1);
        context.ExecuteCommandBuffer(cmd);

        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        if(m_isInit == true)
        {
            m_BlurTexHandle.Release();
        }
    }
    ~BlurRenderPass()
    {
        Dispose();
    }
    public override void FrameCleanup(CommandBuffer cmd)
    {

    }
}
