Shader "KuanMi/Blit"
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
            Name "BlitAdd"
            
            Blend One One

            HLSLPROGRAM
            #pragma vertex defaultVert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct DefaultAttributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
            };

            struct DefaultVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(BlurCommon)
            float4 _BlitTexture_ST;
            float4 _BlitTexture_TexelSize;
            CBUFFER_END

            DefaultVaryings defaultVert(DefaultAttributes IN)
            {
                DefaultVaryings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                output.uv = output.positionCS.xy * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                output.uv.y = 1 - output.uv.y;
                #endif
                return output;
            }

            float4 frag(DefaultVaryings IN) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv);
                return color;
            }
            ENDHLSL
        }
    }
}