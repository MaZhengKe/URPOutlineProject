using Blur.Runtime.Scripts.Settings;
using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    
    [VolumeComponentMenu("KuanMi/Blur/BokehBlur")]
    public class BokehBlur : BaseBlur<BlurSetting>
    {
        BokehBlur()
        {
            Iteration.max = 256;
        }
    }
}