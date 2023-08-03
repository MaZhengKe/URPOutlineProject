using UnityEngine.Rendering;

namespace BLur.Runtime.Volume
{
    [VolumeComponentMenu("KuanMi/Blur/KawaseBlur")]
    public class KawaseBlur : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter BlurRadius = new ClampedFloatParameter(0f, 0f, 10f);
        public ClampedIntParameter Iteration = new ClampedIntParameter(0, 0, 10);
        public ClampedIntParameter DownSample = new ClampedIntParameter(1, 1, 10);
        
        public bool IsActive()
        {
            return BlurRadius.value > 0f;
        }
    }
}