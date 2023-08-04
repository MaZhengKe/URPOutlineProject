using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class RadialBlurTool : BaseTool<RadialBlur>
    {
        public float BlurRadius;
        public float Iteration;
        public Vector2 RadialCenter;


        private RTHandle m_BlurTexture;

        public RadialBlurTool(ScriptableRenderPass renderPass) : base(renderPass)
        {
        }

        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.RadialBlur;
        public override string ShaderName => "KuanMi/RadialBlur";

        public override void UpdateTool(float width, float height)
        {
            base.UpdateTool(width, height);
            BlurRadius = blurVolume.BlurRadius.value;
            Iteration = blurVolume.Iteration.value;
            RadialCenter = blurVolume.RadialCenter.value;
        }

        public override void OnCameraSetup(RenderTextureDescriptor descriptor)
        {
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurTex");
        }

        public override void Execute(CommandBuffer cmd, RTHandle source, RTHandle target)
        {
            Blit(cmd, source, m_BlurTexture);

            m_Material.SetVector(Params, new Vector4(BlurRadius, Iteration, RadialCenter.x, RadialCenter.y));

            Blit(cmd, m_BlurTexture, target, m_Material);
        }
    }
}