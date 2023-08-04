using UnityEngine;
using UnityEngine.Rendering;

namespace KuanMi.VolumetricLighting
{
    
    [VolumeComponentMenu("KuanMi/Volumetric Lighting")]
    public class VolumetricLighting : VolumeComponent , IPostProcessComponent
    {
        [Header("全局平行光")]
        public MinFloatParameter intensity = new MinFloatParameter(1, 0);
        public ClampedFloatParameter mieK = new ClampedFloatParameter(0.8f, -1,1);
        
        public ClampedIntParameter numSteps = new ClampedIntParameter(16, 1, 32);
        
        
        public ClampedFloatParameter BlurRadius = new ClampedFloatParameter(0f, 0f, 10f);
        public ClampedIntParameter Iteration = new ClampedIntParameter(0, 0, 10);

        public bool IsActive() => intensity.value > 0;
        
    }
}