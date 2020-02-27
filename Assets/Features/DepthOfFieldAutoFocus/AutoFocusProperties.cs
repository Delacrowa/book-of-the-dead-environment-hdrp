
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[Serializable]
public class AutoFocusProperties : PropertyVolumeComponent<AutoFocusProperties>
{
    public ExposedAutoFocusReferenceParameter target =
        new ExposedAutoFocusReferenceParameter(default(ExposedReference<DepthOfFieldAutoFocus>));

    public ClampedFloatParameter influence = new ClampedFloatParameter(1f, 0f, 1f);
    public MinFloatParameter focusDistanceWalk = new MinFloatParameter(10f, 0.1f);
    public ClampedFloatParameter apertureWalk = new ClampedFloatParameter(5.6f, 0.05f, 32f);
    public MinFloatParameter focusDistanceRun = new MinFloatParameter(10f, 0.1f);
    public ClampedFloatParameter apertureRun = new ClampedFloatParameter(5.6f, 0.05f, 32f);

    public override void OverrideProperties(PropertyMaster master)
    {
        var af = target.value.Resolve(master);
        if (!af)
            return;

        var infl = af.influence;
        Override(influence, ref infl);
        af.influence = infl;
        Override(focusDistanceWalk, ref af.focusDistanceWalk);
        Override(apertureWalk, ref af.apertureWalk);
        Override(focusDistanceRun, ref af.focusDistanceRun);
        Override(apertureRun, ref af.apertureRun);
    }
}

