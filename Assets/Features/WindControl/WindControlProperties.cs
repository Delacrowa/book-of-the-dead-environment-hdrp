
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[Serializable]
public sealed class ExposedWindControlReferenceParameter : VolumeParameter<ExposedReference<WindControl>> {
    public ExposedWindControlReferenceParameter(ExposedReference<WindControl> value, bool overrideState = false)
        : base(value, overrideState) {
    }
}

[Serializable]
public class WindControlProperties : PropertyVolumeComponent<WindControlProperties> {
    public ExposedWindControlReferenceParameter target =
        new ExposedWindControlReferenceParameter(default(ExposedReference<WindControl>));

    // Global
    public ClampedFloatParameter windGlobalStrengthScale = new ClampedFloatParameter(1f, 0f, 3f);
    public ClampedFloatParameter windGlobalStrengthAudioInfluence = new ClampedFloatParameter(0f, 0f, 1f);
    public ClampedFloatParameter windGlobalStrengthAudioAmplitudeThreshold = new ClampedFloatParameter(0.05f, 0f, 1f);
    public ClampedFloatParameter windDirection = new ClampedFloatParameter(65f, 0f, 360f);
    public ClampedFloatParameter windDirectionVariance = new ClampedFloatParameter(25f, 0f, 30f);
    public ClampedFloatParameter windDirectionVariancePeriod = new ClampedFloatParameter(15f, 0.01f, 20f);
    public ClampedFloatParameter windZoneIntensityOffset = new ClampedFloatParameter(0.1f, 0f, 5f);
    public ClampedFloatParameter windZoneIntensityBaseScale = new ClampedFloatParameter(0.25f, 0f, 5f);
    public ClampedFloatParameter windZoneIntensityGustScale = new ClampedFloatParameter(0.5f, 0f, 5f);
    public BoolParameter windZoneIntensityFromGrass = new BoolParameter(true);

    // Grass Base
    public ClampedFloatParameter windBaseStrength = new ClampedFloatParameter(15f, 0f, 75f);
    public ClampedFloatParameter windBaseStrengthOffset = new ClampedFloatParameter(0.25f, 0f, 3f);
    public ClampedFloatParameter windBaseStrengthPhase = new ClampedFloatParameter(3f, 0f, 10f);
    public ClampedFloatParameter windBaseStrengthVariancePeriod = new ClampedFloatParameter(10f, 0.01f, 20f);

    // Grass Gust
    public ClampedFloatParameter windGustStrength = new ClampedFloatParameter(25f, 0f, 75f);
    public ClampedFloatParameter windGustStrengthOffset = new ClampedFloatParameter(1f, 0f, 5f);
    public ClampedFloatParameter windGustStrengthPhase = new ClampedFloatParameter(3f, 0f, 10f);
    public ClampedFloatParameter windGustStrengthVariancePeriod = new ClampedFloatParameter(2f, 0.01f, 10f);
    public ClampedFloatParameter windGustInnerCosScale = new ClampedFloatParameter(2f, 0f, 5f);

    // Grass Flutter
    public ClampedFloatParameter windFlutterStrength = new ClampedFloatParameter(0.4f, 0f, 10f);
    public ClampedFloatParameter windFlutterGustStrength = new ClampedFloatParameter(0.2f, 0f, 10f);
    public ClampedFloatParameter windFlutterGustStrengthOffset = new ClampedFloatParameter(50f, 0f, 75f);
    public ClampedFloatParameter windFlutterGustStrengthScale = new ClampedFloatParameter(75f, 0f, 75f);
    public ClampedFloatParameter windFlutterGustVariancePeriod = new ClampedFloatParameter(0.25f, 0.01f, 2f);

    // Tree Base
    public ClampedFloatParameter windTreeBaseStrength = new ClampedFloatParameter(0.25f, 0f, 10f);
    public ClampedFloatParameter windTreeBaseStrengthOffset = new ClampedFloatParameter(1f, 0f, 5f);
    public ClampedFloatParameter windTreeBaseStrengthPhase = new ClampedFloatParameter(0.5f, 0f, 10f);
    public ClampedFloatParameter windTreeBaseStrengthVariancePeriod = new ClampedFloatParameter(6f, 0.01f, 20f);

    // Tree Gust
    public ClampedFloatParameter windTreeGustStrength = new ClampedFloatParameter(3f, 0f, 10f);
    public ClampedFloatParameter windTreeGustStrengthOffset = new ClampedFloatParameter(1f, 0f, 5f);
    public ClampedFloatParameter windTreeGustStrengthPhase = new ClampedFloatParameter(2f, 0f, 10f);
    public ClampedFloatParameter windTreeGustStrengthVariancePeriod = new ClampedFloatParameter(4f, 0.01f, 10f);
    public ClampedFloatParameter windTreeGustInnerCosScale = new ClampedFloatParameter(2f, 0f, 5f);

