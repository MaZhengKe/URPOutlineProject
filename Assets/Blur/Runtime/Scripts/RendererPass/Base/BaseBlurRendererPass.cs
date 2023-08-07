using Blur.Runtime.Scripts.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseBlurRendererPass : ScriptableRenderPass
    {
        protected abstract BlurRendererFeature.ProfileId ProfileId { get; }

        protected ScriptableRenderer m_Renderer;

        protected RenderTextureDescriptor descriptor;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            descriptor = cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
        }

        public abstract void ExecuteWithCmd(CommandBuffer cmd, ref RenderingData renderingData);

        public bool Setup(ScriptableRenderer renderer)
        {
            m_Renderer = renderer;
            return true;
        }
    }
}