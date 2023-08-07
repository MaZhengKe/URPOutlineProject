using System;
using Blur.Runtime.Scripts.Settings;
using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    public abstract class BaseBlur<T>: VolumeComponent, IPostProcessComponent where T : BlurSetting
    {
        public ClampedFloatParameter BlurRadius = new ClampedFloatParameter(0f, 0f, 10f);
        public ClampedIntParameter Iteration = new ClampedIntParameter(0, 0, 10);
        public ClampedIntParameter DownSample = new ClampedIntParameter(1, 1, 10);
        
        public bool IsActive()
        {
            return BlurRadius.value > 0f && Iteration.value > 0;
        }
        
        public T GetSetting()
        {
            var setting = (T)Activator.CreateInstance(typeof(T));
            
            setting.BlurRadius = BlurRadius.value;
            setting.Iteration = Iteration.value;
            setting.DownSample = DownSample.value;

            return setting;
        }
    }
}