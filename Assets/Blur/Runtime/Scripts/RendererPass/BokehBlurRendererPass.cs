using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class BokehBlurRendererPass : BaseBlurRendererPassWithVolume<BokehBlur>
    {
        private RTHandle m_BlurTexture;

        private Vector4 mGoldenRot;

        public BokehBlurRendererPass()
        {
            float c = Mathf.Cos(2.39996323f);
            float s = Mathf.Sin(2.39996323f);
            mGoldenRot.Set(c, s, -s, c);
        }
        protected override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.BokehBlur;

        protected override string ShaderName => "KuanMi/BokehBlur";

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex");
        }

        public override void ExecuteWithCmd(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Blit(cmd, m_Renderer.cameraColorTargetHandle, m_BlurTexture);
            
            var width = m_BlurTexture.rt.width;
            var height = m_BlurTexture.rt.height;
            
            m_Material.SetVector(GoldenRot, mGoldenRot);
            m_Material.SetVector(Params, new Vector4(blurVolume.Iteration.value, blurVolume.BlurRadius.value, 1f / width, 1f / height));
            Blit(cmd, m_BlurTexture, isMask ? m_MaskTexture : m_Renderer.cameraColorTargetHandle, m_Material);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_BlurTexture?.Release();
        }
    }
}