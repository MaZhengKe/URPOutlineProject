using KuanMi.Blur;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.VolumetricLighting
{
    public class DirectionalVolumeRenderPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler =
            ProfilingSampler.Get(VolumeRenderFeature.ProfileId.DirectionalVolume);

        protected ScriptableRenderer m_Renderer;

        private static readonly int Intensity = Shader.PropertyToID("_Intensity");
        private static readonly int MieK = Shader.PropertyToID("_MieK");
        private static readonly int NumSteps = Shader.PropertyToID("_NumSteps");
        private static readonly int BlueNoise = Shader.PropertyToID("_BlueNoise");

        private VolumetricLighting m_VolumetricLighting;
        private Material m_Material;
        public Texture2DArray blueNoise { get; set; }
        public GaussianBlurTool blurTool { get; set; }

        public RTHandle volumetricLightingTexture;
        public RTHandle blurVolumetricLightingTexture;

        public string BlitShaderName = "KuanMi/Blit";
        public Material BlitMaterial { get; private set; }

        public DirectionalVolumeRenderPass()
        {
            blurTool = new GaussianBlurTool(this,new GaussianSetting());
            BlitMaterial = CoreUtils.CreateEngineMaterial(Shader.Find(BlitShaderName));
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            var descriptor = cameraTargetDescriptor;

            descriptor = cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref volumetricLightingTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_VolumeTex");
            RenderingUtils.ReAllocateIfNeeded(ref blurVolumetricLightingTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_BlurVolumeTex");
            blurTool.OnCameraSetup(descriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogErrorFormat(
                    "{0}.Execute(): Missing material. DirectionalVolumeRenderPass pass will not execute. Check for missing reference in the renderer resources.",
                    GetType().Name);
                return;
            }

            var stack = VolumeManager.instance.stack;
            m_VolumetricLighting = stack.GetComponent<VolumetricLighting>();

            m_Material.SetFloat(Intensity, m_VolumetricLighting.intensity.value);
            m_Material.SetFloat(MieK, m_VolumetricLighting.mieK.value);
            m_Material.SetFloat(NumSteps, m_VolumetricLighting.numSteps.value);

            m_Material.SetTexture(BlueNoise, blueNoise);

            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                if (m_VolumetricLighting.IsActive())
                {
                    CoreUtils.SetRenderTarget(cmd, volumetricLightingTexture);
                    CoreUtils.DrawFullScreen(cmd, m_Material);
                }
                //
                // blurTool.blurRadius = m_VolumetricLighting.BlurRadius.value;
                // blurTool.iteration = m_VolumetricLighting.Iteration.value;
                //
                // blurTool.width = m_Renderer.cameraColorTargetHandle.rt.width;
                // blurTool.height = m_Renderer.cameraColorTargetHandle.rt.height;

                blurTool.Execute(cmd, volumetricLightingTexture, blurVolumetricLightingTexture);

                BlitMaterial.SetTexture("_BlitTexture", blurVolumetricLightingTexture);
                CoreUtils.DrawFullScreen(cmd, BlitMaterial, m_Renderer.cameraColorTargetHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public bool Setup(ScriptableRenderer renderer, Material material, Material blurMat,
            GaussianSetting gaussianSetting)
        {
            m_Renderer = renderer;
            m_Material = material;
            blurTool.m_Material = blurMat;
            blurTool.setting = gaussianSetting;
            return true;
        }

        public void Dispose()
        {
            volumetricLightingTexture?.Release();
            blurVolumetricLightingTexture?.Release();
            blurTool.Dispose();
        }
    }
}