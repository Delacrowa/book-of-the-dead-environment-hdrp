
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[Serializable]
public sealed class ExposedAtmosphericScatteringReferenceParameter :
        VolumeParameter<ExposedReference<AtmosphericScattering>> {
    public ExposedAtmosphericScatteringReferenceParameter(
            ExposedReference<AtmosphericScattering> value, bool overrideState = false)
        : base(value, overrideState) {
    }
}

[Serializable]
public sealed class OcclusionDownscaleParameter : VolumeParameter<AtmosphericScattering.OcclusionDownscale> {
    public OcclusionDownscaleParameter(AtmosphericScattering.OcclusionDownscale value, bool overrideState = false)
        : base(value, overrideState) {
    }
}

[Serializable]
public sealed class OcclusionSamplesParameter : VolumeParameter<AtmosphericScattering.OcclusionSamples> {
    public OcclusionSamplesParameter(AtmosphericScattering.OcclusionSamples value, bool overrideState = false)
        : base(value, overrideState) {
    }
}

[Serializable]
public class AtmosphericScatteringProperties : PropertyVolumeComponent<AtmosphericScatteringProperties> {
    public ExposedAtmosphericScatteringReferenceParameter target =
        new ExposedAtmosphericScatteringReferenceParameter(default(ExposedReference<AtmosphericScattering>));

    public GradientParameter worldRayleighColorRamp = new GradientParameter(new Gradient());
    public FloatParameter worldRayleighColorIntensity = new FloatParameter(1f);
    public FloatParameter worldRayleighDensity = new FloatParameter(150f);
    public FloatParameter worldRayleighExtinctionFactor = new FloatParameter(1.4f);
    public FloatParameter worldRayleighIndirectScatter = new FloatParameter(0.4f);

    public GradientParameter worldMieColorRamp = new GradientParameter(new Gradient());
    public FloatParameter worldMieColorIntensity = new FloatParameter(1f);
    public FloatParameter worldMieDensity = new FloatParameter(20f);
    public FloatParameter worldMieExtinctionFactor = new FloatParameter(1f);
    public FloatParameter worldMiePhaseAnisotropy = new FloatParameter(0.9f);

    public FloatParameter worldNearScatterPush = new FloatParameter(0f);
    public FloatParameter worldNormalDistance = new FloatParameter(900f);

    public ColorParameter heightRayleighColor = new ColorParameter(Color.white);
    public FloatParameter heightRayleighIntensity = new FloatParameter(1f);
    public FloatParameter heightRayleighDensity = new FloatParameter(20f);
    public FloatParameter heightMieDensity = new FloatParameter(100f);
    public FloatParameter heightExtinctionFactor = new FloatParameter(1.1f);
    public FloatParameter heightSeaLevel = new FloatParameter(1.63f);
    public FloatParameter heightDistance = new FloatParameter(5.16f);
    public Vector3Parameter heightPlaneShift = new Vector3Parameter(Vector3.zero);
    public FloatParameter heightNearScatterPush = new FloatParameter(19f);
    public FloatParameter heightNormalDistance = new FloatParameter(100f);

    public Vector3Parameter skyDomeScale = new Vector3Parameter(Vector3.one);
    public Vector3Parameter skyDomeRotation = new Vector3Parameter(Vector3.zero);
    public TransformParameter skyDomeTrackedYawRotation = new TransformParameter(null);
    public BoolParameter skyDomeVerticalFlip = new BoolParameter(false);
    public CubemapParameter skyDomeCube = new CubemapParameter(null);
    public FloatParameter skyDomeExposure = new FloatParameter(1f);
    public ColorParameter skyDomeTint = new ColorParameter(Color.white);

    public BoolParameter useOcclusion = new BoolParameter(true);
    public FloatParameter occlusionBias = new FloatParameter(0f);
    public FloatParameter occlusionBiasIndirect = new FloatParameter(0.1f);
    public FloatParameter occlusionBiasClouds = new FloatParameter(0.1f);
    public OcclusionDownscaleParameter occlusionDownscale =
        new OcclusionDownscaleParameter(AtmosphericScattering.OcclusionDownscale.x4);
    public OcclusionSamplesParameter occlusionSamples =
        new OcclusionSamplesParameter(AtmosphericScattering.OcclusionSamples.x64);
    public BoolParameter occlusionDepthFixup = new BoolParameter(false);
    public FloatParameter occlusionDepthThreshold = new FloatParameter(25f);
    public BoolParameter occlusionFullSky = new BoolParameter(false);
    public FloatParameter occlusionBiasSkyRayleigh = new FloatParameter(0.2f);
    public FloatParameter occlusionBiasSkyMie = new FloatParameter(0.4f);

