//forest-begin: sky occlusion

#define SKY_OCCLUSION 1
#if SKY_OCCLUSION

// Occlusion probes
sampler3D _OcclusionProbes;
float4x4 _OcclusionProbesWorldToLocal;
sampler3D _OcclusionProbesDetail;
float4x4 _OcclusionProbesWorldToLocalDetail;
float4 _AmbientProbeSH[7];

// Grass occlusion
sampler2D _GrassOcclusion;
float _GrassOcclusionAmountTerrain;
float _GrassOcclusionAmountGrass;
float _GrassOcclusionHeightFadeBottom;
float _GrassOcclusionHeightFadeTop;
float4x4 _GrassOcclusionWorldToLocal;
sampler2D _GrassOcclusionHeightmap;
float _GrassOcclusionHeightRange;
float _GrassOcclusionCullHeight;

float SampleGrassOcclusion(float2 terrainUV)
{
    return lerp(1.0, tex2D(_GrassOcclusion, terrainUV).a, _GrassOcclusionAmountTerrain);
}

float SampleGrassOcclusion(float3 positionWS)
{
    float3 pos = mul(_GrassOcclusionWorldToLocal, float4(positionWS, 1)).xyz;
    float terrainHeight = tex2D(_GrassOcclusionHeightmap, pos.xz).a;
    float height = pos.y - terrainHeight * _GrassOcclusionHeightRange;

    UNITY_BRANCH
    if(height < _GrassOcclusionCullHeight)
    {
        float xz = lerp(1.0, tex2D(_GrassOcclusion, pos.xz).a, _GrassOcclusionAmountGrass);
        return saturate(xz + smoothstep(_GrassOcclusionHeightFadeBottom, _GrassOcclusionHeightFadeTop, height));

        // alternatively:    
        // float amount = saturate(smoothstep(_GrassOcclusionHeightFade, 0, pos.y) * _GrassOcclusionAmount);
        // return lerp(1.0, tex2D(_GrassOcclusion, pos.xz).a, amount);
    }
    else
        return 1;
}

float SampleOcclusionProbes(float3 positionWS)
{
	// TODO: no full matrix mul needed, just scale and offset the pos (don't really need to support rotation)
    float occlusionProbes = 1;

    float3 pos = mul(_OcclusionProbesWorldToLocalDetail, float4(positionWS, 1)).xyz;

    UNITY_BRANCH
	if(all(pos > 0) && all(pos < 1))
    {
		occlusionProbes = tex3D(_OcclusionProbesDetail, pos).a;
	}
    else
    {
		pos = mul(_OcclusionProbesWorldToLocal, float4(positionWS, 1)).xyz;
		occlusionProbes = tex3D(_OcclusionProbes, pos).a;
	}

    return occlusionProbes;
}

float SampleSkyOcclusion(float3 positionRWS, out float grassOcclusion)
{
    float3 positionWS = GetAbsolutePositionWS(positionRWS);
    grassOcclusion = SampleGrassOcclusion(positionWS);
    return grassOcclusion * SampleOcclusionProbes(positionWS);
}

float SampleSkyOcclusion(float3 positionRWS, float2 terrainUV, out float grassOcclusion)
{
    float3 positionWS = GetAbsolutePositionWS(positionRWS);
    grassOcclusion = SampleGrassOcclusion(terrainUV);
    return grassOcclusion * SampleOcclusionProbes(positionWS);
}

#else
float SampleGrassOcclusion(float2 terrainUV) { return 1; }
float SampleSkyOcclusion(float3 positionRWS, out float grassOcclusion) { grassOcclusion = 1; return 1; }
float SampleSkyOcclusion(float3 positionRWS, float2 terrainUV, out float grassOcclusion) { grassOcclusion = 1; return 1; }
#endif
//forest-end

//forest-begin: Tree occlusion

//UnityPerMaterial
//float _UseTreeOcclusion;
//float _TreeAO;
//float _TreeAOBias;
//float _TreeAO2;
//float _TreeAOBias2;
//float _TreeDO;
//float _TreeDOBias;
//float _TreeDO2;
//float _TreeDOBias2;
//float _Tree12Width;

// Freeload of an already passed global sun vector
float3 _AtmosphericScatteringSunVector;

