
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using System;
using UnityEngine;

namespace Hapki.Interpolation {

public enum Interpolation {
    Linear,
    SmoothStep,
    EaseInQuad,
    EaseOutQuad,
    EaseInCubic,
    EaseOutCubic,
    EaseInSine,
    EaseOutSine,
    EaseInOutSine,
}

public static class Interpolations {
    static readonly IInterpolation[] interpolations;

    static Interpolations() {
#if XXX
        // FIXME: this is not working with il2cpp, throws TypeLoadException
        interpolations = EnumExtensions.GetValuesAsInstances<IInterpolation>(typeof(Interpolation));
#else
        interpolations = new IInterpolation[] {
            new Linear(),
            new SmoothStep(),
            new EaseInQuad(),
            new EaseOutQuad(),
            new EaseInCubic(),
            new EaseOutCubic(),
            new EaseInSine(),
            new EaseOutSine(),
            new EaseInOutSine()
        };
#endif
    }

    public static IInterpolation GetInterpolation(Interpolation i) {
        return interpolations[(int) i];
    }

    public static IInterpolation GetInterpolation(string name) {
        return GetInterpolation(EnumExtensions.Parse<Interpolation>(name));
    }

    public static float Linear(float x, float y, float t) {
        return (y - x) * t + x;
    }

    public static float SmoothStep(float x, float y, float t) {
        float u = -2f * t * t * t + 3f * t * t;
        return y * u + x * (1f - u);
    }

    public static float EaseInQuad(float x, float y, float t) {
        return (y - x) * t * t + x;
    }

    public static float EaseOutQuad(float x, float y, float t) {
        return -(y - x) * t * (t - 2) + x;
    }

    public static float EaseInCubic(float x, float y, float t) {
        return (y - x) * t * t * t + x;
    }

    public static float EaseOutCubic(float x, float y, float t) {
        return (y - x) * ((t - 1) * (t - 1) * (t - 1) + 1f) + x;
    }

    public static float EaseInSine(float x, float y, float t) {
        return -(y - x) * Mathf.Cos(Mathf.PI * 0.5f * t) + (y - x) + x;
    }

    public static float EaseOutSine(float x, float y, float t) {
        return (y - x) * Mathf.Sin(Mathf.PI * 0.5f * t) + x;
    }

    public static float EaseInOutSine(float x, float y, float t) {
        return (y - x) * 0.5f * (1f - Mathf.Cos(Mathf.PI * t)) + x;
    }
}

public interface IInterpolation {
    float Interpolate(float x, float y, float t);
}

public struct Linear : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.Linear(x, y, t);
    }
}

public struct SmoothStep : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.SmoothStep(x, y, t);
    }
}

public struct EaseInQuad : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.EaseInQuad(x, y, t);
    }
}

public struct EaseOutQuad : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.EaseOutQuad(x, y, t);
    }
}

public struct EaseInCubic : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.EaseInCubic(x, y, t);
    }
}

public struct EaseOutCubic : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.EaseOutCubic(x, y, t);
    }
}

public struct EaseInSine : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.EaseInSine(x, y, t);
    }
}

public struct EaseOutSine : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.EaseOutSine(x, y, t);
    }
}

public struct EaseInOutSine : IInterpolation {
    public float Interpolate(float x, float y, float t) {
        return Interpolations.EaseInOutSine(x, y, t);
    }
}

public static class TypedInterpolatorExtensions {
    public static float Interpolate<E, I>(this E e, I i)
            where E : IInterpolation
            where I : IInterpolator {
        return e.Interpolate(i.value, i.target, i.time);
    }
}

public interface IInterpolator {
    float target { get; }
    float value { get; set; }
    float time { get; set; }
}

[Serializable]
public struct Interpolator : IInterpolator {
    public static float Update<I, E>(ref I i, float dt, E e)
            where I : struct, IInterpolator
            where E : IInterpolation {
        i.time = Mathf.Clamp01(i.time + dt);
        i.value = e.Interpolate(i);
        return i.value;
    }

    public float target { get; set; }
    public float value { get; set; }
    public float time { get; set; }

    public void Target(float v) {
        if (!Mathf.Approximately(target, v)) {
            target = v;
            time = 0f;
        }
    }

    public void Reset(float v) {
        target = v;
        value = v;
        time = 0f;
    }

    public float Update<E>()
            where E : struct, IInterpolation {
        return Update(Time.deltaTime, new E());
    }

    public float Update<E>(float dt)
            where E : struct, IInterpolation {
        return Update(dt, new E());
    }

    public float Update<E>(float dt, E e)
            where E : struct, IInterpolation {
        return Update(ref this, dt, e);
    }

    public float Update(float dt, Interpolation i) {
        return Update(ref this, dt, Interpolations.GetInterpolation(i));
    }

    public static implicit operator float(Interpolator i) {
        return i.value;
    }

    public override string ToString() {
        return value.ToString();
    }
}

[Serializable]
public struct Interpolator<E> : IInterpolator
        where E : struct, IInterpolation {
    public float target { get; set; }
    public float value { get; set; }
    public float time { get; set; }

    public void Target(float v) {
        if (!Mathf.Approximately(target, v)) {
            target = v;
            time = 0f;
        }
    }

    public void Reset(float v) {
        target = v;
        value = v;
        time = 0f;
    }

    public float Update() {
        return Interpolator.Update(ref this, Time.deltaTime, new E());
    }

    public float Update(float dt) {
        return Interpolator.Update(ref this, dt, new E());
    }

    public static implicit operator float(Interpolator<E> i) {
        return i.value;
    }

    public override string ToString() {
        return value.ToString();
    }
}

} // Hapki.Interpolation

