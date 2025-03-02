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
#if UNITY_EDITOR
        m_isActive = m_BlurSettings.IsActive() && Application.isPlaying;
#else
        m_isActive = m_BlurSettings.IsActive();
#endif
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
        //pass -1이 기본값 -1 = 모든패스 실행 하지만 urp에서 멀티패스 지원하지 않음 즉 0번째패스만 실행이라고 한다.
        //cmd.SetGlobalTexture 로 텍스쳐 넘겨줄 수 있음 
        //SetGlobalTexture 안부르면 _MainTex 로 자동으로 넘어감
        //아마 _MainTex_TexelSize 도 비슷한 구조일듯
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
