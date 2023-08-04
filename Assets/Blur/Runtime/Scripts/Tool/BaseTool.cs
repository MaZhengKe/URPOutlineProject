using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseTool
    {
        public abstract string ShaderName { get; }
        public Material m_Material { get; protected set; }
        
        protected static readonly int BlurRadius = Shader.PropertyToID("_Offset");
        protected static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");
        protected static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        protected static readonly int BlitTextureSt = Shader.PropertyToID("_BlitTexture_ST");
        internal static readonly int GoldenRot = Shader.PropertyToID("_GoldenRot");
        internal static readonly int Params = Shader.PropertyToID("_Params");
        
        protected ScriptableRenderPass _renderPass;
        
        public BaseTool(ScriptableRenderPass renderPass)
        {
            _renderPass = renderPass;
            
            m_Material = CoreUtils.CreateEngineMaterial(Shader.Find(ShaderName));
        }
        public abstract void OnCameraSetup(RenderTextureDescriptor descriptor);
        public abstract void Execute(CommandBuffer cmd, RTHandle source, RTHandle target);
        
        public virtual void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }
        
        protected void Blit(CommandBuffer cmd, RTHandle source, RTHandle target)
        {
            _renderPass.Blit(cmd, source, target);
        }
    }
}