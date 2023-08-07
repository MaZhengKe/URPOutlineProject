using KuanMi.Blur;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blur.Runtime
{
    public class RenderBlurTexture: ScriptableRendererFeature
    {
        
        [Reload("Shaders/GaussianBlur.shader")]
        public Shader GaussianBlurShader;
        
        public Material GaussianBlurMaterial;
        
        public GaussianSetting gaussianBlur;
        public string targetTextureName = "_BlurTexture";
        
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        private GaussianBlurRendererPass m_GaussianBlurRendererPass;
        
        
        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/Blur/Runtime");
#endif
            
            m_GaussianBlurRendererPass = new GaussianBlurRendererPass()
            {
                renderPassEvent = renderPassEvent,
                TargetTextureName = targetTextureName,
                renderToTexture = true
            };


            // Debug.Log(gaussianBlur.BlurRadius.value);
            
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            
            if(renderingData.cameraData.camera.name == "Preview Scene Camera")
                return;
            if (!GetMaterials())
            {
                {
                    Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.",
                        GetType().Name, name);
                    return;
                }
            }

            Debug.Log(GaussianBlurMaterial);
            m_GaussianBlurRendererPass.Setup(renderer,GaussianBlurMaterial,gaussianBlur,null);
            renderer.EnqueuePass(m_GaussianBlurRendererPass);

        }
        
        protected bool GetMaterials()
        {
            if (GaussianBlurMaterial == null && GaussianBlurShader != null)
                GaussianBlurMaterial = CoreUtils.CreateEngineMaterial(GaussianBlurShader);
            return GaussianBlurMaterial != null;
        }
    }
}