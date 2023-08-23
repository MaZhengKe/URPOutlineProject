using System;
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
        
        public RTHandle DepthPyramidAllTexture;
        public RTHandle DepthPyramidAllTMPTexture;
        
        public int mipCount = 10;

        private PackedMipChainInfo info;
        
        public DepthPyramidPass()
        {
            info = new PackedMipChainInfo();
            info.Allocate();
        }
        
        
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
            
            
            
            info.ComputePackedMipChainInfo(new Vector2Int(descriptor.width, descriptor.height));
            
            var allDescriptor = cameraTargetDescriptor;
            allDescriptor.depthBufferBits = 0;
            allDescriptor.msaaSamples = 1;
            allDescriptor.colorFormat = RenderTextureFormat.RFloat;
            allDescriptor.height = (int)(allDescriptor.height * 1.5);

            RenderingUtils.ReAllocateIfNeeded(ref DepthPyramidTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DepthPyramidTexture");
            RenderingUtils.ReAllocateIfNeeded(ref DepthPyramidTMPTexture, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DepthPyramidTMPTexture");
            
            RenderingUtils.ReAllocateIfNeeded(ref DepthPyramidAllTexture, allDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DepthPyramidAllTexture");
            RenderingUtils.ReAllocateIfNeeded(ref DepthPyramidAllTMPTexture, allDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_DepthPyramidAllTMPTexture");
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
                // cmd.SetRenderTarget(DepthPyramidTMPTexture, 0, CubemapFace.Unknown, -1);
                // Blitter.BlitTexture(cmd, m_Renderer.cameraDepthTargetHandle,new Vector4(1,1,0,0),0,false);
                //
                // cmd.SetRenderTarget(DepthPyramidTexture, 0, CubemapFace.Unknown, -1);
                // Blitter.BlitTexture(cmd, m_Renderer.cameraDepthTargetHandle,new Vector4(1,1,0,0),0,false);
                //
                // for (int i = 1; i < mipCount; i++)
                // {
                //     MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                //     propertyBlock.SetTexture("_TMPCameraDepthTexture", DepthPyramidTMPTexture);
                //     propertyBlock.SetFloat("_DepthMipLevel", i);
                //     
                //     cmd.SetRenderTarget(DepthPyramidTexture, i, CubemapFace.Unknown, -1);
                //     cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
                //
                //     cmd.SetRenderTarget(DepthPyramidTMPTexture, i, CubemapFace.Unknown, -1);
                //     Blitter.BlitTexture(cmd, DepthPyramidTexture,new Vector4(1,1,0,0),i,false);
                //     
                // }
                //
                // cmd.SetGlobalTexture("_DepthPyramidTexture",DepthPyramidTexture);
                //
                
                cmd.SetRenderTarget(DepthPyramidAllTexture, 0, CubemapFace.Unknown, -1);
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetTexture("_TMPCameraDepth", m_Renderer.cameraDepthTargetHandle);
                cmd.DrawProcedural(Matrix4x4.identity, m_Material, 1, MeshTopology.Triangles, 3, 1, propertyBlock);
                
                
                cmd.SetRenderTarget(DepthPyramidAllTMPTexture, 0, CubemapFace.Unknown, -1);
                cmd.DrawProcedural(Matrix4x4.identity, m_Material, 1, MeshTopology.Triangles, 3, 1, propertyBlock);
                
                
                Vector4 m_SrcOffset = new Vector4();
                Vector4 m_DstOffset = new Vector4();

                for (int i = 1; i < info.mipLevelCount; i++)
                {
                    
                    Vector2Int dstSize = info.mipLevelSizes[i];
                    Vector2Int dstOffset = info.mipLevelOffsets[i];
                    Vector2Int srcSize = info.mipLevelSizes[i - 1];
                    Vector2Int srcOffset = info.mipLevelOffsets[i - 1];
                    Vector2Int srcLimit = srcOffset + srcSize - Vector2Int.one;
                    
                    
                    m_SrcOffset[0] = srcOffset.x;
                    m_SrcOffset[1] = srcOffset.y;
                    m_SrcOffset[2] = srcLimit.x;
                    m_SrcOffset[3] = srcLimit.y;

                    m_DstOffset[0] = dstOffset.x;
                    m_DstOffset[1] = dstOffset.y;
                    m_DstOffset[2] = dstSize.x;
                    m_DstOffset[3] = dstSize.y;

                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    block.SetVector("_SrcOffsetAndLimit",m_SrcOffset);
                    block.SetVector("_DstOffset",m_DstOffset);
                    block.SetTexture("_TMPCameraDepth", DepthPyramidAllTMPTexture);
                    
                    cmd.SetRenderTarget(DepthPyramidAllTexture, 0, CubemapFace.Unknown, -1);
                    cmd.SetViewport(new Rect(dstOffset.x, dstOffset.y, dstSize.x, dstSize.y));
                    cmd.DrawProcedural(Matrix4x4.identity, m_Material, 2, MeshTopology.Triangles, 3, 1, block);
                    
                    cmd.SetRenderTarget(DepthPyramidAllTMPTexture, 0, CubemapFace.Unknown, -1);
                    
                    Blitter.BlitTexture(cmd, DepthPyramidAllTexture,new Vector4(1,1,0,0),i,false);
                }

                cmd.SetGlobalTexture("_DepthPyramidTexture",DepthPyramidAllTexture);

                CoreUtils.SetRenderTarget(cmd,m_Renderer.cameraColorTargetHandle);

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
        }
        
        public struct PackedMipChainInfo
        {
            public Vector2Int textureSize;
            public int mipLevelCount;
            public Vector2Int[] mipLevelSizes;
            public Vector2Int[] mipLevelOffsets;

            private Vector2 cachedTextureScale;
            private Vector2Int cachedHardwareTextureSize;

            private bool m_OffsetBufferWillNeedUpdate;

            public void Allocate()
            {
                mipLevelOffsets = new Vector2Int[15];
                mipLevelSizes = new Vector2Int[15];
                m_OffsetBufferWillNeedUpdate = true;
            }

            // We pack all MIP levels into the top MIP level to avoid the Pow2 MIP chain restriction.
            // We compute the required size iteratively.
            // This function is NOT fast, but it is illustrative, and can be optimized later.
            public void ComputePackedMipChainInfo(Vector2Int viewportSize)
            {
                bool isHardwareDrsOn = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
                Vector2Int hardwareTextureSize = isHardwareDrsOn ? DynamicResolutionHandler.instance.ApplyScalesOnSize(viewportSize) : viewportSize;
                Vector2 textureScale = isHardwareDrsOn ? new Vector2((float)viewportSize.x / (float)hardwareTextureSize.x, (float)viewportSize.y / (float)hardwareTextureSize.y) : new Vector2(1.0f, 1.0f);

                // No work needed.
                if (cachedHardwareTextureSize == hardwareTextureSize && cachedTextureScale == textureScale)
                    return;

                cachedHardwareTextureSize = hardwareTextureSize;
                cachedTextureScale = textureScale;

                mipLevelSizes[0] = hardwareTextureSize;
                mipLevelOffsets[0] = Vector2Int.zero;

                int mipLevel = 0;
                Vector2Int mipSize = hardwareTextureSize;

                do
                {
                    mipLevel++;

                    // Round up.
                    mipSize.x = Math.Max(1, (mipSize.x + 1) >> 1);
                    mipSize.y = Math.Max(1, (mipSize.y + 1) >> 1);

                    mipLevelSizes[mipLevel] = mipSize;

                    Vector2Int prevMipBegin = mipLevelOffsets[mipLevel - 1];
                    Vector2Int prevMipEnd = prevMipBegin + mipLevelSizes[mipLevel - 1];

                    Vector2Int mipBegin = new Vector2Int();

                    if ((mipLevel & 1) != 0) // Odd
                    {
                        mipBegin.x = prevMipBegin.x;
                        mipBegin.y = prevMipEnd.y;
                    }
                    else // Even
                    {
                        mipBegin.x = prevMipEnd.x;
                        mipBegin.y = prevMipBegin.y;
                    }

                    mipLevelOffsets[mipLevel] = mipBegin;

                    hardwareTextureSize.x = Math.Max(hardwareTextureSize.x, mipBegin.x + mipSize.x);
                    hardwareTextureSize.y = Math.Max(hardwareTextureSize.y, mipBegin.y + mipSize.y);
                }
                while ((mipSize.x > 1) || (mipSize.y > 1));

                textureSize = new Vector2Int(
                    (int)Mathf.Ceil((float)hardwareTextureSize.x * textureScale.x), (int)Mathf.Ceil((float)hardwareTextureSize.y * textureScale.y));

                mipLevelCount = mipLevel + 1;
                m_OffsetBufferWillNeedUpdate = true;
            }

        }
         
    }
}