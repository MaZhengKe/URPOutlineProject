using KuanMi.Blur;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blur.Runtime
{
    public class RenderBlurTexture: ScriptableRendererFeature
    {
        public GaussianSetting gaussianBlur;
        public string targetTextureName = "_BlurTexture";
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [SerializeField] [HideInInspector] [Reload("Shaders/GaussianBlur.shader")]
        public Shader GaussianBlurShader;
        
        [SerializeField] [HideInInspector] [Reload("Shaders/MaskBlend.shader")]
        public Shader BlendShader;

        private Material GaussianBlurMaterial;
        private Material BlendMaterial;
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

            if(m_GaussianBlurRendererPass.Setup(renderer,GaussianBlurMaterial,gaussianBlur,BlendMaterial))
                renderer.EnqueuePass(m_GaussianBlurRendererPass);
        }

        private bool GetMaterials()
        {
            if (GaussianBlurMaterial == null && GaussianBlurShader != null)
                GaussianBlurMaterial = CoreUtils.CreateEngineMaterial(GaussianBlurShader);
            
            if (BlendMaterial == null && BlendShader != null)
                BlendMaterial = CoreUtils.CreateEngineMaterial(BlendShader);
            return GaussianBlurMaterial != null && BlendMaterial != null;
        }
    }
}