#include "CoreRP/ShaderLibrary/Macros.hlsl"

//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

void ApplyDebug(LightLoopContext lightLoopContext, float3 positionWS, inout float3 diffuseLighting, inout float3 specularLighting)
{
#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING)
    {
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_SPECULAR_LIGHTING)
    {
        diffuseLighting = float3(0.0, 0.0, 0.0); // Disable diffuse lighting
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
        // Take the luminance
        diffuseLighting = Luminance(diffuseLighting).xxx;
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_VISUALIZE_CASCADE)
    {
        specularLighting = float3(0.0, 0.0, 0.0);

        const float3 s_CascadeColors[] = {
            float3(1.0, 0.0, 0.0),
            float3(0.0, 1.0, 0.0),
            float3(0.0, 0.0, 1.0),
            float3(1.0, 1.0, 0.0),
            float3(1.0, 1.0, 1.0)
        };

        diffuseLighting = float3(1.0, 1.0, 1.0);
        if (_DirectionalLightCount > 0)
        {
            int shadowIdx = _DirectionalLightDatas[0].shadowIndex;
            float shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, float3(0.0, 1.0, 0.0 ), shadowIdx, -_DirectionalLightDatas[0].forward, float2(0.0, 0.0));
            uint  payloadOffset;
            real  alpha;
            int cascadeCount;
            int shadowSplitIndex = EvalShadow_GetSplitIndex(lightLoopContext.shadowContext, shadowIdx, positionWS, payloadOffset, alpha, cascadeCount);
            if (shadowSplitIndex >= 0)
            {
                diffuseLighting = lerp(s_CascadeColors[shadowSplitIndex], s_CascadeColors[shadowSplitIndex+1], alpha) * shadow;
            }

        }
    }

    // We always apply exposure when in debug mode. The exposure value will be at a neutral 0.0 when not needed.
    diffuseLighting *= exp2(_DebugExposure);
    specularLighting *= exp2(_DebugExposure);
#endif
}

// bakeDiffuseLighting is part of the prototype so a user is able to implement a "base pass" with GI and multipass direct light (aka old unity rendering path)
void LightLoop( float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, BakeLightingData bakeLightingData, uint featureFlags,
                out float3 diffuseLighting,
                out float3 specularLighting)
{
    LightLoopContext context;
    context.sampleReflection = 0;
    context.shadowContext = InitShadowContext();
    context.contactShadow = InitContactShadow(posInput);

    // This struct is define in the material. the Lightloop must not access it
    // PostEvaluateBSDF call at the end will convert Lighting to diffuse and specular lighting
    AggregateLighting aggregateLighting;
    ZERO_INITIALIZE(AggregateLighting, aggregateLighting); // LightLoop is in charge of initializing the struct

    uint i = 0; // Declare once to avoid the D3D11 compiler warning.

    if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        for (i = 0; i < _DirectionalLightCount; ++i)
        {
            DirectLighting lighting = EvaluateBSDF_Directional(context, V, posInput, preLightData, _DirectionalLightDatas[i], bsdfData, bakeLightingData);
            AccumulateDirectLighting(lighting, aggregateLighting);
        }
    }

    if (featureFlags & LIGHTFEATUREFLAGS_PUNCTUAL)
    {
        uint lightCount, lightStart;

    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, lightStart, lightCount);
    #else
        lightCount = _PunctualLightCount;
        lightStart = 0;
    #endif

        for (i = 0; i < lightCount; i++)
        {
            LightData lightData = FetchLight(lightStart, i);

            DirectLighting lighting = EvaluateBSDF_Punctual(context, V, posInput, preLightData, lightData, bsdfData, bakeLightingData);
            AccumulateDirectLighting(lighting, aggregateLighting);
        }
    }

    if (featureFlags & LIGHTFEATUREFLAGS_AREA)
    {
        uint lightCount, lightStart;

    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_AREA, lightStart, lightCount);
    #else
        lightCount = _AreaLightCount;
        lightStart = _PunctualLightCount;
    #endif

        // COMPILER BEHAVIOR WARNING!
        // If rectangle lights are before line lights, the compiler will duplicate light matrices in VGPR because they are used differently between the two types of lights.
        // By keeping line lights first we avoid this behavior and save substantial register pressure.
        // TODO: This is based on the current Lit.shader and can be different for any other way of implementing area lights, how to be generic and ensure performance ?

        if (lightCount > 0)
        {
            i = 0;

            uint      last      = lightCount - 1;
            LightData lightData = FetchLight(lightStart, i);

            while (i <= last && lightData.lightType == GPULIGHTTYPE_LINE)
            {
                lightData.lightType = GPULIGHTTYPE_LINE; // Enforce constant propagation

                DirectLighting lighting = EvaluateBSDF_Area(context, V, posInput, preLightData, lightData, bsdfData, bakeLightingData);
                AccumulateDirectLighting(lighting, aggregateLighting);

                lightData = FetchLight(lightStart, min(++i, last));
            }

            while (i <= last) // GPULIGHTTYPE_RECTANGLE
            {
                lightData.lightType = GPULIGHTTYPE_RECTANGLE; // Enforce constant propagation

                DirectLighting lighting = EvaluateBSDF_Area(context, V, posInput, preLightData, lightData, bsdfData, bakeLightingData);
                AccumulateDirectLighting(lighting, aggregateLighting);

                lightData = FetchLight(lightStart, min(++i, last));
            }
        }
    }

    // Define macro for a better understanding of the loop
