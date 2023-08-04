using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class GrainyBlurTool : BaseTool<GaussianBlur>
    {
        public float BlurRadius;
        public float Iteration;
        
        private RTHandle m_BlurTexture;

        public GrainyBlurTool(ScriptableRenderPass renderPass) : base(renderPass)
        {
        }

        public override void UpdateTool(float width, float height)
        {
            base.UpdateTool(width, height);
            BlurRadius = blurVolume.BlurRadius.value;
            Iteration = blurVolume.Iteration.value;
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
            _renderPass.Blit(cmd, m_BlurTexture, target, m_Material);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            m_BlurTexture?.Release();
        }
        
    }
}