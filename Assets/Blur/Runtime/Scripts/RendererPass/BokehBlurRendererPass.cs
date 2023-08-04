namespace KuanMi.Blur
{
    public class BokehBlurRendererPass : BaseBlurPass<BokehBlurTool,BokehBlur>
    {
        protected override BlurRendererFeature.ProfileId ProfileId => BlurRendererFeature.ProfileId.BokehBlur;

        protected override string ShaderName => "KuanMi/BokehBlur";

        public override void UpdateTool()
        {
            base.UpdateTool();
            tool.Iteration = blurVolume.Iteration.value;
            tool.BlurRadius = blurVolume.BlurRadius.value;
        }
    }
}