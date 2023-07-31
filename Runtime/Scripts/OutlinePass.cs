using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi
{
    internal class OutlinePass : ScriptableRenderPass
    {
        readonly List<ShaderTagId> _shaderTagIds = new();
        private readonly FilteringSettings _filteringSettings;

        private RenderTextureDescriptor m_MaskDescriptor;
        private RTHandle m_MaskTexture;
        private static readonly int s_MaskTextureID = Shader.PropertyToID("_MaskTex");

        private readonly ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(OutlineFeature.OutlineProfileId.Outline);

        private ScriptableRenderer m_Renderer;
        private Material m_Material;

        private static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int SamplePrecisionId = Shader.PropertyToID("_SamplePrecision");

        private readonly OutlineFeature outlineFeature;
        private static readonly int ZTest = Shader.PropertyToID("_ZTest");

        public OutlinePass(RenderQueueRange renderQueueRange, LayerMask layerMask, uint renderingLayerMask, OutlineFeature outlineFeature)
        {
            this.outlineFeature = outlineFeature;
            _filteringSettings = new FilteringSettings(renderQueueRange, layerMask,renderingLayerMask);

            _shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
            _shaderTagIds.Add(new ShaderTagId("LightweightForward"));

            // m_MaskTexture = RTHandles.Alloc(ComputeRTHandleSize,depthBufferBits:0,colorFormat: GraphicsFormat.R8_UNorm, name:"_MaskTex");
        }
        
        Vector2Int ComputeRTHandleSize(Vector2Int screenSize)
        {
            return screenSize;
        }

        public bool Setup(ScriptableRenderer renderer, Material material)
        {
            m_Material = material;
            m_Renderer = renderer;
            return true;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            RenderTextureDescriptor descriptor = cameraTargetDescriptor;

            m_MaskDescriptor = descriptor;
            m_MaskDescriptor.colorFormat = RenderTextureFormat.R8;
            m_MaskDescriptor.depthBufferBits = 0;
            // m_MaskDescriptor.msaaSamples = 4;

            RenderingUtils.ReAllocateIfNeeded(ref m_MaskTexture, m_MaskDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_MaskTex");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogErrorFormat(
                    "{0}.Execute(): Missing material. OutlineFeaturePass pass will not execute. Check for missing reference in the renderer resources.",
                    GetType().Name);
                return;
            }

            m_Material.SetFloat(ZTest, outlineFeature.show ? 8 : 4);
            m_Material.SetFloat(WidthId, outlineFeature.width);
            m_Material.SetInteger(SamplePrecisionId, outlineFeature.samplePrecision);
            m_Material.SetColor(ColorId, outlineFeature.color);


            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(_shaderTagIds, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                ref var cameraData = ref renderingData.cameraData;
                var camera = cameraData.camera;
                if (cameraData.xrRendering)
                    context.StartMultiEye(camera);

                drawSettings.overrideMaterial = m_Material;

                // 切换
                CoreUtils.SetRenderTarget(cmd, m_MaskTexture, m_Renderer.cameraDepthTargetHandle, ClearFlag.Color, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // 绘制遮罩
                var listParams = new RendererListParams(renderingData.cullResults, drawSettings, _filteringSettings);
                var rendererList = context.CreateRendererList(ref listParams);
                cmd.DrawRendererList(rendererList);
                
                cmd.SetGlobalTexture(s_MaskTextureID, m_MaskTexture);
                // 绘制轮廓线
                CoreUtils.SetRenderTarget(cmd, m_Renderer.cameraColorTargetHandle);
                CoreUtils.DrawFullScreen(cmd, m_Material,null,1);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
            m_MaskTexture?.Release();
        }
    }
}