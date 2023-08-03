using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class GaussianBlurRendererPass : BaseBlurRendererPassWithVolume<GaussianBlur>
    {
        protected override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.GaussianBlur;
        
        protected override string ShaderName => "KuanMi/GaussianBlur";


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

            var width = m_Renderer.cameraColorTargetHandle.rt.width;
            var height = m_Renderer.cameraColorTargetHandle.rt.height;

            Blit(cmd, m_Renderer.cameraColorTargetHandle, m_BlurTexture1);
            for (int i = 0; i < iteration; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetVector(BlurOffset, new Vector4(blurRadius / width, 0, 0, 0));
                block.SetTexture(BlitTexture, m_BlurTexture1);
                CoreUtils.DrawFullScreen(cmd, m_Material, m_BlurTexture2, block, 0);


                MaterialPropertyBlock block2 = new MaterialPropertyBlock();
                block2.SetVector(BlurOffset, new Vector4(0, blurRadius / height, 0, 0));
                block2.SetTexture(BlitTexture, m_BlurTexture2);
                CoreUtils.DrawFullScreen(cmd, m_Material, m_BlurTexture1, block2, 0);
            }

            Blit(cmd, m_BlurTexture1, isMask ? m_MaskTexture : m_Renderer.cameraColorTargetHandle);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_BlurTexture1?.Release();
            m_BlurTexture2?.Release();
        }
    }
}