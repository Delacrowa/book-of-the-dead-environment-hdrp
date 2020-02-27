Shader "Volund/Atmospheric Scattering Sky" {

HLSLINCLUDE

#pragma vertex vert
#pragma fragment frag

#pragma multi_compile _ ATMOSPHERICS ATMOSPHERICS_PER_PIXEL
//#pragma multi_compile _ ATMOSPHERICS_OCCLUSION_FULLSKY
#pragma multi_compile _ ATMOSPHERICS_DEBUG

#pragma only_renderers d3d11 ps4 xboxone vulkan metal

#include "CoreRP/ShaderLibrary/Common.hlsl"
#include "HDRP/ShaderVariables.hlsl"

// Stuff from UnityCG.cginc
//

inline float4 ComputeNonStereoScreenPos(float4 pos) {
	float4 o = pos * 0.5f;
	o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
	o.zw = pos.zw;
	return o;
}

inline float4 ComputeScreenPos(float4 pos) {
	float4 o = ComputeNonStereoScreenPos(pos);
#if defined(UNITY_SINGLE_PASS_STEREO)
	o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
#endif
	return o;
}

#define unity_ColorSpaceDouble float4(4.59479380, 4.59479380, 4.59479380, 2.0)

inline float Luminance(float3 rgb) {
	#define unity_ColorSpaceLuminance float4(0.0396819152, 0.458021790, 0.00609653955, 1.0) // Legacy: alpha is set to 1.0 to specify linear mode
	return dot(rgb, unity_ColorSpaceLuminance.rgb);
}

// Decodes HDR textures
inline float3 DecodeHDR(float4 data, float4 decodeInstructions) {
	// Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
	float alpha = decodeInstructions.w * (data.a - 1.0) + 1.0;

	// If Linear mode is not supported we can skip exponent part
#   if defined(UNITY_USE_NATIVE_HDR)
	return decodeInstructions.x * data.rgb; // Multiplier for future HDRI relative to absolute conversion.
#   else
	return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * data.rgb;
#   endif
}

//
//

struct v2f {
	float4 hPos				: SV_POSITION;
#if defined(ATMOSPHERICS)
	float4 c1				: TEXCOORD1;
	float3 c2				: TEXCOORD2;
#endif
	float4 worldViewVec		: TEXCOORD4;
	float3 worldViewVecRot	: TEXCOORD5;
#if defined(ATMOSPHERICS_PER_PIXEL)
	float3 worldPos			: TEXCOORD6;
#endif
#if defined(ATMOSPHERICS_OCCLUSION_FULLSKY)
	float4 screenPos		: TEXCOORD7;
#endif
};

uniform float3		u_SkyDomeOffset;
uniform float4x4	u_SkyDomeRotation;
uniform float3		u_SkyDomeScale;
uniform samplerCUBE	u_SkyDomeCube;
uniform half4		u_SkyDomeCube_HDR;
uniform float		u_SkyDomeExposure;
uniform float4		u_SkyDomeTint;
uniform float		u_SkyDomeClipHeight;
uniform float2 		u_ShadowBiasSkyRayleighMie;

#define IS_RENDERING_SKY
#include "AtmosphericScattering.cginc"

struct a2v {
	float4 vertex	: POSITION;
};

v2f vert(a2v IN) {
	v2f OUT;
	
	float4 worldPos = IN.vertex * float4(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][2], 1.f);
#if defined(ATMOSPHERICS_PER_PIXEL)
	OUT.worldPos = worldPos.xyz;
#endif

	worldPos.xyz *= float3(u_SkyDomeScale.x, worldPos.y > 0.f ? u_SkyDomeScale.y : 1.f, u_SkyDomeScale.z);
	
	float3 worldViewVec = worldPos.xyz;
	worldPos.xyz += _WorldSpaceCameraPos.xyz;
	
	OUT.hPos = TransformWorldToHClip(GetCameraRelativePositionWS(worldPos.xyz));

#if defined(UNITY_REVERSED_Z)
	OUT.hPos.z = 1e-5f;
#else
	OUT.hPos.z = OUT.hPos.w - 1e-5f;
#endif
	OUT.worldViewVec.xyz = normalize(worldViewVec);
	OUT.worldViewVec.w = dot(OUT.worldViewVec.xyz, u_SunDirection);
	OUT.worldViewVecRot = mul((float3x3)u_SkyDomeRotation, OUT.worldViewVec.xyz);
	
#if defined(ATMOSPHERICS)
	float4 c1, c2, c3;
	_VolundTransferScatter(worldPos.xyz, c1, c2, c3);
	OUT.c1.rgb = c1.rgb + c3.rgb;
	OUT.c1.a = max(0.f, 1.f - c1.a - c3.a);
	OUT.c2 = c2.rgb;
	
#ifdef ATMOSPHERICS_DEBUG
	if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_RAYLEIGH)
		OUT.c1 = c1;
	else if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_MIE)
		OUT.c1 = c2;
	else if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_HEIGHT)
		OUT.c1 = c3;
