using System;
using UnityEngine;

namespace Other.VolumetricLighting.Scripts
{
    [ExecuteAlways]
    public class SpotVolumeLight : BaseVolumeLight
    {
        [HideInInspector] public int num = 4;

        private float lastSpotAngle;

        private Vector3[] GetVertices()
        {
            var vertices = new Vector3[num + 2];
            float halfAngle = Light.spotAngle * 0.5f * Mathf.Deg2Rad;

            float qAngle = halfAngle * 0.5f;

            float r = Light.range / Mathf.Cos(qAngle);

            float h = r * Mathf.Cos(halfAngle);
            float v = Mathf.Sin(halfAngle) * r;

            v /= Mathf.Cos(Mathf.PI * 2 / (num * 2));

            vertices[0] = Vector3.zero;

            for (int i = 0; i < num; i++)
            {
                vertices[i + 1] = new Vector3(Mathf.Cos(i * Mathf.PI * 2 / num) * v,
                    Mathf.Sin(i * Mathf.PI * 2 / num) * v, h);
            }

            vertices[num + 1] = new Vector3(0, 0, r);

            return vertices;
        }

        private void AddTriangle(ref int[] triangles, ref int index, int a, int b, int c)
        {
            triangles[index++] = a;
            triangles[index++] = b;
            triangles[index++] = c;
        }

        private int[] GetTriangles()
        {
            var triangles = new int[num * 2 * 3];
            int index = 0;

            for (int i = 0; i < num; i++)
            {
                AddTriangle(ref triangles, ref index, num + 1, i + 1, (i + 1) % num + 1);
                AddTriangle(ref triangles, ref index, 0, (i + 1) % num + 1, i + 1);
            }

            return triangles;
        }

        protected override void GenMesh()
        {
            mesh = new Mesh
            {
                name = "SpotLightMesh",
            };
            UpdateMesh();
        }

        protected override void UpdateMesh()
        {
            mesh.Clear();
            mesh.vertices = GetVertices();
            mesh.triangles = GetTriangles();

            lastRange = Light.range;
            lastSpotAngle = Light.spotAngle;

            meshNeedUpdate = false;
        }

        public override bool LightTypeIsSupported()
        {
            return Light.type == LightType.Spot;
        }


        protected override void CheckIfMeshNeedUpdate()
        {
            meshNeedUpdate = Math.Abs(Light.range - lastRange) > float.Epsilon ||
                             Math.Abs(Light.spotAngle - lastSpotAngle) > float.Epsilon;
        }
    }
}