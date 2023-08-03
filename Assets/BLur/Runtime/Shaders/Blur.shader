Shader "KuanMi/Blur"
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

            Name "GaussianBlur"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);


            CBUFFER_START(UnityPerMaterial)

            half4 _BlurOffset;

            CBUFFER_END

            struct Attributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 uv01: TEXCOORD1;
                float4 uv23: TEXCOORD2;
                float4 uv45: TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                output.uv = output.positionCS.xy * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                output.uv.y = 1 - output.uv.y;
                #endif

                output.uv01 = output.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1);
                output.uv23 = output.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1) * 2.0;
                output.uv45 = output.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1) * 6.0;

                return output;
            }


            float4 frag(Varyings IN) : SV_Target
            {
                half4 color = float4(0, 0, 0, 0);

                color += 0.40 * SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv);
                color += 0.15 * SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv01.xy);
                color += 0.15 * SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv01.zw);
                color += 0.10 * SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv23.xy);
                color += 0.10 * SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv23.zw);
                color += 0.05 * SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv45.xy);
                color += 0.05 * SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv45.zw);

                return color;
            }
            ENDHLSL

        }
        Pass
        {
            Name "KawaseBlur"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);


            CBUFFER_START(UnityPerMaterial)

            float4 _BlitTexture_TexelSize;
            uniform half _Offset;

            CBUFFER_END

            struct Attributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

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

            Varyings vert(Attributes IN)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                output.uv = output.positionCS.xy * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                output.uv.y = 1 - output.uv.y;
                #endif
                return output;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return KawaseBlur(
                    TEXTURE2D_ARGS(_BlitTexture, sampler_BlitTexture), IN.uv.xy, _BlitTexture_TexelSize.xy, _Offset);
            }
            ENDHLSL
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