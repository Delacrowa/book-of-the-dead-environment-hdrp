#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#ifdef _ALPHATEST_ON
#define ATTRIBUTES_NEED_TEXCOORD0

#define VARYINGS_NEED_TEXCOORD0
#endif

// This include will define the various Attributes/Varyings structure
#include "HDRP/ShaderPass/VaryingMesh.hlsl"
