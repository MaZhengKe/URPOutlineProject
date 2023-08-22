Shader "KuanMi/SSR"
{

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

            Name "Tracing"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"


            TEXTURE2D_X_FLOAT(_DepthPyramidTexture);
            SAMPLER(sampler_DepthPyramidTexture);
            float4 _DepthPyramidTexture_TexelSize;

            float _SsrThicknessScale;
            float _SsrThicknessBias;


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


            float3 EncodeIntoNormalBuffer(float3 normalWS)
            {
                // The sign of the Z component of the normal MUST round-trip through the G-Buffer, otherwise
                // the reconstruction of the tangent frame for anisotropic GGX creates a seam along the Z axis.
                // The constant was eye-balled to not cause artifacts.
                // TODO: find a proper solution. E.g. we could re-shuffle the faces of the octahedron
                // s.t. the sign of the Z component round-trips.
                const float seamThreshold = 1.0 / 1024.0;
                normalWS.z = CopySign(max(seamThreshold, abs(normalWS.z)), normalWS.z);

                // RT1 - 8:8:8:8
                // Our tangent encoding is based on our normal.
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));

                return packNormalWS;
            }


            float3 DecodeFromNormalBuffer(float3 normalBuffer)
            {
                float3 packNormalWS = normalBuffer.rgb;
                float2 octNormalWS = Unpack888ToFloat2(packNormalWS);
                return UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);
            }


            #if UNITY_REVERSED_Z
            # define MIN_DEPTH(l, r) max(l, r)
            #else
            # define MIN_DEPTH(l, r) min(l, r)
            #endif

            void GetNormalAndPerceptualRoughness(uint2 positionSS, out float3 normalWS, out float perceptualRoughness)
            {
                float4 packedCoatMask = LOAD_TEXTURE2D_X(_CameraNormalsTexture, positionSS);
                normalWS = packedCoatMask.xyz;
                perceptualRoughness = packedCoatMask.w;
            }

            #define SSR_TRACE_EPS               0.000488281f // 2^-11, should be good up to 4K
            #define MIN_GGX_ROUGHNESS           0.00001f
            #define MAX_GGX_ROUGHNESS           0.99999f


            half2 frag(Varyings IN) : SV_Target
            {
                int _SsrReflectsSky = 0;


                float2 uv = IN.texCoord0;

                uint2 positionSS = uv * _ScreenParams.xy;

                // return (float2)positionSS / 512;

                float deviceDepth = LOAD_TEXTURE2D_X(_DepthPyramidTexture, positionSS).r;


                float2 positionNDC = positionSS * _ScreenSize.zw + (0.5 * _ScreenSize.zw);

                float3 positionWS = ComputeWorldSpacePosition(positionNDC, deviceDepth, UNITY_MATRIX_I_VP); // Jittered
                float3 V = GetWorldSpaceNormalizeViewDir(positionWS);

                float3 N;
                float perceptualRoughness;
                GetNormalAndPerceptualRoughness(positionSS, N, perceptualRoughness);

                float3 packN = EncodeIntoNormalBuffer(N);

                N = DecodeFromNormalBuffer(packN);


                // return 0.5f;
                // return float3(positionNDC,0);

                float3 R = reflect(-V, N);

                // return R.xy;


                float3 camPosWS = GetCurrentViewPosition();

                positionWS = camPosWS + (positionWS - camPosWS) * (1 - 0.001 * rcp(max(dot(N, V), FLT_EPS)));
                deviceDepth = ComputeNormalizedDeviceCoordinatesWithZ(positionWS, UNITY_MATRIX_VP).z;
                bool killRay = deviceDepth == UNITY_RAW_FAR_CLIP_VALUE;

                float3 rayOrigin = float3(positionSS + 0.5, deviceDepth);

                float3 reflPosWS = positionWS + R;
                float3 reflPosNDC = ComputeNormalizedDeviceCoordinatesWithZ(reflPosWS, UNITY_MATRIX_VP); // Jittered
                float3 reflPosSS = float3(reflPosNDC.xy * _ScreenSize.xy, reflPosNDC.z);
                float3 rayDir = reflPosSS - rayOrigin;
                float3 rcpRayDir = rcp(rayDir);
                int2 rayStep = int2(rcpRayDir.x >= 0 ? 1 : 0,
                                    rcpRayDir.y >= 0 ? 1 : 0);
                float3 raySign = float3(rcpRayDir.x >= 0 ? 1 : -1,
                                        rcpRayDir.y >= 0 ? 1 : -1,
                                        rcpRayDir.z >= 0 ? 1 : -1);
                bool rayTowardsEye = rcpRayDir.z >= 0;


                killRay = killRay || (reflPosSS.z <= 0);
                killRay = killRay || (dot(N, V) <= 0);
                killRay = killRay || (perceptualRoughness > 1);
                // #ifndef SSR_TRACE_TOWARDS_EYE
                // killRay = killRay || rayTowardsEye;
                // #endif

                if (killRay)
                {
                    return 0;
                }

                // float2 outV = pow(rayOrigin.xy / _ScreenParams.xy, 1/2.3);
                // return float3(0.5,0,0);
                // return rayDir ;
                // return 0;

                // Extend and clip the end point to the frustum.
                float tMax;
                {
                    // Shrink the frustum by half a texel for efficiency reasons.
                    const float halfTexel = 0.5;

                    float3 bounds;
                    bounds.x = (rcpRayDir.x >= 0) ? _ScreenSize.x - halfTexel : halfTexel;
                    bounds.y = (rcpRayDir.y >= 0) ? _ScreenSize.y - halfTexel : halfTexel;
                    // If we do not want to intersect the skybox, it is more efficient to not trace too far.

                    float maxDepth = (_SsrReflectsSky != 0) ? -0.00000024 : 0.00000024; // 2^-22
                    bounds.z = (rcpRayDir.z >= 0) ? 1 : maxDepth;

                    float3 dist = bounds * rcpRayDir - (rayOrigin * rcpRayDir);
                    tMax = Min3(dist.x, dist.y, dist.z);
                }

                // return tMax/5;

                // Clamp the MIP level to give the compiler more information to optimize.
                //_SsrDepthPyramidMaxMip 10
                const int maxMipLevel = min(10, 14);

                // Start ray marching from the next texel to avoid self-intersections.
                float t;
                {
                    // 'rayOrigin' is the exact texel center.
                    float2 dist = abs(0.5 * rcpRayDir.xy);
                    t = min(dist.x, dist.y);
                }

                float3 rayPos;

                int mipLevel = 0;
                int iterCount = 0;
                bool hit = false;
                bool miss = false;
                bool belowMip0 = false; // This value is set prior to entering the cell

                //_SsrIterLimit 64
                while (!(hit || miss) && (t <= tMax) && (iterCount < 128))
                {
                    rayPos = rayOrigin + t * rayDir;

                    float2 sgnEdgeDist = round(rayPos.xy) - rayPos.xy;
                    float2 satEdgeDist = clamp(raySign.xy * sgnEdgeDist + SSR_TRACE_EPS, 0, SSR_TRACE_EPS);
                    rayPos.xy += raySign.xy * satEdgeDist;

                    int2 mipCoord = (int2)rayPos.xy >> mipLevel;

                    // int2 mipOffset = _DepthPyramidMipLevelOffsets[mipLevel];

                    // Bounds define 4 faces of a cube:
                    // 2 walls in front of the ray, and a floor and a base below it.
                    float4 bounds;

                    bounds.xy = (mipCoord + rayStep) << mipLevel;
                    bounds.z = LOAD_TEXTURE2D_X_LOD(_DepthPyramidTexture, mipCoord, mipLevel).r;

                    // We define the depth of the base as the depth value as:
                    // b = DeviceDepth((1 + thickness) * LinearDepth(d))
                    // b = ((f - n) * d + n * (1 - (1 + thickness))) / ((f - n) * (1 + thickness))
                    // b = ((f - n) * d - n * thickness) / ((f - n) * (1 + thickness))
                    // b = d / (1 + thickness) - n / (f - n) * (thickness / (1 + thickness))
                    // b = d * k_s + k_b

                    bounds.w = bounds.z * _SsrThicknessScale + _SsrThicknessBias;

                    float4 dist = bounds * rcpRayDir.xyzz - (rayOrigin.xyzz * rcpRayDir.xyzz);
                    float distWall = min(dist.x, dist.y);
                    float distFloor = dist.z;
                    float distBase = dist.w;


                    #if 0
                    bool belowFloor  = (raySign.z * (t - distFloor)) <  0;
                    bool aboveBase   = (raySign.z * (t - distBase )) >= 0;
                    #else
                    bool belowFloor = rayPos.z < bounds.z;
                    bool aboveBase = rayPos.z >= bounds.w;
                    #endif

                    bool insideFloor = belowFloor && aboveBase;
                    bool hitFloor = (t <= distFloor) && (distFloor <= distWall);


                    miss = belowMip0 && insideFloor;
                    // miss = belowMip0;


                    hit = (mipLevel == 0) && (hitFloor || insideFloor);
                    belowMip0 = (mipLevel == 0) && belowFloor;


                    t = hitFloor ? distFloor : (((mipLevel != 0) && belowFloor) ? t : distWall);
                    rayPos.z = bounds.z; // Retain the depth of the potential intersection

                    // Warning: both rays towards the eye, and tracing behind objects has linear
                    // rather than logarithmic complexity! This is due to the fact that we only store
                    // the maximum value of depth, and not the min-max.
                    mipLevel += (hitFloor || belowFloor || rayTowardsEye) ? -1 : 1;
                    mipLevel = clamp(mipLevel, 0, maxMipLevel);

                    // mipLevel = 0;

                    iterCount++;
                }
                miss = miss || ((_SsrReflectsSky == 0) && (rayPos.z == 0));
                hit = hit && !miss;


                if (hit)
                {
                    // Note that we are using 'rayPos' from the penultimate iteration, rather than
                    // recompute it using the last value of 't', which would result in an overshoot.
                    // It also needs to be precisely at the center of the pixel to avoid artifacts.
                    float2 hitPositionNDC = floor(rayPos.xy) * _ScreenSize.zw + (0.5 * _ScreenSize.zw);
                    // Should we precompute the half-texel bias? We seem to use it a lot.
                    return float3(hitPositionNDC.xy, 0);
                }


                return float3(0, 0, 0);
            }
            ENDHLSL
        }


        Pass
        {

            Name "Reprojection"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"


            TEXTURE2D_X_FLOAT(_SsrHitPointTexture);
            SAMPLER(sampler_SsrHitPointTexture);
            float4 _SsrHitPointTexture_TexelSize;


            TEXTURE2D_X_FLOAT(_DepthPyramidTexture);
            SAMPLER(sampler_DepthPyramidTexture);
            float4 _DepthPyramidTexture_TexelSize;


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

            #define MIN_GGX_ROUGHNESS           0.00001f
            #define MAX_GGX_ROUGHNESS           0.99999f

            void GetNormalAndPerceptualRoughness(uint2 positionSS, out float3 normalWS, out float perceptualRoughness)
            {
                float4 packedCoatMask = LOAD_TEXTURE2D_X(_CameraNormalsTexture, positionSS);
                normalWS = packedCoatMask.xyz;
                perceptualRoughness = packedCoatMask.w;
            }

            half3 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texCoord0;
                uint2 positionSS0 = uv * _ScreenParams.xy;

                float3 N;
                float perceptualRoughness;
                GetNormalAndPerceptualRoughness(positionSS0, N, perceptualRoughness);


                float roughness = (perceptualRoughness);
                roughness = clamp(roughness, MIN_GGX_ROUGHNESS, MAX_GGX_ROUGHNESS);

                float2 hitPositionNDC = LOAD_TEXTURE2D_X(_SsrHitPointTexture, positionSS0).xy;

                if (max(hitPositionNDC.x, hitPositionNDC.y) == 0)
                {
                    // Miss.
                    return 0;
                }


                float depthOrigin = LOAD_TEXTURE2D_X(_DepthPyramidTexture, positionSS0).r;

                PositionInputs posInputOrigin = GetPositionInput(positionSS0.xy, _ScreenSize.zw, depthOrigin,
                                                                 UNITY_MATRIX_I_VP, UNITY_MATRIX_V, uint2(8, 8));
                float3 originWS = posInputOrigin.positionWS + _WorldSpaceCameraPos;

                // TODO: this texture is sparse (mostly black). Can we avoid reading every texel? How about using Hi-S?
                // float2 motionVectorNDC;
                // DecodeMotionVector(SAMPLE_TEXTURE2D_X_LOD(_CameraMotionVectorsTexture, s_linear_clamp_sampler,
                //                                           min(hitPositionNDC, 1.0f - 0.5f * _ScreenSize.zw) *
                //                                           _RTHandleScale.xy, 0), motionVectorNDC);
                // float2 prevFrameNDC = hitPositionNDC ;
                // float2 prevFrameUV = prevFrameNDC * _ColorPyramidUvScaleAndLimitPrevFrame.xy;

    float  mipLevel = lerp(0, 1, perceptualRoughness);
                
    // float2 diffLimit = _ColorPyramidUvScaleAndLimitPrevFrame.xy - _ColorPyramidUvScaleAndLimitPrevFrame.zw;
    // float2 diffLimitMipAdjusted = diffLimit * pow(2.0,1.5 + ceil(abs(mipLevel)));
    // float2 limit = _ColorPyramidUvScaleAndLimitPrevFrame.xy - diffLimitMipAdjusted;
    // if (any(prevFrameUV < float2(0.0,0.0)) || any(prevFrameUV > limit))
    // {
    //     // Off-Screen.
    //     return;
    // }

                
    float3 color    = SampleSceneColor(hitPositionNDC);
                


                

                return color;
            }
            ENDHLSL
        }
    }
}