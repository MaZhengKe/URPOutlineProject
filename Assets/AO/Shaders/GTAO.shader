Shader "KuanMi/GTAO"
{
    HLSLINCLUDE
        #pragma editor_sync_compilation
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off
        ZTest Always

        // ------------------------------------------------------------------
        // Ambient Occlusion
        // ------------------------------------------------------------------

        // 0 - Occlusion estimation
        Pass
        {
            Name "SSAO_Occlusion"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment GTAO
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            
                #pragma multi_compile_local_fragment _BLUE_NOISE
            
                #pragma multi_compile_local_fragment _SOURCE_DEPTH_NORMALS
                #pragma multi_compile_local_fragment _
                #pragma multi_compile_local_fragment _SAMPLE_COUNT_HIGH

                #include "GTAO.hlsl"
            ENDHLSL
        }
    }
}
