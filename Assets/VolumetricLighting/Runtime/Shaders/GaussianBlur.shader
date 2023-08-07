Shader "KuanMi/GaussianBlur"
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
            Name "GaussianBlur"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "BlurCommon.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 uv01: TEXCOORD1;
                float4 uv23: TEXCOORD2;
                float4 uv45: TEXCOORD3;
            };

            Varyings vert(DefaultAttributes IN)
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
    }
}