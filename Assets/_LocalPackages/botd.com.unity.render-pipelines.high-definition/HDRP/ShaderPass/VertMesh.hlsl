struct VaryingsToPS
{
    VaryingsMeshToPS vmesh;
#ifdef VARYINGS_NEED_PASS
    VaryingsPassToPS vpass;
#endif
};

struct PackedVaryingsToPS
{
    PackedVaryingsMeshToPS vmesh;
#ifdef VARYINGS_NEED_PASS
    PackedVaryingsPassToPS vpass;
#endif
};

PackedVaryingsToPS PackVaryingsToPS(VaryingsToPS input)
{
    PackedVaryingsToPS output;
    output.vmesh = PackVaryingsMeshToPS(input.vmesh);
#ifdef VARYINGS_NEED_PASS
    output.vpass = PackVaryingsPassToPS(input.vpass);
#endif

    return output;
}

#ifdef TESSELLATION_ON


struct VaryingsToDS
{
    VaryingsMeshToDS vmesh;
#ifdef VARYINGS_NEED_PASS
    VaryingsPassToDS vpass;
#endif
};

struct PackedVaryingsToDS
{
    PackedVaryingsMeshToDS vmesh;
#ifdef VARYINGS_NEED_PASS
    PackedVaryingsPassToDS vpass;
#endif
};

PackedVaryingsToDS PackVaryingsToDS(VaryingsToDS input)
{
    PackedVaryingsToDS output;
    output.vmesh = PackVaryingsMeshToDS(input.vmesh);
#ifdef VARYINGS_NEED_PASS
    output.vpass = PackVaryingsPassToDS(input.vpass);
#endif

    return output;
}

VaryingsToDS UnpackVaryingsToDS(PackedVaryingsToDS input)
{
    VaryingsToDS output;
    output.vmesh = UnpackVaryingsMeshToDS(input.vmesh);
#ifdef VARYINGS_NEED_PASS
    output.vpass = UnpackVaryingsPassToDS(input.vpass);
#endif

    return output;
}

VaryingsToDS InterpolateWithBaryCoordsToDS(VaryingsToDS input0, VaryingsToDS input1, VaryingsToDS input2, float3 baryCoords)
{
    VaryingsToDS output;

    output.vmesh = InterpolateWithBaryCoordsMeshToDS(input0.vmesh, input1.vmesh, input2.vmesh, baryCoords);
#ifdef VARYINGS_NEED_PASS
    output.vpass = InterpolateWithBaryCoordsPassToDS(input0.vpass, input1.vpass, input2.vpass, baryCoords);
#endif

    return output;
}

#endif // TESSELLATION_ON

#ifdef TESSELLATION_ON
#define VaryingsType VaryingsToDS
#define VaryingsMeshType VaryingsMeshToDS
#define PackedVaryingsType PackedVaryingsToDS
#define PackVaryingsType PackVaryingsToDS
#else
#define VaryingsType VaryingsToPS
#define VaryingsMeshType VaryingsMeshToPS
#define PackedVaryingsType PackedVaryingsToPS
#define PackVaryingsType PackVaryingsToPS
#endif

// TODO: Here we will also have all the vertex deformation (GPU skinning, vertex animation, morph target...) or we will need to generate a compute shaders instead (better! but require work to deal with unpacking like fp16)
VaryingsMeshType VertMesh(AttributesMesh input)
{
#if defined(HAVE_MESH_MODIFICATION)
    input = ApplyMeshModification(input);
#endif

    VaryingsMeshType output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    // This return the camera relative position (if enable)
    float3 positionRWS = TransformObjectToWorld(input.positionOS);
#ifdef ATTRIBUTES_NEED_NORMAL
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#else
    float3 normalWS = float3(0.0, 0.0, 0.0); // We need this case to be able to compile ApplyVertexModification that doesn't use normal.
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
     float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

//forest-begin: G-Buffer motion vectors
     // Do vertex modification in camera relative space (if enable)
#if defined(HAVE_VERTEX_MODIFICATION)
    #if defined(HAS_VEGETATION_ANIM)
        float3 prevPositionRWS = positionRWS;
        float3 prevNormalWS = normalWS;
        float prevTimeDelta = -unity_DeltaTime.x;
        ApplyVertexModification(input, prevNormalWS, prevPositionRWS, prevTimeDelta.xxxx);
    #endif

    ApplyVertexModification(input, normalWS, positionRWS, float4(0,0,0,0));

    #if defined(HAS_VEGETATION_ANIM)
        output.mvPrevPositionCS = mul(_PrevViewProjMatrix, float4(prevPositionRWS, 1.0));
        output.mvPositionCS = mul(_NonJitteredViewProjMatrix, float4(positionRWS, 1.0));
    #endif
//forest-end:
#endif

#ifdef TESSELLATION_ON
    output.positionRWS = positionRWS;
    output.normalWS = normalWS;
    #if defined(VARYINGS_NEED_TANGENT_TO_WORLD) || defined(VARYINGS_DS_NEED_TANGENT)
    output.tangentWS = tangentWS;
    #endif
#else
    #ifdef VARYINGS_NEED_POSITION_WS
    output.positionRWS = positionRWS;
    #endif
    output.positionCS = TransformWorldToHClip(positionRWS);
    #ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.normalWS = normalWS;
    output.tangentWS = tangentWS;
    #endif
#endif

#if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.texCoord0 = input.uv0;
#endif
#if defined(VARYINGS_NEED_TEXCOORD1) || defined(VARYINGS_DS_NEED_TEXCOORD1)
    output.texCoord1 = input.uv1;
#endif
#if defined(VARYINGS_NEED_TEXCOORD2) || defined(VARYINGS_DS_NEED_TEXCOORD2)
//forest-begin: Tree occlusion
    output.texCoord2 = input.uv2.xy;
//forest-end:
#endif
#if defined(VARYINGS_NEED_TEXCOORD3) || defined(VARYINGS_DS_NEED_TEXCOORD3)
//forest-begin: Tree occlusion
	output.texCoord3 = input.uv3.xy;
//forest-end:
#endif
#if defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR)
    output.color = input.color;
#endif
//forest-begin: Tree occlusion
#if defined(_ANIM_SINGLE_PIVOT) || defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_HIERARCHY_PIVOT)
	#if defined(VARYINGS_NEED_TEXCOORD2) && defined(VARYINGS_NEED_TEXCOORD3)
		output.texCoord2 = input.uv2.xy;
		output.texCoord3 = input.uv2.zw;
	#endif
#endif
//forest-end:

    return output;
}

#ifdef TESSELLATION_ON

VaryingsMeshToPS VertMeshTesselation(VaryingsMeshToDS input)
{
    VaryingsMeshToPS output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = TransformWorldToHClip(input.positionRWS);

//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM)
	output.mvPrevPositionCS = input.mvPrevPositionCS;
	output.mvPositionCS = input.mvPositionCS;
#endif
//forest-end:

#ifdef VARYINGS_NEED_POSITION_WS
    output.positionRWS = input.positionRWS;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.normalWS = input.normalWS;
    output.tangentWS = input.tangentWS;
#endif

#ifdef VARYINGS_NEED_TEXCOORD0
    output.texCoord0 = input.texCoord0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.texCoord1 = input.texCoord1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.texCoord2 = input.texCoord2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.texCoord3 = input.texCoord3;
#endif
#ifdef VARYINGS_NEED_COLOR
    output.color = input.color;
#endif

    return output;
}

#endif // TESSELLATION_ON
