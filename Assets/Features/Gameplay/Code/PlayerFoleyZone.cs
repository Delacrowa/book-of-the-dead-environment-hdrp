
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay {

public class PlayerFoleyZone : AxelF.Zone {
    public static readonly List<PlayerFoleyZone> overrides = new List<PlayerFoleyZone>();

    public PlayerFoleyAsset foley;

    internal int lastFrame = -1;

    internal bool isOverride;

    protected new void OnDisable() {
        base.OnDisable();
        overrides.Remove(this);
    }

    protected override void OnProbe(Vector3 lpos, int thisFrame) {
        if (lastFrame != thisFrame) {
            lastFrame = thisFrame;

            var pos = transform.position;
            float sqrDistance = (lpos - pos).sqrMagnitude;
            float sqrRadius = radius * radius;
            bool active = (sqrDistance <= sqrRadius);

            if (active) {
                if (!isOverride) {
                    isOverride = true;
                    overrides.Add(this);
                }
            } else {
                if (isOverride) {
                    isOverride = false;
                    overrides.Remove(this);
                }
            }

            SetActive(active);
        }
    }
}

} // Gameplay

