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
            #pragma shader_feature _BLUE_NOISE

            #pragma vertex defaultVert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BlurCommon.hlsl"

            TEXTURE2D_ARRAY(_BlueNoise);
            SAMPLER(sampler_BlueNoise);
            


            #if _BLUE_NOISE
            float random(float value)
            {
                return  frac(sin(value) * 43758.5453);
            }
            
            float Rand(float2 n)
            {
                int index = random(_Time * _TimeSpeed) * 64;
                return SAMPLE_TEXTURE2D_ARRAY(_BlueNoise, sampler_BlueNoise, n* _ScreenSize.xy/64,index).r;
            }
            #else
            float Rand(float2 n)
            {
                
                return sin(dot(n, half2(1233.224 + _Time.x * _TimeSpeed, 1743.335)));
            }

            #endif


            half4 GrainyBlur(float2 uv)
            {
                half2 randomOffset = float2(0.0, 0.0);
                half4 finalColor = half4(0.0, 0.0, 0.0, 0.0);
                float random = Rand(uv);

                for (int k = 0; k < int(_Iteration); k++)
                {
                    random = frac(43758.5453 * random + 0.61432);
                    randomOffset.x = (random - 0.5) * 2.0;
                    random = frac(43758.5453 * random + 0.61432);
                    randomOffset.y = (random - 0.5) * 2.0;

                    finalColor += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                                                   half2(uv + randomOffset * _Offset));
                }
                return finalColor / _Iteration;
            }

            float4 frag(DefaultVaryings IN) : SV_Target
            {
                // float r = Rand(IN.uv* _ScreenSize.xy/64);
                // return float4(r,r,r, 1.0);
                return GrainyBlur(IN.uv);
            }
            ENDHLSL
        }
    }
}