    public FloatParameter worldScaleExponent = new FloatParameter(1f);
    public BoolParameter forcePerPixel = new BoolParameter(true);
    public BoolParameter forcePostEffect = new BoolParameter(false);

    public override void OverrideProperties(PropertyMaster master) {
        var scattering = target.value.Resolve(master);
        if (!scattering)
            return;

        Override(worldRayleighColorRamp, ref scattering.worldRayleighColorRamp);
        Override(worldRayleighColorIntensity, ref scattering.worldRayleighColorIntensity);
        Override(worldRayleighDensity, ref scattering.worldRayleighDensity);
        Override(worldRayleighExtinctionFactor, ref scattering.worldRayleighExtinctionFactor);
        Override(worldRayleighIndirectScatter, ref scattering.worldRayleighIndirectScatter);

        Override(worldMieColorRamp, ref scattering.worldMieColorRamp);
        Override(worldMieColorIntensity, ref scattering.worldMieColorIntensity);
        Override(worldMieDensity, ref scattering.worldMieDensity);
        Override(worldMieExtinctionFactor, ref scattering.worldMieExtinctionFactor);
        Override(worldMiePhaseAnisotropy, ref scattering.worldMiePhaseAnisotropy);

        Override(worldNearScatterPush, ref scattering.worldNearScatterPush);
        Override(worldNormalDistance, ref scattering.worldNormalDistance);

        Override(heightRayleighColor, ref scattering.heightRayleighColor);
        Override(heightRayleighIntensity, ref scattering.heightRayleighIntensity);
        Override(heightRayleighDensity, ref scattering.heightRayleighDensity);
        Override(heightMieDensity, ref scattering.heightMieDensity);
        Override(heightExtinctionFactor, ref scattering.heightExtinctionFactor);
        Override(heightSeaLevel, ref scattering.heightSeaLevel);
        Override(heightDistance, ref scattering.heightDistance);
        Override(heightPlaneShift, ref scattering.heightPlaneShift);
        Override(heightNearScatterPush, ref scattering.heightNearScatterPush);
        Override(heightNormalDistance, ref scattering.heightNormalDistance);

        Override(skyDomeScale, ref scattering.skyDomeScale);
        Override(skyDomeRotation, ref scattering.skyDomeRotation);
        Override(skyDomeTrackedYawRotation, ref scattering.skyDomeTrackedYawRotation);
        Override(skyDomeVerticalFlip, ref scattering.skyDomeVerticalFlip);
        Override(skyDomeCube, ref scattering.skyDomeCube);
        Override(skyDomeExposure, ref scattering.skyDomeExposure);
        Override(skyDomeTint, ref scattering.skyDomeTint);

        Override(useOcclusion, ref scattering.useOcclusion);
        Override(occlusionBias, ref scattering.occlusionBias);
        Override(occlusionBiasIndirect, ref scattering.occlusionBiasIndirect);
        Override(occlusionBiasClouds, ref scattering.occlusionBiasClouds);
        Override(occlusionDownscale, ref scattering.occlusionDownscale);
        Override(occlusionSamples, ref scattering.occlusionSamples);
        Override(occlusionDepthFixup, ref scattering.occlusionDepthFixup);
        Override(occlusionDepthThreshold, ref scattering.occlusionDepthThreshold);
        Override(occlusionFullSky, ref scattering.occlusionFullSky);
        Override(occlusionBiasSkyRayleigh, ref scattering.occlusionBiasSkyRayleigh);
        Override(occlusionBiasSkyMie, ref scattering.occlusionBiasSkyMie);

        Override(worldScaleExponent, ref scattering.worldScaleExponent);
        Override(forcePerPixel, ref scattering.forcePerPixel);
        Override(forcePostEffect, ref scattering.forcePostEffect);
    }
}

