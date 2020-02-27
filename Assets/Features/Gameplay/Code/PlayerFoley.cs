
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using TerrainFoley;

namespace Gameplay {

[DisallowMultipleComponent]
public class PlayerFoley : MonoBehaviour {
    public enum BreathType {
        Normal,
        Fast,
        Animated
    }

    public PlayerFoleyAsset foley;

    [Serializable]
    struct BreathingState {
        float _time;
        float _period;
        int _state;
        float _pace;
        float _intensity;

        BreathType _setType;
        float _setPace;
        float _setIntensity;

        public bool initialized { get { return _state != 0; }}
        public bool inhaling { get { return _state >= 0; }}

        public void Initialize(PlayerFoleyAsset foley) {
            _time = foley ? foley.breathing.GetInitialDelay() : 0f;
            _period = 0f;
            _state = 1;
            _pace = 0f;
            _intensity = 0f;

            _setType = BreathType.Normal;
            _setPace = 0f;
            _setIntensity = 0f;
        }

        public void OnJump() {
            if (inhaling && Mathf.Abs(_time - _period) >= 1f)
                _time = 0f;
        }

        public void OnLand() {
            if (!inhaling && Mathf.Abs(_time - _period) >= 0.667f)
                _time = 0f;
        }

        public bool Update(
                PlayerFoleyAsset foley, float intensity, BreathType type, bool isJumping,
                out AxelF.Patch asset, out float volume) {
            if (foley && _state != 0) {
                float deltaTime = Time.deltaTime;

                if (isJumping)
                    deltaTime = 0f; // infinite time dilation when jumping to synchronize landing

                _intensity += (intensity - foley.breathing.intensityDampening) * deltaTime;
                _intensity = Mathf.Clamp01(_intensity);

                _pace = Mathf.Lerp(
                    _pace, _intensity * _intensity * _intensity,
                    deltaTime * foley.breathing.intensityTransference);

                _period = foley.breathing.GetBreathingPeriod() -
                    _intensity * foley.breathing.breathingPeriodIntensityFactor;

                _time -= deltaTime / _period;

                if (_time <= 0f) {
                    if (inhaling) {
                        _time = foley.breathing.GetExhalePeriod();
                        _setType = type;
                        _setPace = _pace;
                        _setIntensity = _intensity;
                    } else {
                        _time = foley.breathing.GetInhalePeriod();
                        _time -= _time * _setPace * foley.breathing.inhalePeriodPacingFactor;
                    }
                    asset = foley.breathing.GetAsset(_setType, _setPace, _setIntensity, inhaling);
                    volume = foley.breathing.GetVolumeOverPace();
                    volume = Mathf.Clamp01(volume + _pace * volume);
                    _state = -_state;
                    return true;
                }
            }
            asset = null;
            volume = 0f;
            return false;
        }
    }

    [Serializable]
    struct VegetationState {
        public Vector3 position;
        public VegetationType type;
        public bool intersecting;

        public void Update(Bounds bounds) {
            var terrainFoley = TerrainFoleyManager.current;
            if (terrainFoley)
                intersecting = terrainFoley.QueryVegetation(bounds, out position, out type);
        }
    }

    BreathingState _breathingState;
    VegetationState _vegetationState;
    FoleyMap _foleyMap;
    Dictionary<PhysicMaterial, int> _materialMap;
    bool _mapInitialized;

    public float breathingIntensity { get; set; }
    public BreathType breathType { get; set; }
    public bool playerJumping { get; set; }
    public Bounds playerBounds { get; set; }

    public bool intersectingVegetation { get { return _vegetationState.intersecting; }}