#endif
#endif

#if defined(ATMOSPHERICS_OCCLUSION_FULLSKY)
	OUT.screenPos = ComputeScreenPos(OUT.hPos);
#endif

	return OUT;
}

float4 frag(v2f IN) : SV_Target {
#if defined(ATMOSPHERICS_PER_PIXEL)
	IN.worldPos *= float3(u_SkyDomeScale.x, IN.worldPos.y > 0.f ? u_SkyDomeScale.y : 1.f, u_SkyDomeScale.z);
	IN.worldViewVec.xyz = IN.worldPos;
	IN.worldPos.xyz += _WorldSpaceCameraPos.xyz;
	IN.worldViewVec.xyz = normalize(IN.worldViewVec.xyz);
	IN.worldViewVec.w = dot(IN.worldViewVec.xyz, u_SunDirection);
	IN.worldViewVecRot = mul((float3x3)u_SkyDomeRotation, IN.worldViewVec.xyz);
#endif

	float3 uvThingy = IN.worldViewVecRot;
	float4 skyDome = texCUBE(u_SkyDomeCube, uvThingy.xyz);
	skyDome.rgb = DecodeHDR(skyDome, u_SkyDomeCube_HDR);
	skyDome.rgb *= u_SkyDomeTint.rgb * unity_ColorSpaceDouble.rgb * u_SkyDomeExposure;

	float2 occlusion = float2(1,1);
#ifdef ATMOSPHERICS_OCCLUSION_FULLSKY
	occlusion.x = UNITY_SAMPLE_TEX2D(u_OcclusionTexture, IN.screenPos.xy/IN.screenPos.w).r;
	occlusion.xy = saturate(occlusion.xx + u_ShadowBiasSkyRayleighMie.xy);
#endif

	float extinction = 1.f;
	float3 scatter = float3(0,0,0);
	
#if defined(ATMOSPHERICS_PER_PIXEL)
	float4 c1, c2, c3;
	_VolundTransferScatter(IN.worldPos, c1, c2, c3);
	
	float4 coord1;
	coord1.rgb = c1.rgb + c3.rgb;
	coord1.a = max(0.f, 1.f - c1.a - c3.a);
	float3 coord2 = c2.rgb;
	
	float sunCos = dot(normalize(IN.worldViewVec.xyz), u_SunDirection);
	float miePh = miePhase(sunCos, u_MiePhaseAnisotropy);

#ifdef ATMOSPHERICS_DEBUG
	if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_RAYLEIGH)
		return c1;
	else if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_MIE)
		return c2 * miePh;
	else if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_HEIGHT)
		return c3;
#endif

	extinction = coord1.a;
	scatter = coord1.rgb * occlusion.x + coord2 * miePh * occlusion.y;
#elif defined(ATMOSPHERICS) 
	float sunCos = IN.worldViewVec.w;
	float miePh = miePhase(sunCos, u_MiePhaseAnisotropy);
	
#ifdef ATMOSPHERICS_DEBUG
	if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_RAYLEIGH || u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_HEIGHT)
		return IN.c1;
	else if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_MIE)
		return IN.c1 * miePh;
#endif

	extinction = IN.c1.a;
	scatter = IN.c1.rgb * occlusion.x + IN.c2 * miePh * occlusion.y;
#endif

#ifdef ATMOSPHERICS_DEBUG
	if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_SCATTERING || u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_OCCLUDEDSCATTERING)
		return float4(scatter, 0);
	else if(u_AtmosphericsDebugMode == ATMOSPHERICS_DBG_OCCLUSION)
		return occlusion.x;
#endif

	// Alpha = 1, since TAA excludes anything with alpha 0
	return float4(skyDome.rgb * extinction + scatter, 1.f);
}
ENDHLSL


SubShader {
	Tags { "Queue" = "AlphaTest+24" }
	ZWrite Off

	Pass {
		// This makes the pass be scheduled in the always-on opaque-only forward pass. (after deferred and regular forward)
        Tags { "LightMode" = "ForwardOnly" }

		HLSLPROGRAM
		ENDHLSL
	}
}

Fallback off
}

