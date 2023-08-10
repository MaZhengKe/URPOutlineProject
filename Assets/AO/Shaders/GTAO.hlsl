#ifndef UNIVERSAL_SSAO_INCLUDED
#define UNIVERSAL_SSAO_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

// Textures & Samplers
TEXTURE2D_HALF(_BlueNoiseTexture);
TEXTURE2D_X_HALF(_ScreenSpaceOcclusionTexture);

SAMPLER(sampler_BlitTexture);

CBUFFER_START(ShaderVariablesAmbientOcclusion)

float _AORadius;
uint _AOStepCount;
float _AODirectionCount;
int _AOMaxRadiusInPixels;
float4 _AODepthToViewParams;
float _AOFOVCorrection;

CBUFFER_END

#define _AOBaseResMip  (int)_AOParams0.x
// #define _AOFOVCorrection _AOParams0.y
#define _AOIntensity _AOParams1.x
#define _AOInvRadiusSq _AOParams1.y
#define _AOTemporalOffsetIdx _AOParams1.z
#define _AOTemporalRotationIdx _AOParams1.w
#define _AOInvStepCountPlusOne _AOParams2.z
#define _AOHistorySize _AOParams2.xy
#define _FirstDepthMipOffset _FirstTwoDepthMipOffsets.xy
#define _SecondDepthMipOffset _FirstTwoDepthMipOffsets.zw

// For denoising, whether temporal or not
#define _BlurTolerance _AOParams3.x
#define _UpsampleTolerance _AOParams3.y
#define _NoiseFilterStrength _AOParams3.z
#define _AOTemporalUpperNudgeLimit _AOParams4.y
#define _AOTemporalLowerNudgeLimit _AOParams4.z
#define _AOSpatialBilateralAggressiveness _AOParams4.w

// Function defines
#define SCREEN_PARAMS               GetScaledScreenParams()
#define SAMPLE_BASEMAP(uv)          half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(uv)));
#define SAMPLE_BASEMAP_R(uv)        half(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(uv)).r);
#define SAMPLE_BLUE_NOISE(uv)       SAMPLE_TEXTURE2D(_BlueNoiseTexture, sampler_PointRepeat, UnityStereoTransformScreenSpaceTex(uv)).r;



float3 GetPositionVS(float2 positionSS, float depth)
{
    float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
    return float3((positionSS * _AODepthToViewParams.xy - _AODepthToViewParams.zw) * linearDepth, linearDepth);
}


float SampleDepth(float2 uv)
{
    return SampleSceneDepth(uv.xy);
}

half3 SampleNormal(float2 uv)
{
    return half3(SampleSceneNormals(uv));
}

// float3 GetPositionVS(float2 positionSS, float depth)
// {
//     return ComputeViewSpacePosition(positionSS / _ScreenParams.xy, depth, UNITY_MATRIX_I_P);
// }

float2 GetDirection(uint2 positionSS, int offset)
{
    float noise = InterleavedGradientNoise(positionSS.xy, 0);
    float rotations[] = {60.0, 300.0, 180.0, 240.0, 120.0, 0.0};

    float rotation = (rotations[offset] / 360.0);

    noise += rotation;
    noise *= PI;

    return float2(cos(noise), sin(noise));
}

float GetOffset(uint2 positionSS)
{
    // Spatial offset
    float offset = 0.25 * ((positionSS.y - positionSS.x) & 0x3);

    return frac(offset);
}

float GetDepthForCentral(float2 positionSS)
{
    return SampleDepth(positionSS / _ScreenParams.xy);
}

float GetDepthSample(float2 positionSS, bool lowerRes)
{
    return GetDepthForCentral(positionSS);
}

float UpdateHorizon(float maxH, float candidateH, float distSq)
{
    float falloff = saturate((1.0 - (distSq * 1 / (_AORadius * _AORadius))));

    return (candidateH > maxH) ? lerp(maxH, candidateH, falloff) : lerp(maxH, candidateH, 0.03f);
    // TODO: Thickness heuristic here.
}

