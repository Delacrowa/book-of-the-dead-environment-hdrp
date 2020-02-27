#ifndef UNITY_BUILTIN_DATA_INCLUDED
#define UNITY_BUILTIN_DATA_INCLUDED

//-----------------------------------------------------------------------------
// BuiltinData
// This structure include common data that should be present in all material
// and are independent from the BSDF parametrization.
// Note: These parameters can be store in GBuffer if the writer wants
//-----------------------------------------------------------------------------

#include "BuiltinData.cs.hlsl"

//-----------------------------------------------------------------------------
// common Encode/Decode functions
//-----------------------------------------------------------------------------
struct BakeLightingData
{
    float3 bakeDiffuseLighting;
#ifdef SHADOWS_SHADOWMASK
    float4 bakeShadowMask;
#endif
};
// Guideline for velocity buffer.
// We support various architecture for HDRenderPipeline
// - Forward only rendering
// - Hybrid forward/deferred opaque
// - Regular deferred
// The velocity buffer is potentially fill in several pass.
// - In gbuffer pass with extra RT
// - In forward opaque pass (Can happen even when deferred) with MRT
// - In dedicated velocity pass
// Also the velocity buffer is only fill in case of dynamic or deformable objects, static case can use camera reprojection to retrieve motion vector (<= TODO: this may be false with TAA due to jitter matrix)
// or just previous and current transform

// So here we decide the following rules:
// - A deferred material can't override the velocity buffer format of builtinData, must use appropriate function
// - If velocity buffer is enable in deferred material it is the last one
// - Velocity buffer can be optionally enabled (either in forward or deferred)
// - Velocity data can't be pack with other properties
// - Same velocity buffer is use for all scenario, so if deferred define a velocity buffer, the same is reuse for forward case.
// For these reasons we chose to avoid to pack velocity buffer with anything else in case of PackgbufferInFP16 (and also in case the format change)

// Encode/Decode shadowmask/velocity/distortion in a buffer (either forward of deferred)

// Design note: We assume that shadowmask/velocity/distortion fit into a single buffer (i.e not spread on several buffer)
void EncodeShadowMask(float4 shadowMask, out float4 outBuffer)
{
    // RT - RGBA
    outBuffer = shadowMask;
}

void DecodeShadowMask(float4 inBuffer, out float4 shadowMask)
{
    shadowMask = inBuffer;
}

// TODO: CAUTION: current DecodeVelocity is not used in motion vector / TAA pass as it come from Postprocess stack
// This will be fix when postprocess will be integrated into HD, but it mean that we must not change the
// EncodeVelocity / DecodeVelocity code for now, i.e it must do nothing like it is doing currently.
// Note2: Motion blur code of posptrocess stack do * 2 - 1 to uncompress velocity which is not expected, TAA is correct.
// Design note: We assume that velocity/distortion fit into a single buffer (i.e not spread on several buffer)
void EncodeVelocity(float2 velocity, out float4 outBuffer)
{
    // RT - 16:16 float
    outBuffer = float4(velocity.xy, 0.0, 0.0);
}

void DecodeVelocity(float4 inBuffer, out float2 velocity)
{
    velocity = inBuffer.xy;
}

void EncodeDistortion(float2 distortion, float distortionBlur, bool isValidSource, out float4 outBuffer)
{
    // RT - 16:16:16:16 float
    // distortionBlur in alpha for a different blend mode
    outBuffer = float4(distortion, isValidSource, distortionBlur);
}

void DecodeDistortion(float4 inBuffer, out float2 distortion, out float distortionBlur, out bool isValidSource)
{
    distortion = inBuffer.xy;
    distortionBlur = inBuffer.a;
    isValidSource = (inBuffer.z != 0.0);
}

void GetBuiltinDataDebug(uint paramId, BuiltinData builtinData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBuiltinDataDebug(paramId, builtinData, result, needLinearToSRGB);

    switch (paramId)
    {
    case DEBUGVIEW_BUILTIN_BUILTINDATA_BAKE_DIFFUSE_LIGHTING:
        // TODO: require a remap
        // TODO: we should not gamma correct, but easier to debug for now without correct high range value
        result = builtinData.bakeDiffuseLighting; needLinearToSRGB = true;
        break;
    case DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET:
        result = builtinData.depthOffset.xxx * 10.0; // * 10 assuming 1 unity is 1m
        break;
    case DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION:
        result = float3((builtinData.distortion / (abs(builtinData.distortion) + 1) + 1) * 0.5, 0.5);
        break;
    }
}

void GetLightTransportDataDebug(uint paramId, LightTransportData lightTransportData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedLightTransportDataDebug(paramId, lightTransportData, result, needLinearToSRGB);

    switch (paramId)
    {
    case DEBUGVIEW_BUILTIN_LIGHTTRANSPORTDATA_EMISSIVE_COLOR:
        // TODO: Need a tonemap ?
        result = lightTransportData.emissiveColor;
        break;
    }
}

#endif // UNITY_BUILTIN_DATA_INCLUDED
