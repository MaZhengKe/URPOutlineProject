using System;
using UnityEngine;

namespace KuanMi.VolumetricLighting
{
    [ExecuteAlways]
    public class PointVolumeLight : BaseVolumeLight
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            material.EnableKeyword("_POINT_LIGHT");
        }

        protected override void GenMesh()
        {
            UpdateMesh();
        }

        protected override void UpdateMesh()
        {
            lastRange = Light.range;

            meshNeedUpdate = false;
        }

        public override bool LightTypeIsSupported()
        {
            return Light.type == LightType.Point;
        }

        protected override void CheckIfMeshNeedUpdate()
        {
            meshNeedUpdate = Math.Abs(Light.range - lastRange) > float.Epsilon;
        }

        protected override Matrix4x4 GetMatrix()
        {
            return Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one * Light.range * 2);
        }
    }
}