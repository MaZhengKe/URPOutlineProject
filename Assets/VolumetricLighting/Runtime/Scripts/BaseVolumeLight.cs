using System.Collections.Generic;
using UnityEngine;

namespace Other.VolumetricLighting.Scripts
{
    public abstract class BaseVolumeLight : MonoBehaviour
    {
        public static readonly List<BaseVolumeLight> BaseVolumeLightList = new();
        
        public static readonly int SpotAngle = Shader.PropertyToID("_SpotAngle");
        public static readonly int Range = Shader.PropertyToID("_Range");
        public static readonly int Intensity = Shader.PropertyToID("_Intensity");
        public static readonly int MieK = Shader.PropertyToID("_MieK");
        public static readonly int LightIndex = Shader.PropertyToID("_lightIndex");

        public float intensity
        {
            get => m_intensity;
            set
            {
                m_intensity = value;
                matNeedUpdate = true;
            }
        }

        public float mieK
        {
            get => m_mieK;
            set
            {
                m_mieK = value;
                matNeedUpdate = true;
            }
        }

        public int lightIndex
        {
            get => m_lightIndex;
            set
            {
                m_lightIndex = value;
                matNeedUpdate = true;
            }
        }

        [SerializeField]
        private float m_intensity = 1;
        [SerializeField]
        private float m_mieK = 0.8f;
        
        
        private int m_lightIndex = 1;

        [HideInInspector] public Mesh mesh;

        [HideInInspector] public Material material;

        protected bool meshNeedUpdate;
        protected bool matNeedUpdate;
        
        protected float lastRange;
        
        public Light Light;

        public abstract void UpdateIfNeed();
    }
}