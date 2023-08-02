using System;
using System.Collections.Generic;
using UnityEngine;

namespace Other.VolumetricLighting.Scripts
{
    [ExecuteAlways]
    public class SpotVolumeLight : MonoBehaviour
    {
        public static readonly List<SpotVolumeLight> spotHelpers = new();
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

        [HideInInspector] public int num = 4;

        [HideInInspector] public Mesh mesh;

        [HideInInspector] public Material material;

        private readonly string shaderName = "KuanMi/SpotVolumetricLighting";
        public Light spotLight;

        private float lastSpotAngle;
        private float lastRange;
        
        private bool meshNeedUpdate;
        private bool matNeedUpdate;

        private void OnEnable()
        {
            spotLight = GetComponent<Light>();
            material = new Material(Shader.Find(shaderName));
            GenMesh();
            spotHelpers.Add(this);
        }

        private void OnDisable()
        {
            spotHelpers.Remove(this);

            DestroyImmediate(material);
            material = null;
        }

        private Vector3[] GetVertices()
        {
            var vertices = new Vector3[num + 2];
            float halfAngle = spotLight.spotAngle * 0.5f * Mathf.Deg2Rad;

            float qAngle = halfAngle * 0.5f;

            float r = spotLight.range / Mathf.Cos(qAngle);

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

        private void GenMesh()
        {
            mesh = new Mesh
            {
                name = "SpotLightMesh",
            };
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            mesh.Clear();
            mesh.vertices = GetVertices();
            mesh.triangles = GetTriangles();

            lastRange = spotLight.range;
            lastSpotAngle = spotLight.spotAngle;
            
            meshNeedUpdate = false;
        }

        private void UpdateMaterial()
        {
            material.SetFloat(SpotAngle, spotLight.spotAngle);
            material.SetFloat(Range, spotLight.range);
            material.SetFloat(Intensity, intensity);
            material.SetFloat(MieK, mieK);
            material.SetInt(LightIndex, lightIndex);
            
            // material.SetInt(LightIndex, spotLight);

            matNeedUpdate = false;
        }

        public void UpdateIfNeed()
        {
            if (matNeedUpdate)
                UpdateMaterial();
            CheckIfMeshNeedUpdate();
            if (meshNeedUpdate)
                UpdateMesh();
        }

        private void CheckIfMeshNeedUpdate()
        {
            meshNeedUpdate = Math.Abs(spotLight.range - lastRange) > float.Epsilon ||
                             Math.Abs(spotLight.spotAngle - lastSpotAngle) > float.Epsilon;
        }
    }
}