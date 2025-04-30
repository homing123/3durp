using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.VisualScripting.Member;

public class BlurFeature : HMScriptableRenderFeature
{
    public const string Name = "GaussianBlur";
    public override void Create()
    {
        if (IsPlay())
        {
            m_Pass = new BlurPass(m_Mat);
            name = Name;
        }
    }
    public class BlurPass : HMScriptableRenderPass
    {
        const float E = 2.17828f;

        RTHandle m_BlurTexHandle;
        private RTHandle m_SourceHandle; //���̴� �����ϱ��� ȭ�� �ؽ���
        float m_CurStrength;
        Material m_Mat;
        public BlurPass(Material mat)
        {
            m_Mat = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            //RenderTextureDescriptor des = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32);
            m_BlurTexHandle = RTHandles.Alloc(
                Vector2.one,
                dimension: TextureDimension.Tex2D,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                useDynamicScale: true,
                name: Name,
                wrapMode: TextureWrapMode.Clamp);
        }

        public override bool SetUp(ScriptableRenderer renderer)
        {
            BlurSettings setting = VolumeManager.instance.stack.GetComponent<BlurSettings>();
            m_SourceHandle = renderer.cameraColorTargetHandle;
            if (setting == null)
            {
                return false;
            }
            bool isActive = setting.IsActive();

            if (isActive && m_CurStrength != setting.strength.value)
            {
                m_CurStrength = setting.strength.value;
                //Set Blur effect propertties.
                int gridSize = Mathf.CeilToInt(m_CurStrength * 6.0f);
                gridSize = gridSize < 3 ? 3 : gridSize;
                if (gridSize % 2 == 0)
                {
                    gridSize++;
                }

                m_Mat.SetInteger("_GridSize", gridSize);
                m_Mat.SetFloat("_Spread", m_CurStrength);
            }
            if (isActive)
            {
                if (Camera.main.depthTextureMode != DepthTextureMode.Depth && Camera.main.depthTextureMode != DepthTextureMode.DepthNormals)
                {
                    Debug.Log("Blur must has depth");
                    return false;
                }
            }
            return isActive;
        }

        //�����Ӵ� �ѹ��� �����
        //���̴��Ӽ� ����
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //Execute effect using effect material with two passes.
            //pass -1�� �⺻�� -1 = ����н� ���� ������ urp���� ��Ƽ�н� �������� ���� �� 0��°�н��� �����̶�� �Ѵ�.
            //cmd.SetGlobalTexture �� �ؽ��� �Ѱ��� �� ���� 
            //SetGlobalTexture �Ⱥθ��� _MainTex �� �ڵ����� �Ѿ
            //�Ƹ� _MainTex_TexelSize �� ����� �����ϵ�

            CommandBuffer cmd = CommandBufferPool.Get(Name);
            cmd.Blit(m_SourceHandle, m_BlurTexHandle, m_Mat, 0);
            cmd.Blit(m_BlurTexHandle, m_SourceHandle, m_Mat, 1);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void Dispose()
        {
            m_BlurTexHandle.Release();           
        }
        float gaussian(int x, float _Spread)
        {
            float sigmaSqu = _Spread * _Spread;
            return (1 / Mathf.Sqrt(Mathf.PI * 2 * sigmaSqu)) * Mathf.Pow(E, -(x * x) / (2 * sigmaSqu));
        }
    }


}
