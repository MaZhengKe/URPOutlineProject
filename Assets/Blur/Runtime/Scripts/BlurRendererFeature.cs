using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class BlurRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        private GaussianBlurRendererPass m_GaussianBlurRendererPass;
        private KawaseBlurRenderPass m_KawaseBlurRenderPass;
        private DualBlurRenderPass m_DualBlurRenderPass;
        private BokehBlurRendererPass m_BokehBlurRendererPass;
        private GrainyBlurRendererPass m_GrainyBlurRendererPass;
        private RadialBlurRenderPass m_RadialBlurRenderPass;
        
        protected List<BaseBlurRendererPass> rendererPasses = new();

        
        public enum ProfileId
        {
            GaussianBlur,
            KawaseBlur,
            DualBlur,
            BokehBlur,
            GrainyBlur,
            RadialBlur
        }

        public override void Create()
        {
            m_GaussianBlurRendererPass = new GaussianBlurRendererPass()
            {
                renderPassEvent = renderPassEvent
            };
            rendererPasses.Add(m_GaussianBlurRendererPass);
            
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
            
            rendererPasses.Add(m_GrainyBlurRendererPass = new GrainyBlurRendererPass()
            {
                renderPassEvent = renderPassEvent
            });
            
            rendererPasses.Add(m_RadialBlurRenderPass = new RadialBlurRenderPass()
            {
                renderPassEvent = renderPassEvent
            });

        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(renderingData.cameraData.camera.name == "Preview Scene Camera")
                return;
            
            if(m_GaussianBlurRendererPass.Setup(renderer))
            {
                renderer.EnqueuePass(m_GaussianBlurRendererPass);
            }
            
            if(m_KawaseBlurRenderPass.Setup(renderer))
            {
                renderer.EnqueuePass(m_KawaseBlurRenderPass);
            }
            
            if(m_DualBlurRenderPass.Setup(renderer))
            {
                renderer.EnqueuePass(m_DualBlurRenderPass);
            }
            
            if(m_BokehBlurRendererPass.Setup(renderer))
            {
                renderer.EnqueuePass(m_BokehBlurRendererPass);
            }

            if (m_GrainyBlurRendererPass.Setup(renderer))
            {
                renderer.EnqueuePass(m_GrainyBlurRendererPass);
            }
            
            if (m_RadialBlurRenderPass.Setup(renderer))
            {
                renderer.EnqueuePass(m_RadialBlurRenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_GaussianBlurRendererPass?.Dispose();
            m_KawaseBlurRenderPass?.Dispose();
            m_DualBlurRenderPass?.Dispose();
            m_BokehBlurRendererPass?.Dispose();
        }

    }
}