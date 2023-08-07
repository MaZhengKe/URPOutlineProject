using KuanMi.Blur;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blur.Runtime.Scripts
{
    public class GaussianBlurPass : ScriptableRenderPass
    {
        // Internal Variables
        internal string profilerTag;
        
        // Private Variables
        
        private Material m_Material;
        private SetupPassData m_PassData;
        private Texture2D[] m_BlueNoiseTextures;
        
        private GaussianBlurSettings m_CurrentSettings;
        
        private ScriptableRenderer m_Renderer = null;
        
        
        private ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(BlurRendererFeature.ProfileId.GaussianBlur);
        
        // Constants
        private const string k_BlurTextureName = "_BlurTex";
        
        // Statics
        
        private static readonly int s_BlurOffsetID = Shader.PropertyToID("_BlurOffset");
        private static readonly int s_IterationID = Shader.PropertyToID("_Iteration");
        
        private static readonly ShaderPasses[] m_Passes =
        {
            ShaderPasses.GaussianBlurHorizontal,
            ShaderPasses.GaussianBlurVertical
        };
        
        // Enums
        private enum ShaderPasses
        {
            AmbientOcclusion = 0,

            BilateralBlurHorizontal = 1,
            BilateralBlurVertical = 2,
            BilateralBlurFinal = 3,
            BilateralAfterOpaque = 4,

            GaussianBlurHorizontal = 5,
            GaussianBlurVertical = 6,
            GaussianAfterOpaque = 7,

            KawaseBlur = 8,
            KawaseAfterOpaque = 9,
        }

        internal GaussianBlurPass()
        {
            m_CurrentSettings = new GaussianBlurSettings();
            m_PassData = new SetupPassData();
        }
        
        
        internal bool Setup(ref GaussianBlurSettings settings, ref ScriptableRenderer renderer, ref Material material, ref Texture2D[] blueNoiseTextures)
        {
            
            m_BlueNoiseTextures = blueNoiseTextures;
            m_Material = material;
            m_Renderer = renderer;
            m_CurrentSettings = settings;
            
            return m_Material!=null
                && m_CurrentSettings.Intensity > 0.0f
                && m_CurrentSettings.Radius > 0.0f
                && m_CurrentSettings.Iteration > 0;
        }

        private static void SetupKeywordsAndParameters(ref SetupPassData passData, ref RenderingData renderingData)
        {
            passData.material.SetFloat(s_IterationID, passData.settings.Intensity);
        }
        
        
        /*----------------------------------------------------------------------------------------------------------------------------------------
         ------------------------------------------------------------- RENDER-GRAPH --------------------------------------------------------------
         ----------------------------------------------------------------------------------------------------------------------------------------*/

        private class SetupPassData
        {
            internal GaussianBlurSettings settings;
            internal RenderingData renderingData;
            internal TextureHandle cameraColor;
            internal RTHandle blurTexture;
            internal Texture2D[] blueNoiseTextures;
            internal Material material;
        }
        
        private class PassData
        {
            internal int shaderPassID;
            internal Material material;
            internal TextureHandle source;
            internal TextureHandle destination;
        }
        
        private void InitSetupPassData(ref SetupPassData data)
        {
            data.settings = m_CurrentSettings; 
            data.blueNoiseTextures = m_BlueNoiseTextures;
            data.material = m_Material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources,
            ref RenderingData renderingData)
        {
            UniversalRenderer renderer = (UniversalRenderer) renderingData.cameraData.renderer;
            
            CreateRenderTextureHandles(renderGraph,
                ref renderer, 
                ref renderingData,
                out TextureHandle blurTexture);
            
            ExecuteSetupPass(renderGraph, frameResources, ref renderingData);
            
            ExecuteBlurPasses(renderGraph, in blurTexture);
        }

        private static void RenderGraphRenderFunc(PassData data, RasterGraphContext context)
        {
            Blitter.BlitTexture(context.cmd, data.source, Vector2.one, data.material, data.shaderPassID);
        }
        
        private void ExecuteBlurPasses(RenderGraph renderGraph, in TextureHandle blurTexture)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("Blur_HorizontalBlur", out var passData, m_ProfilingSampler))
            {
                passData.source = builder.UseTexture(blurTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.destination = builder.UseTextureFragment(blurTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.material = m_Material;
                passData.shaderPassID = (int) ShaderPasses.BilateralBlurHorizontal;

                // Set up the builder
                builder.SetRenderFunc<PassData>((data, context) => RenderGraphRenderFunc(data, context));

            }
        }

        private void ExecuteSetupPass(RenderGraph renderGraph, FrameResources frameResources,
            ref RenderingData renderingData)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<SetupPassData>("Blur_Setup", out var passData, m_ProfilingSampler))
            {
                // Initialize the pass data
                InitSetupPassData(ref passData);
                passData.renderingData = renderingData;
                UniversalRenderer renderer = (UniversalRenderer) renderingData.cameraData.renderer;
                passData.cameraColor = frameResources.GetTexture(UniversalResource.CameraColor);
                
                builder.SetRenderFunc((SetupPassData data , RasterGraphContext rgContext) =>
                { 
                    SetupKeywordsAndParameters(ref data, ref data.renderingData);
                });
            }
        }

        private void CreateRenderTextureHandles(RenderGraph renderGraph, ref UniversalRenderer renderer, ref RenderingData renderingData, out TextureHandle blurTexture)
        {
            RenderTextureDescriptor blurTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blurTextureDescriptor.depthBufferBits = 0;
            blurTextureDescriptor.msaaSamples = 1;

            int downSampleDivider = m_CurrentSettings.DownSample;
            blurTextureDescriptor.width /= downSampleDivider;
            blurTextureDescriptor.height /= downSampleDivider;
            
            blurTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph,blurTextureDescriptor,"_BlurTex",false,FilterMode.Bilinear,TextureWrapMode.Clamp);
            
            // renderer.resources
        }

    }
}