using BLur.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BLur.Runtime
{
    public class BlurRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        private const string k_ShaderName = "KuanMi/Blur";
        
        private GaussianBlurRendererPass _mGaussianBlurRendererPass;
        private KawaseBlurRenderPass m_KawaseBlurRenderPass;
        private DualBlurRenderPass m_DualBlurRenderPass;

        private Material m_Material;
        
        [SerializeField, HideInInspector] private Shader m_Shader;
        public enum ProfileId
        {
            GaussianBlur,
            KawaseBlur,
            DualBlur,
        }

        public override void Create()
        {
            _mGaussianBlurRendererPass = new GaussianBlurRendererPass()
            {
                renderPassEvent = renderPassEvent
            };
            
            m_KawaseBlurRenderPass = new KawaseBlurRenderPass()
            {
                renderPassEvent = renderPassEvent
            };
            
            m_DualBlurRenderPass = new DualBlurRenderPass()
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

            bool shouldAdd = _mGaussianBlurRendererPass.Setup(renderer, m_Material);
            if (shouldAdd)
            {
                renderer.EnqueuePass(_mGaussianBlurRendererPass);
            }
            
            bool shouldAdd2 = m_KawaseBlurRenderPass.Setup(renderer, m_Material);
            if (shouldAdd2)
            {
                renderer.EnqueuePass(m_KawaseBlurRenderPass);
            }
            
            bool shouldAdd3 = m_DualBlurRenderPass.Setup(renderer, m_Material);
            if (shouldAdd3)
            {
                renderer.EnqueuePass(m_DualBlurRenderPass);
            }
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

        protected override void Dispose(bool disposing)
        {
            _mGaussianBlurRendererPass?.Dispose();
            m_KawaseBlurRenderPass?.Dispose();
            
            CoreUtils.Destroy(m_Material);
        }

    }
}