using KuanMi.Blur;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Blur.Runtime
{
    public class RenderBlurTexture: ScriptableRendererFeature
    {
        public GaussianBlur gaussianBlur;
        public string targetTextureName = "_BlurTexture";
        
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        private GaussianBlurRendererPass m_GaussianBlurRendererPass;
        
        
        public override void Create()
        {
            m_GaussianBlurRendererPass = new GaussianBlurRendererPass()
            {
                renderPassEvent = renderPassEvent,
                TargetTextureName = targetTextureName,
                renderToTexture = true
            };
            m_GaussianBlurRendererPass.tool.blurVolume = gaussianBlur;

            // Debug.Log(gaussianBlur.BlurRadius.value);
            
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            
            if(renderingData.cameraData.camera.name == "Preview Scene Camera")
                return;

            
            m_GaussianBlurRendererPass.Setup(renderer);
            renderer.EnqueuePass(m_GaussianBlurRendererPass);

        }
    }
}