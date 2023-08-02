using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Other.VolumetricLighting.Scripts
{
    public class DirectionalVolumeRenderPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(VolumeRenderFeature.ProfileId.DirectionalVolume);

        
        private static readonly int Intensity = Shader.PropertyToID("_Intensity");
        private static readonly int MieK = Shader.PropertyToID("_MieK");
        
        
        private VolumetricLighting m_VolumetricLighting;
        private Material m_Material;
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            if (m_Material == null)
            {
                Debug.LogErrorFormat(
                    "{0}.Execute(): Missing material. DirectionalVolumeRenderPass pass will not execute. Check for missing reference in the renderer resources.",
                    GetType().Name);
                return;
            }
            var stack = VolumeManager.instance.stack;
            m_VolumetricLighting = stack.GetComponent<VolumetricLighting>();

            if (!m_VolumetricLighting.IsActive())
                return;

            m_Material.SetFloat(Intensity, m_VolumetricLighting.intensity.value);
            m_Material.SetFloat(MieK, m_VolumetricLighting.mieK.value);

            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                CoreUtils.DrawFullScreen(cmd, m_Material);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public bool Setup(Material material)
        {
            m_Material = material;
            return true;
        }
        
        public void Dispose()
        {
            
            
        }
    }
}