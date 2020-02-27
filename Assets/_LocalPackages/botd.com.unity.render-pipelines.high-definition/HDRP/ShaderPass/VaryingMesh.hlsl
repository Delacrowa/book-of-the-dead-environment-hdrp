//forest-begin: Tree occlusion
#if defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_HIERARCHY_PIVOT)
	#define VARYINGS_NEED_TEXCOORD2
	#define VARYINGS_NEED_TEXCOORD3
#endif
//forest-end:
//forest-begin: Added vertex animation
#if defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_HIERARCHY_PIVOT)
	#define ATTRIBUTES_NEED_COLOR
#endif
//forest-end:
//forest-begin: Tessellated displacement scale
#if defined(_TESSELLATION_DISPLACEMENT)
	#ifndef ATTRIBUTES_NEED_COLOR
		#define ATTRIBUTES_NEED_COLOR
	#endif
	#define VARYINGS_DS_NEED_COLOR
#endif
//forest-end:
//forest-begin: Procedural bark peel
#if defined(_DETAIL_MAP_PEEL)
	#ifndef ATTRIBUTES_NEED_COLOR
		#define ATTRIBUTES_NEED_COLOR
	#endif
	#ifndef VARYINGS_NEED_COLOR
		#define VARYINGS_NEED_COLOR
	#endif
#endif
//forest-end:

struct AttributesMesh
{
    float3 positionOS   : POSITION;
#ifdef ATTRIBUTES_NEED_NORMAL
    float3 normalOS     : NORMAL;
#endif
#ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentOS    : TANGENT; // Store sign in w
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD0
    float2 uv0          : TEXCOORD0;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD1
    float2 uv1          : TEXCOORD1;
#endif
//forest-begin: Tree occlusion
#if defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_HIERARCHY_PIVOT)
	float4 uv2          : TEXCOORD2;
#elif defined(ATTRIBUTES_NEED_TEXCOORD2)
    float2 uv2          : TEXCOORD2;
#endif
//forest-end:
//forest-begin: Added vertex animation
#if defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_HIERARCHY_PIVOT)
	float3 uv3			: TEXCOORD3;
#elif defined(ATTRIBUTES_NEED_TEXCOORD3)
    float2 uv3          : TEXCOORD3;
#endif
#ifdef ATTRIBUTES_NEED_COLOR
    float4 color        : COLOR;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsMeshToPS
{
    float4 positionCS;
//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
	float4 mvPrevPositionCS;
	float4 mvPositionCS;
#endif
//forest-end:
#ifdef VARYINGS_NEED_POSITION_WS
    float3 positionRWS;
#endif
#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    float3 normalWS;
    float4 tangentWS;  // w contain mirror sign
#endif
#ifdef VARYINGS_NEED_TEXCOORD0
    float2 texCoord0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    float2 texCoord3;
#endif
#ifdef VARYINGS_NEED_COLOR
    float4 color;
#endif

UNITY_VERTEX_INPUT_INSTANCE_ID

};

struct PackedVaryingsMeshToPS
{
    float4 positionCS : SV_Position;
//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
	float4 mvPrevPositionCS		: TEXCOORD6;
	float4 mvPositionCS			: TEXCOORD7;
#endif
//forest-end:

#ifdef VARYINGS_NEED_POSITION_WS
    float3 interpolators0 : TEXCOORD0;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    float3 interpolators1 : TEXCOORD1;
    float4 interpolators2 : TEXCOORD2;
#endif

    // Allocate only necessary space if shader compiler in the future are able to automatically pack
#ifdef VARYINGS_NEED_TEXCOORD1
    float4 interpolators3 : TEXCOORD3;
#elif defined(VARYINGS_NEED_TEXCOORD0)
    float2 interpolators3 : TEXCOORD3;
#endif

#ifdef VARYINGS_NEED_TEXCOORD3
    float4 interpolators4 : TEXCOORD4;
#elif defined(VARYINGS_NEED_TEXCOORD2)
    float2 interpolators4 : TEXCOORD4;
#endif

#ifdef VARYINGS_NEED_COLOR
    float4 interpolators5 : TEXCOORD5;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID // Must be declare before FRONT_FACE_SEMANTIC

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

// Functions to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryingsMeshToPS PackVaryingsMeshToPS(VaryingsMeshToPS input)
{
    PackedVaryingsMeshToPS output;

    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = input.positionCS;
//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
	output.mvPrevPositionCS = input.mvPrevPositionCS;
	output.mvPositionCS = input.mvPositionCS;
#endif
//forest-end:

#ifdef VARYINGS_NEED_POSITION_WS
    output.interpolators0 = input.positionRWS;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.interpolators1 = input.normalWS;
    output.interpolators2 = input.tangentWS;
#endif

#ifdef VARYINGS_NEED_TEXCOORD0
    output.interpolators3.xy = input.texCoord0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.interpolators3.zw = input.texCoord1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.interpolators4.xy = input.texCoord2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.interpolators4.zw = input.texCoord3;
#endif

#ifdef VARYINGS_NEED_COLOR
    output.interpolators5 = input.color;
#endif

    return output;
}

FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    UNITY_SETUP_INSTANCE_ID(input);

