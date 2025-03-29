using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
public class VL_First : VolumeComponent, IPostProcessComponent
{
    public MaterialParameter m_Mat = new MaterialParameter(null);
    public ClampedIntParameter m_Samples = new ClampedIntParameter(32,1, 256);
    public FloatParameter m_ShadowScatteringDistance = new FloatParameter(1);
    public FloatParameter m_Add = new FloatParameter(0);
    public FloatParameter m_Out = new FloatParameter(0);
    public int m_LastSamples;
    public float m_LastShadowScatteringDistance;
    public float m_LastAdd;
    public float m_LastOut;
    public bool IsUpdate()
    {
        return m_LastSamples != m_Samples.value || m_LastShadowScatteringDistance != m_ShadowScatteringDistance.value ||  m_LastAdd != m_Add.value || m_LastOut != m_Out.value;
    }
    public void Update()
    {
        m_LastSamples = m_Samples.value;
        m_LastShadowScatteringDistance = m_ShadowScatteringDistance.value;
        m_LastAdd = m_Add.value;
        m_LastOut = m_Out.value;
    }
    public bool IsActive()
    {
        return active;
    }

    public bool IsTileCompatible()
    {
        //2023 버전 이후 안쓴다더라 return false 해도 상관없음
        return false;
    }

}
public class VL_Pass : ScriptableRenderPass
{
    public const string RenderTargetName = "VL_First";
    public const string ShaderFindName = "VL_Test/First";
    public const string CMDBufferName = "VL_First";

    bool m_Active = false;
    bool m_Init = false;
    RTHandle m_SourceColor;
    Material m_Mat;
    VL_First m_Setting;
    int m_RTDestiNameID;
    public void Init()
    {
        m_Setting = VolumeManager.instance.stack.GetComponent<VL_First>();
        m_Mat = m_Setting.m_Mat.value;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        m_Init = m_Mat != null && m_Setting != null;
    }



    public bool Setup(ScriptableRenderer renderer)
    {
        if (m_Init == false)
        {
            Init();
        }
        m_SourceColor = renderer.cameraColorTargetHandle;

#if UNITY_EDITOR
        m_Active = m_Setting.IsActive() && Application.isPlaying;
#else
        m_Active = m_Setting.IsActive();
#endif
        if (m_Active && m_Init)
        {
            if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
            {
                Debug.Log("VL_First must has depth");
                return false;
            }
        }

        return m_Init && m_Active;
    }
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        if (m_Init == false || m_Active == false)
        {
            return;
        }

        cmd.GetTemporaryRT(m_RTDestiNameID, cameraTextureDescriptor);
        base.Configure(cmd, cameraTextureDescriptor);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_Init == false || m_Active == false)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(CMDBufferName);

        if (m_Setting.IsUpdate())
        {
            m_Setting.Update();
            m_Mat.SetFloat("_Samples", (float)m_Setting.m_Samples.value);
            m_Mat.SetFloat("_ShadowScatteringDistance", m_Setting.m_ShadowScatteringDistance.value);
            m_Mat.SetFloat("_Add", m_Setting.m_Add.value);
            m_Mat.SetFloat("_Out", m_Setting.m_Out.value);

        }
        m_Mat.SetVector("_LightPos", VL_FirstScene.Ins.m_Light.transform.position);
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
    public void Dispose()
    {
        if (m_Init == true)
        {
            //m_DestiColor.Release();
        }
    }
    ~VL_Pass()
    {
        Dispose();
    }
}
