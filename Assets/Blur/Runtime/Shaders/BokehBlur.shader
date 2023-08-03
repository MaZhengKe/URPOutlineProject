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
            #pragma vertex defaultVert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "BlurCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _GoldenRot;
            half4 _Params;
            CBUFFER_END

            #define _Iteration _Params.x
            #define _Radius _Params.y
            #define _PixelSize _Params.zw


            half4 BokehBlur(DefaultVaryings i)
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

            float4 frag(DefaultVaryings IN) : SV_Target
            {
                return BokehBlur(IN);
            }
            ENDHLSL
        }
    }
}