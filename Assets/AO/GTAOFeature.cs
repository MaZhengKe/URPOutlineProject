using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AO
{
    [Serializable]
    public class GTAOSettings
    {
        [Range(0, 4)]
        public float intensity = 1.0f;
        
        [Range(0.25f, 5f)]
        public float radius = 2.0f;
        
        [Range(16,256)]
        public int maximumRadiusInPixels = 40;
        
        [Range(2,32)]
        public int stepCount = 4;
        
        [Range(1,6)]
        public int directionCount = 2;
    }

    public class GTAOFeature : ScriptableRendererFeature
    {
        [Reload("Shaders/GTAO.shader")] public Shader shader;

        public Material material;
        public GTAOPass GTAOPass;
        
        public GTAOSettings m_Settings = new GTAOSettings();

        internal ref GTAOSettings settings => ref m_Settings;

        public override void Create()
        {


#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/AO");
#endif

            material = CoreUtils.CreateEngineMaterial(shader);

            GTAOPass = new GTAOPass();


        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            GTAOPass.Setup(renderer, material,settings);
            renderer.EnqueuePass(GTAOPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            material = null;
            
            GTAOPass.Dispose();
        }

        public enum ProfileId
        {
            GTAO,
        }
    }
}