//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef BUILTINDATA_CS_HLSL
#define BUILTINDATA_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Builtin+BuiltinData:  static fields
//
#define DEBUGVIEW_BUILTIN_BUILTINDATA_OPACITY (100)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_BAKE_DIFFUSE_LIGHTING (101)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOW_MASK_0 (102)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOW_MASK_1 (103)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOW_MASK_2 (104)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOW_MASK_3 (105)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_EMISSIVE_COLOR (106)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_VELOCITY (107)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION (108)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION_BLUR (109)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET (110)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Builtin+LightTransportData:  static fields
//
#define DEBUGVIEW_BUILTIN_LIGHTTRANSPORTDATA_DIFFUSE_COLOR (150)
#define DEBUGVIEW_BUILTIN_LIGHTTRANSPORTDATA_EMISSIVE_COLOR (151)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Builtin+BuiltinData
// PackingRules = Exact
struct BuiltinData
{
    float opacity;
    float3 bakeDiffuseLighting;
    float shadowMask0;
    float shadowMask1;
    float shadowMask2;
    float shadowMask3;
    float3 emissiveColor;
    float2 velocity;
    float2 distortion;
    float distortionBlur;
    float depthOffset;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Builtin+LightTransportData
// PackingRules = Exact
struct LightTransportData
{
    float3 diffuseColor;
    float3 emissiveColor;
};

//
// Debug functions
//
void GetGeneratedBuiltinDataDebug(uint paramId, BuiltinData builtindata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_BUILTIN_BUILTINDATA_OPACITY:
            result = builtindata.opacity.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_BAKE_DIFFUSE_LIGHTING:
            result = builtindata.bakeDiffuseLighting;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOW_MASK_0:
            result = builtindata.shadowMask0.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOW_MASK_1:
            result = builtindata.shadowMask1.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOW_MASK_2:
            result = builtindata.shadowMask2.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOW_MASK_3:
            result = builtindata.shadowMask3.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_EMISSIVE_COLOR:
            result = builtindata.emissiveColor;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_VELOCITY:
            result = float3(builtindata.velocity, 0.0);
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION:
            result = float3(builtindata.distortion, 0.0);
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION_BLUR:
            result = builtindata.distortionBlur.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET:
            result = builtindata.depthOffset.xxx;
            break;
    }
}

//
// Debug functions
//
void GetGeneratedLightTransportDataDebug(uint paramId, LightTransportData lighttransportdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_BUILTIN_LIGHTTRANSPORTDATA_DIFFUSE_COLOR:
            result = lighttransportdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_BUILTIN_LIGHTTRANSPORTDATA_EMISSIVE_COLOR:
            result = lighttransportdata.emissiveColor;
            break;
    }
}


#endif
