using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class BlurRendererFeature : ScriptableRendererFeature
    {
        // Serialized fields

        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [SerializeField] [HideInInspector] [Reload("Textures/BlueNoise/LDR_LLL1_{0}.png", 0, 7)]
        public Texture2D[] blueNoise;

        [SerializeField] [HideInInspector] [Reload("Shaders/GaussianBlur.shader")]
        public Shader GaussianBlurShader;

        [SerializeField] [HideInInspector] [Reload("Shaders/KawaseBlur.shader")]
        public Shader KawaseBlurShader;

        [SerializeField] [HideInInspector] [Reload("Shaders/DualBlur.shader")]
        public Shader DualBlurShader;

        [SerializeField] [HideInInspector] [Reload("Shaders/BokehBlur.shader")]
        public Shader BokehBlurShader;

        [SerializeField] [HideInInspector] [Reload("Shaders/GrainyBlur.shader")]
        public Shader GrainyBlurShader;

        [SerializeField] [HideInInspector] [Reload("Shaders/RadialBlur.shader")]
        public Shader RadialBlurShader;

        [SerializeField] [HideInInspector] [Reload("Shaders/MaskBlend.shader")]
        public Shader BlendShader;

        // Private fields
        private Material GaussianBlurMaterial;
        private Material KawaseBlurMaterial;
        private Material DualBlurMaterial;
        private Material BokehBlurMaterial;
        private Material GrainyBlurMaterial;
        private Material RadialBlurMaterial;
        private Material BlendMaterial;

        private GaussianBlurRendererPass m_GaussianBlurRendererPass;
        private KawaseBlurRenderPass m_KawaseBlurRenderPass;
        private DualBlurRenderPass m_DualBlurRenderPass;
        private BokehBlurRendererPass m_BokehBlurRendererPass;
        private GrainyBlurRendererPass m_GrainyBlurRendererPass;
        private RadialBlurRenderPass m_RadialBlurRenderPass;

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
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/Blur/Runtime");
#endif

            m_GaussianBlurRendererPass = new GaussianBlurRendererPass()
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

            m_BokehBlurRendererPass = new BokehBlurRendererPass()
            {
                renderPassEvent = renderPassEvent
            };

            m_GrainyBlurRendererPass = new GrainyBlurRendererPass()
            {
                renderPassEvent = renderPassEvent
            };

            m_RadialBlurRenderPass = new RadialBlurRenderPass()
            {
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.name == "Preview Scene Camera")
                return;

            if (!GetMaterials())
            {
                {
                    Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.",
                        GetType().Name, name);
                    return;
                }
            }

            var stack = VolumeManager.instance.stack;


            if (m_GaussianBlurRendererPass.Setup(renderer, GaussianBlurMaterial,
                    stack.GetComponent<GaussianBlur>().GetSetting(), BlendMaterial))
            {
                renderer.EnqueuePass(m_GaussianBlurRendererPass);
            }

            if (m_KawaseBlurRenderPass.Setup(renderer, KawaseBlurMaterial,
                    stack.GetComponent<KawaseBlur>().GetSetting(), BlendMaterial))
            {
                renderer.EnqueuePass(m_KawaseBlurRenderPass);
            }

            if (m_DualBlurRenderPass.Setup(renderer, DualBlurMaterial, stack.GetComponent<DualBlur>().GetSetting(),
                    BlendMaterial))
            {
                renderer.EnqueuePass(m_DualBlurRenderPass);
            }

            if (m_BokehBlurRendererPass.Setup(renderer, BokehBlurMaterial, stack.GetComponent<BokehBlur>().GetSetting(),
                    BlendMaterial))
            {
                renderer.EnqueuePass(m_BokehBlurRendererPass);
            }

            if (m_GrainyBlurRendererPass.Setup(renderer, GrainyBlurMaterial,
                    stack.GetComponent<GrainyBlur>().GetSetting(), BlendMaterial))
            {
                renderer.EnqueuePass(m_GrainyBlurRendererPass);
            }

            if (m_RadialBlurRenderPass.Setup(renderer, RadialBlurMaterial,
                    stack.GetComponent<RadialBlur>().GetSetting(), BlendMaterial))
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
            m_GrainyBlurRendererPass?.Dispose();
            m_RadialBlurRenderPass?.Dispose();

            m_GaussianBlurRendererPass = null;
            m_KawaseBlurRenderPass = null;
            m_DualBlurRenderPass = null;
            m_BokehBlurRendererPass = null;
            m_GrainyBlurRendererPass = null;
            m_RadialBlurRenderPass = null;

            CoreUtils.Destroy(GaussianBlurMaterial);
            CoreUtils.Destroy(KawaseBlurMaterial);
            CoreUtils.Destroy(DualBlurMaterial);
            CoreUtils.Destroy(BokehBlurMaterial);
            CoreUtils.Destroy(GrainyBlurMaterial);
            CoreUtils.Destroy(RadialBlurMaterial);
            CoreUtils.Destroy(BlendMaterial);
        }

        private bool GetMaterials()
        {
            if (GaussianBlurMaterial == null && GaussianBlurShader != null)
                GaussianBlurMaterial = CoreUtils.CreateEngineMaterial(GaussianBlurShader);
            if (KawaseBlurMaterial == null && KawaseBlurShader != null)
                KawaseBlurMaterial = CoreUtils.CreateEngineMaterial(KawaseBlurShader);
            if (DualBlurMaterial == null && DualBlurShader != null)
                DualBlurMaterial = CoreUtils.CreateEngineMaterial(DualBlurShader);
            if (BokehBlurMaterial == null && BokehBlurShader != null)
                BokehBlurMaterial = CoreUtils.CreateEngineMaterial(BokehBlurShader);
            if (GrainyBlurMaterial == null && GrainyBlurShader != null)
                GrainyBlurMaterial = CoreUtils.CreateEngineMaterial(GrainyBlurShader);
            if (RadialBlurMaterial == null && RadialBlurShader != null)
                RadialBlurMaterial = CoreUtils.CreateEngineMaterial(RadialBlurShader);

            if (BlendMaterial == null && BlendShader != null)
                BlendMaterial = CoreUtils.CreateEngineMaterial(BlendShader);

            return (GrainyBlurMaterial != null && GaussianBlurMaterial != null && KawaseBlurMaterial != null &&
                    DualBlurMaterial != null && BokehBlurMaterial != null && RadialBlurMaterial != null &&
                    BlendMaterial != null);
        }
    }
}