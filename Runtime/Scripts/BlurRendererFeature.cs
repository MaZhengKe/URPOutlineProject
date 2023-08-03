using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur.Runtime
{
    public class BlurRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        private const string k_ShaderName = "KuanMi/Blur";
        
        private GaussianBlurRendererPass _mGaussianBlurRendererPass;
        private KawaseBlurRenderPass m_KawaseBlurRenderPass;
        private DualBlurRenderPass m_DualBlurRenderPass;
        private BokehBlurRendererPass m_BokehBlurRendererPass;
        
        
        protected List<BaseBlurRendererPass> rendererPasses = new List<BaseBlurRendererPass>();

        private Material m_Material;
        
        [SerializeField, HideInInspector] private Shader m_Shader;
        public enum ProfileId
        {
            GaussianBlur,
            KawaseBlur,
            DualBlur,
            BokehBlur
        }

        public override void Create()
        {
            _mGaussianBlurRendererPass = new GaussianBlurRendererPass()
            {
                renderPassEvent = renderPassEvent
            };
            rendererPasses.Add(_mGaussianBlurRendererPass);
            
            m_KawaseBlurRenderPass = new KawaseBlurRenderPass()
            {
                renderPassEvent = renderPassEvent
            };
            rendererPasses.Add(m_KawaseBlurRenderPass);
            
            m_DualBlurRenderPass = new DualBlurRenderPass()
            {
                renderPassEvent = renderPassEvent
            };
            rendererPasses.Add(m_DualBlurRenderPass);
            
            rendererPasses.Add(m_BokehBlurRendererPass = new BokehBlurRendererPass()
            {
                renderPassEvent = renderPassEvent
            });

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
            
            // foreach (var rendererPass in rendererPasses.Where(rendererPass => rendererPass.Setup(renderer, m_Material)))
            // {
            //     Debug.Log(rendererPass + " " + Time.frameCount);
            //     renderer.EnqueuePass(rendererPass);
            // }
            
            if(_mGaussianBlurRendererPass.Setup(renderer, m_Material))
            {
                renderer.EnqueuePass(_mGaussianBlurRendererPass);
            }
            
            if(m_KawaseBlurRenderPass.Setup(renderer, m_Material))
            {
                renderer.EnqueuePass(m_KawaseBlurRenderPass);
            }
            
            if(m_DualBlurRenderPass.Setup(renderer, m_Material))
            {
                renderer.EnqueuePass(m_DualBlurRenderPass);
            }
            
            if(m_BokehBlurRendererPass.Setup(renderer, m_Material))
            {
                renderer.EnqueuePass(m_BokehBlurRendererPass);
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