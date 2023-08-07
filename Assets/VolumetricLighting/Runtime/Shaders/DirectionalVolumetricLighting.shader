Shader "KuanMi/DirectionalVolumetricLighting"
{
    Properties
    {
        _Intensity("_Intensity",Range(0.0,1.0)) = 1.0
        _MieK("_MieK",Range(-1.0,1.0)) = 1.0
        _NumSteps("NumSteps",float) = 15

        _BlueNoise("BlueNoise",2D) = "white"
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {

            Name "DrawProcedural"


            Cull Off
            // 附加
            Blend one one
            //            Blend Off
            ZTest Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define MAIN_LIGHT_CALCULATE_SHADOWS
            #define _ADDITIONAL_LIGHT_SHADOWS
            // #define _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            // #define PI 3.14159265359f
            #define MieScattering(cosAngle, g) g.w * (g.x / (pow(g.y - g.z * cosAngle, 1.5)))
            #define MieScattering2(cosAngle, k) (1-k*k)/(4*PI*(1+k*cosAngle)*(1+k*cosAngle))


            struct Attributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord0 : TEXCOORD0;
            };

            TEXTURE2D(_BlueNoise);
            SAMPLER(sampler_BlueNoise);

            CBUFFER_START(UnityPerMaterial)
            float _Intensity;
            float _MieK;
            float _NumSteps;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                output.texCoord0 = output.positionCS.xy * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                output.texCoord0.y = 1 - output.texCoord0.y;
                #endif
                return output;
            }


            float random(float value)
            {
                return  frac(sin(value) * 43758.5453);
            }

            float noise(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BlueNoise, sampler_BlueNoise, uv).r;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                
                // int index = random(_Time) * 10;
                //
                // return float4(index, index, index, 1.0);
                #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(IN.texCoord0);
                #else
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(IN.texCoord0));
                #endif
                float3 worldPos = ComputeWorldSpacePosition(IN.texCoord0, depth, UNITY_MATRIX_I_VP);

                float3 rayOrigin = _WorldSpaceCameraPos;

                float3 ray = worldPos - rayOrigin;

                float2 screenUV = (IN.positionCS.xy) * 0.015625;

                float noisev = (noise(screenUV));
                float3 rayDir = (worldPos - rayOrigin) / _NumSteps;

                rayOrigin += rayDir * noisev;
                float n = length(rayDir);

                float density = 0;

                float cosAngle = dot(-_MainLightPosition.xyz, normalize(ray));

                UNITY_LOOP
                for (int i = 1; i < _NumSteps; i++)
                {
                    float3 pos = rayOrigin + rayDir * i;
                    float4 shadowCoord = TransformWorldToShadowCoord(pos);
                    float light = MainLightRealtimeShadow(shadowCoord);
                    light *= MieScattering2(cosAngle, _MieK);
                    light *= n;
                    density += light;
                }

                // return float4(1, 1, 1, 1.0);
                density = 1 - exp(-density);
                density = saturate(density);
                density *= _Intensity;

                return half4(density, density, density, 1.0);
            }
            ENDHLSL
        }
    }
}