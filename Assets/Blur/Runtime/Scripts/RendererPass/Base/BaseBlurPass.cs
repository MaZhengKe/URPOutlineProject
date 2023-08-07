using System;
using Blur.Runtime.Scripts.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseBlurPass<T, K> : BaseBlurRendererPassWithMask<K> where T : BaseTool<K> where K : BlurSetting
    {
        private readonly T tool;
        protected override BlurRendererFeature.ProfileId ProfileId => tool.ProfileId;

        protected BaseBlurPass()
        {
            blurSetting = (K)Activator.CreateInstance(typeof(K));
            tool = (T)Activator.CreateInstance(typeof(T), this, blurSetting);
        }

        public override bool Setup(ScriptableRenderer renderer, Material material, K featureSettings,
            Material blendMaterial)
        {
            tool.m_Material = material;
            tool.setting = featureSettings;
            return base.Setup(renderer, material, featureSettings,blendMaterial);
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
            tool.Execute(cmd, m_Renderer.cameraColorTargetHandle,
                isMask ? m_MaskTexture : renderToTexture ? m_TargetTexture : m_Renderer.cameraColorTargetHandle);
        }
    }
}