float GetTreeOcclusion(float3 positionRWS, float4 treeOcclusionInput) {
#if defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_HIERARCHY_PIVOT)
	if(_UseTreeOcclusion) {
		float3 positionWS = GetAbsolutePositionWS(positionRWS);
		float treeWidth = _Tree12Width == 0 ? 1.f : saturate((positionWS.y - UNITY_MATRIX_M._m13) / _Tree12Width);
		float treeDO = lerp(_TreeDO, _TreeDO2, treeWidth);
		float treeAO = lerp(_TreeAO, _TreeAO2, treeWidth);
		float4 lightDir = float4(-_AtmosphericScatteringSunVector * treeDO, treeAO);
		float treeDOBias = lerp(_TreeDOBias, _TreeDOBias2, treeWidth);
		float treeAOBias = lerp(_TreeAOBias, _TreeAOBias2, treeWidth);
		return saturate(dot(saturate(treeOcclusionInput + float4(treeDOBias.rrr, treeAOBias)), lightDir));
	}
	else
#endif
	{
		return 1.f;
	}
}
//forest-end:

// Return camera relative probe volume world to object transformation
float4x4 GetProbeVolumeWorldToObject()
{
    return ApplyCameraTranslationToInverseMatrix(unity_ProbeVolumeWorldToObject);
}

// In unity we can have a mix of fully baked lightmap (static lightmap) + enlighten realtime lightmap (dynamic lightmap)
// for each case we can have directional lightmap or not.
// Else we have lightprobe for dynamic/moving entity. Either SH9 per object lightprobe or SH4 per pixel per object volume probe
//forest-begin: sky occlusion / Tree occlusion
float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, float skyOcclusion, float grassOcclusion, float treeOcclusion);

float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap) {
	return SampleBakedGI(positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap, 1.f, 1.f, 1.f);
}

float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, float skyOcclusion, float grassOcclusion, float treeOcclusion)
//forest-end
{
    // If there is no lightmap, it assume lightprobe
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)

// TODO: Confirm with Ionut but it seems that UNITY_LIGHT_PROBE_PROXY_VOLUME is always define for high end and
// unity_ProbeVolumeParams always bind.
    if (unity_ProbeVolumeParams.x == 0.0)
    {
        // TODO: pass a tab of coefficient instead!
        real4 SHCoefficients[7];
        SHCoefficients[0] = unity_SHAr;
        SHCoefficients[1] = unity_SHAg;
        SHCoefficients[2] = unity_SHAb;
        SHCoefficients[3] = unity_SHBr;
        SHCoefficients[4] = unity_SHBg;
        SHCoefficients[5] = unity_SHBb;
        SHCoefficients[6] = unity_SHC;

//forest-begin: sky occlusion
        #if SKY_OCCLUSION
			SHCoefficients[0] += _AmbientProbeSH[0] * skyOcclusion;
			SHCoefficients[1] += _AmbientProbeSH[1] * skyOcclusion;
			SHCoefficients[2] += _AmbientProbeSH[2] * skyOcclusion;
			SHCoefficients[3] += _AmbientProbeSH[3] * skyOcclusion;
			SHCoefficients[4] += _AmbientProbeSH[4] * skyOcclusion;
			SHCoefficients[5] += _AmbientProbeSH[5] * skyOcclusion;
			SHCoefficients[6] += _AmbientProbeSH[6] * skyOcclusion;
       #endif
//forest-end


//forest-begin: Tree occlusion
        return SampleSH9(SHCoefficients, normalWS) * treeOcclusion;
//forest-end
    }
    else
    {
        return SampleProbeVolumeSH4(TEXTURE3D_PARAM(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, normalWS, GetProbeVolumeWorldToObject(),
//forest-begin: Tree occlusion
            unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin, unity_ProbeVolumeSizeInv) * treeOcclusion;
//forest-end
    }

#else

    float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

#ifdef UNITY_LIGHTMAP_FULL_HDR
    bool useRGBMLightmap = false;
    float4 decodeInstructions = float4(0.0, 0.0, 0.0, 0.0); // Never used but needed for the interface since it supports gamma lightmaps
#else
    bool useRGBMLightmap = true;
    #if defined(UNITY_LIGHTMAP_RGBM_ENCODING)
        float4 decodeInstructions = float4(34.493242, 2.2, 0.0, 0.0); // range^2.2 = 5^2.2, gamma = 2.2
    #else
        float4 decodeInstructions = float4(2.0, 2.2, 0.0, 0.0); // range = 2.0^2.2 = 4.59
    #endif
#endif

    #ifdef LIGHTMAP_ON
        #ifdef DIRLIGHTMAP_COMBINED
        bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_Lightmap, samplerunity_Lightmap),
                                                        TEXTURE2D_PARAM(unity_LightmapInd, samplerunity_Lightmap),
                                                        uvStaticLightmap, unity_LightmapST, normalWS, useRGBMLightmap, decodeInstructions);
        #else
        bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_PARAM(unity_Lightmap, samplerunity_Lightmap), uvStaticLightmap, unity_LightmapST, useRGBMLightmap, decodeInstructions);
        #endif
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        #ifdef DIRLIGHTMAP_COMBINED
        bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_DynamicLightmap, samplerunity_DynamicLightmap),
                                                        TEXTURE2D_PARAM(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
                                                        uvDynamicLightmap, unity_DynamicLightmapST, normalWS, false, decodeInstructions);
        #else
        bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_PARAM(unity_DynamicLightmap, samplerunity_DynamicLightmap), uvDynamicLightmap, unity_DynamicLightmapST, false, decodeInstructions);
        #endif
    #endif

