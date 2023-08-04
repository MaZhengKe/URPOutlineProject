Shader "KuanMi/GrainyBlur"
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
            Name "GrainyBlur"

            HLSLPROGRAM
            #pragma vertex defaultVert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "BlurCommon.hlsl"

            float Rand(float2 n)
            {
                return sin(dot(n, half2(1233.224, 1743.335)));
            }


            half4 GrainyBlur(DefaultVaryings i)
            {
                half2 randomOffset = float2(0.0, 0.0);
                half4 finalColor = half4(0.0, 0.0, 0.0, 0.0);
                float random = Rand(i.uv);

                for (int k = 0; k < int(_Iteration); k++)
                {
                    random = frac(43758.5453 * random + 0.61432);;
                    randomOffset.x = (random - 0.5) * 2.0;
                    random = frac(43758.5453 * random + 0.61432);
                    randomOffset.y = (random - 0.5) * 2.0;

                    finalColor += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                                                   half2(i.uv + randomOffset * _Offset));
                }
                return finalColor / _Iteration;
            }

            float4 frag(DefaultVaryings IN) : SV_Target
            {
                return GrainyBlur(IN);
            }
            ENDHLSL
        }
    }
}