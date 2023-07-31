using UnityEngine;

namespace KuanMi
{
    public class Outline : MonoBehaviour
    {
        [SerializeField] private bool show;

        private Renderer _renderer;
        private Renderer[] _rendererList;

        public RenderingLayerMask renderLayer;

        public bool containSub;

        public void Show()
        {
            show = true;
            SetLayer();
        }

        public void Hide()
        {
            show = false;
            SetLayer();
        }

        private void OnValidate()
        {
            SetLayer();
        }

        private void SetLayer()
        {
            if (containSub)
            {
                _rendererList ??= GetComponentsInChildren<Renderer>();
                foreach (var renderer1 in _rendererList)
                {
                    SetLayer(renderer1);
                }
            }
            else
            {
                if (_renderer == null)
                    _renderer = GetComponent<Renderer>();
                if (_renderer != null)
                    SetLayer(_renderer);
            }
        }

        private void SetLayer(Renderer renderer)
        {
            if (show)
                renderer.renderingLayerMask |= renderLayer;
            else
                renderer.renderingLayerMask &= ~renderLayer;
        }
    }

    public static class OutlineExtension
    {
        
        
        public static void ShowOutline(this Transform transform, bool containSub = true)
        {
            ShowOutline(transform.gameObject, containSub);
        }

        public static void ShowOutline(this GameObject go, bool containSub = true, string mask = "Outline")
        {
            ShowOutline(go, containSub, RenderingLayerMask.GetMask(mask));
        }

        public static void ShowOutline(this GameObject go, bool containSub, RenderingLayerMask mask)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null)
            {
                outline = go.AddComponent<Outline>();
            }

            outline.containSub = containSub;
            outline.renderLayer = mask;
            outline.Show();
        }

        public static void HideOutline(this GameObject go, bool containSub = true)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null)
            {
                outline = go.AddComponent<Outline>();
            }

            outline.containSub = containSub;
            outline.Hide();
        }
    }
}