//forest-begin: sky occlusion
    return bakeDiffuseLighting * grassOcclusion;
//forest-end

#endif
}

float4 SampleShadowMask(float3 positionRWS, float2 uvStaticLightmap) // normalWS not use for now
{
#if defined(LIGHTMAP_ON)
    float2 uv = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;
    float4 rawOcclusionMask = SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_Lightmap, uv); // Reuse sampler from Lightmap
#else
    float4 rawOcclusionMask;
    if (unity_ProbeVolumeParams.x == 1.0)
    {
        rawOcclusionMask = SampleProbeOcclusion(TEXTURE3D_PARAM(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, GetProbeVolumeWorldToObject(),
                                                unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin, unity_ProbeVolumeSizeInv);
    }
    else
    {
        // Note: Default value when the feature is not enabled is float(1.0, 1.0, 1.0, 1.0) in C++
        rawOcclusionMask = unity_ProbesOcclusion;
    }
#endif

    return rawOcclusionMask;
}

// Calculate velocity in Clip space [-1..1]
float2 CalculateVelocity(float4 positionCS, float4 previousPositionCS)
{
    // This test on define is required to remove warning of divide by 0 when initializing empty struct
    // TODO: Add forward opaque MRT case...
//forest-begin: We need this in any pass!
//#if (SHADERPASS == SHADERPASS_VELOCITY)
    // Encode velocity
    positionCS.xy = positionCS.xy / positionCS.w;
    previousPositionCS.xy = previousPositionCS.xy / previousPositionCS.w;

    float2 velocity = (positionCS.xy - previousPositionCS.xy);
#if UNITY_UV_STARTS_AT_TOP
    velocity.y = -velocity.y;
#endif
    return velocity;
//forest-end:
//#else
//    return float2(0.0, 0.0);
//#endif
}

// Flipping or mirroring a normal can be done directly on the tangent space. This has the benefit to apply to the whole process either in surface gradient or not.
// This function will modify FragInputs and this is not propagate outside of GetSurfaceAndBuiltinData(). This is ok as tangent space is not use outside of GetSurfaceAndBuiltinData().
void ApplyDoubleSidedFlipOrMirror(inout FragInputs input)
{
#ifdef _DOUBLESIDED_ON
    // _DoubleSidedConstants is float3(-1, -1, -1) in flip mode and float3(1, 1, -1) in mirror mode
    // To get a flipped normal with the tangent space, we must flip bitangent (because it is construct from the normal) and normal
    // To get a mirror normal with the tangent space, we only need to flip the normal and not the tangent
    float2 flipSign = input.isFrontFace ? float2(1.0, 1.0) : _DoubleSidedConstants.yz; // TOCHECK :  GetOddNegativeScale() is not necessary here as it is apply for tangent space creation.
    input.worldToTangent[1] = flipSign.x * input.worldToTangent[1]; // bitangent
    input.worldToTangent[2] = flipSign.y * input.worldToTangent[2]; // normal

    #ifdef SURFACE_GRADIENT
    // TOCHECK: seems that we don't need to invert any genBasisTB(), sign cancel. Which is expected as we deal with surface gradient.

    // TODO: For surface gradient we must invert or mirror the normal just after the interpolation. It will allow to work with layered with all basis. Currently it is not the case
    #endif
#endif
}

// This function convert the tangent space normal/tangent to world space and orthonormalize it + apply a correction of the normal if it is not pointing towards the near plane
void GetNormalWS(FragInputs input, float3 V, float3 normalTS, out float3 normalWS)
{
    #ifdef SURFACE_GRADIENT
    normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], normalTS);
    #else
    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    normalWS = normalize(TransformTangentToWorld(normalTS, input.worldToTangent));
    #endif
}
