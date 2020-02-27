
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay {

using Random = UnityEngine.Random;

[CreateAssetMenu]
public class PlayerFoleyAsset : ScriptableObject {
    [Serializable]
    public struct Footstep {
        public string name;
        public PhysicMaterial physicalMaterial;
        public AxelF.Patch walking;
        public AxelF.Patch walkingUndergrowth;
        public AxelF.Patch jogging;
        public AxelF.Patch joggingUndergrowth;
        public AxelF.Patch running;
        public AxelF.Patch runningUndergrowth;
        public AxelF.Patch landing;
    }

    public Footstep[] footsteps;

    [Tooltip("Amount of attenuation when walking opposed to running")]
    [Range(0, 1)] public float footstepSpeedAttenuation = 0.5f;

    [Tooltip("Amount of attenuation when walking on flat ground opposed to rocky terrain")]
    [Range(0, 1)] public float footstepElevationAttenuation = 0.5f;

    [Serializable]
    public struct Breath {
        public AxelF.Patch inhale;
        public AxelF.Patch exhale;

        public static AxelF.Patch operator%(Breath breath, bool inhaling) {
            return inhaling ? breath.inhale : breath.exhale;
        }
    }

    [Serializable]
    public struct Breathing {
        [Range(0, 5)] public float initialDelay;
        [Range(0, 1)] public float initialDelayVariance;

        public float GetInitialDelay() {
            return initialDelay + Random.Range(-initialDelayVariance, initialDelayVariance);
        }

        [Range(0, 5)] public float inhalePeriod;
        [Range(0, 1)] public float inhalePeriodVariance;
        [Range(0, 1)] public float inhalePeriodPacingFactor;

        public float GetInhalePeriod() {
            return inhalePeriod + Random.Range(-inhalePeriodVariance, inhalePeriodVariance);
        }

        [Range(0, 5)] public float exhalePeriod;
        [Range(0, 1)] public float exhalePeriodVariance;

        public float GetExhalePeriod() {
            return exhalePeriod + Random.Range(-exhalePeriodVariance, exhalePeriodVariance);
        }

        [Range(0, 10)] public float breathingPeriod;
        [Range(0, 1)] public float breathingPeriodVariance;
        [Range(0, 10)] public float breathingPeriodIntensityFactor;

        public float GetBreathingPeriod() {
            return breathingPeriod + Random.Range(-breathingPeriodVariance, breathingPeriodVariance);
        }

        [Range(0, 1)] public float intensityDampening;
        [Range(0, 1)] public float intensityTransference;

        [Range(0, 1)] public float volumeOverPace;
        [Range(0, 1)] public float volumeOverPaceVariance;

        public float GetVolumeOverPace() {
            return volumeOverPace + Random.Range(-volumeOverPaceVariance, volumeOverPaceVariance);
        }

        public Breath slowMouth;
        public Breath slowNose;

        public Breath mediumMouth;
        public Breath mediumNose;

        public Breath fastNose;

        public Breath animatedMouth;
        public Breath animatedNose;

        public Breath fastAnimatedMouth;
        public Breath fastAnimatedNose;

        public AxelF.Patch GetAsset(PlayerFoley.BreathType type, float pace, float intensity, bool inhaling) {
            if (type == PlayerFoley.BreathType.Fast)
                return fastNose % inhaling;

            if (intensity >= 0.5f) {
                if (type == PlayerFoley.BreathType.Animated) {
                    if (pace >= 0.5f)
                        return fastAnimatedMouth % inhaling;
                    return fastAnimatedNose % inhaling;
                }

                if (pace >= 0.5f)
                    return mediumMouth % inhaling;
                return mediumNose % inhaling;
            } else {
                if (type == PlayerFoley.BreathType.Animated) {
                    if (pace >= 0.5f)
                        return animatedMouth % inhaling;
                    return animatedNose % inhaling;
                }

                if (pace >= 0.5f)
                    return slowMouth % inhaling;
                return slowNose % inhaling;
            }
        }
    }

    public Breathing breathing = new Breathing {
        initialDelay = 0.3f,
        initialDelayVariance = 0.1f,
        inhalePeriod = 1.6f,
        inhalePeriodVariance = 0.2f,
        inhalePeriodPacingFactor = 0.3f,
        exhalePeriod = 0.5f,
        exhalePeriodVariance = 0.15f,
        breathingPeriod = 3f,
        breathingPeriodVariance = 0.15f,
        breathingPeriodIntensityFactor = 1.5f,
        intensityDampening = 0.1f,
        intensityTransference = 0.1f,
        volumeOverPace = 0.8f,
        volumeOverPaceVariance = 0.2f
    };
}

} // Gameplay

