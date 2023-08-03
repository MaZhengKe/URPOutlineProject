Shader "KuanMi/MaskBlend"
{
    Properties {}

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }


        Pass
        {
            Name "MaskBlend"

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma shader_feature_local _CIRCLE

            #pragma vertex defaultVert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "BlurCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Area;
                float _Spread;
                float3 _MaskColor;

                #ifdef _CIRCLE
                float2 _Center;
                #endif
            CBUFFER_END


            float Mask(float2 uv)
            {
                #ifdef _CIRCLE
                float2 center = uv * 2.0 - 1.0 + _Center; // [0,1] -> [-1,1] 
                return pow(dot(center, center) * _Area, _Spread);
                #else
                
                float y = uv.y * 2 - 1 + + _Offset;
                return pow(abs(y * _Area), _Spread);
                #endif
            }

            float4 frag(DefaultVaryings IN) : SV_Target
            {
                float mask = saturate(Mask(IN.uv));
                half4 bokeh = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv);

                bokeh.rgb *= _MaskColor * mask  ; 

                return float4(bokeh.rgb, mask);
            }
            ENDHLSL
        }

    }
}