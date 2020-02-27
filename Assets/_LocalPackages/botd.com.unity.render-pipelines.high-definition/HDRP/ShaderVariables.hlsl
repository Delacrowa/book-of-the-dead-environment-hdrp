// UNITY_SHADER_NO_UPGRADE

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
#define UNITY_SHADER_VARIABLES_INCLUDED

#include "ShaderConfig.cs.hlsl"

// CAUTION:
// Currently the shaders compiler always include regualr Unity shaderVariables, so I get a conflict here were UNITY_SHADER_VARIABLES_INCLUDED is already define, this need to be fixed.
// As I haven't change the variables name yet, I simply don't define anything, and I put the transform function at the end of the file outside the guard header.
// This need to be fixed.

#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define USING_STEREO_MATRICES
#endif

#if defined(USING_STEREO_MATRICES)
    #define glstate_matrix_projection unity_StereoMatrixP[unity_StereoEyeIndex]
    #define unity_MatrixV unity_StereoMatrixV[unity_StereoEyeIndex]
    #define unity_MatrixInvV unity_StereoMatrixInvV[unity_StereoEyeIndex]
    #define unity_MatrixVP unity_StereoMatrixVP[unity_StereoEyeIndex]

    #define unity_CameraProjection unity_StereoCameraProjection[unity_StereoEyeIndex]
    #define unity_CameraInvProjection unity_StereoCameraInvProjection[unity_StereoEyeIndex]
    #define unity_WorldToCamera unity_StereoWorldToCamera[unity_StereoEyeIndex]
    #define unity_CameraToWorld unity_StereoCameraToWorld[unity_StereoEyeIndex]
    #define _WorldSpaceCameraPos unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex]
#endif

#define UNITY_LIGHTMODEL_AMBIENT (glstate_lightmodel_ambient * 2)

// ----------------------------------------------------------------------------

//  *********************************************************
//  *                                                       *
//  *  UnityPerCameraRare has been deprecated. Do NOT use!  *
//  *         Please refer to UnityPerView instead.         *
//  *                                                       *
//  *********************************************************

CBUFFER_START(UnityPerCameraRare)
    // DEPRECATED: use _FrustumPlanes
    float4 unity_CameraWorldClipPlanes[6];

#if !defined(USING_STEREO_MATRICES)
    // Projection matrices of the camera. Note that this might be different from projection matrix
    // that is set right now, e.g. while rendering shadows the matrices below are still the projection
    // of original camera.
    // DEPRECATED: use _ProjMatrix, _InvProjMatrix, _ViewMatrix, _InvViewMatrix
    float4x4 unity_CameraProjection;
    float4x4 unity_CameraInvProjection;
    float4x4 unity_WorldToCamera;
    float4x4 unity_CameraToWorld;
#endif
CBUFFER_END

// ----------------------------------------------------------------------------

CBUFFER_START(UnityPerDraw)

    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
    float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    // SH lighting environment
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    // x = Disabled(0)/Enabled(1)
    // y = Computation are done in global space(0) or local space(1)
    // z = Texel size on U texture coordinate
    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float3 unity_ProbeVolumeSizeInv;
    float3 unity_ProbeVolumeMin;

    // This contain occlusion factor from 0 to 1 for dynamic objects (no SH here)
    float4 unity_ProbesOcclusion;

    // Velocity
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
    //X : Use last frame positions (right now skinned meshes are the only objects that use this
    //Y : Force No Motion
    //Z : Z bias value
    float4 unity_MotionVectorsParams;

CBUFFER_END

#if defined(USING_STEREO_MATRICES)
CBUFFER_START(UnityStereoGlobals)
    float4x4 unity_StereoMatrixP[2];
    float4x4 unity_StereoMatrixV[2];
    float4x4 unity_StereoMatrixInvV[2];
    float4x4 unity_StereoMatrixVP[2];

    float4x4 unity_StereoCameraProjection[2];
    float4x4 unity_StereoCameraInvProjection[2];
    float4x4 unity_StereoWorldToCamera[2];
    float4x4 unity_StereoCameraToWorld[2];

    float3 unity_StereoWorldSpaceCameraPos[2];
    float4 unity_StereoScaleOffset[2];
CBUFFER_END
#endif

#if defined(USING_STEREO_MATRICES) && defined(UNITY_STEREO_MULTIVIEW_ENABLED)
CBUFFER_START(UnityStereoEyeIndices)
    float4 unity_StereoEyeIndices[2];
CBUFFER_END
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
    #define unity_StereoEyeIndex UNITY_VIEWID
    UNITY_DECLARE_MULTIVIEW(2);
#elif defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    static uint unity_StereoEyeIndex;
#elif defined(UNITY_SINGLE_PASS_STEREO)
    CBUFFER_START(UnityStereoEyeIndex)
        int unity_StereoEyeIndex;
    CBUFFER_END
#endif

CBUFFER_START(UnityPerDrawRare)
    float4x4 glstate_matrix_transpose_modelview0;
