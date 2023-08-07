using System;
using Blur.Runtime.Scripts.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    [Serializable]
    public class GaussianSetting : BlurSetting
    {
    }

    [CreateAssetMenu(fileName = "GaussianBlur", menuName = "KuanMi/Blur/GaussianBlur")]
    [VolumeComponentMenu("KuanMi/Blur/GaussianBlur")]
    public class GaussianBlur : BaseBlur<GaussianSetting>
    {
    }
}