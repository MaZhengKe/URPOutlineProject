using BLur.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BLur.Runtime
{
    public class GaussianBlurRendererPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(BlurRendererFeature.ProfileId.GaussianBlur);

        private ScriptableRenderer m_Renderer;
        private Material m_Material;

        private RTHandle m_BlurTexture1;
        private RTHandle m_BlurTexture2;

        private GaussianBlur m_GaussianBlur;
        private static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        private static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            var stack = VolumeManager.instance.stack;
            m_GaussianBlur = stack.GetComponent<GaussianBlur>();

            RenderTextureDescriptor descriptor = cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.width /= m_GaussianBlur.DownSample.value;
            descriptor.height /= m_GaussianBlur.DownSample.value;

            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture1, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name:"_BlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture2, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name:"_BlurTex2");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogError("Material is null");
                return;
            }

            var stack = VolumeManager.instance.stack;
            m_GaussianBlur = stack.GetComponent<GaussianBlur>();

            if (!m_GaussianBlur.IsActive())
                return;

            var blurRadius = m_GaussianBlur.BlurRadius.value;
            var iteration = m_GaussianBlur.Iteration.value;

            var width = m_Renderer.cameraColorTargetHandle.rt.width;
            var height = m_Renderer.cameraColorTargetHandle.rt.height;

            var cmd = CommandBufferPool.Get();


            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blit(cmd, m_Renderer.cameraColorTargetHandle, m_BlurTexture1);
                for (int i = 0; i < iteration; i++)
                {
                    
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    block.SetVector(BlurOffset, new Vector4(blurRadius / width, 0, 0, 0));
                    block.SetTexture(BlitTexture, m_BlurTexture1);
                    CoreUtils.DrawFullScreen(cmd, m_Material, m_BlurTexture2,block,0);

                    
                    MaterialPropertyBlock block2 = new MaterialPropertyBlock();
                    block2.SetVector(BlurOffset, new Vector4(0, blurRadius / height, 0, 0));
                    block2.SetTexture(BlitTexture, m_BlurTexture2);
                    CoreUtils.DrawFullScreen(cmd, m_Material, m_BlurTexture1,block2,0);
                }

                Blit(cmd, m_BlurTexture1, m_Renderer.cameraColorTargetHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public bool Setup(ScriptableRenderer renderer, Material material)
        {
            m_Material = material;
            m_Renderer = renderer;

            m_GaussianBlur = VolumeManager.instance.stack.GetComponent<GaussianBlur>();
            return m_GaussianBlur.IsActive();
        }

        public void Dispose()
        {
            m_BlurTexture1?.Release();
            m_BlurTexture2?.Release();
        }
    }
}