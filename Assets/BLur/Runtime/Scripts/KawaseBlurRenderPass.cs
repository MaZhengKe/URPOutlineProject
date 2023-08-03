using BLur.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BLur.Runtime
{
    public class KawaseBlurRenderPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(BlurRendererFeature.ProfileId.KawaseBlur);

        private ScriptableRenderer m_Renderer;
        private Material m_Material;

        private RTHandle m_BlurTexture1;
        private RTHandle m_BlurTexture2;

        private KawaseBlur m_kawaseBlur;
        private static readonly int BlurRadius = Shader.PropertyToID("_Offset");
        private static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            var stack = VolumeManager.instance.stack;
            m_kawaseBlur = stack.GetComponent<KawaseBlur>();

            RenderTextureDescriptor descriptor = cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.width /= m_kawaseBlur.DownSample.value;
            descriptor.height /= m_kawaseBlur.DownSample.value;
            

            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture1, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name:"_BlurTex1");
            RenderingUtils.ReAllocateIfNeeded(ref m_BlurTexture2, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp,name:"_BlurTex2");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogError("Material is null");
                return;
            }

            var stack = VolumeManager.instance.stack;
            m_kawaseBlur = stack.GetComponent<KawaseBlur>();

            if (!m_kawaseBlur.IsActive())
                return;

            var blurRadius = m_kawaseBlur.BlurRadius.value;
            var iteration = m_kawaseBlur.Iteration.value;

            var cmd = CommandBufferPool.Get();


            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blit(cmd, m_Renderer.cameraColorTargetHandle, m_BlurTexture1);

                bool needSwitch = true;
                
                for (int i = 0; i < iteration; i++)
                {
                    var source = needSwitch? m_BlurTexture1 : m_BlurTexture2;
                    var target = needSwitch? m_BlurTexture2 : m_BlurTexture1;
                    
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    block.SetFloat(BlurRadius,  (float)i/m_kawaseBlur.DownSample.value + blurRadius);
                    block.SetTexture(BlitTexture, source);

                    CoreUtils.DrawFullScreen(cmd, m_Material, target,block,1);
                    
                    needSwitch = !needSwitch;
                }

                m_Material.SetFloat(BlurRadius,  (float)iteration/m_kawaseBlur.DownSample.value + blurRadius);
                Blit(cmd, needSwitch? m_BlurTexture1 : m_BlurTexture2, m_Renderer.cameraColorTargetHandle, m_Material,1);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public bool Setup(ScriptableRenderer renderer, Material material)
        {
            m_Material = material;
            m_Renderer = renderer;

            m_kawaseBlur = VolumeManager.instance.stack.GetComponent<KawaseBlur>();
            return m_kawaseBlur.IsActive();
        }

        public void Dispose()
        {
            m_BlurTexture1?.Release();
        }
    }
}