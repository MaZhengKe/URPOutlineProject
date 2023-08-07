using Blur.Runtime.Scripts.Settings;
using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    public class GrainyBlurSetting : BlurSetting
    {
        public bool blueNoise;
        public float TimeSpeed;
    }
    
    [VolumeComponentMenu("KuanMi/Blur/GrainyBlur")]
    public class GrainyBlur : BaseBlur<GrainyBlurSetting>
    {
        public BoolParameter blueNoise = new BoolParameter(false);
        public MinFloatParameter TimeSpeed = new MinFloatParameter(0, 0);
        GrainyBlur()
        {
            BlurRadius.max = 1;
        }
    }
}