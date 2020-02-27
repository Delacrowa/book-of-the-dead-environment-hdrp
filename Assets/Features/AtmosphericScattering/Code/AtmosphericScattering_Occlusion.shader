Shader "Hidden/AtmosphericScattering_Occlusion" {
HLSLINCLUDE
	#pragma target 5.0

	#pragma only_renderers d3d11 ps4 xboxone vulkan metal
	
	#pragma multi_compile _ ATMOSPHERICS_OCCLUSION
	#pragma multi_compile _ ATMOSPHERICS_OCCLUSION_JITTER_ALU
	#pragma multi_compile _ ATMOSPHERICS_OCCLUSION_UPSAMPLE_NOISE_L
	#pragma multi_compile _ ATMOSPHERICS_OCCLUSION_RESOLVE_CHEAP

	#include "CoreRP/ShaderLibrary/Common.hlsl"
	#include "HDRP/ShaderVariables.hlsl"
	#include "HDRP/Lighting/LightLoop/ShadowContext.hlsl"

	// Patch up UnityCG.cginc missing things before including AtmosphericScattering.cginc
	inline float Luminance(float3 rgb) { return dot(rgb, float3(0.0396819152, 0.458021790, 0.00609653955)); }
	#include "AtmosphericScattering.cginc"

	uniform float			u_Downscale;
	uniform float2			u_DownscaledScreenSize;
	uniform int				g_AtmosphericScatteringSunShadowIndex;

	uniform float			u_JitterPhase;
	uniform float			u_JitterScale;

	uniform float			u_UpsampleRadius;
	uniform float			u_UpsampleNoiseL;

	uniform float			u_NoisePhase;
	Texture2D				u_NoiseTex;
	SamplerState			sampleru_NoiseTex;
	uniform float4			u_NoiseTex_TexelSize;

	uniform float			u_OcclusionHistorySunMotion;
	uniform float			u_OcclusionHistoryWeight;
	Texture2D				u_OcclusionHistory;
	SamplerState			sampleru_OcclusionHistory;
	SamplerState			sampler_point_clamp;

	SamplerState			sampler_CameraMotionVectorsTexture;

	struct v2f {
		float4 positionCS : SV_POSITION;
		float2 texcoord : TEXCOORD0;
	};
	
	v2f vert(uint vertexID : SV_VertexID) {
		v2f output;
		output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
		output.texcoord = GetNormalizedFullScreenTriangleTexCoord(vertexID);
		return output;
	}

	#define SHADOWS_SHORTCUT 1
	//#define SHADOWS_SHORTCUT_EARLY_OUT 1

	float xGetDirectionalShadowAttenuation(ShadowContext shadowContext, float3 positionWS) {
		// somewhat sub-optimal, we don't really need blending, however there are some shader compiler bugs
		// that makes iterating on this shader near impossible.

		Texture2DArray	        tex = shadowContext.tex2DArray[0];
		SamplerComparisonState	compSamp = shadowContext.compSamplers[0];
		uint			        algo = GPUSHADOWALGORITHM_PCF_1TAP;

		return EvalShadow_CascadedDepth_Blend(shadowContext, algo, tex, compSamp, positionWS, float3(0, 1, 0)/*normalWS*/, g_AtmosphericScatteringSunShadowIndex/*shadowDataIndex*/, float3(0, 0, 0)/*L*/);
	}

/*
	float3 xEvalShadow_GetTexcoords(ShadowData sd, float3 positionWS) {
		float4 posCS = mul(float4(positionWS, 1.0), sd.worldToShadow);
		float3 posNDC = posCS.xyz / posCS.w;

		float3 posTC = posNDC * 0.5 + 0.5;
		posTC.xy = clamp(posTC.xy, sd.texelSizeRcp.zw*0.5, 1.0.xx - sd.texelSizeRcp.zw*0.5);
		posTC.xy = posTC.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;
#if UNITY_REVERSED_Z
		posTC.z = 1.0 - posTC.z;
#endif
		return posTC;
	}
*/

	float xEvalShadow_CascadedDepth(ShadowContext shadowContext, Texture2DArray tex, SamplerComparisonState samp, float3 positionWS, int index, inout int outsideCascades) {
		//TODO: re-optimize helpers

		uint payloadOffset; real alpha;
		int  cascadeCount;
		int shadowSplitIndex = EvalShadow_GetSplitIndex(shadowContext, index, positionWS, payloadOffset, alpha, cascadeCount);

		ShadowData sd = shadowContext.shadowDatas[index];
		EvalShadow_LoadCascadeData(shadowContext, index + 1 + shadowSplitIndex, sd);

		if(shadowSplitIndex < 0) {
#ifdef SHADOWS_SHORTCUT_EARLY_OUT
			++outsideCascades;
#endif
			return 1.0;
		}

		ShadowData shadowData = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

		float3 posTC = /*x*/EvalShadow_GetTexcoords(shadowData, positionWS, false);
		
		return SAMPLE_TEXTURE2D_ARRAY_SHADOW(tex, samp, posTC, shadowData.slice);
	}

	float nrand(float2 uv) {
		return frac(sin(dot(uv, float2(12.9898, 78.233))) * float2(43758.5453, 28001.8384)).x;
	}

	float srand(float2 uv) {
		return 2.0 * nrand(uv) - 1.0;
	}

	float srand_noisetex(float2 uv) {
		return 2.0 * SAMPLE_TEXTURE2D(u_NoiseTex, sampleru_NoiseTex, uv).r - 1.0;
	}

	float frag_collect(const v2f i, const int it) {
		const float itF = 1.f / (float)it;

		float rawDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, s_point_clamp_sampler, i.texcoord).r;
		PositionInputs posInputs = GetPositionInput(i.positionCS.xy, u_DownscaledScreenSize, rawDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

		float occlusion = 0.f;

#if !defined(ATMOSPHERICS_OCCLUSION_FULLSKY)
		[branch]
#if defined(UNITY_REVERSED_Z)
		if(rawDepth < 0.0000001f)
#else
		if(rawDepth > 0.9999999f)
#endif
		{
			occlusion = 0.75f;
		}
		else
#endif
		{
			ShadowContext shadowContext = InitShadowContext();
#ifdef SHADOWS_SHORTCUT
			Texture2DArray shadowContextTex = shadowContext.tex2DArray[0];
			SamplerComparisonState shadowContextCompSamp = shadowContext.compSamplers[0];
#endif

			float3 worldPos = posInputs.positionWS;
			float3 worldDir = worldPos - GetPrimaryCameraPosition();
			float3 deltaStep = -worldDir * itF;

#if defined(ATMOSPHERICS_OCCLUSION_JITTER_ALU)
			worldPos += deltaStep * (0.5 + 0.5 * u_JitterScale * srand(i.texcoord + u_JitterPhase.xx));
#else
			float2 rt_size = 1.0 / u_DownscaledScreenSize.xy;
			float2 uv = i.texcoord / _ScreenToTargetScale.xy;
			float2 uv_noise = (uv + 100.0 * u_JitterPhase.xx) * rt_size * u_NoiseTex_TexelSize.xy;
			worldPos += deltaStep * (0.5 + 0.5 * u_JitterScale * srand_noisetex(uv_noise));
#endif

#ifdef SHADOWS_SHORTCUT
			int outsideCascades = 0;
#endif
			for(int i = 0; i < it; ++i, worldPos += deltaStep) {
#ifdef SHADOWS_SHORTCUT
				float shadow = xEvalShadow_CascadedDepth(shadowContext, shadowContextTex, shadowContextCompSamp, worldPos, g_AtmosphericScatteringSunShadowIndex, outsideCascades);

	#ifdef SHADOWS_SHORTCUT_EARLY_OUT
				[branch]
				if(outsideCascades > 1)
					return occlusion / (float)it;
	#endif
				occlusion += shadow;
#else
				occlusion += xGetDirectionalShadowAttenuation(shadowContext, worldPos);
#endif
			}

			occlusion *= itF;
		}

		return occlusion;
	}
	
	float frag_collect24 (v2f i) : SV_Target { return frag_collect(i,  24); }
	float frag_collect64 (v2f i) : SV_Target { return frag_collect(i,  64); }
	float frag_collect80 (v2f i) : SV_Target { return frag_collect(i,  80); }
	float frag_collect96 (v2f i) : SV_Target { return frag_collect(i,  96); }
	float frag_collect164(v2f i) : SV_Target { return frag_collect(i, 164); }

	float frag_upsample(v2f i) : SV_Target {
		float2 rt_size = 2.0 * u_OcclusionTexture_TexelSize.zw;

		float2 uv = i.texcoord / _ScreenToTargetScale.xy;
		float2 uv_noise = (uv + u_NoisePhase.xx) * rt_size * u_NoiseTex_TexelSize.xy;

		float2 noise = 2.0 * SAMPLE_TEXTURE2D(u_NoiseTex, sampleru_NoiseTex, uv_noise).rg - 1.0;
		float2 noisy_uv = uv + (noise * u_UpsampleRadius) * u_OcclusionTexture_TexelSize.xy;

		float occlusion = SAMPLE_TEXTURE2D(u_OcclusionTexture, sampleru_OcclusionTexture, noisy_uv).r;

#if defined(ATMOSPHERICS_OCCLUSION_UPSAMPLE_NOISE_L)
		float4 nb = u_OcclusionTexture.Gather(sampleru_OcclusionTexture, noisy_uv);
		float nb_min = min(nb.x, min(nb.y, min(nb.z, nb.w)));
		float nb_max = max(nb.x, max(nb.y, max(nb.z, nb.w)));
		float nb_width = nb_max - nb_min;

		//return saturate(noise);
		return saturate(occlusion + (noise.r * u_UpsampleNoiseL * nb_width));
#else
		return saturate(occlusion);
#endif
	}

	float frag_resolve(v2f i) : SV_Target {
		const float2 k = u_OcclusionTexture_TexelSize.xy;
		float2 uv = i.texcoord / _ScreenToTargetScale.xy;
		float2 motion = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, i.texcoord).xy;

		float occlusion_current = SAMPLE_TEXTURE2D(u_OcclusionTexture, sampleru_OcclusionTexture, uv).r;
		float occlusion_history = SAMPLE_TEXTURE2D(u_OcclusionHistory, sampleru_OcclusionHistory, uv - motion).r;

#if defined(ATMOSPHERICS_OCCLUSION_RESOLVE_CHEAP)
		float4 region = u_OcclusionTexture.Gather(sampleru_OcclusionTexture, uv);
		float region_min = min(region.x, min(region.y, min(region.z, region.w)));
		float region_max = max(region.x, max(region.y, max(region.z, region.w)));
#else
		float4 region00 = u_OcclusionTexture.Gather(sampleru_OcclusionTexture, uv - 0.5 * k);
		float4 region11 = u_OcclusionTexture.Gather(sampleru_OcclusionTexture, uv + 0.5 * k);

		float region00_min = min(region00.x, min(region00.y, min(region00.z, region00.w)));
		float region00_max = max(region00.x, max(region00.y, max(region00.z, region00.w)));

		float region11_min = min(region11.x, min(region11.y, min(region11.z, region11.w)));
		float region11_max = max(region11.x, max(region11.y, max(region11.z, region11.w)));

	#if 0// enable if headroom
		float sample10 = u_OcclusionTexture.Sample(sampler_point_clamp, uv + float2(1, -1) * k).r;
		float sample01 = u_OcclusionTexture.Sample(sampler_point_clamp, uv + float2(-1, 1) * k).r;
		float region_min = min(region00_min, min(region11_min, min(sample10, sample01)));
		float region_max = max(region00_max, max(region11_max, max(sample10, sample01)));
	#else
		float region_min = min(region00_min, region11_min);
		float region_max = max(region00_max, region11_max);
	#endif
#endif

		float occlusion_history_clamped = clamp(occlusion_history, region_min, region_max);

		return lerp(
			lerp(occlusion_current, occlusion_history_clamped, u_OcclusionHistoryWeight),
			lerp(occlusion_current, occlusion_history, 0.5),
			u_OcclusionHistorySunMotion
		);
	}

ENDHLSL

SubShader {
	ZTest Always Cull Off ZWrite Off
	
	Pass {
		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag_collect24
		ENDHLSL
	}

	Pass {
		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag_collect64
		ENDHLSL
	}

	Pass {
		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag_collect80
		ENDHLSL
	}

	Pass {
		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag_collect96
		ENDHLSL
	}

	Pass {
		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag_collect164
		ENDHLSL
	}	

	Pass{
		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag_upsample
		ENDHLSL
	}

	Pass{
		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag_resolve
		ENDHLSL
	}
}
Fallback off
}

