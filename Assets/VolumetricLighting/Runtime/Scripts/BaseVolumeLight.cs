using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Other.VolumetricLighting.Scripts
{
    public abstract class BaseVolumeLight : MonoBehaviour
    {
        public static readonly List<BaseVolumeLight> BaseVolumeLightList = new();
        
        private readonly string shaderName = "KuanMi/SpotVolumetricLighting";

        private static readonly int SpotAngle = Shader.PropertyToID("_SpotAngle");
        private static readonly int Range = Shader.PropertyToID("_Range");
        private static readonly int Intensity = Shader.PropertyToID("_Intensity");
        private static readonly int MieK = Shader.PropertyToID("_MieK");
        private static readonly int LightIndex = Shader.PropertyToID("_lightIndex");

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

        public Matrix4x4 matrix => GetMatrix();

        protected virtual Matrix4x4 GetMatrix()
        {
            return transform.localToWorldMatrix;
        }

        protected bool meshNeedUpdate;
        protected bool matNeedUpdate;
        
        protected float lastRange;
        
        public Light Light;
        
        
        protected virtual void OnEnable()
        {
            Light = GetComponent<Light>();
            if (Light == null)
            {
                Debug.LogError("Light is null, please add a light component");
                return;
            }

            if (!LightTypeIsSupported())
            {
                Debug.LogError("Light type is not supported");
                return;
            }
            material = CoreUtils.CreateEngineMaterial(Shader.Find(shaderName));
            GenMesh();
            BaseVolumeLightList.Add(this);
        }

        private void OnDisable()
        {
            BaseVolumeLightList.Remove(this);
            DestroyImmediate(material);
            material = null;
        }
        
        protected abstract void GenMesh();

        public virtual void UpdateIfNeed()
        {
            
            if (matNeedUpdate)
                UpdateMaterial();
            CheckIfMeshNeedUpdate();
            if (meshNeedUpdate)
                UpdateMesh();
        }

        protected abstract void CheckIfMeshNeedUpdate();
        protected abstract void UpdateMesh();


        private void UpdateMaterial()
        {
            material.SetFloat(SpotAngle, Light.spotAngle);
            material.SetFloat(Range, Light.range);
            material.SetFloat(Intensity, intensity);
            material.SetFloat(MieK, mieK);
            material.SetInt(LightIndex, lightIndex);

            matNeedUpdate = false;
        }

        public abstract bool LightTypeIsSupported();

    }
}