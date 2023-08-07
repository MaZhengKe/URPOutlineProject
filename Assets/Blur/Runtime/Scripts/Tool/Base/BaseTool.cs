using Blur.Runtime.Scripts.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseTool<K> where K : BlurSetting
    {
        public abstract BlurRendererFeature.ProfileId ProfileId { get; }
        public Material m_Material { get;  set; }
        
        protected static readonly int BlurRadiusID = Shader.PropertyToID("_Offset");
        protected static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");
        protected static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        protected static readonly int BlitTextureSt = Shader.PropertyToID("_BlitTexture_ST");
        internal static readonly int GoldenRot = Shader.PropertyToID("_GoldenRot");
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int IterationID = Shader.PropertyToID("_Iteration");
        internal static readonly int BlueNoiseID = Shader.PropertyToID("_BlueNoise");
        internal static readonly int TimeSpeedID = Shader.PropertyToID("_TimeSpeed");
        
        protected ScriptableRenderPass _renderPass;
        
        public K setting;
        
        
        public BaseTool(ScriptableRenderPass renderPass, K setting)
        {
            _renderPass = renderPass;
            this.setting = setting;
            
        }
        public abstract void OnCameraSetup(RenderTextureDescriptor descriptor);
        public abstract void Execute(CommandBuffer cmd, RTHandle source, RTHandle target);
        
        public virtual void Dispose()
        {
        }
        
        protected void Blit(CommandBuffer cmd, RTHandle source, RTHandle target)
        {
            _renderPass.Blit(cmd, source, target);
        }
        protected void Blit(CommandBuffer cmd, RTHandle source, RTHandle target,Material material)
        {
            _renderPass.Blit(cmd, source, target,material);
        }
    }
}