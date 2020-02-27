
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using System.Collections.Generic;
using UnityEngine;

namespace Hapki.Randomization {

public struct Randomizer {
    public const float denominator = 1f / (float) 0x80000000;

    public static int seed;
    public static int next { get { return seed = (seed + 35757) * 31313; }}
    public static float plusMinusOne { get { return (float) next * denominator; }}
    public static float zeroToOne { get { return (plusMinusOne + 1f) * 0.5f; }}

    public static int operator%(Randomizer _, int count) {
        return Mathf.FloorToInt(zeroToOne * (count - 1));
    }
}

public struct WeightedRandomizer {
    public List<float> weights;

    public static implicit operator WeightedRandomizer(int _) {
        return new WeightedRandomizer {weights = new List<float>()};
    }

    public static int operator%(WeightedRandomizer rand, int count) {
        float sum;
        int i;
        if (rand.weights.Count != count) {
            rand.weights.Clear();
            sum = count;
            for (i = 0; i < count; ++i)
                rand.weights.Add(1f);
        } else {
            sum = 0f;
            for (i = 0; i < count; ++i) {
                const float restore = 0.1f;
                rand.weights[i] = Mathf.Clamp01(rand.weights[i] + restore);
                sum += rand.weights[i];
            }
        }

        float val = sum * Randomizer.zeroToOne;
        for (i = 0; i < count - 1 && val >= rand.weights[i]; ++i)
            val -= rand.weights[i];

        const float penalty = 1f;
        rand.weights[i] = Mathf.Clamp01(rand.weights[i] - penalty);

        return i;
    }
}

} // Hapki.Randomization

