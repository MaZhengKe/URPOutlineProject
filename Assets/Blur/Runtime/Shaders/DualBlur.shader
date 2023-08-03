Shader "KuanMi/DualBlur"
{
    Properties {}


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "BlurCommon.hlsl"
        #include "DualBlur.hlsl"
        ENDHLSL
        
        Pass
        {
            Name "DualBlurUpSample"

            HLSLPROGRAM
            #pragma vertex vert_UpSample
            #pragma fragment frag_UpSample
            ENDHLSL
        }
        Pass
        {
            Name "DualBlurDownSample"

            HLSLPROGRAM
            #pragma vertex vert_DownSample
            #pragma fragment frag_DownSample
            ENDHLSL
        }
    }
}