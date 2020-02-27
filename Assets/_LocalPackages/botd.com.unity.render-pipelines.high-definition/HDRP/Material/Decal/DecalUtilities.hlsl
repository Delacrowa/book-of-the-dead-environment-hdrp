#include "Decal.hlsl"

DECLARE_DBUFFER_TEXTURE(_DBufferTexture);

DecalData FetchDecal(uint start, uint i)
{
#ifdef LIGHTLOOP_TILE_PASS
    int j = FetchIndex(start, i);
#else
    int j = start + i;
#endif
    return _DecalDatas[j];
}

// Caution: We can't compute LOD inside a dynamic loop. The gradient are not accessible.
// we need to find a way to calculate mips. For now just fetch first mip of the decals
void ApplyBlendNormal(inout float4 dst, inout int matMask, float2 texCoords, int mapMask, float3x3 decalToWorld, float blend, float lod)
{
    float4 src;
    src.xyz = mul(decalToWorld, UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, texCoords, lod))) * 0.5f + 0.5f;
    src.w = blend;
    dst.xyz = src.xyz * src.w + dst.xyz * (1.0f - src.w);
    dst.w = dst.w * (1.0f - src.w);
    matMask |= mapMask;
}

void ApplyBlendDiffuse(inout float4 dst, inout int matMask, float2 texCoords, float4 src, int mapMask, inout float blend, float lod, int diffuseTextureBound)
{
	if(diffuseTextureBound)
	{ 
		src *= SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, texCoords, lod);
	}
    src.w *= blend;
    blend = src.w;  // diffuse texture alpha affects all other channels
    dst.xyz = src.xyz * src.w + dst.xyz * (1.0f - src.w);
    dst.w = dst.w * (1.0f - src.w);
    matMask |= mapMask;
}

void ApplyBlendMask(inout float4 dst, inout int matMask, float2 texCoords, int mapMask, float blend, float lod)
{
    float4 src = SAMPLE_TEXTURE2D_LOD(_DecalAtlas2D, _trilinear_clamp_sampler_DecalAtlas2D, texCoords, lod);
    src.z = src.w;
    src.w = blend;
    dst.xyz = src.xyz * src.w + dst.xyz * (1.0f - src.w);
    dst.w = dst.w * (1.0f - src.w);
    matMask |= mapMask;
}

