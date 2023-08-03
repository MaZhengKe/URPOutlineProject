using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    [VolumeComponentMenu("KuanMi/Blur/BokehBlur")]
    public class BokehBlur : BaseBlur
    {
        BokehBlur()
        {
            Iteration.max = 256;
        }
    }
}