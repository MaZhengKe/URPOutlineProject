using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace KuanMi
{
    [System.Serializable]
    public struct RenderingLayerMask
    {
        [SerializeField]
        private uint m_Mask;

        public static implicit operator uint(RenderingLayerMask mask) => mask.m_Mask;

        public static implicit operator RenderingLayerMask(uint uintVal)
        {
            RenderingLayerMask renderingLayerMask;
            renderingLayerMask.m_Mask = uintVal;
            return renderingLayerMask;
        }
        
        public static RenderingLayerMask GetMask(params string[] layerNames)
        {
            if (layerNames == null)
                throw new ArgumentNullException(nameof (layerNames));
            uint mask = 0;
            foreach (string layerName in layerNames)
            {
                int layer = RenderingLayerMask.NameToLayer(layerName);
                if (layer != -1)
                    mask |= (uint)(1 << layer);
            }
            return mask;
        }

        private static int NameToLayer(string layerName)
        {
            var options = GraphicsSettings.defaultRenderPipeline.renderingLayerMaskNames;
            for (var i = 0; i < options.Length; i++)
            {
                if (options[i] == layerName)
                {
                    return i;
                }
            }

            return -1;
        }


        public uint Value
        {
            get => this.m_Mask;
            set => this.m_Mask = value;
        }
    }
}