CBUFFER_END

// ----------------------------------------------------------------------------

CBUFFER_START(UnityPerFrame)
    float4 glstate_lightmodel_ambient;
    float4 unity_AmbientSky;
    float4 unity_AmbientEquator;
    float4 unity_AmbientGround;
    float4 unity_IndirectSpecColor;

#if !defined(USING_STEREO_MATRICES)
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;
    float4 unity_StereoScaleOffset;
    int unity_StereoEyeIndex;
#endif

    float4 unity_ShadowColor;
CBUFFER_END

// ----------------------------------------------------------------------------

// These are the samplers available in the HDRenderPipeline.
// Avoid declaring extra samplers as they are 4x SGPR each on GCN.
SAMPLER(s_point_clamp_sampler);
SAMPLER(s_linear_clamp_sampler);
SAMPLER(s_linear_repeat_sampler);
SAMPLER(s_trilinear_clamp_sampler);

// ----------------------------------------------------------------------------

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);

// Dynamic GI lightmap
TEXTURE2D(unity_DynamicLightmap);
SAMPLER(samplerunity_DynamicLightmap);

TEXTURE2D(unity_DynamicDirectionality);

// We can have shadowMask only if we have lightmap, so no sampler
TEXTURE2D(unity_ShadowMask);

// TODO: Change code here so probe volume use only one transform instead of all this parameters!
TEXTURE3D(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

// ----------------------------------------------------------------------------

// Important: please use macros or functions to access the CBuffer data.
// The member names and data layout can (and will) change!
CBUFFER_START(UnityPerView)
    // TODO: all affine matrices should be 3x4.
    float4x4 _ViewMatrix;
    float4x4 _InvViewMatrix;
    float4x4 _ProjMatrix;
    float4x4 _InvProjMatrix;
    float4x4 _ViewProjMatrix;
    float4x4 _InvViewProjMatrix;
    float4x4 _NonJitteredViewProjMatrix;
    float4x4 _PrevViewProjMatrix;       // non-jittered

    // TODO: put commonly used vars together (below), and then sort them by the frequency of use (descending).
    // Note: a matrix is 4 * 4 * 4 = 64 bytes (1x cache line), so no need to sort those.
#if defined(USING_STEREO_MATRICES)
    float3 _Align16;
#else
    float3 _WorldSpaceCameraPos;
#endif
    float  _DetViewMatrix;              // determinant(_ViewMatrix)
    float4 _ScreenSize;                 // { w, h, 1 / w, 1 / h }
    float4 _ScreenToTargetScale;        // { w / RTHandle.maxWidth, h / RTHandle.maxHeight } : xy = currFrame, zw = prevFrame

    // Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
    // x = 1 - f/n
    // y = f/n
    // z = 1/f - 1/n
    // w = 1/n
    // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
    // x = -1 + f/n
    // y = 1
    // z = -1/n + -1/f
    // w = 1/f
    float4 _ZBufferParams;

    // x = 1 or -1 (-1 if projection is flipped)
    // y = near plane
    // z = far plane
    // w = 1/far plane
    float4 _ProjectionParams;

    // x = orthographic camera's width
    // y = orthographic camera's height
    // z = unused
    // w = 1.0 if camera is ortho, 0.0 if perspective
    float4 unity_OrthoParams;

    // x = width
    // y = height
    // z = 1 + 1.0/width
    // w = 1 + 1.0/height
    float4 _ScreenParams;

    float4 _FrustumPlanes[6];           // { (a, b, c) = N, d = -dot(N, P) } [L, R, T, B, N, F]

    // TAA Frame Index ranges from 0 to 7. This gives you two rotations per cycle.
    float4 _TaaFrameRotation;           // { sin(taaFrame * PI/2), cos(taaFrame * PI/2), 0, 0 }
    // t = animateMaterials ? Time.realtimeSinceStartup : 0.
    float4 _Time;                       // { t/20, t, t*2, t*3 }
    float4 _LastTime;                   // { t/20, t, t*2, t*3 }
    float4 _SinTime;                    // { sin(t/8), sin(t/4), sin(t/2), sin(t) }
    float4 _CosTime;                    // { cos(t/8), cos(t/4), cos(t/2), cos(t) }
    float4 unity_DeltaTime;             // { dt, 1/dt, smoothdt, 1/smoothdt }
    int _FrameCount;

    // Volumetric lighting.
    float4 _AmbientProbeCoeffs[7];      // 3 bands of SH, packed, rescaled and convolved with the phase function
    float  _GlobalAnisotropy;
    float3 _GlobalScattering;
    float  _GlobalExtinction;
    float4 _VBufferResolution;          // { w, h, 1/w, 1/h }
    float4 _VBufferSliceCount;          // { count, 1/count, 0, 0 }
    float4 _VBufferUvScaleAndLimit;     // Necessary us to work with sub-allocation (resource aliasing) in the RTHandle system
    float4 _VBufferDepthEncodingParams; // See the call site for description
    float4 _VBufferDepthDecodingParams; // See the call site for description

    // TODO: these are only used for reprojection.
    // Once reprojection is performed in a separate pass, we should probably
    // move these to a dedicated CBuffer to avoid polluting the global one.
    float4 _VBufferPrevResolution;
    float4 _VBufferPrevSliceCount;
    float4 _VBufferPrevUvScaleAndLimit;
    float4 _VBufferPrevDepthEncodingParams;
    float4 _VBufferPrevDepthDecodingParams;
CBUFFER_END

CBUFFER_START(UnityLightingParameters)
// Buffer pyramid
float4  _ColorPyramidSize;              // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
float4  _DepthPyramidSize;              // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
float4  _CameraMotionVectorsSize;       // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
float4  _ColorPyramidScale;             // (x,y) = Screen Scale, z = lod count, w = unused
float4  _DepthPyramidScale;             // (x,y) = Screen Scale, z = lod count, w = unused
float4  _CameraMotionVectorsScale;      // (x,y) = Screen Scale, z = lod count, w = unused

                                // Screen space lighting
float   _SSRefractionInvScreenWeightDistance;     // Distance for screen space smoothstep with fallback
float   _SSReflectionInvScreenWeightDistance;     // Distance for screen space smoothstep with fallback
int     _SSReflectionEnabled;
int     _SSReflectionProjectionModel;
int     _SSReflectionHiZRayMarchBehindObject;
int     _SSRefractionHiZRayMarchBehindObject;

                                                // Ambiant occlusion
float4 _AmbientOcclusionParam; // xyz occlusion color, w directLightStrenght
CBUFFER_END

// Rough refraction texture
// Color pyramid (width, height, lodcount, Unused)
TEXTURE2D(_ColorPyramidTexture);
// Depth pyramid (width, height, lodcount, Unused)
TEXTURE2D(_DepthPyramidTexture);
// Ambient occlusion texture
TEXTURE2D(_AmbientOcclusionTexture);
TEXTURE2D(_CameraMotionVectorsTexture);

// Custom generated by HDRP, not from Unity Engine (passed in via HDCamera)
#if defined(USING_STEREO_MATRICES)

CBUFFER_START(UnityPerPassStereo)
float4x4 _ViewMatrixStereo[2];
// Proj not needed...yet?
float4x4 _ViewProjMatrixStereo[2];
float4x4 _InvViewMatrixStereo[2];
float4x4 _InvProjMatrixStereo[2];
float4x4 _InvViewProjMatrixStereo[2];
CBUFFER_END

#endif // USING_STEREO_MATRICES

float4x4 OptimizeProjectionMatrix(float4x4 M)
{
    // Matrix format (x = non-constant value).
    // Orthographic Perspective  Combined(OR)
    // | x 0 0 x |  | x 0 x 0 |  | x 0 x x |
    // | 0 x 0 x |  | 0 x x 0 |  | 0 x x x |
    // | x x x x |  | x x x x |  | x x x x | <- oblique projection row
    // | 0 0 0 1 |  | 0 0 x 0 |  | 0 0 x x |
    // Notice that some values are always 0.
    // We can avoid loading and doing math with constants.
    M._21_41 = 0;
    M._12_42 = 0;
    return M;
}

// Helper to handle camera relative space

float4x4 ApplyCameraTranslationToMatrix(float4x4 modelMatrix)
{
    // To handle camera relative rendering we substract the camera position in the model matrix
    // User must not use UNITY_MATRIX_M directly, unless they understand what they do
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    modelMatrix._m03_m13_m23 -= _WorldSpaceCameraPos;
#endif
    return modelMatrix;
}

float4x4 ApplyCameraTranslationToInverseMatrix(float4x4 inverseModelMatrix)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    // To handle camera relative rendering we need to apply translation before converting to object space
    float4x4 translationMatrix = { { 1.0, 0.0, 0.0, _WorldSpaceCameraPos.x },{ 0.0, 1.0, 0.0, _WorldSpaceCameraPos.y },{ 0.0, 0.0, 1.0, _WorldSpaceCameraPos.z },{ 0.0, 0.0, 0.0, 1.0 } };
    return mul(inverseModelMatrix, translationMatrix);
#else
    return inverseModelMatrix;
#endif
}

#ifdef USE_LEGACY_UNITY_MATRIX_VARIABLES
    #include "ShaderVariablesMatrixDefsLegacyUnity.hlsl"
#else
    #include "ShaderVariablesMatrixDefsHDCamera.hlsl"
#endif


// This define allow to tell to unity instancing that we will use our camera relative functions (ApplyCameraTranslationToMatrix and  ApplyCameraTranslationToInverseMatrix) for the model view matrix
#define MODIFY_MATRIX_FOR_CAMERA_RELATIVE_RENDERING
#include "CoreRP/ShaderLibrary/UnityInstancing.hlsl"
#include "ShaderVariablesFunctions.hlsl"

#endif // UNITY_SHADER_VARIABLES_INCLUDED
