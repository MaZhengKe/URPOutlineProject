using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class BokehBlurTool : BaseTool<BokehBlur>
    {
        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.BokehBlur;

        public float Iteration;
        public float BlurRadius;

        private RTHandle m_BlurTexture;
        private Vector4 mGoldenRot;

        public override string ShaderName => "KuanMi/BokehBlur";

        public BokehBlurTool(ScriptableRenderPass renderPass) : base(renderPass)
        {
            var c = Mathf.Cos(2.39996323f);
            var s = Mathf.Sin(2.39996323f);
            mGoldenRot.Set(c, s, -s, c);
        }

        public override void UpdateTool(float width, float height)
        {
            base.UpdateTool(width, height);
            Iteration = blurVolume.Iteration.value;
            BlurRadius = blurVolume.BlurRadius.value;
        }

        public override void OnCameraSetup(RenderTextureDescriptor descriptor)
        {
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex");
        }

        public override void Execute(CommandBuffer cmd, RTHandle source, RTHandle target)
        {
            _renderPass.Blit(cmd, source, m_BlurTexture);

            var width = m_BlurTexture.rt.width;
            var height = m_BlurTexture.rt.height;

            m_Material.SetVector(GoldenRot, mGoldenRot);
            m_Material.SetVector(Params, new Vector4(Iteration, BlurRadius, 1f / width, 1f / height));
            _renderPass.Blit(cmd, m_BlurTexture, target, m_Material);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_BlurTexture?.Release();
        }
    }
}