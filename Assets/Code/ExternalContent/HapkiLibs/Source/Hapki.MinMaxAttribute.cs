
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using System;
using UnityEngine;
using Hapki.Randomization;

namespace Hapki {

[Serializable]
public struct MinMaxFloat {
    public float min;
    public float max;

    public float GetRandomValue() {
        return GetRangedValue(Randomizer.zeroToOne);
    }

    public float GetRangedValue(float v) {
        return v * (max - min) + min;
    }

    public float GetClampedValue(float v) {
        return Mathf.Clamp(v, min, max);
    }
}

public class MinMaxAttribute : PropertyAttribute {
    public float min;
    public float max;
    public bool colorize;

    public MinMaxAttribute(float mv, float nv) {
        min = mv;
        max = nv;
    }
}

} // Hapki

