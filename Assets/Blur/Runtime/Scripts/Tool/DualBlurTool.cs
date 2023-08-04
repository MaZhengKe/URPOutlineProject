using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class DualBlurTool : BaseTool<DualBlur>
    {
        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.DualBlur;

        public float blurRadius;
        public int iteration;

        RTHandle[] m_Down;
        RTHandle[] m_Up;
        const int k_MaxPyramidSize = 16;

        public DualBlurTool(ScriptableRenderPass renderPass) : base(renderPass)
        {
            m_Down = new RTHandle[k_MaxPyramidSize];
            m_Up = new RTHandle[k_MaxPyramidSize];
        }

        public override void UpdateTool(float width, float height)
        {
            base.UpdateTool(width, height);

            blurRadius = blurVolume.BlurRadius.value;
            iteration = blurVolume.Iteration.value;
        }

        public override string ShaderName => "KuanMi/DualBlur";

        public override void OnCameraSetup(RenderTextureDescriptor descriptor)
        {
            for (int i = 0; i < iteration; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_Down[i], descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp,
                    name: "_Down" + i);
                RenderingUtils.ReAllocateIfNeeded(ref m_Up[i], descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp,
                    name: "_Up" + i);

                descriptor.width /= 2;
                descriptor.height /= 2;
            }
        }

        public override void Execute(CommandBuffer cmd, RTHandle source, RTHandle target)
        {
            m_Material.SetVector(BlitTextureSt, new Vector4(1, 1, 0, 0));
            m_Material.SetFloat(BlurRadius, blurRadius);

            Blitter.BlitCameraTexture(cmd, source, m_Down[0],
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 1);

            var lastDown = m_Down[0];

            for (int i = 1; i < iteration; i++)
            {
                Blitter.BlitCameraTexture(cmd, lastDown, m_Down[i], RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, m_Material, 1);
                lastDown = m_Down[i];
            }

            var lastUp = lastDown;
            for (int i = iteration - 2; i >= 0; i--)
            {
                Blitter.BlitCameraTexture(cmd, lastUp, m_Up[i], RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, m_Material, 0);
                lastUp = m_Up[i];
            }

            // Blit(cmd, lastUp, m_Renderer.cameraColorTargetHandle);
            Blitter.BlitCameraTexture(cmd, lastUp, target, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store, m_Material, 0);
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var rtHandle in m_Down)
            {
                rtHandle?.Release();
            }

            foreach (var rtHandle in m_Up)
            {
                rtHandle?.Release();
            }
        }
    }
}