using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace KuanMi.Blur
{
    [VolumeComponentMenu("KuanMi/Blur/Blur Mask")]
    public class MaskBlur : VolumeComponent, IPostProcessComponent
    {
        public enum MaskType
        {
            Rectangle,
            Circle,
        }

        public BoolParameter isMask = new BoolParameter(false);
        public EnumParameter<MaskType> maskType = new EnumParameter<MaskType>(MaskType.Rectangle);
        
        public ClampedFloatParameter areaSmooth = new ClampedFloatParameter(1, 1, 20);
        public ColorParameter maskColor = new ColorParameter(Color.white);

        public ClampedFloatParameter areaSize = new ClampedFloatParameter(1, 0, 20);
        public ClampedFloatParameter offset = new ClampedFloatParameter(0, -1, 1);
        
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0f, 0f));
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.5f, 0, 10);

        public bool IsActive()
        {
            return isMask.value;
        }

        public MaskBlur()
        {
            displayName = "Blur Mask";
        }
    }
}