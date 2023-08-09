using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AO
{
    public class SSAOPass : ScriptableRenderPass
    {
        protected ScriptableRenderer m_Renderer;
        protected Material material;

        private Texture2D[] blueNoiseTextures;
        protected int blueNoiseTextureIndex = 0;
        
        private SSAOSettings settings;


        protected SSAOFeature.ProfileId ProfileId = SSAOFeature.ProfileId.SSAO;
        
        private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
        private static readonly int s_SSAOBlueNoiseParamsID = Shader.PropertyToID("_SSAOBlueNoiseParams");
        private static readonly int s_BlueNoiseTextureID = Shader.PropertyToID("_BlueNoiseTexture");
        private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
        private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
        private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
        private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");

        
        private static readonly int s_NUM_STEPSID = Shader.PropertyToID("_NUM_STEPS");
        private static readonly int s_NUM_DIRECTIONSID = Shader.PropertyToID("_NUM_DIRECTIONS");
        private static readonly int s_NDotVBiasID = Shader.PropertyToID("_NDotVBias");
        private static readonly int s_RID = Shader.PropertyToID("R");
        private static readonly int s_R2ID = Shader.PropertyToID("R2");
        private static readonly int s_NegInvR2ID = Shader.PropertyToID("_NegInvR2");
        private static readonly int s_RadiusToScreenID = Shader.PropertyToID("_RadiusToScreen");
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            var cmd = CommandBufferPool.Get();

            
            
            Matrix4x4 view = renderingData.cameraData.GetViewMatrix();
            Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix();
            Matrix4x4 cameraViewProjections = proj * view;

            // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
            Matrix4x4 cview = view;
            cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            Matrix4x4 cviewProj = proj * cview;
            Matrix4x4 cviewProjInv = cviewProj.inverse;

            Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
            Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
            Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
            Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
            var cameraTopLeftCorner = topLeftCorner;
            var cameraXExtent = topRightCorner - topLeftCorner;
            var cameraYExtent = bottomLeftCorner - topLeftCorner;
            var cameraZExtent = farCentre;
            
            
            material.SetVector(s_ProjectionParams2ID, new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
            
            material.SetMatrix(s_CameraViewProjectionsID, cameraViewProjections);
            material.SetVector(s_CameraViewTopLeftCornerID, cameraTopLeftCorner);
            material.SetVector(s_CameraViewXExtentID, cameraXExtent);
            material.SetVector(s_CameraViewYExtentID, cameraYExtent);
            material.SetVector(s_CameraViewZExtentID, cameraZExtent);
            
            
            
            
            // Update keywords
            CoreUtils.SetKeyword(material, SSAOFeature.k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);
            CoreUtils.SetKeyword(material, SSAOFeature.k_AOBlueNoiseKeyword, false);
            CoreUtils.SetKeyword(material, SSAOFeature.k_AOInterleavedGradientKeyword, false);
            
            
            CoreUtils.SetKeyword(material, SSAOFeature.k_AOBlueNoiseKeyword, true);
            
            
            blueNoiseTextureIndex = (blueNoiseTextureIndex + 1) % blueNoiseTextures.Length;
            var blurRandomOffsetX = Random.value;
            var blurRandomOffsetY = Random.value;

            Texture2D noiseTexture = blueNoiseTextures[blueNoiseTextureIndex];
            material.SetTexture(s_BlueNoiseTextureID, noiseTexture);

            material.SetVector(s_SSAOParamsID, new Vector4(
                settings.Intensity, // Intensity
                settings.Radius * 1.5f, // Radius
                1.0f, // Downsampling
                settings.Falloff // Falloff
            ));

            material.SetVector(s_SSAOBlueNoiseParamsID, new Vector4(
                1451 / (float)noiseTexture.width, // X Scale
                411 / (float)noiseTexture.height, // Y Scale
                blurRandomOffsetX, // X Offset
                blurRandomOffsetY // Y Offset
            ));
            
            
            
            material.SetFloat(s_NUM_STEPSID, settings.NUM_STEPS);
            material.SetFloat(s_NUM_DIRECTIONSID, settings.NUM_DIRECTIONS);
            material.SetFloat(s_NDotVBiasID, settings.NDotVBias);
            material.SetFloat(s_RID, settings.Radius);
            material.SetFloat(s_R2ID, settings.Radius * settings.Radius);
            material.SetFloat(s_NegInvR2ID, -1.0f / (settings.Radius * settings.Radius));
            material.SetFloat(s_RadiusToScreenID,settings.RadiusToScreen);
            
            
            CoreUtils.SetKeyword(material, SSAOFeature.k_SampleCountLowKeyword, false);
            CoreUtils.SetKeyword(material, SSAOFeature.k_SampleCountMediumKeyword, false);
            CoreUtils.SetKeyword(material, SSAOFeature.k_SampleCountHighKeyword, true);
            
            CoreUtils.SetKeyword(material, SSAOFeature.k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);
            
            
            CoreUtils.SetKeyword(material, SSAOFeature.k_SourceDepthLowKeyword, false);
            CoreUtils.SetKeyword(material, SSAOFeature.k_SourceDepthMediumKeyword, false);
            CoreUtils.SetKeyword(material, SSAOFeature.k_SourceDepthHighKeyword, false);
            CoreUtils.SetKeyword(material, SSAOFeature.k_SourceDepthNormalsKeyword, true);
            

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId)))
            {
                Blitter.BlitTexture(cmd, Texture2D.blackTexture, Vector2.one, material, 0);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
        }

        public void Setup(ScriptableRenderer renderer, Material material, SSAOSettings ssaoSettings, Texture2D[] blueNoise)
        {
            
            this.m_Renderer = renderer;
            this.material = material;
            this.settings = ssaoSettings;
            this.blueNoiseTextures = blueNoise;
            ConfigureInput(ScriptableRenderPassInput.Normal);
            
        }


        public void Dispose()
        {
            
        }
    }
}