    AxelF.Patch GetFootstepAsset(
            PlayerFoleyAsset foley, Vector3 position, PhysicMaterial physicalMaterial,
            float speedScalar, VegetationType type, bool landing = false) {
        var footstep = default(PlayerFoleyAsset.Footstep);
        int footstepIndex;
        bool validFootstep = false;

        if (physicalMaterial != null)
            if (_materialMap.TryGetValue(physicalMaterial, out footstepIndex)) {
                footstep = foley.footsteps[footstepIndex];
                validFootstep = true;
            }

        if (!validFootstep) {
            var terrainFoley = TerrainFoleyManager.current;
            footstepIndex = _foleyMap.GetFoleyIndexAtPosition(position, terrainFoley.splatMap);
            footstep = foley.footsteps[footstepIndex];
            validFootstep = true;
        }

        Debug.Assert(validFootstep);

        bool running = speedScalar >= 0.7f;
        bool jogging = !running && speedScalar >= 0.2f;

        var asset =
            type == VegetationType.None ?
                (landing ? footstep.landing :
                    running ? footstep.running :
                    jogging ? footstep.jogging :
                    footstep.walking) :
            type == VegetationType.Undergrowth ?
                (running ? footstep.runningUndergrowth :
                    jogging ? footstep.joggingUndergrowth :
                    footstep.walkingUndergrowth) :
            null;

        return asset;
    }

    PlayerFoleyAsset GetFoleyAsset() {
        int overrideCount = PlayerFoleyZone.overrides.Count;
        while (overrideCount > 0) {
            var @override = PlayerFoleyZone.overrides[overrideCount - 1].foley;
            if (@override)
                return @override;
            --overrideCount;
        }
        return foley;
    }

    public void PlayFootstep(
            Transform transform, Vector3 position, Vector3 normal, PhysicMaterial physicalMaterial,
            float speedScalar, bool landing = false) {
        var foley = GetFoleyAsset();

        if (foley && foley.footsteps.Length > 0) {
            var asset = GetFootstepAsset(foley, position, physicalMaterial, speedScalar, VegetationType.None, landing);

            float elevationFactor = Vector3.Dot(normal, Vector3.up);
            float attackScalar = elevationFactor * elevationFactor * elevationFactor;
            var envelope = new AxelF.Parameters.EnvelopeParams {
                attack = Mathf.Lerp(
                    0f,
                    Mathf.Clamp01(
                        (landing ? 0.1f : 1f) *
                        foley.footstepElevationAttenuation * (1f - speedScalar)),
                    attackScalar)
            };
            float volume = Mathf.Lerp(1f - foley.footstepSpeedAttenuation, 1f, speedScalar);
            bool looping;

            AxelF.Synthesizer.KeyOn(out looping, asset, envelope, transform, volume: volume);
        }
    }

    public void PlayVegetation(Transform transform, Vector3 position, float speedScalar) {
        var foley = GetFoleyAsset();

        if (foley && foley.footsteps.Length > 0) {
            var asset = GetFootstepAsset(foley, position, null, speedScalar, _vegetationState.type);

            float volume = Mathf.Lerp(1f - foley.footstepSpeedAttenuation, 1f, speedScalar);
            bool looping;

            AxelF.Synthesizer.KeyOn(
                out looping, asset, pos: _vegetationState.position, volume: volume);
        }
    }

    public void OnJump() {
        _breathingState.OnJump();
    }

    public void OnLand() {
        _breathingState.OnLand();
    }

    protected void LateUpdate() {
        var foley = GetFoleyAsset();

        if (!_mapInitialized && foley) {
            var terrainFoley = TerrainFoleyManager.current;

            if (terrainFoley) {
                _materialMap = new Dictionary<PhysicMaterial, int>();

                var list = new List<string>(foley.footsteps.Length);
                for (int i = 0, n = foley.footsteps.Length; i < n; ++i) {
                    var footstep = foley.footsteps[i];
                    list.Add(footstep.name);

                    if (footstep.physicalMaterial)
                        _materialMap.Add(footstep.physicalMaterial, i);
                }

                _foleyMap.Initialize(list.ToArray(), terrainFoley.splatMap);
                _mapInitialized = true;
            }
        }

        if (!_breathingState.initialized && foley)
            _breathingState.Initialize(foley);

        if (_breathingState.initialized) {
            AxelF.Patch patch;
            float volume;

            if (_breathingState.Update(
                    foley, breathingIntensity, breathType, playerJumping, out patch, out volume)) {
                bool looping;
                AxelF.Synthesizer.KeyOn(out looping, patch, null, Vector3.zero, 0f, volume);
            }
        }

        _vegetationState.Update(playerBounds);
    }
}

} // Gameplay

