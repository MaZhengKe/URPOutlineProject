using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseBlurPass<T, K> : BaseBlurRendererPassWithVolume<K>
        where T : BaseTool<K> where K : BaseBlur
    {
        public T tool;
        protected override BlurRendererFeature.ProfileId ProfileId => tool.ProfileId;


        public BaseBlurPass()
        {
            tool = (T)Activator.CreateInstance(typeof(T), this);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            UpdateTool();
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
                isMask ? m_MaskTexture :  renderToTexture? m_TargetTexture: m_Renderer.cameraColorTargetHandle);
        }

        public virtual void UpdateTool()
        {
            if (m_Renderer != null && m_Renderer.cameraColorTargetHandle != null &&
                m_Renderer.cameraColorTargetHandle.rt != null)
                tool.UpdateTool(m_Renderer.cameraColorTargetHandle.rt.width,
                    m_Renderer.cameraColorTargetHandle.rt.height);
        }
    }
}