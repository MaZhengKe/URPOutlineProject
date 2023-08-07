using Blur.Runtime.Scripts.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class DualBlurTool : BaseTool<BlurSetting>
    {
        public override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.DualBlur;



        RTHandle[] m_Down;
        RTHandle[] m_Up;
        const int k_MaxPyramidSize = 16;

        public DualBlurTool(ScriptableRenderPass renderPass,BlurSetting setting) : base(renderPass,setting)
        {
            m_Down = new RTHandle[k_MaxPyramidSize];
            m_Up = new RTHandle[k_MaxPyramidSize];
        }

        public override void OnCameraSetup(RenderTextureDescriptor descriptor)
        {
            for (int i = 0; i < setting.Iteration; i++)
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
            m_Material.SetFloat(BlurRadiusID, setting.BlurRadius);

            Blitter.BlitCameraTexture(cmd, source, m_Down[0],
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 1);

            var lastDown = m_Down[0];

            for (int i = 1; i < setting.Iteration; i++)
            {
                Blitter.BlitCameraTexture(cmd, lastDown, m_Down[i], RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, m_Material, 1);
                lastDown = m_Down[i];
            }

            var lastUp = lastDown;
            for (int i = setting.Iteration - 2; i >= 0; i--)
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