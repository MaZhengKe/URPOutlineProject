using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.VolumetricLighting
{
    public class SpotVolumeRenderPass : ScriptableRenderPass
    {
        private static readonly int BlueNoise = Shader.PropertyToID("_BlueNoise");
        
        
        private readonly ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(VolumeRenderFeature.ProfileId.SpotVolume);
        public Mesh defaultMesh { get; set; }
        public Texture2DArray blueNoise { get; set; }
        
        public RTHandle volumetricLightingTexture;

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
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            CoreUtils.SetRenderTarget(cmd, volumetricLightingTexture, ClearFlag.All);

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                var volumeLights = BaseVolumeLight.BaseVolumeLightList;

                foreach (var volumeLight in volumeLights)
                {
                    var visibleLights = renderingData.lightData.visibleLights;

                    var lightIndex = -1;
                    for (int i = 0; i < visibleLights.Length; i++)
                    {
                        if (visibleLights[i].light == volumeLight.Light)
                        {
                            lightIndex = i - 1;
                        }
                    }
                    
                    if(lightIndex == -1 || volumeLight.MaxIntensity == Color.black) continue;

                    volumeLight.LightIndex = lightIndex;
                    volumeLight.UpdateIfNeed();
                    
                    var mesh  = volumeLight.mesh? volumeLight.mesh : defaultMesh;
                    
                    if (blueNoise != null)
                    {
                        volumeLight.material.SetTexture(BlueNoise, blueNoise);
                    }

                    cmd.DrawMesh(mesh, volumeLight.Matrix, volumeLight.material);
                }
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public void Dispose()
        {
            
        }

        public void Setup(ScriptableRenderer renderer, Material mMaterial, RTHandle volumetricLightingTexture)
        {
            this.volumetricLightingTexture = volumetricLightingTexture;
        }
    }
}