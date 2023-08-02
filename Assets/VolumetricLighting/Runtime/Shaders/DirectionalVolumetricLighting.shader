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

            float noise(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BlueNoise, sampler_BlueNoise,uv).r;   
                // return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float3 noise3(float2 uv)
            {
                return float3(noise(uv), noise(uv + float2(1, 0)), noise(uv + float2(0, 1)));
            }


            Light GetAdditionalLight2(uint i, float3 positionWS, half4 shadowMask)
            {
                int lightIndex = (i);

                Light light = GetAdditionalPerObjectLight(lightIndex, positionWS);

                #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    half4 occlusionProbeChannels = _AdditionalLightsBuffer[lightIndex].occlusionProbeChannels;
                #else
                half4 occlusionProbeChannels = _AdditionalLightsOcclusionProbes[lightIndex];
                #endif
                light.shadowAttenuation = AdditionalLightShadow(lightIndex, positionWS, light.direction, shadowMask,
                                                                occlusionProbeChannels);
                #if defined(_LIGHT_COOKIES)
    real3 cookieColor = SampleAdditionalLightCookie(lightIndex, positionWS);
    light.color *= cookieColor;
                #endif

                return light;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                // return half4(IN.texCoord0.xy,0, 1.0);
                #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(IN.texCoord0);
                #else
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(IN.texCoord0));
                #endif
                float3 worldPos = ComputeWorldSpacePosition(IN.texCoord0, depth, UNITY_MATRIX_I_VP);

                float3 rayOrigin = _WorldSpaceCameraPos;

                float3 ray = worldPos - rayOrigin;
                
                float2 screenUV =  IN.positionCS.xy / 64;
                    
                // screenUV *= 64;
                float noisev = (noise(screenUV - _Time.yy) );

                // return float4(noisev, noisev, noisev, 1.0);
                _NumSteps += noisev * 4;
                float3 rayDir = (worldPos - rayOrigin) / _NumSteps;

                float n = length(rayDir);

                float density = 0;

                float cosAngle = dot(-_MainLightPosition.xyz, normalize(ray));

                UNITY_LOOP
                for (int i = 1; i < _NumSteps; i++)
                {
                    float3 pos = rayOrigin + rayDir * (i + noise(IN.texCoord0 * i + _Time.xy));
                    float4 shadowCoord = TransformWorldToShadowCoord(pos);
                    float light = MainLightRealtimeShadow(shadowCoord);
                    light *= MieScattering2(cosAngle, _MieK);
                    light *= n;
                    density += light;
                }


                // uint pixelLightCount = GetAdditionalLightsCount();
                // pixelLightCount = 2;
                //
                // for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                // {
                //     for (int i = 1; i < numSteps; i++)
                //     {
                //         float3 pos = rayOrigin + rayDir * (i + noise(IN.texCoord0 * i + _Time.xy));
                //
                //         half4 shadowMask = half4(1, 1, 1, 1);
                //         Light addLight = GetAdditionalLight2(lightIndex, pos, shadowMask);
                //         // float light = addLight.distanceAttenuation * addLight.shadowAttenuation;
                //         float light = addLight.distanceAttenuation;
                //         light *= n;
                //         density += light;
                //     }
                // }
                density = 1 - exp(-density);
                density *= _Intensity;

                return half4(density, density, density, 1.0);
            }
            ENDHLSL
        }


        Pass
        {

            Name "Blit"


            Cull Off
            Blend Off
            ZTest Off
            ZWrite Off

            // 附加
            //            Blend one one
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define MAIN_LIGHT_CALCULATE_SHADOWS
            #define _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"


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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)

            float4 _BaseMap_ST;
            float _scale;
            float _MieK;
            float _Max;
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

            half4 frag(Varyings IN) : SV_Target
            {
                float3 color = SampleSceneColor(IN.texCoord0);

                #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(IN.texCoord0);
                #else
    // Adjust z to match NDC for OpenGL
    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(IN.texCoord0));
                #endif


                float3 worldPos = ComputeWorldSpacePosition(IN.texCoord0, depth, UNITY_MATRIX_I_VP);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float numSteps = 200;
                float3 ray = worldPos - rayOrigin;
                float3 rayDir = (worldPos - rayOrigin) / numSteps;

                float n = length(rayDir);


                float density = 0;

                float cosAngle = dot(-_MainLightPosition.xyz, normalize(ray));


                for (int i = 1; i < numSteps; i++)
                {
                    float3 pos = rayOrigin + rayDir * i;
                    float4 shadowCoord = TransformWorldToShadowCoord(pos);
                    float light = MainLightRealtimeShadow(shadowCoord);
                    light *= MieScattering2(cosAngle, _MieK);
                    float dis = distance(pos, rayOrigin);
                    // light = light / (dis *dis);
                    light *= n;
                    density += light;
                }

                density *= _scale;
                density = min(_Max, density);

                return half4(1, 0, 0, 1.0);
                return half4(worldPos, 1.0);
                return half4(density, density, density, 1.0);
            }
            ENDHLSL
        }
    }
}