    // Tree Flutter
    public ClampedFloatParameter windTreeFlutterStrength = new ClampedFloatParameter(0.1f, 0f, 5f);
    public ClampedFloatParameter windTreeFlutterGustStrength = new ClampedFloatParameter(0.5f, 0f, 5f);
    public ClampedFloatParameter windTreeFlutterGustStrengthOffset = new ClampedFloatParameter(12.5f, 0f, 75f);
    public ClampedFloatParameter windTreeFlutterGustStrengthScale = new ClampedFloatParameter(25f, 0f, 75f);
    public ClampedFloatParameter windTreeFlutterGustVariancePeriod = new ClampedFloatParameter(0.1f, 0.01f, 2f);

    public override void OverrideProperties(PropertyMaster master) {
        var control = target.value.Resolve(master);
        if (!control)
            return;

        // Global
        Override(windGlobalStrengthScale, ref control.windGlobalStrengthScale);

        // Ship hack: let wind ambience influence wind strength
        if (Application.isPlaying) {
            float threshold = 0.05f;
            Override(windGlobalStrengthAudioAmplitudeThreshold, ref threshold);
            AxelF.Heartbeat.ambienceWindThreshold = threshold;

            float influence = 0f;
            Override(windGlobalStrengthAudioInfluence, ref influence);
            control.windGlobalStrengthScale +=
                control.windGlobalStrengthScale * AxelF.Heartbeat.ambienceWindStrength * influence;
        }

        Override(windDirection, ref control.windDirection);
        Override(windDirectionVariance, ref control.windDirectionVariance);
        Override(windDirectionVariancePeriod, ref control.windDirectionVariancePeriod);
        Override(windZoneIntensityOffset, ref control.windZoneIntensityOffset);
        Override(windZoneIntensityBaseScale, ref control.windZoneIntensityBaseScale);
        Override(windZoneIntensityGustScale, ref control.windZoneIntensityGustScale);
        Override(windZoneIntensityFromGrass, ref control.windZoneIntensityFromGrass);

        // Grass Base
        Override(windBaseStrength, ref control.windBaseStrength);
        Override(windBaseStrengthOffset, ref control.windBaseStrengthOffset);
        Override(windBaseStrengthPhase, ref control.windBaseStrengthPhase);
        Override(windBaseStrengthVariancePeriod, ref control.windBaseStrengthVariancePeriod);

        // Grass Gust
        Override(windGustStrength, ref control.windGustStrength);
        Override(windGustStrengthOffset, ref control.windGustStrengthOffset);
        Override(windGustStrengthPhase, ref control.windGustStrengthPhase);
        Override(windGustStrengthVariancePeriod, ref control.windGustStrengthVariancePeriod);
        Override(windGustInnerCosScale, ref control.windGustInnerCosScale);

        // Grass Flutter
        Override(windFlutterStrength, ref control.windFlutterStrength);
        Override(windFlutterGustStrength, ref control.windFlutterGustStrength);
        Override(windFlutterGustStrengthOffset, ref control.windFlutterGustStrengthOffset);
        Override(windFlutterGustStrengthScale, ref control.windFlutterGustStrengthScale);
        Override(windFlutterGustVariancePeriod, ref control.windFlutterGustVariancePeriod);

        // Tree Base
        Override(windTreeBaseStrength, ref control.windTreeBaseStrength);
        Override(windTreeBaseStrengthOffset, ref control.windTreeBaseStrengthOffset);
        Override(windTreeBaseStrengthPhase, ref control.windTreeBaseStrengthPhase);
        Override(windTreeBaseStrengthVariancePeriod, ref control.windTreeBaseStrengthVariancePeriod);

        // Tree Gust
        Override(windTreeGustStrength, ref control.windTreeGustStrength);
        Override(windTreeGustStrengthOffset, ref control.windTreeGustStrengthOffset);
        Override(windTreeGustStrengthPhase, ref control.windTreeGustStrengthPhase);
        Override(windTreeGustStrengthVariancePeriod, ref control.windTreeGustStrengthVariancePeriod);
        Override(windTreeGustInnerCosScale, ref control.windTreeGustInnerCosScale);

        // Tree Flutter
        Override(windTreeFlutterStrength, ref control.windTreeFlutterStrength);
        Override(windTreeFlutterGustStrength, ref control.windTreeFlutterGustStrength);
        Override(windTreeFlutterGustStrengthOffset, ref control.windTreeFlutterGustStrengthOffset);
        Override(windTreeFlutterGustStrengthScale, ref control.windTreeFlutterGustStrengthScale);
        Override(windTreeFlutterGustVariancePeriod, ref control.windTreeFlutterGustVariancePeriod);
    }
}

