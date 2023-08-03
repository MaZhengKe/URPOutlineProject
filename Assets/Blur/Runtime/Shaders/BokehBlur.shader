Shader "KuanMi/BokehBlur"
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
            Name "BokehBlur"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);


            CBUFFER_START(UnityPerMaterial)

            half4 _GoldenRot;
            half4 _Params;

            CBUFFER_END


            #define _Iteration _Params.x
            #define _Radius _Params.y
            #define _PixelSize _Params.zw

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


            half4 BokehBlur(Varyings i)
            {
                half2x2 rot = half2x2(_GoldenRot);
                half4 accumulator = 0.0;
                half4 divisor = 0.0;

                half r = 1.0;
                half2 angle = half2(0.0, _Radius);

                for (int j = 0; j < _Iteration; j++)
                {
                    r += 1.0 / r;
                    angle = mul(rot, angle);
                    half4 bokeh = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(i.uv + _PixelSize * (r - 1.0) * angle));
                    accumulator += bokeh * bokeh;
                    divisor += bokeh;
                }
                return accumulator / divisor;
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
                return BokehBlur(IN);
            }
            ENDHLSL
        }
    }
}