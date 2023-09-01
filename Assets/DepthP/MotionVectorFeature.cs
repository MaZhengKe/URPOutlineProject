using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DepthP
{
    public class MotionVectorFeature : ScriptableRendererFeature
    {
        private MotionVectorPass m_MotionVectorPass;
        
        [Reload("Shaders/MotionVector.shader")]
        public Shader MotionVectorShader;
        
        public Material motionVectorMaterial;
        
        
        public enum ProfileId
        {
            MotionVector
        }
        public override void Create()
        {
            
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/DepthP/");
#endif
            
            if(m_MotionVectorPass == null)
                m_MotionVectorPass = new MotionVectorPass()
                {
                    renderPassEvent =  RenderPassEvent.BeforeRenderingOpaques - 2
                };
            
            if(motionVectorMaterial == null)
                motionVectorMaterial = CoreUtils.CreateEngineMaterial(MotionVectorShader);
            
            
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            
            m_MotionVectorPass.Setup(renderer, motionVectorMaterial);
            renderer.EnqueuePass(m_MotionVectorPass);
        }
        
        
        public void OnDestroy()
        {
            m_MotionVectorPass.Dispose();
        }
    }
    
    public class MotionVectorPass : ScriptableRenderPass
    {
        
        private readonly ProfilingSampler m_ProfilingSampler =
            ProfilingSampler.Get(MotionVectorFeature.ProfileId.MotionVector);
        
        protected ScriptableRenderer m_Renderer;
        
        private Material m_Material;
        
        private Type cameraDataType;
        
        public RTHandle MotionVectorTexture;
        public MotionVectorPass()
        {
            Assembly reloadAssembly = typeof(CameraData).Assembly;
            cameraDataType = reloadAssembly.GetType("UnityEngine.Rendering.Universal.CameraData");
        }
        
        
        public bool Setup(ScriptableRenderer renderer, Material material)
        {
            m_Renderer = renderer;
            m_Material = material;
            return true;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            var descriptor = cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref MotionVectorTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_MotionVectorTexture");

            
        }

        public void Dispose()
        {
            
        }
        protected Matrix4x4 m_PreviousViewProjection;
        protected Matrix4x4 m_ViewProjection;
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                
                m_Material.SetMatrix("_PrevViewProjMatrix", m_PreviousViewProjection);
                m_Material.SetMatrix("_NonJitteredViewProjMatrix", m_ViewProjection);
                
                cmd.SetRenderTarget(MotionVectorTexture);
                cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Triangles, 3, 1, null);

                cmd.SetGlobalTexture("_MotionVectorTexture", MotionVectorTexture);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
            var cameraData = renderingData.cameraData;
            
            MethodInfo methodInfo = cameraDataType.GetMethod("GetProjectionMatrixNoJitter", BindingFlags.Instance | BindingFlags.NonPublic);
            Matrix4x4 p = (Matrix4x4)methodInfo.Invoke(cameraData, new object[] {0});

            
            var gpuVP = GL.GetGPUProjectionMatrix(p, true) * cameraData.GetViewMatrix(0);
            // Debug.Log(gpuVP);
            m_PreviousViewProjection =  m_ViewProjection;
            m_ViewProjection = gpuVP;
            
            
            
        }
    }
}