using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class GaussianBlurTool : BaseTool<GaussianSetting>
    {
        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.GaussianBlur;

        private RTHandle m_BlurTexture1;
        private RTHandle m_BlurTexture2;

        public GaussianBlurTool(ScriptableRenderPass renderPass,GaussianSetting setting) : base(renderPass,setting)
        {
        }

        public override void OnCameraSetup(RenderTextureDescriptor descriptor)
        {
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture1, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture2, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex2");
        }

        public override void Execute(CommandBuffer cmd, RTHandle source, RTHandle target)
        {
            Blit(cmd, source, m_BlurTexture1);
            for (int i = 0; i < setting.Iteration; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetVector(BlurOffset, new Vector4(setting.BlurRadius / 1920, 0, 0, 0));
                block.SetTexture(BlitTexture, m_BlurTexture1);
                CoreUtils.DrawFullScreen(cmd, m_Material, m_BlurTexture2, block, 0);


                MaterialPropertyBlock block2 = new MaterialPropertyBlock();
                block2.SetVector(BlurOffset, new Vector4(0, setting.BlurRadius / 1080, 0, 0));
                block2.SetTexture(BlitTexture, m_BlurTexture2);
                CoreUtils.DrawFullScreen(cmd, m_Material, m_BlurTexture1, block2, 0);
            }

            Blit(cmd, m_BlurTexture1, target);
        }

        public override void Dispose()
        {
            base.Dispose();

            m_BlurTexture1?.Release();
            m_BlurTexture2?.Release();
        }
    }
}