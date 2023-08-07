using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AO
{
    [Serializable]
    public class SSAOSettings
    {
        public float Intensity = 3.0f;
        public float Radius = 0.035f;
        public float Falloff = 100f;
    }

    public class SSAOFeature : ScriptableRendererFeature
    {
        [Reload("Shaders/SSAO.shader")] public Shader shader;

        public Material material;
        public SSAOPass SSAOPass;
        
        public SSAOSettings m_Settings = new SSAOSettings();

        internal ref SSAOSettings settings => ref m_Settings;
        internal const string k_AOInterleavedGradientKeyword = "_INTERLEAVED_GRADIENT";
        internal const string k_AOBlueNoiseKeyword = "_BLUE_NOISE";
        internal const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        internal const string k_SourceDepthLowKeyword = "_SOURCE_DEPTH_LOW";
        internal const string k_SourceDepthMediumKeyword = "_SOURCE_DEPTH_MEDIUM";
        internal const string k_SourceDepthHighKeyword = "_SOURCE_DEPTH_HIGH";
        internal const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";
        internal const string k_SampleCountLowKeyword = "_SAMPLE_COUNT_LOW";
        internal const string k_SampleCountMediumKeyword = "_SAMPLE_COUNT_MEDIUM";
        internal const string k_SampleCountHighKeyword = "_SAMPLE_COUNT_HIGH";


        [SerializeField] [HideInInspector] [Reload("Textures/BlueNoise/LDR_LLL1_{0}.png", 0, 7)]
        public Texture2D[] blueNoise;
        
        public override void Create()
        {


#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/AO");
#endif

            material = CoreUtils.CreateEngineMaterial(shader);

            SSAOPass = new SSAOPass();


        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            SSAOPass.Setup(renderer, material,settings,blueNoise);
            renderer.EnqueuePass(SSAOPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
            material = null;
            
            SSAOPass.Dispose();
        }

        public enum ProfileId
        {
            SSAO,
        }
    }
}