    // Init to some default value to make the computer quiet (else it output "divide by zero" warning even if value is not used).
    // TODO: this is a really poor workaround, but the variable is used in a bunch of places
    // to compute normals which are then passed on elsewhere to compute other values...
    output.worldToTangent = k_identity3x3;

    output.positionSS = input.positionCS; // input.positionCS is SV_Position

#ifdef VARYINGS_NEED_POSITION_WS
    output.positionRWS.xyz = input.interpolators0.xyz;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    float4 tangentWS = float4(input.interpolators2.xyz, input.interpolators2.w > 0.0 ? 1.0 : -1.0); // must not be normalized (mikkts requirement)

    // Normalize normalWS vector but keep the renormFactor to apply it to bitangent and tangent
    float3 unnormalizedNormalWS = input.interpolators1.xyz;
    float renormFactor = 1.0 / length(unnormalizedNormalWS);

    // bitangent on the fly option in xnormal to reduce vertex shader outputs.
    // this is the mikktspace transformation (must use unnormalized attributes)
    float3x3 worldToTangent = CreateWorldToTangent(unnormalizedNormalWS, tangentWS.xyz, tangentWS.w);

    // surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
    // by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
    output.worldToTangent[0] = worldToTangent[0] * renormFactor;
    output.worldToTangent[1] = worldToTangent[1] * renormFactor;
    output.worldToTangent[2] = worldToTangent[2] * renormFactor;        // normalizes the interpolated vertex normal
#endif // VARYINGS_NEED_TANGENT_TO_WORLD

#ifdef VARYINGS_NEED_TEXCOORD0
    output.texCoord0 = input.interpolators3.xy;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.texCoord1 = input.interpolators3.zw;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.texCoord2 = input.interpolators4.xy;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.texCoord3 = input.interpolators4.zw;
#endif
#ifdef VARYINGS_NEED_COLOR
    output.color = input.interpolators5;
#endif

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
    // Handle handness of the view matrix (In Unity view matrix default to a determinant of -1)
    // when we render a cubemap the view matrix handness is flipped (due to convention used for cubemap) we have a determinant of +1
    output.isFrontFace = _DetViewMatrix < 0.0 ? output.isFrontFace : !output.isFrontFace;
#endif

