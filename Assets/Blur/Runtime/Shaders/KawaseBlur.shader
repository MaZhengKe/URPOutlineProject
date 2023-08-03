Shader "KuanMi/KawaseBlur"
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
            Name "KawaseBlur"

            HLSLPROGRAM
            #pragma vertex defaultVert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "BlurCommon.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            half4 KawaseBlur(TEXTURE2D_PARAM(tex, samplerTex), float2 uv, float2 texelSize, half pixelOffset)
            {
                half4 o = 0;
                o += SAMPLE_TEXTURE2D(tex, samplerTex, uv + float2(pixelOffset +0.5, pixelOffset +0.5) * texelSize);
                o += SAMPLE_TEXTURE2D(tex, samplerTex, uv + float2(-pixelOffset -0.5, pixelOffset +0.5) * texelSize);
                o += SAMPLE_TEXTURE2D(tex, samplerTex, uv + float2(-pixelOffset -0.5, -pixelOffset -0.5) * texelSize);
                o += SAMPLE_TEXTURE2D(tex, samplerTex, uv + float2(pixelOffset +0.5, -pixelOffset -0.5) * texelSize);
                return o * 0.25;
            }

            float4 frag(DefaultVaryings IN) : SV_Target
            {
                return KawaseBlur(
                    TEXTURE2D_ARGS(_BlitTexture, sampler_BlitTexture), IN.uv.xy, _BlitTexture_TexelSize.xy, _Offset);
            }
            ENDHLSL
        }
    }
}