float HorizonLoop(float3 positionVS, float3 V, float2 rayStart, float2 rayDir, float rayOffset, float rayStep)
{
    float maxHorizon = -1.0f; // cos(pi)
    float t = rayOffset * rayStep + rayStep;

    const uint startWithLowerRes = min(max(0, _AOStepCount / 2 - 2), 3);
    for (uint i = 0; i < _AOStepCount; i++)
    {
        float2 samplePos = max(2, min(rayStart + t * rayDir, _ScreenParams.xy - 2));

        // Find horizons at these steps:
        float sampleDepth = GetDepthSample(samplePos, i > startWithLowerRes);
        float3 samplePosVS = GetPositionVS(samplePos.xy, sampleDepth);

        float3 deltaPos = samplePosVS - positionVS;
        float deltaLenSq = dot(deltaPos, deltaPos);

        float currHorizon = dot(deltaPos, V) * rsqrt(deltaLenSq);
        maxHorizon = UpdateHorizon(maxHorizon, currHorizon, deltaLenSq);

        t += rayStep;
    }

    return maxHorizon;
}


float GTAOFastAcos(float x)
{
    float outVal = -0.156583 * abs(x) + HALF_PI;
    outVal *= sqrt(1.0 - abs(x));
    return x >= 0 ? outVal : PI - outVal;
}


float IntegrateArcCosWeighted(float horzion1, float horizon2, float n, float cosN)
{
    float h1 = horzion1 * 2.0;
    float h2 = horizon2 * 2.0;
    float sinN = sin(n);
    return 0.25 * ((-cos(h1 - n) + cosN + h1 * sinN) + (-cos(h2 - n) + cosN + h2 * sinN));
}


float3 GetNormalVS(float3 normalWS)
{
    float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
    return float3(normalVS.xy, -normalVS.z);
}

half4 GTAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);


    float2 uv = input.texcoord;
    float2 posSS = uv * _ScreenParams.xy;

    float currDepth = GetDepthForCentral(posSS);

    // return float4(currDepth, currDepth, currDepth, 1);
    float3 positionVS = GetPositionVS(posSS, currDepth);
    // return float4(positionVS, 1);

    float3 worldNormal = SampleNormal(uv);
    float3 normalVS = GetNormalVS(worldNormal);

    float offset = GetOffset(posSS);
    // offset = 0;

    // return float4(offset, offset, offset, 1);
    float2 rayStart = posSS;
    // return float4(rayStart / _ScreenParams.xy, 0, 1);
    float integral = 0;

    const int dirCount = _AODirectionCount;

    float3 V = normalize(-positionVS);
    // return float4(positionVS, 1);
    float fovCorrectedradiusSS = clamp(_AORadius *  _AOFOVCorrection  * 0.25f * rcp(positionVS.z), _AOStepCount,_AOMaxRadiusInPixels);

    fovCorrectedradiusSS = _AOMaxRadiusInPixels / positionVS.z;
    // fovCorrectedradiusSS /= _RadiusToScreen;
    // return float4(fovCorrectedradiusSS,fovCorrectedradiusSS,fovCorrectedradiusSS, 1);

    float step = max(1, fovCorrectedradiusSS * (1 / _AOStepCount + 1));

    [unroll]
    for (int i = 0; i < dirCount; ++i)
    {
        float2 dir = GetDirection(posSS, i);
        float2 negDir = -dir + 1e-30;

        float2 maxHorizons;
        maxHorizons.x = HorizonLoop(positionVS, V, rayStart, dir, offset, step);
        maxHorizons.y = HorizonLoop(positionVS, V, rayStart, negDir, offset, step);


        float3 sliceN = normalize(cross(float3(dir.xy, 0.0f), V.xyz));
        float3 projN = normalVS - sliceN * dot(normalVS, sliceN);
        float projNLen = length(projN);
        float cosN = dot(projN / projNLen, V);


        float3 T = cross(V, sliceN);
        float N = -sign(dot(projN, T)) * GTAOFastAcos(cosN);

        maxHorizons.x = -GTAOFastAcos(maxHorizons.x);
        maxHorizons.y = GTAOFastAcos(maxHorizons.y);
        maxHorizons.x = N + max(maxHorizons.x - N, -HALF_PI);
        maxHorizons.y = N + min(maxHorizons.y - N, HALF_PI);


        integral += AnyIsNaN(maxHorizons) ? 1 : IntegrateArcCosWeighted(maxHorizons.x, maxHorizons.y, N, cosN);
    }

    integral /= dirCount;


    // if (currDepth == UNITY_RAW_FAR_CLIP_VALUE || integral < -1e-2f)
    // {
    //     integral = 1;
    // }

    return float4(integral, integral, integral, 1);
}


#endif //UNIVERSAL_SSAO_INCLUDED
