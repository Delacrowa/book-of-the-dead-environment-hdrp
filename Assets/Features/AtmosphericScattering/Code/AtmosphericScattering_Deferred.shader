Shader "Hidden/AtmosphericScattering_Deferred" {
	HLSLINCLUDE
	#pragma vertex vert
	#pragma fragment frag

	#pragma multi_compile _ ATMOSPHERICS ATMOSPHERICS_PER_PIXEL
	#pragma multi_compile _ ATMOSPHERICS_OCCLUSION
	//#pragma multi_compile _ ATMOSPHERICS_OCCLUSION_EDGE_FIXUP
	#pragma multi_compile _ ATMOSPHERICS_DEBUG

	#include "CoreRP/ShaderLibrary/Common.hlsl"
	#include "HDRP/ShaderVariables.hlsl"

	// Stuff from UnityCG.cginc
	//

	inline float Luminance(float3 rgb) {
		#define unity_ColorSpaceLuminance float4(0.0396819152, 0.458021790, 0.00609653955, 1.0) // Legacy: alpha is set to 1.0 to specify linear mode
		return dot(rgb, unity_ColorSpaceLuminance.rgb);
	}
	
	//
	//

	#include "AtmosphericScattering.cginc"

	uniform sampler2D		_MainTex;
	uniform float4			_MainTex_TexelSize;
	
	struct v2f {
		float4 positionCS : SV_POSITION;
		float4 texcoord : TEXCOORD0;
	};

	v2f vert(uint vertexID : SV_VertexID) {
		v2f output;
		output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
		output.texcoord.xy = GetFullScreenTriangleTexCoord(vertexID);
		output.texcoord.zw = output.texcoord.xy *_ScreenToTargetScale.xy;
		return output;
	}

	struct ScatterInput {
		float2 pos;
		half4 scatterCoords1;
		half3 scatterCoords2;
	};

	half4 frag(v2f i) : SV_Target {
#ifndef USE_FINAL_BLEND
		half4 sceneColor = tex2D(_MainTex, i.texcoord.zw);
#else
		half4 sceneColor = half4(0, 0, 0, 0);
#endif

		float rawDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, s_point_clamp_sampler, i.texcoord.zw).r;
		PositionInputs posInputs = GetPositionInput(i.positionCS.xy, _ScreenSize.zw, rawDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

		// Don't double-scatter on skybox
		[branch]
#if defined(UNITY_REVERSED_Z)
		if(rawDepth < 0.0000001f)
#else
		if(rawDepth > 0.9999999f)
#endif
#ifndef USE_FINAL_BLEND
			return sceneColor;
#else
			clip(-1);
#endif
		float3 wsPos = GetAbsolutePositionWS(posInputs.positionWS.xyz);
		
		// Apply scattering
		ScatterInput si;
		si.pos = i.texcoord.xy;
		VOLUND_TRANSFER_SCATTER(wsPos.xyz, si);
		VOLUND_APPLY_SCATTER(si, sceneColor.rgb);

#ifdef USE_FINAL_BLEND
		sceneColor.a = si.scatterCoords1.a;
#endif

		return sceneColor;
	}
	ENDHLSL

	SubShader {
		ZTest Always Cull Off ZWrite Off		
		Pass {
			HLSLPROGRAM
				#pragma target 3.0
				#pragma only_renderers d3d11 ps4 xboxone vulkan metal
			ENDHLSL
		}

		Pass {
			Blend One SrcAlpha

			HLSLPROGRAM
				#pragma target 3.0
				#pragma only_renderers d3d11 ps4 xboxone vulkan metal
				#pragma multi_compile USE_FINAL_BLEND
			ENDHLSL
		}
	
	}

	Fallback off
}
