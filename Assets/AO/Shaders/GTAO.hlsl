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

float _Debug;
float _AORadius;
uint _AOStepCount;
float _AODirectionCount;
int _AOMaxRadiusInPixels;
float4 _AODepthToViewParams;
float _AOFOVCorrection;
float _AOInvStepCountPlusOne;
float _AOTemporalOffsetIdx;
float _AOTemporalRotationIdx;
float _AOIntensity;

CBUFFER_END

#define _AOBaseResMip  (int)_AOParams0.x
// #define _AOFOVCorrection _AOParams0.y
// #define _AOIntensity _AOParams1.x
#define _AOInvRadiusSq _AOParams1.y
// #define _AOTemporalOffsetIdx _AOParams1.z
// #define _AOTemporalRotationIdx _AOParams1.w
// #define _AOInvStepCountPlusOne _AOParams2.z
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

float SampleDepth(float2 uv)
{
    return SampleSceneDepth(uv.xy);
}

half3 SampleNormal(float2 uv)
{
    return half3(SampleSceneNormals(uv));
}

float GetDepthForCentral(float2 positionSS)
{
    return SampleDepth(positionSS / _ScreenParams.xy);
}

float GetDepthSample(float2 positionSS, bool lowerRes)
{
    return GetDepthForCentral(positionSS);
}

