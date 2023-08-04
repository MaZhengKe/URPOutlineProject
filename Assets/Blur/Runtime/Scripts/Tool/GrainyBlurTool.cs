using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class GrainyBlurTool : BaseTool<GrainyBlur>
    {
        public float BlurRadius;
        public float Iteration;
        public float TimeSpeed;
        public bool BlueNoise;

        private Texture blueNoiseTexture;
        
        public Texture2DArray blueNoise { get; set; }
        

        private RTHandle m_BlurTexture;

        public GrainyBlurTool(ScriptableRenderPass renderPass) : base(renderPass)
        {
            blueNoiseTexture = Resources.Load<Texture>("KuanMi/LDR_LLL1_0 1");
            
            blueNoise = Resources.Load<Texture2DArray>("KuanMi/output");
        }

        public override void UpdateTool(float width, float height)
        {
            base.UpdateTool(width, height);
            BlurRadius = blurVolume.BlurRadius.value;
            Iteration = blurVolume.Iteration.value;
            BlueNoise = blurVolume.blueNoise.value;
            TimeSpeed = blurVolume.TimeSpeed.value;
        }

        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.GrainyBlur;
        public override string ShaderName => "KuanMi/GrainyBlur";

        public override void OnCameraSetup(RenderTextureDescriptor descriptor)
        {
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex");
        }

        public override void Execute(CommandBuffer cmd, RTHandle source, RTHandle target)
        {
            _renderPass.Blit(cmd, source, m_BlurTexture);
            m_Material.SetFloat(BlurRadiusID, BlurRadius);
            m_Material.SetFloat(IterationID, Iteration);
            m_Material.SetFloat(TimeSpeedID, TimeSpeed);
            if (BlueNoise)
            {
                m_Material.EnableKeyword("_BLUE_NOISE");
                m_Material.SetTexture(BlueNoiseID, blueNoise);
            }
            else
            {
                m_Material.DisableKeyword("_BLUE_NOISE");
            }

            _renderPass.Blit(cmd, m_BlurTexture, target, m_Material);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_BlurTexture?.Release();
        }
    }
}