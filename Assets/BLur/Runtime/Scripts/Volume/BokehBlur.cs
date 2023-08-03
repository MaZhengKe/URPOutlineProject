using UnityEngine.Rendering;

namespace KuanMi.Blur.Runtime
{
    [VolumeComponentMenu("KuanMi/Blur/KawaseBlur")]
    public class BokehBlur : BaseBlur
    {
        public ClampedIntParameter Iteration2 = new ClampedIntParameter(32, 16, 128);
    }
}