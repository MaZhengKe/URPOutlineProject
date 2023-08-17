using Blur.Runtime.Scripts.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseBlurRendererPassWithMask<K> : BaseBlurRendererPass where K : BlurSetting
    {
        protected K blurSetting;

        protected MaskBlur maskBlur;

        protected Material m_BlendMaterial;

        protected bool isMask => maskBlur.isMask.value;

        public bool renderToTexture;
        public string TargetTextureName;

        protected RTHandle m_MaskTexture;
        protected RTHandle m_TargetTexture;


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var stack = VolumeManager.instance.stack;
            maskBlur = stack.GetComponent<MaskBlur>();
            if (blurSetting != null)
            {
                descriptor.width /= blurSetting.DownSample;
                descriptor.height /= blurSetting.DownSample;
            }

            if (isMask)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_MaskTexture, descriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_MaskTex");
            }

            if (renderToTexture)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_TargetTexture, descriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: TargetTextureName);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlendMaterial == null)
            {
                Debug.LogError("Material is null");
                return;
            }

            var stack = VolumeManager.instance.stack;

            maskBlur = stack.GetComponent<MaskBlur>();

            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId)))
            {
                // Debug.Log("ad" + Time.frameCount);
                ExecuteWithCmd(cmd, ref renderingData);


                if (isMask)
                {
                    m_BlendMaterial.SetFloat("_Spread", maskBlur.areaSmooth.value);
                    m_BlendMaterial.SetColor("_MaskColor", maskBlur.maskColor.value);
                    if (maskBlur.maskType.value == MaskBlur.MaskType.Circle)
                    {
                        m_BlendMaterial.EnableKeyword("_CIRCLE");
                        m_BlendMaterial.SetVector("_Center", maskBlur.center.value);
                        m_BlendMaterial.SetFloat("_Area", maskBlur.radius.value);
                    }
                    else
                    {
                        m_BlendMaterial.DisableKeyword("_CIRCLE");
                        m_BlendMaterial.SetFloat("_Area", maskBlur.areaSize.value);
                        m_BlendMaterial.SetFloat("_Offset", maskBlur.offset.value);
                    }


                    if (renderToTexture)
                    {
                        Blit(cmd, m_MaskTexture, m_TargetTexture, m_BlendMaterial);
                        cmd.SetGlobalTexture(TargetTextureName, m_TargetTexture);
                    }
                    else

                        Blit(cmd, m_MaskTexture, m_Renderer.cameraColorTargetHandle, m_BlendMaterial);
                }

                CoreUtils.SetRenderTarget(cmd, m_Renderer.cameraColorTargetHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public virtual bool Setup(ScriptableRenderer renderer, Material material, K featureSettings,
            Material blendMaterial)
        {
            blurSetting = featureSettings;
            m_BlendMaterial = blendMaterial;

            if (!base.Setup(renderer))
                return false;
            return blurSetting.Iteration > 0 && blurSetting.BlurRadius > 0;
        }

        public virtual void Dispose()
        {
            m_MaskTexture?.Release();
        }
    }
}