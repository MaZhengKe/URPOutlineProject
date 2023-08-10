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

// Params
half4 _BlurOffset;
half4 _SSAOParams;
float4 _CameraViewTopLeftCorner[2];
float4x4 _CameraViewProjections[2];
// This is different from UNITY_MATRIX_VP (platform-agnostic projection matrix is used). Handle both non-XR and XR modes.

float4 _SourceSize;
float4 _ProjectionParams2;
float4 _CameraViewXExtent[2];
float4 _CameraViewYExtent[2];
float4 _CameraViewZExtent[2];

// SSAO Settings
#define INTENSITY _SSAOParams.x
#define RADIUS _SSAOParams.y
#define DOWNSAMPLE _SSAOParams.z
#define FALLOFF _SSAOParams.w

half4 _SSAOBlueNoiseParams;
#define BlueNoiseScale          _SSAOBlueNoiseParams.xy
#define BlueNoiseOffset         _SSAOBlueNoiseParams.zw


// #if defined(_SAMPLE_COUNT_HIGH)
static const int SAMPLE_COUNT = 8;
// #elif defined(_SAMPLE_COUNT_MEDIUM)
//     static const int SAMPLE_COUNT = 8;
// #else
//     static const int SAMPLE_COUNT = 4;
// #endif
// Hardcoded random UV values that improves performance.
// The values were taken from this function:
// r = frac(43758.5453 * sin( dot(float2(12.9898, 78.233), uv)) ));
// Indices  0 to 19 are for u = 0.0
// Indices 20 to 39 are for u = 1.0
static half SSAORandomUV[40] =
{
    0.00000000, // 00
    0.33984375, // 01
    0.75390625, // 02
    0.56640625, // 03
    0.98437500, // 04
    0.07421875, // 05
    0.23828125, // 06
    0.64062500, // 07
    0.35937500, // 08
    0.50781250, // 09
    0.38281250, // 10
    0.98437500, // 11
    0.17578125, // 12
    0.53906250, // 13
    0.28515625, // 14
    0.23137260, // 15
    0.45882360, // 16
    0.54117650, // 17
    0.12941180, // 18
    0.64313730, // 19

    0.92968750, // 20
    0.76171875, // 21
    0.13333330, // 22
    0.01562500, // 23
    0.00000000, // 24
    0.10546875, // 25
    0.64062500, // 26
    0.74609375, // 27
    0.67968750, // 28
    0.35156250, // 29
    0.49218750, // 30
    0.12500000, // 31
    0.26562500, // 32
    0.62500000, // 33
    0.44531250, // 34
    0.17647060, // 35
    0.44705890, // 36
    0.93333340, // 37
    0.87058830, // 38
    0.56862750, // 39
};

// Function defines
#define SCREEN_PARAMS               GetScaledScreenParams()
#define SAMPLE_BASEMAP(uv)          half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(uv)));
#define SAMPLE_BASEMAP_R(uv)        half(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(uv)).r);
#define SAMPLE_BLUE_NOISE(uv)       SAMPLE_TEXTURE2D(_BlueNoiseTexture, sampler_PointRepeat, UnityStereoTransformScreenSpaceTex(uv)).r;

// Constants
// kContrast determines the contrast of occlusion. This allows users to control over/under
// occlusion. At the moment, this is not exposed to the editor because it's rarely useful.
// The range is between 0 and 1.
static const half kContrast = half(0.6);

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const half kGeometryCoeff = half(0.8);

// The constants below are used in the AO estimator. Beta is mainly used for suppressing
// self-shadowing noise, and Epsilon is used to prevent calculation underflow. See the paper
// (Morgan 2011 https://casual-effects.com/research/McGuire2011AlchemyAO/index.html)
// for further details of these constants.
static const half kBeta = half(0.004);
static const half kEpsilon = half(0.0001);

static const float SKY_DEPTH_VALUE = 0.00001;
static const half HALF_POINT_ONE = half(0.1);
static const half HALF_MINUS_ONE = half(-1.0);
static const half HALF_ZERO = half(0.0);
static const half HALF_HALF = half(0.5);
static const half HALF_ONE = half(1.0);
static const half4 HALF4_ONE = half4(1.0, 1.0, 1.0, 1.0);
static const half HALF_TWO = half(2.0);
static const half HALF_TWO_PI = half(6.28318530717958647693);
static const half HALF_FOUR = half(4.0);
static const half HALF_NINE = half(9.0);
static const half HALF_HUNDRED = half(100.0);


