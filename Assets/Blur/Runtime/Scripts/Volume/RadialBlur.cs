using Blur.Runtime.Scripts.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    public class RadialSetting : BlurSetting
    {
        public Vector2 RadialCenter = new Vector2(0.5f, 0.5f);
        
    }
    [VolumeComponentMenu("KuanMi/Blur/RadialBlur")]
    public class RadialBlur: BaseBlur<RadialSetting>
    {
        public Vector2Parameter RadialCenter = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        RadialBlur()
        {
            BlurRadius.max = 0.1f;
        }
    }
}