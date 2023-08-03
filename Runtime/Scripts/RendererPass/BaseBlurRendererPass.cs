using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur.Runtime
{
    public abstract class BaseBlurRendererPass : ScriptableRenderPass
    {
        protected abstract ProfilingSampler GetProfilingSampler();

        protected ScriptableRenderer m_Renderer;
        protected Material m_Material;

        protected RenderTextureDescriptor descriptor;
        protected static readonly int BlurRadius = Shader.PropertyToID("_Offset");
        protected static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");
        protected static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        protected static readonly int BlitTextureSt = Shader.PropertyToID("_BlitTexture_ST");
        internal static readonly int GoldenRot = Shader.PropertyToID("_GoldenRot");
        internal static readonly int Params = Shader.PropertyToID("_Params");


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            descriptor = cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
        }

        public abstract void ExecuteWithCmd(CommandBuffer cmd, ref RenderingData renderingData);

        public virtual bool Setup(ScriptableRenderer renderer, Material material)
        {
            m_Material = material;
            m_Renderer = renderer;
            return true;
        }

        public abstract void Dispose();
    }
}