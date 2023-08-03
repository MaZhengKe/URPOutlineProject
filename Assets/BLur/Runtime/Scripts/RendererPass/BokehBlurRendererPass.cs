using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur.Runtime
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
        protected override ProfilingSampler GetProfilingSampler()
        {
            return ProfilingSampler.Get(BlurRendererFeature.ProfileId.BokehBlur);
        }

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
            m_Material.SetVector(Params, new Vector4(blurVolume.Iteration2.value, blurVolume.BlurRadius.value, 1f / width, 1f / height));
            Blit(cmd,  m_BlurTexture , m_Renderer.cameraColorTargetHandle, m_Material, 4);
        }

        public override void Dispose()
        {
            m_BlurTexture?.Release();
        }
    }
}