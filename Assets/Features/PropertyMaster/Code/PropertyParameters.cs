
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[Serializable]
public sealed class ExposedLightReferenceParameter : VolumeParameter<ExposedReference<Light>> {
    public ExposedLightReferenceParameter(ExposedReference<Light> value, bool overrideState = false)
        : base(value, overrideState) {
    }
}

[Serializable]
public sealed class GradientParameter : VolumeParameter<Gradient> {
    GradientColorKey[] _colorKeys;
    GradientAlphaKey[] _alphaKeys;

    public GradientParameter(Gradient value, bool overrideState = false)
            : base(value, overrideState) {
        _colorKeys = new GradientColorKey[8];
        _alphaKeys = new GradientAlphaKey[8];
    }

	// XXX: this is hardly efficient

	public override void Interp(Gradient from, Gradient to, float t) {
        float atTime = 0f;

        for (int i = 0; i < 8; ++i) {
            var fromColor = from.Evaluate(atTime);
            var toColor = to.Evaluate(atTime);
            var newColor = Color.Lerp(fromColor, toColor, t);

            _colorKeys[i] = new GradientColorKey {
                color = newColor,
                time = atTime
            };
            _alphaKeys[i] = new GradientAlphaKey {
                alpha = newColor.a,
                time = atTime
            };

            atTime += 0.125f;
        }

        m_Value.SetKeys(_colorKeys, _alphaKeys);
    }
}

[Serializable]
public sealed class NoInterpGradientParameter : VolumeParameter<Gradient> {
    public NoInterpGradientParameter(Gradient value, bool overrideState = false)
        : base(value, overrideState) {
    }
}

[Serializable]
public sealed class TransformParameter : VolumeParameter<Transform> {
    public TransformParameter(Transform value, bool overrideState = false)
        : base(value, overrideState) {
    }

    // TODO: Transform interpolation
}

[Serializable]
public sealed class NoInterpTransformParameter : VolumeParameter<Transform> {
    public NoInterpTransformParameter(Transform value, bool overrideState = false)
        : base(value, overrideState) {
    }
}