const float _NUM_STEPS = 4;
const float _NUM_DIRECTIONS = 8;
const float _NDotVBias = 0.1;

const float _RadiusToScreen = 100;
const float _AORadius = 0.5;
const float R2 = 0.25;
const float _NegInvR2 = -1.0 / 0.24;


#define unity_eyeIndex 0


half4 PackAONormal(half ao, half3 n)
{
    n *= HALF_HALF;
    n += HALF_HALF;
    return half4(ao, n);
}

half3 GetPackedNormal(half4 p)
{
    return p.gba * HALF_TWO - HALF_ONE;
}

half GetPackedAO(half4 p)
{
    return p.r;
}

half EncodeAO(half x)
{
    //     #if UNITY_COLORSPACE_GAMMA
    //         return half(1.0 - max(LinearToSRGB(1.0 - saturate(x)), 0.0));
    // #else
    return x;
    // #endif
}

half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, HALF_ONE, dot(d1, d2));
}

float2 GetScreenSpacePosition(float2 uv)
{
    return float2(uv * SCREEN_PARAMS.xy * DOWNSAMPLE);
}

// Pseudo random number generator
half GetRandomVal(half u, half sampleIndex)
{
    return SSAORandomUV[u * 20 + sampleIndex];
}

float SampleDepth(float2 uv)
{
    return SampleSceneDepth(uv.xy);
}

float GetLinearEyeDepth(float rawDepth)
{
    return LinearEyeDepth(rawDepth, _ZBufferParams);
}

float SampleAndGetLinearEyeDepth(float2 uv)
{
    const float rawDepth = SampleDepth(uv);
    return GetLinearEyeDepth(rawDepth);
}

half3 SampleNormal(float2 uv)
{
    return half3(SampleSceneNormals(uv));
}

float Falloff(float DistanceSquare)
{
    return DistanceSquare * _NegInvR2 + 1.0;
}

float ComputeAO(float3 p, float3 n, float3 s)
{
    float3 v = s - p;
    float VdotV = dot(v, v);
    float NdotV = dot(n, v) * rsqrt(VdotV);

    return clamp(NdotV - _NDotVBias, 0, 1) * clamp(Falloff(VdotV), 0, 1);
}

float3 FetchViewPos(float2 uv)
{
    float rawDepth = SampleDepth(uv);
    float3 viewPos = ComputeViewSpacePosition(uv, rawDepth,UNITY_MATRIX_I_P);
    return viewPos;
}

float3 GetPositionVS(float2 positionSS, float depth)
{
    return ComputeViewSpacePosition(positionSS/_ScreenParams.xy, depth, UNITY_MATRIX_I_P);
}

float random(float v)
{
    return frac(sin(v * 42.5) * 43758.5453123);
}

float4 GetJitter(float2 uv)
{
    // (cos(Alpha),sin(Alpha),rand1,rand2)
    // return textureLod( texRandom, (gl_FragCoord.xy / AO_RANDOMTEX_SIZE), 0);

    float Alpha = SAMPLE_BLUE_NOISE((uv + BlueNoiseOffset) * BlueNoiseScale);
    Alpha *= 2.0 * PI;
    return float4(cos(Alpha), sin(Alpha), random(Alpha), random(Alpha + 57));
}


float2 RotateDirection(float2 Dir, float2 CosSin)
{
    return float2(Dir.x * CosSin.x - Dir.y * CosSin.y,
                  Dir.x * CosSin.y + Dir.y * CosSin.x);
}

//----------------------------------------------------------------------------------
float3 ComputeCoarseAO(float2 FullResUV, float RadiusPixels, float4 Rand, float3 ViewPosition, float3 ViewNormal)
{
    // Divide by NUM_STEPS+1 so that the farthest samples are not fully attenuated
    float StepSizePixels = RadiusPixels / (_NUM_STEPS + 1);

    const float Alpha = 2.0 * PI / _NUM_DIRECTIONS;
    float AO = 0;
    for (float DirectionIndex = 0; DirectionIndex < _NUM_DIRECTIONS; ++DirectionIndex)
    {
        float Angle = Alpha * DirectionIndex;

        // Compute normalized 2D direction
        float2 Direction = RotateDirection(float2(cos(Angle), sin(Angle)), Rand.xy);

        // Jitter starting sample within the first step
        float RayPixels = (Rand.z * StepSizePixels + 1.0);

        for (float StepIndex = 0; StepIndex < _NUM_STEPS; ++StepIndex)
        {
            float2 SnappedUV = round(RayPixels * Direction) / _ScreenParams.xy + FullResUV;
            float3 S = FetchViewPos(SnappedUV);

            // return S;
            RayPixels += StepSizePixels;
            AO += ComputeAO(ViewPosition, ViewNormal, S);
        }
    }

    AO /= _NUM_DIRECTIONS * _NUM_STEPS;
    return clamp(1.0 - AO * 2, 0.0, 1.0);
}

