using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DepthP
{
    public class SSRFeature : ScriptableRendererFeature
    {
        private SSRPass m_SSRPass;
        
        [Reload("Shaders/SSR.shader")]
        public Shader DepthPyramidShader;
        
        public Material DepthPyramidMaterial;


        public enum ProfileId
        {
            Tracing
        }
        
        public override void Create()
        {
            
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/DepthP/");
#endif
            
            if (m_SSRPass == null)
            {
                m_SSRPass = new SSRPass()
                {
                    // renderPassEvent = RenderPassEvent.BeforeRenderingOpaques - 1
                    renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
                };
            }
            if(DepthPyramidMaterial == null)
                DepthPyramidMaterial = CoreUtils.CreateEngineMaterial(DepthPyramidShader);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_SSRPass.Setup(renderer, DepthPyramidMaterial);
            renderer.EnqueuePass(m_SSRPass);
            
        }

        public void OnDestroy()
        {
            m_SSRPass.Dispose();
        }
    }
    
    
    public class SSRPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler m_ProfilingSampler =
            ProfilingSampler.Get(SSRFeature.ProfileId.Tracing);
        
        
        protected ScriptableRenderer m_Renderer;
        
        private Material m_Material;
        
        public RTHandle SsrHitPointTexture;
        public RTHandle SsrAccumTexture;
        
        public int mipCount = 10;
        
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            var descriptor = cameraTargetDescriptor;

            descriptor = cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.colorFormat = RenderTextureFormat.RG32;

            RenderingUtils.ReAllocateIfNeeded(ref SsrHitPointTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_SsrHitPointTexture");
            
            var accumDescriptor = cameraTargetDescriptor;
            accumDescriptor = cameraTargetDescriptor;
            accumDescriptor.depthBufferBits = 0;
            accumDescriptor.msaaSamples = 1;
            accumDescriptor.colorFormat = RenderTextureFormat.ARGBFloat;
            
            

            RenderingUtils.ReAllocateIfNeeded(ref SsrAccumTexture, accumDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_SsrAccumTexture");
            
        }

        public bool Setup(ScriptableRenderer renderer, Material material)
        {
            m_Renderer = renderer;
            m_Material = material;
            return true;
        }
        
        public void Dispose()
        {
            SsrHitPointTexture?.Release();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                float thickness = 0.011f;
                float n = renderingData.cameraData.camera.nearClipPlane;
                float f = renderingData.cameraData.camera.farClipPlane;
                
                float _SsrThicknessScale = 1.0f / (1.0f + thickness);
                float _SsrThicknessBias = -n / (f - n) * (thickness * _SsrThicknessScale);
                
                m_Material.SetFloat("_SsrThicknessScale",_SsrThicknessScale);
                m_Material.SetFloat("_SsrThicknessBias", _SsrThicknessBias);
                
                
                CoreUtils.DrawFullScreen(cmd, m_Material, SsrHitPointTexture);
                cmd.SetGlobalTexture("_SsrHitPointTexture",SsrHitPointTexture);
                
                
                CoreUtils.DrawFullScreen(cmd, m_Material, SsrAccumTexture,null,1);
                cmd.SetGlobalTexture("_SsrAccumTexture",SsrAccumTexture);


                CoreUtils.SetRenderTarget(cmd,m_Renderer.cameraColorTargetHandle);
                Blitter.BlitTexture(cmd, SsrAccumTexture,new Vector4(1,1,0,0),0,false);
                
                
                
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
        }
    }
}