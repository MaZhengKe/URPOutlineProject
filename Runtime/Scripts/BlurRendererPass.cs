using BLur.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BLur.Runtime
{
    public class BlurRendererPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(BlurRendererFeature.ProfileId.Blur);

        private ScriptableRenderer m_Renderer;
        private Material m_Material1;
        private Material m_Material2;

        private RTHandle m_BlurTexture1;
        private RTHandle m_BlurTexture2;

        private GaussianBlur m_GaussianBlur;
        private static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");

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

            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture1, descriptor, FilterMode.Bilinear,TextureWrapMode.Clamp);
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture2, descriptor, FilterMode.Bilinear,TextureWrapMode.Clamp);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material1 == null)
            {
                Debug.LogError("Material is null");
                return;
            }

            var stack = VolumeManager.instance.stack;
            m_GaussianBlur = stack.GetComponent<GaussianBlur>();
            
            if(!m_GaussianBlur.IsActive())
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
                    m_Material1.SetVector(BlurOffset, new Vector4(blurRadius / width, 0, 0, 0));
                    Blit(cmd, m_BlurTexture1, m_BlurTexture2, m_Material1);

                    m_Material2.SetVector(BlurOffset, new Vector4(0, blurRadius / height, 0, 0));
                    Blit(cmd, m_BlurTexture2, m_BlurTexture1, m_Material2);
                }
                Blit(cmd, m_BlurTexture1,m_Renderer.cameraColorTargetHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public bool Setup(ScriptableRenderer renderer, Material material1, Material material2)
        {
            m_Material1 = material1;
            m_Material2 = material2;
            m_Renderer = renderer;
            return true;
        }

        public void Dispose()
        {
            m_BlurTexture1?.Release();
            m_BlurTexture2?.Release();
        }
    }
}