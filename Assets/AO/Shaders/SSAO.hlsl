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

// Sample point picker
half3 PickSamplePoint(float2 uv, half sampleIndexHalf, half rcpSampleCount, half3 normal_o)
{
    const half lerpVal = sampleIndexHalf * rcpSampleCount;
    const half noise = SAMPLE_BLUE_NOISE(((uv + BlueNoiseOffset) * BlueNoiseScale) + lerpVal);
    const half u = frac(GetRandomVal(HALF_ZERO, sampleIndexHalf).x + noise) * HALF_TWO - HALF_ONE;
    const half theta = (GetRandomVal(HALF_ONE, sampleIndexHalf).x + noise) * HALF_TWO_PI * HALF_HUNDRED;
    const half u2 = half(sqrt(HALF_ONE - u * u));

    half3 v = half3(u2 * cos(theta), u2 * sin(theta), u);
    v *= (dot(normal_o, v) >= HALF_ZERO) * HALF_TWO - HALF_ONE;
    v *= lerp(0.1, 1.0, lerpVal * lerpVal);
    v *= RADIUS;
    return v;
}

float SampleDepth(float2 uv)
{
    return SampleSceneDepth(uv.xy);
}

float GetLinearEyeDepth(float rawDepth)
{
    // #if defined(_ORTHOGRAPHIC)
    //     return LinearDepthToEyeDepth(rawDepth);
    // #else
    return LinearEyeDepth(rawDepth, _ZBufferParams);
    // #endif
}

float SampleAndGetLinearEyeDepth(float2 uv)
{
    const float rawDepth = SampleDepth(uv);
    return GetLinearEyeDepth(rawDepth);
}

// This returns a vector in world unit (not a position), from camera to the given point described by uv screen coordinate and depth (in absolute world unit).
half3 ReconstructViewPos(float2 uv, float linearDepth)
{
    // #if defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    //     uv = RemapFoveatedRenderingNonUniformToLinear(uv);
    // #endif

    // Screen is y-inverted.
    uv.y = 1.0 - uv.y;

    // view pos in world space
    // #if defined(_ORTHOGRAPHIC)
    //     float zScale = linearDepth * _ProjectionParams.w; // divide by far plane
    //     float3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
    //                         + _CameraViewXExtent[unity_eyeIndex].xyz * uv.x
    //                         + _CameraViewYExtent[unity_eyeIndex].xyz * uv.y
    //                         + _CameraViewZExtent[unity_eyeIndex].xyz * zScale;
    // #else
    float zScale = linearDepth * _ProjectionParams2.x; // divide by near plane
    float3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
        + _CameraViewXExtent[unity_eyeIndex].xyz * uv.x
        + _CameraViewYExtent[unity_eyeIndex].xyz * uv.y;
    viewPos *= zScale;
    // #endif

    return half3(viewPos);
}

