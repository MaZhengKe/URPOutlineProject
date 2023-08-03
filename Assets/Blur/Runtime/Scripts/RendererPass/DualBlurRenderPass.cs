using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur.Runtime
{
    public class DualBlurRenderPass : BaseBlurRendererPassWithVolume<DualBlur>
    {
        protected override ProfilingSampler GetProfilingSampler()
        {
            return ProfilingSampler.Get(BlurRendererFeature.ProfileId.DualBlur);
        }

        RTHandle[] m_Down;
        RTHandle[] m_Up;

        const int k_MaxPyramidSize = 16;

        public DualBlurRenderPass()
        {
            m_Down = new RTHandle[k_MaxPyramidSize];
            m_Up = new RTHandle[k_MaxPyramidSize];
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            for (int i = 0; i < blurVolume.Iteration.value; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_Down[i], descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp,
                    name: "_Down" + i);
                RenderingUtils.ReAllocateIfNeeded(ref m_Up[i], descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp,
                    name: "_Up" + i);

                descriptor.width /= 2;
                descriptor.height /= 2;
            }
        }

        public override void ExecuteWithCmd(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var blurRadius = blurVolume.BlurRadius.value;
            var iteration = blurVolume.Iteration.value;

            m_Material.SetVector(BlitTextureSt, new Vector4(1, 1, 0, 0));
            m_Material.SetFloat(BlurRadius, blurRadius);

            Blitter.BlitCameraTexture(cmd, m_Renderer.cameraColorTargetHandle, m_Down[0],
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 3);

            var lastDown = m_Down[0];

            for (int i = 1; i < iteration; i++)
            {
                Blitter.BlitCameraTexture(cmd, lastDown, m_Down[i], RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, m_Material, 3);
                lastDown = m_Down[i];
            }

            var lastUp = lastDown;
            for (int i = iteration - 2; i >= 0; i--)
            {
                Blitter.BlitCameraTexture(cmd, lastUp, m_Up[i], RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, m_Material, 2);
                lastUp = m_Up[i];
            }

            // Blit(cmd, lastUp, m_Renderer.cameraColorTargetHandle);
            Blitter.BlitCameraTexture(cmd, lastUp, m_Renderer.cameraColorTargetHandle, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store, m_Material, 2);
        }

        public override void Dispose()
        {
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