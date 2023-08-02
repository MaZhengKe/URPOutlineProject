Shader "KuanMi/SpotVolumetricLighting"
{
    Properties
    {
        _Intensity("Intensity",Range(0.0,50.0)) = 1.0
        _MieK("MieK",Range(-1.0,1.0)) = 0.8
        _NumSteps("NumSteps",float) = 15

        _Range("Range",float) = 1.0
        _SpotAngle("SpotAngle",Range(0,180)) = 1.0
        _lightIndex("lightIndex",int) = 0

        _BlueNoise("BlueNoise",2DArray) = "white"
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {

            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Front
            Blend One One, One One
            //            Blend One Zero, One Zero
            ZTest Always
            ZWrite Off

            //            Cull Off
            //            // 附加
            //            Blend one one
            //            //            Blend Off
            //            ZTest Off
            //            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define MAIN_LIGHT_CALCULATE_SHADOWS
            #define _ADDITIONAL_LIGHT_SHADOWS

            #pragma shader_feature_local _POINT_LIGHT

            // #define _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            #include "KuanMiTool.hlsl"

            #define MieScattering(cosAngle, g) g.w * (g.x / (pow(g.y - g.z * cosAngle, 1.5)))
            #define MieScattering2(cosAngle, k) (1-k*k)/(4*PI*(1+k*cosAngle)*(1+k*cosAngle))


            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D_ARRAY(_BlueNoise);
            SAMPLER(sampler_BlueNoise);

            CBUFFER_START(UnityPerMaterial)
            float _Intensity;
            float _MieK;
            float _SpotAngle;
            float _Range;
            float _NumSteps;
            int _lightIndex;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return output;
            }

            float random(float value)
            {
                return  frac(sin(value) * 43758.5453);
            }
            
            float noise(float2 uv)
            {
                Hash(_Time)
                int index = random(_Time) * 64;
                return SAMPLE_TEXTURE2D_ARRAY(_BlueNoise, sampler_BlueNoise, uv, index).r;
            }

            Light GetAdditionalPerObjectLightForVol(int perObjectLightIndex, float3 positionWS)
            {
                // Abstraction over Light input constants

                float4 lightPositionWS = _AdditionalLightsPosition[perObjectLightIndex];
                half3 color = _AdditionalLightsColor[perObjectLightIndex].rgb;
                half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[perObjectLightIndex];
                half4 spotDirection = _AdditionalLightsSpotDir[perObjectLightIndex];
                uint lightLayerMask = asuint(_AdditionalLightsLayerMasks[perObjectLightIndex]);

                // Directional lights store direction in lightPosition.xyz and have .w set to 0.0.
                // This way the following code will work for both directional and punctual lights.
                float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
                float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

                half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
                // full-float precision required on some platforms
                float attenuation = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy) * AngleAttenuation(
                    spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);

                Light light;
                light.direction = lightDirection;
                light.distanceAttenuation = attenuation;
                light.shadowAttenuation = 1.0;
                // This value can later be overridden in GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
                light.color = color;
                light.layerMask = lightLayerMask;

                return light;
            }


            Light GetAdditionalLightForVol(uint lightIndex, float3 positionWS, half4 shadowMask)
            {
                // int lightIndex = GetPerObjectLightIndex(i);
                Light light = GetAdditionalPerObjectLightForVol(lightIndex, positionWS);
                half4 occlusionProbeChannels = _AdditionalLightsOcclusionProbes[lightIndex];
                light.shadowAttenuation = AdditionalLightShadow(lightIndex, positionWS, light.direction, shadowMask,
                                                                occlusionProbeChannels);
                return light;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                // 0-1
                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                // return float4(noiseUV, 0, 1);

                // float noi = noise(noiseUV * _Time.xy);
                // return float4(noi, noi, noi, 1);

                // return float4(screenUV, 0, 1);

                // return float4(screenUV, 0, 1);
                #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(screenUV);
                #else
                real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                float3 worldPos = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                float3 ray = worldPos - _WorldSpaceCameraPos;

                int perObjectLightIndex = _lightIndex;

                float4 lightPositionWS = _AdditionalLightsPosition[perObjectLightIndex];


                Sphere sphere;
                sphere.origin = lightPositionWS.xyz;
                sphere.r = _Range;

                Line viewLine;
                viewLine.origin = _WorldSpaceCameraPos;
                viewLine.direction = normalize(ray);


                #ifdef _POINT_LIGHT

                float3 nearPoint;
                float3 farPoint;
                float num = LineToSpherePoint(viewLine, sphere, nearPoint, farPoint);
                clip(num - 0.5);

                #else

                half4 spotDirection = _AdditionalLightsSpotDir[perObjectLightIndex];
                const float halfAngle = _SpotAngle * 0.5 * PI / 180;
                float3 spotDir = -spotDirection.xyz;

                Cone cone;
                cone.C = lightPositionWS.xyz + spotDir * _Range * cos(halfAngle);
                cone.H = lightPositionWS.xyz;
                cone.r = _Range * tan(halfAngle) * cos(halfAngle);

                Hemisphere hemisphere;
                hemisphere.sphere = sphere;
                hemisphere.normal = spotDir;
                hemisphere.angle = halfAngle;

                float3 TP1;
                float3 TP2;

                float num = LineToConePoint(viewLine, cone, TP1, TP2);

                float3 P1;
                float3 P2;
                float num2 = LineToHemispherePoint(viewLine, hemisphere, P1, P2);

                float sumNum = num + num2;
                clip(sumNum - 0.5);

                float3 nearPoint = TP1 + P1;
                float3 farPoint = TP2 + P2;

                #endif

                float3 nearLen = nearPoint - _WorldSpaceCameraPos;
                float3 worldLen = worldPos - _WorldSpaceCameraPos;


                float3 rayOrigin = dot(nearLen, ray) > 0 ? nearPoint : _WorldSpaceCameraPos;

                clip(length(worldLen) - length(rayOrigin - _WorldSpaceCameraPos));

                worldPos = dot(farPoint - worldPos, ray) < 0 ? farPoint : worldPos;

                float2 noiseUV = IN.positionCS.xy * 0.015625;

                float noisev = noise(noiseUV);

                _NumSteps += noisev * 2;

                float3 rayDir = (worldPos - rayOrigin) / _NumSteps;
                float n = length(rayDir);

                float3 density = 0;

                float3 currentPos;

                for (int i = 1; i < _NumSteps; i++)
                {
                    // currentPos = rayOrigin + rayDir * (i + noise(noiseUV + _Time.xy));
                    currentPos = rayOrigin + rayDir * i;

                    Light addLight = GetAdditionalLightForVol(_lightIndex, currentPos, half4(1, 1, 1, 1));
                    float3 light = addLight.color * addLight.distanceAttenuation * addLight.shadowAttenuation;

                    float cosAngle = dot(-addLight.direction, normalize(ray));
                    light *= MieScattering2(cosAngle, _MieK);

                    light *= n;
                    density += light;
                }

                density = 1 - exp(-density);
                density *= _Intensity;

                return half4(density, 1.0);
            }
            ENDHLSL
        }
    }
}