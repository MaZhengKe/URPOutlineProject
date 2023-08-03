using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BLur.Runtime
{
    public class BlurRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        private const string k_ShaderName = "KuanMi/Blur";
        
        private BlurRendererPass m_BlurRendererPass;
        private Material m_Material1;
        private Material m_Material2;
        [SerializeField, HideInInspector] private Shader m_Shader;
        public enum ProfileId
        {
            Blur,
        }

        public override void Create()
        {
            m_BlurRendererPass = new BlurRendererPass()
            {
                renderPassEvent = renderPassEvent
            };

        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(renderingData.cameraData.camera.name == "Preview Scene Camera")
                return;
            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }

            bool shouldAdd = m_BlurRendererPass.Setup(renderer, m_Material1, m_Material2);
            if (shouldAdd)
            {
                renderer.EnqueuePass(m_BlurRendererPass);
            }
        }
        
        
        private bool GetMaterial()
        {
            if (m_Material1 != null)
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

            m_Material1 = CoreUtils.CreateEngineMaterial(m_Shader);
            m_Material2 = CoreUtils.CreateEngineMaterial(m_Shader);

            return m_Material1 != null;
        }

        protected override void Dispose(bool disposing)
        {
            m_BlurRendererPass?.Dispose();
            CoreUtils.Destroy(m_Material1);
            CoreUtils.Destroy(m_Material2);
        }

    }
}