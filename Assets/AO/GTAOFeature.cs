using System;
using KuanMi.Blur;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AO
{
    [Serializable]
    public class GTAOSettings
    {
        [Range(0, 4)]
        public float debug = 1.0f;
        [Range(0, 4)]
        public float intensity = 1.0f;
        
        [Range(0.25f, 5f)]
        public float radius = 2.0f;
        
        [Range(16,512)]
        public int maximumRadiusInPixels = 40;
        
        [Range(2,32)]
        public int stepCount = 4;
        
        [Range(1,6)]
        public int directionCount = 2;
    }

    public class GTAOFeature : ScriptableRendererFeature
    {
        [Reload("Shaders/GTAO.shader")] public Shader shader;
        
        [SerializeField] [HideInInspector] [Reload("Shaders/GaussianBlur.shader")]
        public Shader GaussianBlurShader;

        public Material material;
        public GTAOPass GTAOPass;
        
        public GTAOSettings m_Settings = new GTAOSettings();

        
        public GaussianSetting gaussianBlur;
        
        
        internal ref GTAOSettings settings => ref m_Settings;

        public override void Create()
        {

#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/AO");
#endif


            if (GTAOPass == null)
            {
                GTAOPass = new GTAOPass()
                {
                    renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
                };
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            
            if (!GetMaterials())
            {
                {
                    Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.",
                        GetType().Name, name);
                    return;
                }
            }
            GTAOPass.Setup(renderer, material,GaussianBlurMaterial,settings,gaussianBlur);
            renderer.EnqueuePass(GTAOPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            CoreUtils.Destroy(material);
            material = null;
            
            CoreUtils.Destroy(GaussianBlurMaterial);
            GaussianBlurMaterial = null;
            
            GTAOPass.Dispose();
            GTAOPass = null;
        }
        
        private Material GaussianBlurMaterial;
        private bool GetMaterials()
        {
            if (GaussianBlurMaterial == null && GaussianBlurShader != null)
                GaussianBlurMaterial = CoreUtils.CreateEngineMaterial(GaussianBlurShader);
            
            if (material == null && shader != null)
                material = CoreUtils.CreateEngineMaterial(shader);
            
            return GaussianBlurMaterial != null;
        }

        public enum ProfileId
        {
            GTAO,
        }
    }
}