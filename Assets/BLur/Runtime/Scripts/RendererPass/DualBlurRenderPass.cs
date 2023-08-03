using BLur.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BLur.Runtime
{
    public class DualBlurRenderPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler =
            ProfilingSampler.Get(BlurRendererFeature.ProfileId.DualBlur);

        private ScriptableRenderer m_Renderer;
        private Material m_Material;

        RTHandle[] m_Down;
        RTHandle[] m_Up;

        private DualBlur m_DualBlur;
        private static readonly int BlurRadius = Shader.PropertyToID("_Offset");
        private static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");

        const int k_MaxPyramidSize = 16;
        
        
        public DualBlurRenderPass()
        {
            m_Down = new RTHandle[k_MaxPyramidSize];
            m_Up = new RTHandle[k_MaxPyramidSize];
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            var stack = VolumeManager.instance.stack;
            m_DualBlur = stack.GetComponent<DualBlur>();

            RenderTextureDescriptor descriptor = cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            
            descriptor.width /= m_DualBlur.DownSample.value;
            descriptor.height /= m_DualBlur.DownSample.value;

            Debug.Log( m_DualBlur.Iteration.value );
            for (int i = 0; i < m_DualBlur.Iteration.value ; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_Down[i], descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Down"+ i);
                RenderingUtils.ReAllocateIfNeeded(ref m_Up[i], descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Up"+ i);
                
                descriptor.width /= 2;
                descriptor.height /= 2;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogError("Material is null");
                return;
            }

            var stack = VolumeManager.instance.stack;
            m_DualBlur = stack.GetComponent<DualBlur>();

            if (!m_DualBlur.IsActive())
                return;

            var blurRadius = m_DualBlur.BlurRadius.value;
            var iteration = m_DualBlur.Iteration.value;

            var cmd = CommandBufferPool.Get();
            m_Material.SetVector("_BlitTexture_ST", new Vector4(1, 1, 0, 0));
            m_Material.SetFloat(BlurRadius, blurRadius);

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, m_Renderer.cameraColorTargetHandle, m_Down[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 3);
                
                var lastDown = m_Down[0];
                
                for (int i = 1; i < iteration; i++)
                {                
                    Blitter.BlitCameraTexture(cmd, lastDown, m_Down[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 3);
                    lastDown = m_Down[i];
                }
                
                var lastUp = lastDown;
                for (int i = iteration - 2; i >= 0 ; i--)
                {
                    Blitter.BlitCameraTexture(cmd, lastUp, m_Up[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 2);
                    lastUp = m_Up[i];
                }

                // Blit(cmd, lastUp, m_Renderer.cameraColorTargetHandle);
                Blitter.BlitCameraTexture(cmd, lastUp, m_Renderer.cameraColorTargetHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 2);
                
                
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public bool Setup(ScriptableRenderer renderer, Material material)
        {
            m_Material = material;
            m_Renderer = renderer;

            m_DualBlur = VolumeManager.instance.stack.GetComponent<DualBlur>();
            return m_DualBlur.IsActive();
        }

        public void Dispose()
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