half4 HBAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
    float3 ViewPosition = FetchViewPos(uv);
    float3 worldNormal = SampleNormal(uv);
    float3 ViewNormal = TransformWorldToViewNormal(worldNormal);

    ViewNormal.z = -ViewNormal.z;

    float RadiusPixels = _RadiusToScreen / ViewPosition.z;
    float4 Rand = GetJitter(uv);
    float AO = ComputeCoarseAO(uv, RadiusPixels, Rand, ViewPosition, ViewNormal);

    AO = pow(AO, 2);

    AO*= INTENSITY;
    // AO = 1 - AO;
    return float4(AO, AO, AO, 1);
}



float2 GetDirection(uint2 positionSS, int offset)
{
    float noise = InterleavedGradientNoise(positionSS.xy, 0);
    float rotations[] = { 60.0, 300.0, 180.0, 240.0, 120.0, 0.0 };

    #if TEMPORAL_ROTATION
    float rotation = (rotations[_AOTemporalRotationIdx] / 360.0);
    #else
    float rotation = (rotations[offset] / 360.0);
    #endif

    noise += rotation;
    noise *= PI;

    return float2(cos(noise), sin(noise));
}


float GetOffset(uint2 positionSS)
{
    // Spatial offset
    float offset = 0.25 * ((positionSS.y - positionSS.x) & 0x3);

    // Temporal offset
    #if ENABLE_TEMPORAL_OFFSET
    float offsets[] = { 0.0, 0.5, 0.25, 0.75 };
    offset += offsets[_AOTemporalOffsetIdx];
    #endif
    return frac(offset);
}
float GetDepthForCentral(float2 positionSS)
{
    return SampleDepth(positionSS/ _ScreenParams.xy);
}


float GetDepthSample(float2 positionSS, bool lowerRes)
{
    return GetDepthForCentral(positionSS);
}

float UpdateHorizon(float maxH, float candidateH, float distSq)
{
    float falloff = saturate((1.0 - (distSq * 1/(_AORadius * _AORadius))));

    return (candidateH > maxH) ? lerp(maxH, candidateH, falloff) : lerp(maxH, candidateH, 0.03f);        // TODO: Thickness heuristic here.
}

float HorizonLoop(float3 positionVS, float3 V, float2 rayStart, float2 rayDir, float rayOffset, float rayStep)
{
    float maxHorizon = -1.0f;  // cos(pi)
    float t = rayOffset * rayStep + rayStep;

    const uint startWithLowerRes = min(max(0, _NUM_STEPS / 2 - 2), 3);
    for (uint i = 0; i < _NUM_STEPS; i++)
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
    float3 normalVS = TransformWorldToViewNormal(worldNormal);
    normalVS.z = -normalVS.z;
    // return float4(normalVS, 1);
    
    
    float offset = GetOffset(posSS);
    // offset = 0;

    // return float4(offset, offset, offset, 1);
    float2 rayStart = posSS;
    // return float4(rayStart / _ScreenParams.xy, 0, 1);
    float integral = 0;
    
    const int dirCount = _NUM_DIRECTIONS;

    float3 V = normalize(-positionVS);
    // return float4(positionVS, 1);
    float fovCorrectedradiusSS = clamp(_AORadius * _ScreenParams.y * 0.25f* rcp(positionVS.z), _NUM_STEPS, _RadiusToScreen);

    fovCorrectedradiusSS =  _RadiusToScreen / positionVS.z;
    // fovCorrectedradiusSS /= _RadiusToScreen;
 // return float4(fovCorrectedradiusSS,fovCorrectedradiusSS,fovCorrectedradiusSS, 1);

    float step = max(1, fovCorrectedradiusSS * (1/_NUM_STEPS + 1));
    
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
