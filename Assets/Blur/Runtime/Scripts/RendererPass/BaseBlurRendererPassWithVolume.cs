using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KuanMi.Blur
{
    public abstract class BaseBlurRendererPassWithVolume<K> : BaseBlurRendererPass where K : BaseBlur
    {
        protected K blurVolume;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var stack = VolumeManager.instance.stack;
            blurVolume = stack.GetComponent<K>();

            descriptor.width /= blurVolume.DownSample.value;
            descriptor.height /= blurVolume.DownSample.value;
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

            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId)))
            {
                // Debug.Log("ad" + Time.frameCount);
                ExecuteWithCmd(cmd, ref renderingData);
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
    }

}