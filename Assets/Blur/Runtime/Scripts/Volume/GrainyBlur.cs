using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    [VolumeComponentMenu("KuanMi/Blur/GrainyBlur")]
    public class GrainyBlur : BaseBlur
    {
        public BoolParameter blueNoise = new BoolParameter(false);
        public MinFloatParameter TimeSpeed = new MinFloatParameter(0, 0);
        GrainyBlur()
        {
            BlurRadius.max = 1;
        }
    }
}