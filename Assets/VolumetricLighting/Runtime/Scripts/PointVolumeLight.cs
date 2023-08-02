using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Other.VolumetricLighting.Scripts
{
    [ExecuteAlways]
    public class PointVolumeLight : BaseVolumeLight
    {
        private readonly string shaderName = "KuanMi/PointVolumetricLighting";

        private void OnEnable()
        {
            Light = GetComponent<Light>();
            material = new Material(Shader.Find(shaderName));
            GenMesh();
            BaseVolumeLightList.Add(this);
        }

        private void OnDisable()
        {
            BaseVolumeLightList.Remove(this);
            DestroyImmediate(material);
            material = null;
        }

        private void GenMesh()
        {
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            lastRange = Light.range;
            
            meshNeedUpdate = false;
        }

        private void UpdateMaterial()
        {
            material.SetFloat(Range, Light.range);
            material.SetFloat(Intensity, intensity);
            material.SetFloat(MieK, mieK);
            material.SetInt(LightIndex, lightIndex);
            
            // material.SetInt(LightIndex, spotLight);

            matNeedUpdate = false;
        }

        public override void UpdateIfNeed()
        {
            if (matNeedUpdate)
                UpdateMaterial();
            CheckIfMeshNeedUpdate();
            if (meshNeedUpdate)
                UpdateMesh();
        }

        private void CheckIfMeshNeedUpdate()
        {
            meshNeedUpdate = Math.Abs(Light.range - lastRange) > float.Epsilon;
        }
    }
}