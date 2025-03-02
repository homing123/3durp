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
    public const string CMDBufferName = "Blur Post Process";
    const float E = 2.17828f;

    private Material m_Material;
    private BlurSettings m_BlurSettings;
    RTHandle m_BlurTexHandle;
    private RTHandle m_SourceHandle; //셰이더 적용하기전 화면 텍스쳐
    float m_LastStrength;


    public void Init()
    {
        m_BlurSettings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
        m_Material = new Material(Shader.Find(ShaderFindName));
        m_isInit = m_BlurSettings != null && m_Material != null;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        if (m_isInit)
        {
            m_BlurTexHandle = RTHandles.Alloc(
                Vector2.one, // 스크린 크기의 100% (1.0, 1.0)
                dimension: TextureDimension.Tex2D,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                useDynamicScale: true, // 동적 스케일링 지원
                name: RenderTargetName,
                wrapMode: TextureWrapMode.Clamp
            );
        }
    }

    public bool Setup(ScriptableRenderer renderer)
    {
        m_SourceHandle = renderer.cameraColorTargetHandle;
        if (m_isInit == false)
        {
            Init();
        }
        m_isActive = m_BlurSettings.IsActive();
        return m_isInit && m_isActive;
    }

    //프레임당 한번씩 실행됨
    //셰이더속성 설정
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_isInit == false || m_isActive == false)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(CMDBufferName);

        if (m_LastStrength != m_BlurSettings.strength.value)
        {
            m_LastStrength = m_BlurSettings.strength.value;

            //Set Blur effect propertties.
            int gridSize = Mathf.CeilToInt(m_BlurSettings.strength.value * 6.0f);
            gridSize = gridSize < 3 ? 3 : gridSize;
            if (gridSize % 2 == 0)
            {
                gridSize++;
            }

            //max = 91 cbuffer max = 46
            int weightCount = gridSize / 2 + 1;
            float[] arr_Weight = new float[weightCount];
            for(int i=0;i< weightCount; i++)
            {
                arr_Weight[i] = gaussian(i, m_BlurSettings.strength.value);
                //Debug.Log(i + " " + arr_Weight[i]);
            }
            m_Material.SetInteger("_GridSize", gridSize);
            m_Material.SetFloat("_Spread", m_BlurSettings.strength.value);
            //m_Material.SetFloatArray("_Weight", arr_Weight);
            //Debug.Log(m_BlurSettings.strength.value + " " + gridSize);
        }
       

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
    float gaussian(int x, float _Spread)
    {
        float sigmaSqu = _Spread * _Spread;
        return (1 / Mathf.Sqrt(Mathf.PI * 2 * sigmaSqu)) * Mathf.Pow(E, -(x * x) / (2 * sigmaSqu));
    }
}
