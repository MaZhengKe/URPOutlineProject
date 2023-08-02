using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Other.VolumetricLighting.Scripts
{
    public class VolumeRenderFeature : ScriptableRendererFeature
    {
        public enum ProfileId
        {
            SpotVolume,
            DirectionalVolume,
        }

        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        public Mesh defaultMesh;
        public Texture blueNoise;

        private SpotVolumeRenderPass m_SpotVolumeRenderPass;
        private DirectionalVolumeRenderPass m_DirectionalVolumeRenderPass;


        [SerializeField, HideInInspector] private Shader m_Shader;

        private const string k_ShaderName = "KuanMi/DirectionalVolumetricLighting";
        private Material m_Material;
        

        public override void Create()
        {
            defaultMesh = Resources.Load<Mesh>("KuanMi/Meshes/Sphere");
            blueNoise = Resources.Load<Texture>("KuanMi/LDR_LLL1_0 1");

            if(defaultMesh == null)
                Debug.LogError("defaultMesh is null");
            
            if(blueNoise == null)
                Debug.LogError("blueNoise is null");
            
            m_SpotVolumeRenderPass = new SpotVolumeRenderPass()
            {
                blueNoise = blueNoise,
                defaultMesh = defaultMesh,
                renderPassEvent = renderPassEvent
                
            };

            m_DirectionalVolumeRenderPass = new DirectionalVolumeRenderPass()
            {
                blueNoise = blueNoise,
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_SpotVolumeRenderPass);

            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }

            bool shouldAdd = m_DirectionalVolumeRenderPass.Setup(m_Material);
            if (shouldAdd)
            {
                renderer.EnqueuePass(m_DirectionalVolumeRenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_SpotVolumeRenderPass?.Dispose();
            m_SpotVolumeRenderPass = null;

            m_DirectionalVolumeRenderPass?.Dispose();
            m_DirectionalVolumeRenderPass = null;
        }


        private bool GetMaterial()
        {
            if (m_Material != null)
            {
                return true;
            }

            if (m_Shader == null)
            {
                m_Shader = Shader.Find(k_ShaderName);
                if (m_Shader == null)
                {
                    return false;
                }
            }

            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

            return m_Material != null;
        }
    }
}