float GTAOFastAcos(float x)
{
    return acos(x);
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

float UpdateHorizon(float maxH, float candidateH, float distSq)
{
    float falloff = saturate((1.0 - (distSq * 1 / (_AORadius * _AORadius))));

    return (candidateH > maxH) ? lerp(maxH, candidateH, falloff) : lerp(maxH, candidateH, 0.03f);
}

float2 GetDirection(uint2 positionSS, int offset)
{
    float noise = InterleavedGradientNoise(positionSS.xy, 0);
    float rotations[] = {60.0, 300.0, 180.0, 240.0, 120.0, 0.0};

    float rotation = (rotations[ (_AOTemporalRotationIdx)%6] / 360.0);

    noise += rotation;
    noise *= PI;

    return float2(cos(noise), sin(noise));
}

float GetOffset(uint2 positionSS)
{
    // Spatial offset
    float offset = 0.25 * ((positionSS.y - positionSS.x) & 0x3);

    float offsets[] = {0.0, 0.5, 0.25, 0.75};
    offset += offsets[_AOTemporalOffsetIdx];

    return frac(offset);
}

float3 GetPositionVS(float2 positionSS, float depth)
{
    float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
    return float3((positionSS * _AODepthToViewParams.xy - _AODepthToViewParams.zw) * linearDepth, linearDepth);
}

float3 GetNormalVS(float3 normalWS)
{
    float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
    return float3(normalVS.xy, -normalVS.z);
}

float3 GetNormalWS(float3 normalVS)
{
    normalVS.z = -normalVS.z;
    float3 normalWS = normalize(mul((float3x3)UNITY_MATRIX_I_V, normalVS));
    return normalWS;
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

// Based on Oat and Sander's 2007 technique
// Area/solidAngle of intersection of two cone
real SphericalCapIntersectionSolidArea(real cosC1, real cosC2, real cosB)
{
    real r1 = FastACos(cosC1);
    real r2 = FastACos(cosC2);
    real rd = FastACos(cosB);
    real area = 0.0;

    if (rd <= max(r1, r2) - min(r1, r2))
    {
        // One cap is completely inside the other
        area = TWO_PI - TWO_PI * max(cosC1, cosC2);
    }
    else if (rd >= r1 + r2)
    {
        // No intersection exists
        area = 0.0;
    }
    else
    {
        real diff = abs(r1 - r2);
        real den = r1 + r2 - diff;
        real x = 1.0 - saturate((rd - diff) / max(den, 0.0001));
        area = smoothstep(0.0, 1.0, x);
        area *= TWO_PI - TWO_PI * max(cosC1, cosC2);
    }

    return area;
}

real GetSpecularOcclusionFromBentAO_ConeCone(real3 V, real3 bentNormalWS, real3 normalWS, real ambientOcclusion,
                                             real roughness)
{
    // Retrieve cone angle
    // Ambient occlusion is cosine weighted, thus use following equation. See slide 129
    real cosAv = sqrt(1.0 - ambientOcclusion);
    roughness = max(roughness, 0.01); // Clamp to 0.01 to avoid edge cases
    real cosAs = exp2((-log(10.0) / log(2.0)) * Sq(roughness));
    real cosB = dot(bentNormalWS, reflect(-V, normalWS));
    return SphericalCapIntersectionSolidArea(cosAv, cosAs, cosB) / (TWO_PI * (1.0 - cosAs));
}

real GetSpecularOcclusionFromBentAO(real3 V, real3 bentNormalWS, real3 normalWS, real ambientOcclusion, real roughness)
{
    // Pseudo code:
    //SphericalGaussian NDF = WarpedGGXDistribution(normalWS, roughness, V);
    //SphericalGaussian Visibility = VisibilityConeSG(bentNormalWS, ambientOcclusion);
    //SphericalGaussian UpperHemisphere = UpperHemisphereSG(normalWS);
    //return saturate( InnerProduct(NDF, Visibility) / InnerProduct(NDF, UpperHemisphere) );

    // 1. Approximate visibility cone with a spherical gaussian of amplitude A=1
    // For a cone angle X, we can determine sharpness so that all point inside the cone have a value > Y
    // sharpness = (log(Y) - log(A)) / (cos(X) - 1)
    // For AO cone, cos(X) = sqrt(1 - ambientOcclusion)
    // -> for Y = 0.1, sharpness = -1.0 / (sqrt(1-ao) - 1)
    float vs = -1.0f / min(sqrt(1.0f - ambientOcclusion) - 1.0f, -0.001f);

    // 2. Approximate upper hemisphere with sharpness = 0.8 and amplitude = 1
    float us = 0.8f;

    // 3. Compute warped SG Axis of GGX distribution
    // Ref: All-Frequency Rendering of Dynamic, Spatially-Varying Reflectance
    // https://www.microsoft.com/en-us/research/wp-content/uploads/2009/12/sg.pdf
    float NoV = dot(V, normalWS);
    float3 NDFAxis = (2 * NoV * normalWS - V) * (0.5f / max(roughness * roughness * NoV, 0.001f));

    float umLength1 = length(NDFAxis + vs * bentNormalWS);
    float umLength2 = length(NDFAxis + us * normalWS);
    float d1 = 1 - exp(-2 * umLength1);
    float d2 = 1 - exp(-2 * umLength2);

    float expFactor1 = exp(umLength1 - umLength2 + us - vs);

    return saturate(expFactor1 * (d1 * umLength2) / (d2 * umLength1));
}

real GetSpecularOcclusionFromAmbientOcclusion(real NdotV, real ambientOcclusion, real roughness)
{
    return saturate(PositivePow(NdotV + ambientOcclusion, exp2(-16.0 * roughness - 1.0)) - 1.0 + ambientOcclusion);
}

half4 GTAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
    float2 posSS = uv * _ScreenParams.xy;

    float currDepth = GetDepthForCentral(posSS);
    float3 positionVS = GetPositionVS(posSS, currDepth);

    float3 normalWS = SampleNormal(uv);
    float3 normalVS = GetNormalVS(normalWS);

    float offset = GetOffset(posSS);
    float2 rayStart = posSS;
    float integral = 0;

    const int dirCount = _AODirectionCount;
    // const int dirCount = 1;

    float3 V = normalize(-positionVS);
    float fovCorrectedradiusSS = clamp(_AORadius * _AOFOVCorrection * rcp(positionVS.z), _AOStepCount,
                                       _AOMaxRadiusInPixels);
    float step = max(1, fovCorrectedradiusSS * _AOInvStepCountPlusOne);

    float3 bentNormal = 0;

    [loop]
    for (int i = 0; i < dirCount; ++i)
    {
        float2 dir = GetDirection(posSS, i);
        float2 negDir = -dir + 1e-30;

        float2 maxHorizons;
        maxHorizons.x = HorizonLoop(positionVS, V, rayStart, dir, offset, step);
        maxHorizons.y = HorizonLoop(positionVS, V, rayStart, negDir, offset, step);


        float3 sliceN = normalize(cross(float3(dir.xy, 0.0f), V.xyz));
        float3 NN = sliceN * dot(normalVS, sliceN);
        float3 projN = normalVS - NN;
        float projNLen = length(projN);
        float cosN = dot(projN / projNLen, V);

        float3 T = cross(V, sliceN);
        float N = -sign(dot(projN, T)) * GTAOFastAcos(cosN);

        maxHorizons.x = -GTAOFastAcos(maxHorizons.x);
        maxHorizons.y = GTAOFastAcos(maxHorizons.y);

        // if(AnyIsNaN(maxHorizons) )
        // {
        //     return float4(0,1,0,1);
        // }
        // return maxHorizons.x;


        // return bentAngle;

        maxHorizons.x = N + max(maxHorizons.x - N, -HALF_PI);
        maxHorizons.y = N + min(maxHorizons.y - N, HALF_PI);


        integral += AnyIsNaN(maxHorizons) ? 1 : IntegrateArcCosWeighted(maxHorizons.x, maxHorizons.y, N, cosN);

        float bentAngle = AnyIsNaN(maxHorizons) ? 1 : (maxHorizons.x + maxHorizons.y) * 0.5f;

        // bentAngle = abs(bentAngle);
        // return float4(bentAngle, bentAngle, bentAngle, 1);

        // float cosA = cos(bentAngle);
        // cosA = abs(sin(bentAngle));

        // return float4(cosA, cosA, cosA, 1);

        bentNormal += normalize(projNLen * (V * cos(bentAngle) - T * sin(bentAngle)) + NN);
        // bentNormal +=  V * cos(bentAngle) - T * sin(bentAngle) + sliceN;
    }

    bentNormal = normalize(bentNormal);

    // bentNormal = normalize(normalize(bentNormal));
    integral /= dirCount;

    if (currDepth == UNITY_RAW_FAR_CLIP_VALUE || integral < -1e-2f)
    {
        integral = 0;
    }

    // return float4(normalVS,1);

    // float3 Bent = GetNormalWS(bentNormal);
    float3 Bent = bentNormal;

    float3 bentNormalWS = GetNormalWS(bentNormal);
    float3 VWS = GetNormalWS(V);

    float3 show = bentNormalWS;
    //
    if (_Debug > 1)
        show = normalWS;

    // show = VWS;

    //     // show = bentNormal;
    // // bentNormalWS = worldNormal;
    //     // return float4(worldNormal, 1);
    // return float4(0.5 * (show+1), 1);

    float GTAO = saturate(integral);

    // bentNormal.z = -bentNormal.z;
    // normalVS.z = -normalVS.z;

    float GTRO = GetSpecularOcclusionFromBentAO(V, bentNormal, normalVS, GTAO, 0.5f);


    // ro = GTAO;
    // ro = 1 - ro;

    // ro = GetSpecularOcclusionFromAmbientOcclusion((dot(normalVS, V)),GTAO,0.5f);
    // ro = 1 - GTAO;

    GTAO = 1 - _AOIntensity*(1 - GTAO);
    GTAO = saturate(GTAO);

    return float4(GTAO, GTRO, 0, 1);
}


#endif //UNIVERSAL_SSAO_INCLUDED
