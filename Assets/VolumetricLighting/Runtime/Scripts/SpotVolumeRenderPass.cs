using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Other.VolumetricLighting.Scripts
{
    public class SpotVolumeRenderPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(VolumeRenderFeature.ProfileId.SpotVolume);


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                var spotHelpers = BaseVolumeLight.BaseVolumeLightList;

                foreach (var spotHelper in spotHelpers)
                {
                    var mesh = spotHelper.mesh;
                    var transform = spotHelper.transform;
                    
                    var spotLight = spotHelper.Light;
                    var visibleLights = renderingData.lightData.visibleLights;

                    var lightIndex = -1;
                    for (int i = 0; i < visibleLights.Length; i++)
                    {
                        if (visibleLights[i].light == spotLight)
                        {
                            lightIndex = i - 1;
                        }
                    }
                    
                    if(lightIndex == -1) continue;

                    spotHelper.lightIndex = lightIndex;
                    
                    spotHelper.UpdateIfNeed();

                    cmd.DrawMesh(mesh, transform.localToWorldMatrix, spotHelper.material);
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