using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class KawaseBlurTool: BaseTool<KawaseBlur>
    {
        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.KawaseBlur;
        public float blurRadius;
        public int iteration;
        public int downSample;
        
        private RTHandle m_BlurTexture1;
        private RTHandle m_BlurTexture2;

        public KawaseBlurTool(ScriptableRenderPass renderPass) : base(renderPass)
        {
        }

        public override void UpdateTool(float width, float height)
        {
            base.UpdateTool(width, height);
            blurRadius = blurVolume.BlurRadius.value;
            iteration = blurVolume.Iteration.value;
            downSample = blurVolume.DownSample.value;
        }

        public override string ShaderName => "KuanMi/KawaseBlur";
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

            for (int i = 0; i < iteration; i++)
            {
                var sourceT = needSwitch ? m_BlurTexture1 : m_BlurTexture2;
                var targetT = needSwitch ? m_BlurTexture2 : m_BlurTexture1;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetFloat(BlurRadius, (float)i / downSample + blurRadius);
                block.SetTexture(BlitTexture, sourceT);

                CoreUtils.DrawFullScreen(cmd, m_Material, targetT, block);

                needSwitch = !needSwitch;
            }

            m_Material.SetFloat(BlurRadius, (float)iteration / downSample + blurRadius);
            Blit(cmd, needSwitch ? m_BlurTexture1 : m_BlurTexture2, target, m_Material);

        }
    }
}