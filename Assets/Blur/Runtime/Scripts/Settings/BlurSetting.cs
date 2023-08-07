using System;

namespace Blur.Runtime.Scripts.Settings
{
    [Serializable]
    public class BlurSetting
    {
        public float BlurRadius = 0;
        public int Iteration = 0;
        public int DownSample = 1;
        
    }
}