using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AO
{
    public class GTAOPass : ScriptableRenderPass
    {
        protected ScriptableRenderer m_Renderer;
        protected Material material;

        private GTAOSettings settings;


        protected GTAOFeature.ProfileId ProfileId = GTAOFeature.ProfileId.GTAO;
        
        
        public struct ShaderVariablesAmbientOcclusion
        {
            public Vector4 _AOBufferSize;
            public Vector4 _AOParams0;
            public Vector4 _AOParams1;
            public Vector4 _AOParams2;
            public Vector4 _AOParams3;
            public Vector4 _AOParams4;
            public Vector4 _FirstTwoDepthMipOffsets;
            public Vector4 _AODepthToViewParams;
        }

        public struct RenderAOParameters
        {
            public Vector2 runningRes;
            public int viewCount;
            public bool fullResolution;
            public bool runAsync;
            public bool temporalAccumulation;
            public bool bilateralUpsample;

            public ShaderVariablesAmbientOcclusion cb;
        }

        public RenderAOParameters SetPara(float width, float height,Camera camera)
        {

            
            var parameters = new RenderAOParameters();
            ref var cb = ref parameters.cb;
            
            parameters.fullResolution = true;
            
            parameters.runningRes = new Vector2(width, height);
            Debug.Log(parameters.runningRes);
            cb._AOBufferSize = new Vector4(width, height, 1.0f / width, 1.0f / height);

            parameters.temporalAccumulation = false;
            
            // ??
            parameters.viewCount = 0;
            parameters.runAsync = false;
            
            // HDRP中使用的GPU矩阵
            float invHalfTanFOV = camera.projectionMatrix[1, 1];
            float aspectRatio = parameters.runningRes.y / parameters.runningRes.x;
            uint frameCount = 0;
            
            cb._AOParams0 = new Vector4(
                parameters.fullResolution ? 0.0f : 1.0f,
                parameters.runningRes.y * invHalfTanFOV * 0.25f,
                settings.radius,
                settings.stepCount
            );
            
            cb._AOParams1 = new Vector4(
                settings.intensity,
                1.0f / (settings.radius * settings.radius),
                (frameCount / 6) % 4,
                (frameCount % 6)
            );

            Debug.Log(invHalfTanFOV);
            
            cb._AODepthToViewParams = new Vector4(
                2.0f / (invHalfTanFOV * aspectRatio * parameters.runningRes.x),
                2.0f / (invHalfTanFOV * parameters.runningRes.y),
                1.0f / (invHalfTanFOV * aspectRatio),
                1.0f / invHalfTanFOV
            );
            
            float scaleFactor = (parameters.runningRes.x * parameters.runningRes.y) / (540.0f * 960.0f);
            float radInPixels = Mathf.Max(16, settings.maximumRadiusInPixels * Mathf.Sqrt(scaleFactor));
            
            
            cb._AOParams2 = new Vector4(
                width,
                height,
                1.0f / (settings.stepCount + 1.0f),
                radInPixels
            );

            
            float stepSize = 1;
            
            
            float blurTolerance = 1.0f;
            float maxBlurTolerance = 0.25f;
            float minBlurTolerance = -2.5f;
            blurTolerance = minBlurTolerance + (blurTolerance * (maxBlurTolerance - minBlurTolerance));
            
            float bTolerance = 1f - Mathf.Pow(10f, blurTolerance) * stepSize;
            bTolerance *= bTolerance;
            const float upsampleTolerance = -7.0f; // TODO: Expose?
            float uTolerance = Mathf.Pow(10f, upsampleTolerance);
            float noiseFilterWeight = 1f / (Mathf.Pow(10f, 0.0f) + uTolerance);

            cb._AOParams3 = new Vector4(
                bTolerance,
                uTolerance,
                noiseFilterWeight,
                stepSize
            );
            
            
            float upperNudgeFactor = 1.0f ;
            const float maxUpperNudgeLimit = 5.0f;
            const float minUpperNudgeLimit = 0.25f;
            upperNudgeFactor = minUpperNudgeLimit + (upperNudgeFactor * (maxUpperNudgeLimit - minUpperNudgeLimit));
            cb._AOParams4 = new Vector4(
                settings.directionCount,
                upperNudgeFactor,
                minUpperNudgeLimit,
                15.0f
            );
            
            return  parameters;
        }
        
        
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            var cmd = CommandBufferPool.Get();

            var para =SetPara( renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height, renderingData.cameraData.camera);
            
            material.SetFloat("_AORadius", settings.radius);
            Debug.Log(settings.radius);
            material.SetInt("_AOStepCount", settings.stepCount);
            material.SetFloat("_AODirectionCount", settings.directionCount);
            material.SetFloat("_AOInvStepCountPlusOne", 1.0f / (settings.stepCount + 1.0f));
            Debug.Log( 1.0f / (settings.stepCount + 1.0f));
            
            material.SetInt("_AOMaxRadiusInPixels", settings.maximumRadiusInPixels);
            
            material.SetFloat("_AOFOVCorrection", para.cb._AOParams0.y);
            material.SetVector("_AODepthToViewParams", para.cb._AODepthToViewParams);

            float frameCount = Time.frameCount;
            material.SetFloat("_AOTemporalOffsetIdx",(frameCount / 6) % 4);
            material.SetFloat("_AOTemporalRotationIdx",(frameCount / 6));

            Debug.Log(para.cb._AOParams0.y);
            Debug.Log(para.cb._AODepthToViewParams);
            
            
            

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId)))
            {
                Blitter.BlitTexture(cmd, Texture2D.blackTexture, Vector2.one, material, 0);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
        }

        public void Setup(ScriptableRenderer renderer, Material material, GTAOSettings GTAOSettings)
        {
            
            this.m_Renderer = renderer;
            this.material = material;
            this.settings = GTAOSettings;
            ConfigureInput(ScriptableRenderPassInput.Normal);
            
        }


        public void Dispose()
        {
            
        }
    }
}