using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur.Runtime
{
    public class KawaseBlurRenderPass : BaseBlurRendererPassWithVolume<KawaseBlur>
    {
        protected override ProfilingSampler GetProfilingSampler()
        {
            return ProfilingSampler.Get(BlurRendererFeature.ProfileId.KawaseBlur);
        }

        private RTHandle m_BlurTexture1;
        private RTHandle m_BlurTexture2;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture1, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture2, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex2");
        }

        public override void ExecuteWithCmd(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var blurRadius = blurVolume.BlurRadius.value;
            var iteration = blurVolume.Iteration.value;

            Blit(cmd, m_Renderer.cameraColorTargetHandle, m_BlurTexture1);

            bool needSwitch = true;

            for (int i = 0; i < iteration; i++)
            {
                var source = needSwitch ? m_BlurTexture1 : m_BlurTexture2;
                var target = needSwitch ? m_BlurTexture2 : m_BlurTexture1;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetFloat(BlurRadius, (float)i / blurVolume.DownSample.value + blurRadius);
                block.SetTexture(BlitTexture, source);

                CoreUtils.DrawFullScreen(cmd, m_Material, target, block, 1);

                needSwitch = !needSwitch;
            }

            m_Material.SetFloat(BlurRadius, (float)iteration / blurVolume.DownSample.value + blurRadius);
            Blit(cmd, needSwitch ? m_BlurTexture1 : m_BlurTexture2, m_Renderer.cameraColorTargetHandle, m_Material, 1);
        }


        public override void Dispose()
        {
            m_BlurTexture1?.Release();
        }
    }
}