using Blur.Runtime.Scripts.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class KawaseBlurTool: BaseTool<BlurSetting>
    {
        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.KawaseBlur;

        private RTHandle m_BlurTexture1;
        private RTHandle m_BlurTexture2;

        public KawaseBlurTool(ScriptableRenderPass renderPass,BlurSetting setting) : base(renderPass,setting)
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

            bool needSwitch = true;

            for (int i = 0; i < setting. Iteration; i++)
            {
                var sourceT = needSwitch ? m_BlurTexture1 : m_BlurTexture2;
                var targetT = needSwitch ? m_BlurTexture2 : m_BlurTexture1;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetFloat(BlurRadiusID, (float)i / setting.DownSample +setting. BlurRadius);
                block.SetTexture(BlitTexture, sourceT);

                CoreUtils.DrawFullScreen(cmd, m_Material, targetT, block);

                needSwitch = !needSwitch;
            }

            m_Material.SetFloat(BlurRadiusID, (float)setting.Iteration / setting.DownSample + setting.BlurRadius);
            Blit(cmd, needSwitch ? m_BlurTexture1 : m_BlurTexture2, target, m_Material);

        }
    }
}