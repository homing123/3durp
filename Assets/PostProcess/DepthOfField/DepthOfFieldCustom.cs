using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("DepthOfFieldCustom")]
public class DepthOfFieldSetting : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter m_DOFIntensity = new ClampedFloatParameter(0, 0, 10);
    public ClampedFloatParameter m_BlurIntensity = new ClampedFloatParameter(0, 0, 15);
    public ClampedFloatParameter m_Depth = new ClampedFloatParameter(0, 0, 1);
    public MaterialParameter m_DofMat = new MaterialParameter(null);
    public MaterialParameter m_BlurMat = new MaterialParameter(null);


    float m_LastDOFIntensity;
    float m_LastBlurIntensity;
    float m_LastDepth;
    public bool IsUpdate()
    {
        return m_LastDOFIntensity != m_DOFIntensity.value || m_LastBlurIntensity != m_BlurIntensity.value || m_LastDepth != m_Depth.value;
    }
    public void Update()
    {
        m_LastDOFIntensity = m_DOFIntensity.value;
        m_LastBlurIntensity = m_BlurIntensity.value;
        m_LastDepth = m_Depth.value;
        m_DofMat.value.SetFloat("_DOFIntensity", m_DOFIntensity.value);
        m_DofMat.value.SetFloat("_Depth", m_Depth.value);

        m_BlurMat.value.SetFloat("_Spread", m_BlurIntensity.value);

        int gridSize = Mathf.CeilToInt(m_BlurIntensity.value * 6.0f);
        gridSize = gridSize < 3 ? 3 : gridSize;
        if (gridSize % 2 == 0)
        {
            gridSize++;
        }
        m_BlurMat.value.SetInteger("_GridSize", gridSize);
    }
    public bool IsActive()
    {
        return (m_DOFIntensity.value > 0.0f) && active;
    }

    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }

}

public class DepthOfFieldCustomPass : ScriptableRenderPass
{
    public const string Name = "DepthOfFieldCustom";
    DepthOfFieldSetting m_Setting;
    RTHandle m_Source;
    RTHandle m_RTDestBlurX; //temporary를 사용해서 더빠르나 쉐이더에서 settexture로 사용불가능
    RTHandle m_RTDestBlurY;
    RTHandle m_RTSourceCopy; //쉐이더에서 settexture로 사용가능

    bool m_Init;
    bool m_Active;
    void Init()
    {
        m_Setting = VolumeManager.instance.stack.GetComponent<DepthOfFieldSetting>();
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        m_Init = true;
        if (m_Init)
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32);
            m_RTDestBlurX = RTHandles.Alloc(desc);
            m_RTDestBlurY = RTHandles.Alloc(desc);
            m_RTSourceCopy = RTHandles.Alloc(desc);
        }
    }

    public bool Setup(ScriptableRenderer renderer)
    {
        if (m_Init == false)
        {
            Init();
        }

        m_Source = renderer.cameraColorTargetHandle;
#if UNITY_EDITOR
        m_Active = m_Setting.IsActive() && Application.isPlaying;
#else
        m_Active = m_Setting.IsActive() 
#endif

        if (m_Active && m_Init)
        {
            if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
            {
                Debug.Log("DepthOfField must has depth");
                return false;
            }
        }
        return m_Active && m_Init;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        if ((m_Init & m_Active) == false)
        {
            return;
        }
        m_Setting.m_DofMat.value.SetTexture("_OriginTex", m_RTSourceCopy);

        base.Configure(cmd, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if ((m_Init & m_Active) == false)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(Name);
        if (m_Setting.IsUpdate())
        {
            m_Setting.Update();
        }
        cmd.Blit(m_Source, m_RTSourceCopy);
        cmd.Blit(m_Source, m_RTDestBlurX, m_Setting.m_BlurMat.value, 0);
        cmd.Blit(m_RTDestBlurX, m_RTDestBlurY, m_Setting.m_BlurMat.value, 1);
        cmd.Blit(m_RTDestBlurY, m_Source, m_Setting.m_DofMat.value, 1);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
    }

    void Dispose()
    {
        if (m_Init)
        {
            m_RTDestBlurX.Release();
            m_RTDestBlurY.Release();
            m_RTSourceCopy.Release();
        }
    }

    ~DepthOfFieldCustomPass()
    {
        Dispose();
    }
}