// Try reconstructing normal accurately from depth buffer.
// Low:    DDX/DDY on the current pixel
// Medium: 3 taps on each direction | x | * | y |
// High:   5 taps on each direction: | z | x | * | y | w |
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
// half3 ReconstructNormal(float2 uv, float linearDepth, float3 vpos, float2 pixelDensity)
// {
//     #if defined(_SOURCE_DEPTH_LOW)
//         return half3(normalize(cross(ddy(vpos), ddx(vpos))));
//     #else
//         float2 delta = float2(_SourceSize.zw * 2.0);
//
//         pixelDensity = rcp(pixelDensity);
//
//         // Sample the neighbour fragments
//         float2 lUV = float2(-delta.x, 0.0) * pixelDensity;
//         float2 rUV = float2(delta.x, 0.0) * pixelDensity;
//         float2 uUV = float2(0.0, delta.y) * pixelDensity;
//         float2 dUV = float2(0.0, -delta.y) * pixelDensity;
//
//         float3 l1 = float3(uv + lUV, 0.0); l1.z = SampleAndGetLinearEyeDepth(l1.xy); // Left1
//         float3 r1 = float3(uv + rUV, 0.0); r1.z = SampleAndGetLinearEyeDepth(r1.xy); // Right1
//         float3 u1 = float3(uv + uUV, 0.0); u1.z = SampleAndGetLinearEyeDepth(u1.xy); // Up1
//         float3 d1 = float3(uv + dUV, 0.0); d1.z = SampleAndGetLinearEyeDepth(d1.xy); // Down1
//
//         // Determine the closest horizontal and vertical pixels...
//         // horizontal: left = 0.0 right = 1.0
//         // vertical  : down = 0.0    up = 1.0
//         #if defined(_SOURCE_DEPTH_MEDIUM)
//              uint closest_horizontal = l1.z > r1.z ? 0 : 1;
//              uint closest_vertical   = d1.z > u1.z ? 0 : 1;
//         #else
//             float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearEyeDepth(l2.xy); // Left2
//             float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearEyeDepth(r2.xy); // Right2
//             float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearEyeDepth(u2.xy); // Up2
//             float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearEyeDepth(d2.xy); // Down2
//
//             const uint closest_horizontal = abs( (2.0 * l1.z - l2.z) - linearDepth) < abs( (2.0 * r1.z - r2.z) - linearDepth) ? 0 : 1;
//             const uint closest_vertical   = abs( (2.0 * d1.z - d2.z) - linearDepth) < abs( (2.0 * u1.z - u2.z) - linearDepth) ? 0 : 1;
//         #endif
//
//         // Calculate the triangle, in a counter-clockwize order, to
//         // use based on the closest horizontal and vertical depths.
//         // h == 0.0 && v == 0.0: p1 = left,  p2 = down
//         // h == 1.0 && v == 0.0: p1 = down,  p2 = right
//         // h == 1.0 && v == 1.0: p1 = right, p2 = up
//         // h == 0.0 && v == 1.0: p1 = up,    p2 = left
//         // Calculate the view space positions for the three points...
//         half3 P1;
//         half3 P2;
//         if (closest_vertical == 0)
//         {
//             P1 = half3(closest_horizontal == 0 ? l1 : d1);
//             P2 = half3(closest_horizontal == 0 ? d1 : r1);
//         }
//         else
//         {
//             P1 = half3(closest_horizontal == 0 ? u1 : r1);
//             P2 = half3(closest_horizontal == 0 ? l1 : u1);
//         }
//
//         // Use the cross product to calculate the normal...
//         return half3(normalize(cross(ReconstructViewPos(P2.xy, P2.z) - vpos, ReconstructViewPos(P1.xy, P1.z) - vpos)));
//     #endif
// }

half3 SampleNormal(float2 uv)
{
    // #if defined(_SOURCE_DEPTH_NORMALS)
    return half3(SampleSceneNormals(uv));
    // #else
    //     float3 vpos = ReconstructViewPos(uv, linearDepth);
    //     return ReconstructNormal(uv, linearDepth, vpos, pixelDensity);
    // #endif
}


float ComputeAO(float3 p, float3 n, float3 s)
{
    float3 v = s - p;
    float VoV = dot(v, v);
    float NoV = dot(n, v) * rsqrt(VoV);

    return saturate(NoV - 0.1);
}

// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
half4 SSAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord;


    // float noise = SAMPLE_BLUE_NOISE(((uv + BlueNoiseOffset) * BlueNoiseScale));
    //
    // return float4(noise, noise, noise, 1.0);


    // Early Out for Sky...
    float rawDepth_o = SampleDepth(uv);
    if (rawDepth_o < SKY_DEPTH_VALUE)
        return PackAONormal(HALF_ZERO, HALF_ZERO);

    // Early Out for Falloff
    float linearDepth_o = GetLinearEyeDepth(rawDepth_o);
    half halfLinearDepth_o = half(linearDepth_o);
    if (halfLinearDepth_o > FALLOFF)
        return PackAONormal(HALF_ZERO, HALF_ZERO);

    // Normal for this fragment
    half3 normal_o = SampleNormal(uv);

    // float3 normalVS = TransformWorldToViewNormal(normal_o);


    // return float4(normalVS, 1.0);

    // View position for this fragment
    float3 vpos_o = ReconstructViewPos(uv, linearDepth_o);

    // return float4(vpos_o, 1.0);

    // Parameters used in coordinate conversion
    half3 camTransform000102 = half3(_CameraViewProjections[unity_eyeIndex]._m00,
                                     _CameraViewProjections[unity_eyeIndex]._m01,
                                     _CameraViewProjections[unity_eyeIndex]._m02);
    half3 camTransform101112 = half3(_CameraViewProjections[unity_eyeIndex]._m10,
                                     _CameraViewProjections[unity_eyeIndex]._m11,
                                     _CameraViewProjections[unity_eyeIndex]._m12);

    const half rcpSampleCount = half(rcp(SAMPLE_COUNT));
    half ao = HALF_ZERO;
    half sHalf = HALF_MINUS_ONE;


    float noise = SAMPLE_BLUE_NOISE(((uv + BlueNoiseOffset) * BlueNoiseScale));


    float stepSize = 1.0 / SAMPLE_COUNT;
    float stepAngle = HALF_TWO_PI / 4;


    float3 forward = UNITY_MATRIX_V[2];
    float3 right = UNITY_MATRIX_V[0];
    float3 up = UNITY_MATRIX_V[1];
    
    float zDist = half(-dot(UNITY_MATRIX_V[2].xyz, vpos_o));

    UNITY_UNROLL
    for (int d = 0; d < 4; ++d)
    {
        float angle = stepAngle * d;

        float cosAng, sinAng;
        sincos(angle, sinAng, cosAng);
        // 1m 处半径100像素
        // float3 dir = float2(cosAng, sinAng) * 100;

        // float3 dir = float3(0,sinAng,cosAng)*0.1;

        float3 dir = (cosAng* right + sinAng * up)*0.1;
        // dir /= zDist;

        float rayPixels = 0;

        UNITY_UNROLL
        for (int s = 0; s < SAMPLE_COUNT; ++s)
        {
            float3 vpos_s1 = vpos_o + dir * rayPixels;

            
            
            half2 spos_s1 = half2(
                camTransform000102.x * vpos_s1.x + camTransform000102.y * vpos_s1.y + camTransform000102.z * vpos_s1.z,
                camTransform101112.x * vpos_s1.x + camTransform101112.y * vpos_s1.y + camTransform101112.z * vpos_s1.z
            );

            
            float zDist2 = half(-dot(UNITY_MATRIX_V[2].xyz, vpos_s1));
            half2 uv_s1_01 = saturate(half2(spos_s1 * rcp(zDist2) + HALF_ONE) * HALF_HALF);
            
            // float2 uv_s1_01 = (rayPixels * dir) * _ScreenSize.zw + uv;

            

            float rawDepth_s = SampleDepth(uv_s1_01);
            float linearDepth_s = GetLinearEyeDepth(rawDepth_s);
            half3 v_s2 = half3(ReconstructViewPos(uv_s1_01, linearDepth_s));



            // return float4(zDist, zDist, zDist, 1.0);

            half halfLinearDepth_s = half(linearDepth_s);
            half isInsideRadius = length(v_s2 - vpos_o) < 0.5 ? 1.0 : 0.0;

            
            // return float4(rawDepth_s, rawDepth_s, rawDepth_s, 1.0);
            

            

            // return float4(v_s2, 1.0);


            rayPixels += stepSize;
            float tmpAO = ComputeAO(vpos_o, normal_o, v_s2);
            ao += tmpAO * isInsideRadius;
        }
    }

    return 1 - ao / (SAMPLE_COUNT * 4);

    // Intensity normalization
    ao *= RADIUS;

    // Calculate falloff...
    half falloff = HALF_ONE - halfLinearDepth_o * half(rcp(FALLOFF));
    falloff = falloff * falloff;

    // Apply contrast + intensity + falloff^2
    ao = PositivePow(saturate(ao * INTENSITY * falloff * rcpSampleCount), kContrast);

    ao = 1 - ao;

    return float4(ao, ao, ao, HALF_ONE);
    // Return the packed ao + normals
    return PackAONormal(ao, normal_o);
}


#endif //UNIVERSAL_SSAO_INCLUDED
