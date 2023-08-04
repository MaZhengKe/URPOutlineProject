using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public class GaussianBlurRendererPass : BaseBlurPass<GaussianBlurTool,GaussianBlur>
    {
        protected override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.GaussianBlur;
        
        
        protected override string ShaderName => "KuanMi/GaussianBlur";

        public override void UpdateTool(){
            
            tool.blurRadius = blurVolume.BlurRadius.value;
            tool.iteration = blurVolume.Iteration.value;

            tool.width = m_Renderer.cameraColorTargetHandle.rt.width;
            tool.height = m_Renderer.cameraColorTargetHandle.rt.height;
        }
    }
}