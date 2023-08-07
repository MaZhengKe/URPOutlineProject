using KuanMi.Blur;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.VolumetricLighting
{
    public class VolumeRenderFeature : ScriptableRendererFeature
    {
        public enum ProfileId
        {
            SpotVolume,
            DirectionalVolume,
            VolumeBlur
        }


        [Reload("Shaders/GaussianBlur.shader")]
        public Shader GaussianBlurShader;

        public Material GaussianBlurMaterial;

        public GaussianSetting gaussianBlur;

        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Reload("Meshes/Sphere.fbx")] public Mesh defaultMesh;

        [SerializeField] [HideInInspector] [Reload("Textures/BlueNoise/LDR_LLL1_{0}.png", 0, 63)]
        public Texture2D[] blueNoise;

        private SpotVolumeRenderPass m_SpotVolumeRenderPass;
        private DirectionalVolumeRenderPass m_DirectionalVolumeRenderPass;

        [SerializeField, HideInInspector] private Shader m_Shader;

        private const string k_ShaderName = "KuanMi/DirectionalVolumetricLighting";
        private Material m_Material;

        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/VolumetricLighting/Runtime");
#endif

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
            if (renderingData.cameraData.camera.name == "Preview Scene Camera")
                return;
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/Blur/Runtime");
#endif

            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }

            bool shouldAdd =
                m_DirectionalVolumeRenderPass.Setup(renderer, m_Material, GaussianBlurMaterial, gaussianBlur);
            if (shouldAdd)
            {
                renderer.EnqueuePass(m_DirectionalVolumeRenderPass);
            }

            m_SpotVolumeRenderPass.Setup(renderer, m_Material, m_DirectionalVolumeRenderPass.volumetricLightingTexture);
            renderer.EnqueuePass(m_SpotVolumeRenderPass);
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
            if (m_Material == null)
            {
                if (m_Shader == null)
                {
                    m_Shader = Shader.Find(k_ShaderName);
                }

                m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
            }


            if (GaussianBlurMaterial == null && GaussianBlurShader != null)
                GaussianBlurMaterial = CoreUtils.CreateEngineMaterial(GaussianBlurShader);
            return GaussianBlurMaterial != null && m_Material != null;
        }
    }
}