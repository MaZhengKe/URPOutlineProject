using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseBlurPass<T, K> : BaseBlurRendererPassWithVolume<K> where T : BaseTool where K : BaseBlur
    {
        protected T tool;

        public BaseBlurPass()
        {
            tool = (T)Activator.CreateInstance(typeof(T), this);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            tool.OnCameraSetup(descriptor);
        }

        public override void Dispose()
        {
            base.Dispose();
            tool.Dispose();
        }

        public override void ExecuteWithCmd(CommandBuffer cmd, ref RenderingData renderingData)
        {
            UpdateTool();
            tool.Execute(cmd, m_Renderer.cameraColorTargetHandle,
                isMask ? m_MaskTexture : m_Renderer.cameraColorTargetHandle);
        }

        public virtual void UpdateTool()
        {
        }
    }
}