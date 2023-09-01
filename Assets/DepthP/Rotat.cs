using System;
using UnityEngine;

namespace DepthP
{
    [ExecuteAlways]
    public class Rotat : MonoBehaviour
    {
        public float speed = 10f;
        private void Update()
        {
            transform.Rotate(Vector3.up, speed);
        }
    }
}