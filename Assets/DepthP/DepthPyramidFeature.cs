using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DepthP
{
    public class DepthPyramidFeature : ScriptableRendererFeature
    {
        private DepthPyramidPass m_DepthPyramidPass;
        
        [Reload("Shaders/DepthPyramid.shader")]
        public Shader DepthPyramidShader;
        
        public Material DepthPyramidMaterial;

        public enum ProfileId
        {
            DepthPyramid
        }
        
        public override void Create()
        {
            
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/DepthP/");
#endif
            
            if (m_DepthPyramidPass == null)
            {
                m_DepthPyramidPass = new DepthPyramidPass()
                {
                    renderPassEvent = RenderPassEvent.BeforeRenderingOpaques - 1
                };
            }
            if(DepthPyramidMaterial == null)
                DepthPyramidMaterial = CoreUtils.CreateEngineMaterial(DepthPyramidShader);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_DepthPyramidPass.Setup(renderer, DepthPyramidMaterial);
            renderer.EnqueuePass(m_DepthPyramidPass);
        }
        
        public void OnDestroy()
        {
            m_DepthPyramidPass.Dispose();
        }
    }
    
    public class DepthPyramidPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler =
            ProfilingSampler.Get(DepthPyramidFeature.ProfileId.DepthPyramid);
        
        
        protected ScriptableRenderer m_Renderer;
        
        private Material m_Material;
        
        public RTHandle DepthPyramidTexture;
        public RTHandle DepthPyramidTMPTexture;
        
        public int mipCount = 10;
        
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            var descriptor = cameraTargetDescriptor;

            descriptor = cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.mipCount = mipCount;
            descriptor.useMipMap = true;
            descriptor.autoGenerateMips = false;
            descriptor.colorFormat = RenderTextureFormat.RFloat;

            RenderingUtils.ReAllocateIfNeeded(ref DepthPyramidTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DepthPyramidTexture");
            RenderingUtils.ReAllocateIfNeeded(ref DepthPyramidTMPTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DepthPyramidTMPTexture");
        }
        
        
        public bool Setup(ScriptableRenderer renderer, Material material)
        {
            m_Renderer = renderer;
            m_Material = material;
            return true;
        }
        
        public void Dispose()
        {
            DepthPyramidTexture?.Release();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.SetRenderTarget(DepthPyramidTMPTexture, 0, CubemapFace.Unknown, -1);
                Blitter.BlitTexture(cmd, m_Renderer.cameraDepthTargetHandle,new Vector4(1,1,0,0),0,false);

                cmd.SetRenderTarget(DepthPyramidTexture, 0, CubemapFace.Unknown, -1);
                Blitter.BlitTexture(cmd, m_Renderer.cameraDepthTargetHandle,new Vector4(1,1,0,0),0,false);

                for (int i = 1; i < mipCount; i++)
                {
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    propertyBlock.SetTexture("_TMPCameraDepthTexture", DepthPyramidTMPTexture);
                    propertyBlock.SetFloat("_DepthMipLevel", i);
                    
                    cmd.SetRenderTarget(DepthPyramidTexture, i, CubemapFace.Unknown, -1);
                    cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
                    
                    cmd.SetRenderTarget(DepthPyramidTMPTexture, i, CubemapFace.Unknown, -1);
                    Blitter.BlitTexture(cmd, DepthPyramidTexture,new Vector4(1,1,0,0),i,false);
                    
                }
                
                cmd.SetGlobalTexture("_DepthPyramidTexture",DepthPyramidTexture);
                
                CoreUtils.SetRenderTarget(cmd,m_Renderer.cameraColorTargetHandle);

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
        }
    }
}