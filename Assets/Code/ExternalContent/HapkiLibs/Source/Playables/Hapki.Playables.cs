
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace Hapki.Playables {

public interface ITweener {
    int previous { get; }
    int state { get; }
    int target { get; }
}

public struct Tweener : ITweener {
    public int previous { get; private set; }
    public int state { get; private set; }
    public int target { get; private set; }

    public void Target(int v) {
        target = v;
    }

    public void Reset() {
        previous = 0;
        state = 0;
        target = 0;
    }

    public bool ChangeState() {
        if (state != target) {
            previous = state;
            state = target;
            return true;
        }
        return false;
    }
}

public static class PlayableExtensions {
    public static void BlendInputWeights<T>(this T playable, float value, float influence = 1f)
            where T : struct, IPlayable {
        int count = playable.GetInputCount();
        Debug.Assert(count > 0);
        Debug.Assert(value >= 0 && value <= 1f);
        float denorm = value * (count - 1);
        for (int i = 0; i < count; ++i) {
            float distance = Mathf.Clamp((i - denorm) / influence, -1f, 1f);
            playable.SetInputWeight(i, 1f - Mathf.Abs(distance));
        }
    }

    public static void TweenInputWeights<T, U>(this T playable, U tweener, float value)
            where T : struct, IPlayable
            where U : struct, ITweener {
        playable.TweenInputWeights(tweener.previous, tweener.state, value);
    }

    public static void TweenInputWeights<T>(this T playable, int index1, int index2, float value)
            where T : struct, IPlayable {
        int count = playable.GetInputCount();
        Debug.Assert(count > 0);
        Debug.Assert(index1 >= 0 && index1 < count);
        Debug.Assert(index2 >= 0 && index2 < count);
        Debug.Assert(value >= 0 && value <= 1f);
        for (int i = 0; i < count; ++i) {
            float w = 0f;
            if (i == index2)
                w = index1 != index2 ? value : 1f;
            else if (i == index1)
                w = 1f - value;
            playable.SetInputWeight(i, w);
        }
    }
}

} // Hapki.Playables

