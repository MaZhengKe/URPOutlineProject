Shader "KuanMi/RadialBlur"
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
            Name "RadialBlur"

            HLSLPROGRAM
            #pragma vertex defaultVert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BlurCommon.hlsl"


            uniform half4 _Params;

            #define _BlurRadius _Params.x
            #define _Iteration _Params.y
            #define _RadialCenter _Params.zw

            half3 RadialBlur(float2 uv)
            {
                float2 blurVector = (_RadialCenter - uv) * _BlurRadius;


                half3 acumulateColor = half4(0, 0, 0, 0);

                [unroll(30)]
                for (int j = 1; j < _Iteration; j++)
                {

                    float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                    acumulateColor += color ;
                    uv += blurVector;
                }

                return  acumulateColor / _Iteration;
            }

            float4 frag(DefaultVaryings IN) : SV_Target
            {
                return float4(RadialBlur(IN.uv),1);
            }
            ENDHLSL
        }
    }
}