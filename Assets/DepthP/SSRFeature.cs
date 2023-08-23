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

        
        public Texture2D[] blueNoise16RGBTex;
        [Reload("Textures/OwenScrambledNoise4.png")]
        public Texture2D owenScrambledRGBATex;
        [Reload("Textures/OwenScrambledNoise256.png")]
        public Texture2D owenScrambled256Tex;
        [Reload("Textures/ScrambleNoise.png")]
        public Texture2D scramblingTex;
        [Reload("Textures/RankingTile1SPP.png")]
        public Texture2D rankingTile1SPP;
        [Reload("Textures/ScramblingTile1SPP.png")]
        public Texture2D scramblingTile1SPP;
        [Reload("Textures/RankingTile8SPP.png")]
        public Texture2D rankingTile8SPP;
        [Reload("Textures/ScramblingTile8SPP.png")]
        public Texture2D scramblingTile8SPP;
        [Reload("Textures/RankingTile256SPP.png")]
        public Texture2D rankingTile256SPP;
        [Reload("Textures/ScramblingTile256SPP.png")]
        public Texture2D scramblingTile256SPP;

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
            m_SSRPass.Setup(renderer, DepthPyramidMaterial,this);
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
        public RTHandle SsrAccumPrevTexture;
        public RTHandle SsrLightingTexture;
        
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
            
            RenderingUtils.ReAllocateIfNeeded(ref SsrAccumPrevTexture, accumDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_SsrAccumPrevTexture");
            
            RenderingUtils.ReAllocateIfNeeded(ref SsrLightingTexture, accumDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_SsrLightingTexture");
            
        }

        SSRFeature ssrFeature;
        public bool Setup(ScriptableRenderer renderer, Material material, SSRFeature ssrFeature)
        {
            m_Renderer = renderer;
            m_Material = material;
            this.ssrFeature = ssrFeature;
            return true;
        }
        
        public void Dispose()
        {
            SsrHitPointTexture?.Release();
        }

        internal void BindDitheredRNGData1SPP(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture("_OwenScrambledTexture", ssrFeature.owenScrambled256Tex);
            cmd.SetGlobalTexture("_ScramblingTileXSPP", ssrFeature.scramblingTile1SPP);
            cmd.SetGlobalTexture("_RankingTileXSPP", ssrFeature.rankingTile1SPP);
            cmd.SetGlobalTexture("_ScramblingTexture", ssrFeature.scramblingTex);
        }
        
        int FrameCount = 0;
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            FrameCount++;
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                float thickness = 0.01f;
                float accumulationFactor = 0.75f;
                float n = renderingData.cameraData.camera.nearClipPlane;
                float f = renderingData.cameraData.camera.farClipPlane;
                
                float _SsrThicknessScale = 1.0f / (1.0f + thickness);
                float _SsrThicknessBias = -n / (f - n) * (thickness * _SsrThicknessScale);
                
                
                float _SsrAccumulationAmount = Mathf.Pow(2, Mathf.Lerp(0.0f, -7.0f, accumulationFactor));
                
                m_Material.SetFloat("_SsrThicknessScale",_SsrThicknessScale);
                m_Material.SetFloat("_SsrThicknessBias", _SsrThicknessBias);
                m_Material.SetFloat("_SsrPBRBias", 0.5f);
                m_Material.SetInt("_FrameCount", FrameCount);
                m_Material.SetFloat("_SsrAccumulationAmount", _SsrAccumulationAmount);
                
                BindDitheredRNGData1SPP(cmd);
                
                
                CoreUtils.SetRenderTarget(cmd,SsrAccumPrevTexture);
                Blitter.BlitTexture(cmd, SsrLightingTexture,new Vector4(1,1,0,0),0,false);
                cmd.SetGlobalTexture("_SsrAccumPrev",SsrAccumPrevTexture);

                CoreUtils.DrawFullScreen(cmd, m_Material, SsrHitPointTexture);
                cmd.SetGlobalTexture("_SsrHitPointTexture",SsrHitPointTexture);

                CoreUtils.DrawFullScreen(cmd, m_Material, SsrAccumTexture,null,1);
                cmd.SetGlobalTexture("_SSRAccumTexture",SsrAccumTexture);
                
                CoreUtils.DrawFullScreen(cmd, m_Material, SsrLightingTexture,null,2);
                cmd.SetGlobalTexture("_SsrLightingTexture",SsrLightingTexture);

                
                CoreUtils.SetRenderTarget(cmd,m_Renderer.cameraColorTargetHandle);
                Blitter.BlitTexture(cmd, SsrLightingTexture,new Vector4(1,1,0,0),0,false);
                
                
                
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
        }
    }
}