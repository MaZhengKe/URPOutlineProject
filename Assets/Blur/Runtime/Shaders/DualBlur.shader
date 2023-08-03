Shader "KuanMi/DualBlur"
{
    Properties{}
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "DualBlurUpSample"

            HLSLPROGRAM
            #pragma vertex vert_UpSample
            #pragma fragment frag_UpSample

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "DualBlur.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DualBlurDownSample"

            HLSLPROGRAM
            #pragma vertex vert_DownSample
            #pragma fragment frag_DownSample

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "DualBlur.hlsl"
            ENDHLSL
        }
    }
}