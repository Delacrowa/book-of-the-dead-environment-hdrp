Shader "HDRenderPipeline/Decal"
{
    Properties
    {
		_BaseColor("_BaseColor", Color) = (1,1,1,1)
        _BaseColorMap("BaseColorMap", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _MaskMap("MaskMap", 2D) = "white" {}
        _DecalBlend("_DecalBlend", Range(0.0, 1.0)) = 0.5
		[ToggleUI] _AlbedoMode("_AlbedoMode", Range(0.0, 1.0)) = 1.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    //#pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
    #pragma shader_feature _COLORMAP
    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _MASKMAP
	#pragma shader_feature _ALBEDOCONTRIBUTION

    #pragma multi_compile_instancing
    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------
    #define UNITY_MATERIAL_DECAL

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "CoreRP/ShaderLibrary/Common.hlsl"
    #include "CoreRP/ShaderLibrary/Wind.hlsl"
    #include "../../ShaderPass/FragInputs.hlsl"
    #include "../../ShaderPass/ShaderPass.cs.hlsl"


    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "DecalProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline"}

        Pass
        {
            Name "DBufferProjector"  // Name is not used
            Tags { "LightMode" = "DBufferProjector" } // This will be only for opaque object based on the RenderQueue index

            // back faces with zfail, for cases when camera is inside the decal volume
            Cull Front
            ZWrite Off
            ZTest Greater
            // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "../../ShaderVariables.hlsl"
            #include "Decal.hlsl"
            #include "ShaderPass/DecalSharePass.hlsl"
            #include "DecalData.hlsl"
            #include "../../ShaderPass/ShaderPassDBuffer.hlsl"

            ENDHLSL
        }

		Pass
		{
			Name "DBufferMesh"  // Name is not used
			Tags{"LightMode" = "DBufferMesh"} // This will be only for opaque object based on the RenderQueue index
										 
			Cull Back
			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}
    }
    CustomEditor "Experimental.Rendering.HDPipeline.DecalUI"
}
