using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseBlurRendererPassWithVolume<K> : BaseBlurRendererPass where K : BaseBlur
    {
        protected K blurVolume;

        protected MaskBlur maskBlur;

        protected bool isMask => maskBlur.isMask.value;


        protected RTHandle m_MaskTexture;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var stack = VolumeManager.instance.stack;
            blurVolume = stack.GetComponent<K>();
            maskBlur = stack.GetComponent<MaskBlur>();

            descriptor.width /= blurVolume.DownSample.value;
            descriptor.height /= blurVolume.DownSample.value;

            if (isMask)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_MaskTexture, descriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_MaskTex");
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogError("Material is null");
                return;
            }

            var stack = VolumeManager.instance.stack;
            blurVolume = stack.GetComponent<K>();

            maskBlur = stack.GetComponent<MaskBlur>();

            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId)))
            {
                // Debug.Log("ad" + Time.frameCount);
                ExecuteWithCmd(cmd, ref renderingData);


                if (isMask)
                {
                    m_BlendMaterial.SetFloat("_Spread", maskBlur.areaSmooth.value);
                    m_BlendMaterial.SetColor("_MaskColor",maskBlur.maskColor.value);
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

                    Blit(cmd, m_MaskTexture, m_Renderer.cameraColorTargetHandle, m_BlendMaterial);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override bool Setup(ScriptableRenderer renderer)
        {
            if (!base.Setup(renderer))
                return false;

            blurVolume = VolumeManager.instance.stack.GetComponent<K>();
            return blurVolume.IsActive();
        }

        public override void Dispose()
        {
            base.Dispose();
            m_MaskTexture?.Release();
        }
    }
}