#define EVALUATE_BSDF_ENV(envLightData, TYPE, type) \
    IndirectLighting lighting = EvaluateBSDF_Env(context, V, posInput, preLightData, envLightData, bsdfData, envLightData.influenceShapeType, MERGE_NAME(GPUIMAGEBASEDLIGHTINGTYPE_, TYPE), MERGE_NAME(type, HierarchyWeight)); \
    AccumulateIndirectLighting(lighting, aggregateLighting);

    // First loop iteration
    if (featureFlags & (LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_SSREFRACTION | LIGHTFEATUREFLAGS_SSREFLECTION))
    {
        float reflectionHierarchyWeight = 0.0; // Max: 1.0
        float refractionHierarchyWeight = 0.0; // Max: 1.0

        uint envLightStart, envLightCount;

        // Fetch first env light to provide the scene proxy for screen space computation
    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_ENV, envLightStart, envLightCount);
    #else
        envLightCount = _EnvLightCount;
        envLightStart = 0;
    #endif

        // Reflection / Refraction hierarchy is
        //  1. Screen Space Refraction / Reflection
        //  2. Environment Reflection / Refraction
        //  3. Sky Reflection / Refraction
        EnvLightData envLightData;
        if (envLightCount > 0)
        {
            envLightData = FetchEnvLight(envLightStart, 0);
        }
        else
        {
            envLightData = InitSkyEnvLightData(0);
        }

        if (featureFlags & LIGHTFEATUREFLAGS_SSREFLECTION)
        {
            IndirectLighting lighting = EvaluateBSDF_SSLighting(    context, V, posInput, preLightData, bsdfData, envLightData,
                                                                    GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION, reflectionHierarchyWeight);
            AccumulateIndirectLighting(lighting, aggregateLighting);
        }

        if (featureFlags & LIGHTFEATUREFLAGS_SSREFRACTION)
        {
            IndirectLighting lighting = EvaluateBSDF_SSLighting(    context, V, posInput, preLightData, bsdfData, envLightData,
                                                                    GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION, refractionHierarchyWeight);
            AccumulateIndirectLighting(lighting, aggregateLighting);
        }

        // Reflection probes are sorted by volume (in the increasing order).
        if (featureFlags & LIGHTFEATUREFLAGS_ENV)
        {
            context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;

            // Note: In case of IBL we are sorted from smaller to bigger projected solid angle bounds. We are not sorted by type so we can't do a 'while' approach like for area light.
            for (i = 0; i < envLightCount && reflectionHierarchyWeight < 1.0; ++i)
            {
                EVALUATE_BSDF_ENV(FetchEnvLight(envLightStart, i), REFLECTION, reflection);
            }

            // Refraction probe and reflection probe will process exactly the same weight. It will be good for performance to be able to share this computation
            // However it is hard to deal with the fact that reflectionHierarchyWeight and refractionHierarchyWeight have not the same values, they are independent
            // The refraction probe is rarely used and happen only with sphere shape and high IOR. So we accept the slow path that use more simple code and
            // doesn't affect the performance of the reflection which is more important.
            // We reuse LIGHTFEATUREFLAGS_SSREFRACTION flag as refraction is mainly base on the screen. Would be a waste to not use screen and only cubemap.
            if (featureFlags & LIGHTFEATUREFLAGS_SSREFRACTION)
            {
                for (i = 0; i < envLightCount && refractionHierarchyWeight < 1.0; ++i)
                {
                    EVALUATE_BSDF_ENV(FetchEnvLight(envLightStart, i), REFRACTION, refraction);
                }
            }
        }

        // Only apply the sky IBL if the sky texture is available
        if ((featureFlags & LIGHTFEATUREFLAGS_SKY) && _EnvLightSkyEnabled)
        {
            // The sky is a single cubemap texture separate from the reflection probe texture array (different resolution and compression)
            context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;

            // The sky data are generated on the fly so the compiler can optimize the code
            EnvLightData envLightSky = InitSkyEnvLightData(0);

            // Only apply the sky if we haven't yet accumulated enough IBL lighting.
            if (reflectionHierarchyWeight < 1.0)
            {
                EVALUATE_BSDF_ENV(envLightSky, REFLECTION, reflection);
            }

            if (featureFlags & LIGHTFEATUREFLAGS_SSREFRACTION)
            {
                if (refractionHierarchyWeight < 1.0)
                {
                    EVALUATE_BSDF_ENV(envLightSky, REFRACTION, refraction);
                }
            }
        }
    }
#undef EVALUATE_BSDF_ENV

    // Also Apply indiret diffuse (GI)
    // PostEvaluateBSDF will perform any operation wanted by the material and sum everything into diffuseLighting and specularLighting
    PostEvaluateBSDF(   context, V, posInput, preLightData, bsdfData, bakeLightingData, aggregateLighting,
                        diffuseLighting, specularLighting);

    ApplyDebug(context, posInput.positionWS, diffuseLighting, specularLighting);
}
