using UnityEngine;
using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    [VolumeComponentMenu("KuanMi/Blur/RadialBlur")]
    public class RadialBlur: BaseBlur
    {
        public Vector2Parameter RadialCenter = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        RadialBlur()
        {
            BlurRadius.max = 0.1f;
        }
    }
}