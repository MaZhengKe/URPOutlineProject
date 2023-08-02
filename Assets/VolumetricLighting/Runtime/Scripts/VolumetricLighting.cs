using UnityEngine.Rendering;

namespace Other.VolumetricLighting.Scripts
{
    public class VolumetricLighting : VolumeComponent , IPostProcessComponent
    {
        public MinFloatParameter intensity = new MinFloatParameter(1, 0);
        public ClampedFloatParameter mieK = new ClampedFloatParameter(0.8f, -1,1);

        public bool IsActive() => intensity.value > 0;
        
    }
}