void AddDecalContribution(PositionInputs posInput, inout SurfaceData surfaceData, inout float alpha)
{
    if(_EnableDBuffer)
    {
        DecalSurfaceData decalSurfaceData;
        int mask = 0;
        // the code in the macros, gets moved inside the conditionals by the compiler
        FETCH_DBUFFER(DBuffer, _DBufferTexture, posInput.positionSS);

#ifdef _SURFACE_TYPE_TRANSPARENT    // forward transparent using clustered decals
        uint decalCount, decalStart;
        DBuffer0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
        DBuffer1 = float4(0.5f, 0.5f, 0.5f, 1.0f);
        DBuffer2 = float4(0.0f, 0.0f, 0.0f, 1.0f);

    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_DECAL, decalStart, decalCount);
    #else
        decalCount = _DecalCount;
        decalStart = 0;
    #endif

        float3 positionRWS = posInput.positionWS;

        // get world space ddx/ddy for adjacent pixels to be used later in mipmap lod calculation
        float3 positionRWSDdx = ddx(positionRWS);
        float3 positionRWSDdy = ddy(positionRWS);

        for (uint i = 0; i < decalCount; i++)
        {
            DecalData decalData = FetchDecal(decalStart, i);

            // Get the relative world camera to decal matrix
            float4x4 worldToDecal = ApplyCameraTranslationToInverseMatrix(decalData.worldToDecal);

            float3 positionDS = mul(worldToDecal, float4(positionRWS, 1.0)).xyz;
            positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.0f, 0.5);  // decal clip space
            if ((all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
            {
                float2 uvScale = float2(decalData.normalToWorld[3][0], decalData.normalToWorld[3][1]);
                float2 uvBias = float2(decalData.normalToWorld[3][2], decalData.normalToWorld[3][3]);
                positionDS.xz = positionDS.xz * uvScale + uvBias;
                positionDS.xz = frac(positionDS.xz);

                // clamp by half a texel to avoid sampling neighboring textures in the atlas
                float2 clampAmount = float2(0.5f / _DecalAtlasResolution.x, 0.5f / _DecalAtlasResolution.y);

                float2 diffuseMin = decalData.diffuseScaleBias.zw + clampAmount;                                    // offset into atlas is in .zw
                float2 diffuseMax = decalData.diffuseScaleBias.zw + decalData.diffuseScaleBias.xy - clampAmount;    // scale relative to full atlas size is in .xy so total texture extent in atlas is (1,1) * scale

                float2 normalMin = decalData.normalScaleBias.zw + clampAmount;
                float2 normalMax = decalData.normalScaleBias.zw + decalData.normalScaleBias.xy - clampAmount;

                float2 maskMin = decalData.maskScaleBias.zw + clampAmount;
                float2 maskMax = decalData.maskScaleBias.zw + decalData.maskScaleBias.xy - clampAmount;

                float2 sampleDiffuse = clamp(positionDS.xz * decalData.diffuseScaleBias.xy + decalData.diffuseScaleBias.zw, diffuseMin, diffuseMax);
                float2 sampleNormal = clamp(positionDS.xz * decalData.normalScaleBias.xy + decalData.normalScaleBias.zw, normalMin, normalMax);
                float2 sampleMask = clamp(positionDS.xz * decalData.maskScaleBias.xy + decalData.maskScaleBias.zw, maskMin, maskMax);

                // need to compute the mipmap LOD manually because we are sampling inside a loop
                float3 positionDSDdx = mul(worldToDecal, float4(positionRWSDdx, 0.0)).xyz; // transform the derivatives to decal space, any translation is irrelevant
                float3 positionDSDdy = mul(worldToDecal, float4(positionRWSDdy, 0.0)).xyz;

                float2 sampleDiffuseDdx = positionDSDdx.xz * decalData.diffuseScaleBias.xy; // factor in the atlas scale
                float2 sampleDiffuseDdy = positionDSDdy.xz * decalData.diffuseScaleBias.xy;
                float lodDiffuse = ComputeTextureLOD(sampleDiffuseDdx, sampleDiffuseDdy, _DecalAtlasResolution);

                float2 sampleNormalDdx = positionDSDdx.xz * decalData.normalScaleBias.xy;
                float2 sampleNormalDdy = positionDSDdy.xz * decalData.normalScaleBias.xy;
                float lodNormal = ComputeTextureLOD(sampleNormalDdx, sampleNormalDdy, _DecalAtlasResolution);

                float2 sampleMaskDdx = positionDSDdx.xz * decalData.maskScaleBias.xy;
                float2 sampleMaskDdy = positionDSDdy.xz * decalData.maskScaleBias.xy;
                float lodMask = ComputeTextureLOD(sampleMaskDdx, sampleMaskDdy, _DecalAtlasResolution);

                float decalBlend = decalData.normalToWorld[0][3];
				float4 src = decalData.baseColor;
				int diffuseTextureBound = (decalData.diffuseScaleBias.x > 0) && (decalData.diffuseScaleBias.y > 0);
                
				ApplyBlendDiffuse(DBuffer0, mask, sampleDiffuse, src, DBUFFERHTILEBIT_DIFFUSE, decalBlend, lodDiffuse, diffuseTextureBound);		
				alpha = alpha < decalBlend ? decalBlend : alpha;    // use decal alpha if it is higher than transparent alpha

				float albedoContribution = decalData.normalToWorld[1][3];
				if (albedoContribution == 0.0f)
				{
					mask = 0;	// diffuse will not get modified						
				}
				
                if ((decalData.normalScaleBias.x > 0) && (decalData.normalScaleBias.y > 0))
                {
                    ApplyBlendNormal(DBuffer1, mask, sampleNormal, DBUFFERHTILEBIT_NORMAL, (float3x3)decalData.normalToWorld, decalBlend, lodNormal);
                }

                if ((decalData.maskScaleBias.x > 0) && (decalData.maskScaleBias.y > 0))
                {
                    ApplyBlendMask(DBuffer2, mask, sampleMask, DBUFFERHTILEBIT_MASK, decalBlend, lodMask);
                }
            }
        }
#else
        mask = UnpackByte(LOAD_TEXTURE2D(_DecalHTileTexture, posInput.positionSS / 8).r);
#endif
        DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);
        // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
        if(mask & DBUFFERHTILEBIT_DIFFUSE)
        {
            surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
        }

        if(mask & DBUFFERHTILEBIT_NORMAL)
        {
            surfaceData.normalWS.xyz = normalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
        }
        if(mask & DBUFFERHTILEBIT_MASK)
        {
            surfaceData.metallic = surfaceData.metallic * decalSurfaceData.mask.w + decalSurfaceData.mask.x;
            surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.mask.w + decalSurfaceData.mask.y;
            surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
        }
    }
}