    return output;
}

#ifdef TESSELLATION_ON

// Varying DS - use for domain shader
// We can deduce these defines from the other defines
// We need to pass to DS any varying required by pixel shader
// If we have required an attributes that is not present in varyings it mean we will be for DS
#if defined(VARYINGS_NEED_TANGENT_TO_WORLD) || defined(ATTRIBUTES_NEED_TANGENT)
#define VARYINGS_DS_NEED_TANGENT
#endif
#if defined(VARYINGS_NEED_TEXCOORD0) || defined(ATTRIBUTES_NEED_TEXCOORD0)
#define VARYINGS_DS_NEED_TEXCOORD0
#endif
#if defined(VARYINGS_NEED_TEXCOORD1) || defined(ATTRIBUTES_NEED_TEXCOORD1)
#define VARYINGS_DS_NEED_TEXCOORD1
#endif
#if defined(VARYINGS_NEED_TEXCOORD2) || defined(ATTRIBUTES_NEED_TEXCOORD2)
#define VARYINGS_DS_NEED_TEXCOORD2
#endif
#if defined(VARYINGS_NEED_TEXCOORD3) || defined(ATTRIBUTES_NEED_TEXCOORD3)
#define VARYINGS_DS_NEED_TEXCOORD3
#endif
#if defined(VARYINGS_NEED_COLOR) || defined(ATTRIBUTES_NEED_COLOR)
#define VARYINGS_DS_NEED_COLOR
#endif

// Varying for domain shader
// Position and normal are always present (for tessellation) and in world space
struct VaryingsMeshToDS
{
//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
    float4 mvPrevPositionCS    : TEXCOORD5;
    float4 mvPositionCS        : TEXCOORD4;
#endif
//forest-end:
    float3 positionRWS;
    float3 normalWS;
#ifdef VARYINGS_DS_NEED_TANGENT
    float4 tangentWS;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    float2 texCoord0;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    float2 texCoord3;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    float4 color;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PackedVaryingsMeshToDS
{
//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
    float4 mvPrevPositionCS  : TEXCOORD3;
    float4 mvPositionCS      : TEXCOORD4;
#endif
//forest-end:

    float3 interpolators0 : INTERNALTESSPOS; // positionRWS
    float3 interpolators1 : NORMAL; // NormalWS

#ifdef VARYINGS_DS_NEED_TANGENT
    float4 interpolators2 : TANGENT;
#endif

    // Allocate only necessary space if shader compiler in the future are able to automatically pack
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    float4 interpolators3 : TEXCOORD0;
#elif defined(VARYINGS_DS_NEED_TEXCOORD0)
    float2 interpolators3 : TEXCOORD0;
#endif

#ifdef VARYINGS_DS_NEED_TEXCOORD3
    float4 interpolators4 : TEXCOORD1;
#elif defined(VARYINGS_DS_NEED_TEXCOORD2)
    float2 interpolators4 : TEXCOORD1;
#endif

#ifdef VARYINGS_DS_NEED_COLOR
    float4 interpolators5 : TEXCOORD2;
#endif

     UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Functions to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryingsMeshToDS PackVaryingsMeshToDS(VaryingsMeshToDS input)
{
    PackedVaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input, output);

//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
	output.mvPrevPositionCS = input.mvPrevPositionCS;
	output.mvPositionCS = input.mvPositionCS;
#endif
//forest-end:
    output.interpolators0 = input.positionRWS;
    output.interpolators1 = input.normalWS;
#ifdef VARYINGS_DS_NEED_TANGENT
    output.interpolators2 = input.tangentWS;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    output.interpolators3.xy = input.texCoord0;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    output.interpolators3.zw = input.texCoord1;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    output.interpolators4.xy = input.texCoord2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    output.interpolators4.zw = input.texCoord3;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    output.interpolators5 = input.color;
#endif

    return output;
}

VaryingsMeshToDS UnpackVaryingsMeshToDS(PackedVaryingsMeshToDS input)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input, output);

//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
	output.mvPrevPositionCS = input.mvPrevPositionCS;
	output.mvPositionCS = input.mvPositionCS;
#endif
//forest-end:
    output.positionRWS = input.interpolators0;
    output.normalWS = input.interpolators1;
#ifdef VARYINGS_DS_NEED_TANGENT
    output.tangentWS = input.interpolators2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    output.texCoord0 = input.interpolators3.xy;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    output.texCoord1 = input.interpolators3.zw;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    output.texCoord2 = input.interpolators4.xy;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    output.texCoord3 = input.interpolators4.zw;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    output.color = input.interpolators5;
#endif

    return output;
}

VaryingsMeshToDS InterpolateWithBaryCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, float3 baryCoords)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input0, output);

//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
	TESSELLATION_INTERPOLATE_BARY(mvPrevPositionCS, baryCoords);
	TESSELLATION_INTERPOLATE_BARY(mvPositionCS, baryCoords);
#endif
//forest-end:
    TESSELLATION_INTERPOLATE_BARY(positionRWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
#ifdef VARYINGS_DS_NEED_TANGENT
    // This will interpolate the sign but should be ok in practice as we may expect a triangle to have same sign (? TO CHECK)
    TESSELLATION_INTERPOLATE_BARY(tangentWS, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    TESSELLATION_INTERPOLATE_BARY(texCoord2, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    TESSELLATION_INTERPOLATE_BARY(texCoord3, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    TESSELLATION_INTERPOLATE_BARY(color, baryCoords);
#endif

    return output;
}

#endif // TESSELLATION_ON
