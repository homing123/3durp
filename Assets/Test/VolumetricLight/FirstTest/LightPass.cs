//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Rendering;

//[RequireComponent(typeof(Light))]

//public class LightPass : MonoBehaviour
//{
//    CommandBuffer cmd;
//    public RenderTexture m_ShadowMap;

//    Light m_Light;
//    private void OnEnable()
//    {
//        cmd = new CommandBuffer { name = "LightShadowMap" };
//        RenderTargetIdentifier renderTarget = BuiltinRenderTextureType.CurrentActive;

//        m_ShadowMap = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);
//        m_ShadowMap.filterMode = FilterMode.Point;
//        m_ShadowMap.SetGlobalShaderProperty("_ShadowDepthTexture");

//        cmd.SetShadowSamplingMode(renderTarget, ShadowSamplingMode.RawDepth);

//        cmd.Blit(renderTarget, m_ShadowMap);

//        cmd.SetGlobalTexture("_ShadowDepthTexture", m_ShadowMap);

//        m_Light = GetComponent<Light>();
//        m_Light.AddCommandBuffer(LightEvent.AfterShadowMap, cmd);
//    }
//    private void OnDisable()
//    {
//        m_Light.RemoveCommandBuffer(LightEvent.AfterShadowMap, cmd);
//        cmd.Release();
//        m_ShadowMap.Release();
//    }
//}
