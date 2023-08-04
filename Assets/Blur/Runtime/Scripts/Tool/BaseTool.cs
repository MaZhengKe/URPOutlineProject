using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseTool
    {
        public abstract string ShaderName { get; }
        public Material Material { get; protected set; }
        
        internal static readonly int GoldenRot = Shader.PropertyToID("_GoldenRot");
        internal static readonly int Params = Shader.PropertyToID("_Params");
        
        protected ScriptableRenderPass _renderPass;
        
        public BaseTool(ScriptableRenderPass renderPass)
        {
            _renderPass = renderPass;
            
            Material = CoreUtils.CreateEngineMaterial(Shader.Find(ShaderName));
        }
        public abstract void OnCameraSetup(RenderTextureDescriptor descriptor);
        public abstract void Execute(CommandBuffer cmd, RTHandle source, RTHandle target);
        
        public virtual void Dispose()
        {
            CoreUtils.Destroy(Material);
        }
    }
}