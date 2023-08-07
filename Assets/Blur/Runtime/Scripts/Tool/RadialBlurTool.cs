using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class RadialBlurTool : BaseTool<RadialSetting>
    {



        private RTHandle m_BlurTexture;



        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.RadialBlur;



        public override void OnCameraSetup(RenderTextureDescriptor descriptor)
        {
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex");
        }

        public override void Execute(CommandBuffer cmd, RTHandle source, RTHandle target)
        {
            Blit(cmd, source, m_BlurTexture);

            m_Material.SetVector(Params, new Vector4(setting.BlurRadius, setting.Iteration,setting. RadialCenter.x,setting. RadialCenter.y));

            Blit(cmd, m_BlurTexture, target, m_Material);
        }

        public RadialBlurTool(ScriptableRenderPass renderPass, RadialSetting blurVolume) : base(renderPass, blurVolume)
        {
        }
    }
}