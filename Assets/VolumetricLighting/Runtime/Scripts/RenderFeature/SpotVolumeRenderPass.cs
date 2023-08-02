using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Other.VolumetricLighting.Scripts
{
    public class SpotVolumeRenderPass : ScriptableRenderPass
    {
        private static readonly int NumSteps = Shader.PropertyToID("_NumSteps");
        private static readonly int BlueNoise = Shader.PropertyToID("_BlueNoise");
        
        
        private readonly ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(VolumeRenderFeature.ProfileId.SpotVolume);
        public Mesh defaultMesh { get; set; }
        public Texture blueNoise { get; set; }

        private VolumetricLighting m_VolumetricLighting;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            var stack = VolumeManager.instance.stack;
            m_VolumetricLighting = stack.GetComponent<VolumetricLighting>();
            
            var cmd = CommandBufferPool.Get();

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
                    
                    if(lightIndex == -1 || volumeLight.intensity <= float.Epsilon) continue;

                    volumeLight.lightIndex = lightIndex;
                    volumeLight.UpdateIfNeed();
                    
                    var mesh  = volumeLight.mesh? volumeLight.mesh : defaultMesh;
                    
                    volumeLight.material.SetFloat(NumSteps,m_VolumetricLighting.numSteps.value );
                    if (blueNoise != null)
                    {
                        volumeLight.material.SetTexture(BlueNoise, blueNoise);
                    }

                    cmd.DrawMesh(mesh, volumeLight.matrix, volumeLight.material);
                }
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public void Dispose()
        {
            
        }
    }
}