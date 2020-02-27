
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Hapki.Interpolation;
using Hapki.Solvers;
using Gameplay.Utilities;

namespace Gameplay {

[DisallowMultipleComponent]
public class FirstPersonCamera : PlayerCamera {
    public float pitch {
        get { return _pitch; }
        set { _pitch.Target(value); }
    }
    public float yaw {
        get { return _yaw; }
        set { _yaw.Target(value); }
    }
    public float roll {
        get { return _roll; }
        set { _roll.Target(value); }
    }

    public Quaternion targetRotation {
        get { return Quaternion.Euler(_pitch.target, _yaw.target, _roll.target); }
    }

    [Space(9)]
    [Range(0, 10)] public float eyeHeight = 1.8f;
    [Range(0, 10)] public float interpolationSpeed = 6f;

    [Space(9)]
    [Range(0, 10)] public float springMass = 2f;
    [Range(0, 10)] public float springDampening = 1.15f;
    public Vector3 springCoefficients = new Vector3(0.3f, 0.45f, 0.3f);

    struct LinearAngle : IInterpolation {
        public float Interpolate(float x, float y, float t) {
            return Mathf.LerpAngle(x, y, t);
        }
    }

    Interpolator<LinearAngle> _pitch;
    Interpolator<LinearAngle> _yaw;
    Interpolator<LinearAngle> _roll;
    Spring _spring;

    public override void Simulate(Vector3 playerPosition, Vector3 playerAngles, float deltaTime) {
        var transform = this.transform;
        float interpolationDeltaTime = deltaTime * interpolationSpeed;

        _pitch.Update(interpolationDeltaTime);
        _yaw.Update(interpolationDeltaTime);
        _roll.Update(interpolationDeltaTime);

        _pitch.time = 0f;
        _yaw.time = 0f;
        _roll.time = 0f;

        var position = playerPosition;
        var angles = playerAngles;

        // spring

        position = _spring.Update(
            deltaTime * interpolationSpeed,
            position, springMass, springDampening, springCoefficients);

        // craning

        float ax = Angles.ToRelative(pitch);
        float ay = Mathf.DeltaAngle(angles.y, yaw);
        float dy = 1f - Mathf.Cos(ax * Mathf.Deg2Rad);
        float dz = 0f - Mathf.Sin(ax * Mathf.Deg2Rad);
        float ex = 0f - Mathf.Sin(ay * Mathf.Deg2Rad);
        float ez = 1f - Mathf.Cos(ay * Mathf.Deg2Rad);

        ex *= 1f - Mathf.Max(dz + 0.5f, 0f);
        ez *= 1f - Mathf.Max(dz + 0.5f, 0f);

        ex *= Mathf.Lerp(0.15f, 0.30f, Mathf.Abs(dz));
        ez *= Mathf.Lerp(0.15f, 0.25f, Mathf.Abs(dz));
        dy *= 0.05f;
        dz *= 0.35f;

        position.y += eyeHeight;
        position -= Quaternion.Euler(angles) * (new Vector3(0f, dy, dz) + new Vector3(ex, 0f, ez));

        transform.localPosition = position;
        transform.localRotation = Quaternion.Euler(pitch, yaw, roll);
    }

    public override void Warp(Vector3 position, Vector3 angles) {
        _spring.position = position;
        _spring.velocity = Vector3.zero;

        _pitch.value = angles.x + (_pitch.value - _pitch.target);
        _pitch.target = angles.x;
        _yaw.value = angles.y + (_yaw.value - _yaw.target);
        _yaw.target = angles.y;
        _roll.value = angles.z + (_roll.value - _roll.target);
        _roll.target = angles.z;
    }
}

} // Gameplay

