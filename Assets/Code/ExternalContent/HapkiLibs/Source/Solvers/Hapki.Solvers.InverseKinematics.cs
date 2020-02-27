
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using UnityEngine;

namespace Hapki.Solvers {

public static class InverseKinematics {
    public static void Update(Transform tracked, int mask, Vector3 target, float sqrThreshold = 0.000001f) {
        for (var bone = tracked; mask != 0; mask >>= 1) {
            bone = bone.parent;
            if ((mask & 1) == 0)
                continue;

            var current = tracked.position;
            if ((current - target).sqrMagnitude <= sqrThreshold)
                break;

            var position = bone.position;
            var toCurrent = (current - position).normalized;
            var toTarget = (target - position).normalized;
            float adjustment = Vector3.Dot(toTarget, toCurrent);
            if (adjustment < 1f)
                bone.Rotate(
                    Vector3.Cross(toCurrent, toTarget).normalized,
                    Mathf.Acos(adjustment) * Mathf.Rad2Deg, Space.World);
        }
    }
}

} // Hapki.Solvers

