using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace KuanMi.VolumetricLighting
{
    public abstract class BaseVolumeLight : MonoBehaviour
    {
        public static readonly List<BaseVolumeLight> BaseVolumeLightList = new();

        private readonly string shaderName = "KuanMi/SpotVolumetricLighting";

        private static readonly int SpotAngleID = Shader.PropertyToID("_SpotAngle");
        private static readonly int RangeID = Shader.PropertyToID("_Range");
        private static readonly int MaxIntensityID = Shader.PropertyToID("_MaxIntensity");
        private static readonly int MinIntensityID = Shader.PropertyToID("_MinIntensity");
        private static readonly int MieKID = Shader.PropertyToID("_MieK");
        private static readonly int LightIndexID = Shader.PropertyToID("_lightIndex");
        private static readonly int NumStepsID = Shader.PropertyToID("_NumSteps");
        private static readonly int Para01ID = Shader.PropertyToID("_Para01");

        public Color MaxIntensity
        {
            get => maxIntensity;
            set
            {
                maxIntensity =  value;
                matNeedUpdate = true;
            }
        }
        
        public float DistanceAttenuation
        {
            get => distanceAttenuation;
            set
            {
                distanceAttenuation = value;
                matNeedUpdate = true;
            }
        }
        
        public float ShadowAttenuation
        {
            get => shadowAttenuation;
            set
            {
                shadowAttenuation = value;
                matNeedUpdate = true;
            }
        }

        public float MieK
        {
            get => mieK;
            set
            {
                mieK = value;
                matNeedUpdate = true;
            }
        }

        public float NumSteps
        {
            get => numSteps;
            set
            {
                numSteps = value;
                matNeedUpdate = true;
            }
        }

        public float Evenness
        {
            get => evenness;
            set
            {
                evenness = Mathf.Max(0, value);
                matNeedUpdate = true;
            }
        }

        public int LightIndex
        {
            get => _lightIndex;
            set
            {
                _lightIndex = value;
                matNeedUpdate = true;
            }
        }

        [SerializeField] private Color maxIntensity = Color.white;
        [SerializeField] private float distanceAttenuation = 1;
        [SerializeField] private float shadowAttenuation;
        [SerializeField] private float mieK = 0.8f;
        [SerializeField] private float numSteps = 3;
        [SerializeField] private float evenness = 1;


        private int _lightIndex = 1;

        [HideInInspector] public Mesh mesh;

        [HideInInspector] public Material material;

        public Matrix4x4 Matrix => GetMatrix();

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
            material.SetFloat(SpotAngleID, Light.spotAngle);
            material.SetFloat(RangeID, Light.range);
            material.SetColor(MaxIntensityID, MaxIntensity);
            material.SetFloat(MieKID, MieK);
            material.SetInt(LightIndexID, LightIndex);
            material.SetFloat(NumStepsID, NumSteps);
            material.SetVector(Para01ID, new Vector4(Evenness, distanceAttenuation, shadowAttenuation, 0));

            matNeedUpdate = false;
        }

        public abstract bool LightTypeIsSupported();
    }
}