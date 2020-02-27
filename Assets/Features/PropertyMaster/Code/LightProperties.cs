
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[Serializable]
public class LightProperties : PropertyVolumeComponent<LightProperties> {
    public ExposedLightReferenceParameter target =
        new ExposedLightReferenceParameter(default(ExposedReference<Light>));

    public Vector3Parameter rotation = new Vector3Parameter(Vector3.zero);
    public ColorParameter color = new ColorParameter(Color.white);
    public ClampedFloatParameter colorTemperature = new ClampedFloatParameter(5500f, 1000f, 20000f);
    public MinFloatParameter intensity = new MinFloatParameter(1f, 0f);
    public MinFloatParameter bounceIntensity = new MinFloatParameter(0f, 0f);
    public TextureParameter cookie = new TextureParameter(null);
    public MinFloatParameter cookieSize = new MinFloatParameter(0f, 0f);

    public override void OverrideProperties(PropertyMaster master) {
        var light = target.value.Resolve(master);
        if (!light)
            return;

        if (rotation.overrideState)
            light.transform.localRotation = Quaternion.Euler(rotation);

        if (color.overrideState)
            light.color = color.value;

        if (colorTemperature.overrideState)
            light.colorTemperature = colorTemperature.value;

        if (intensity.overrideState)
            light.intensity = intensity.value;

        if (bounceIntensity.overrideState)
            light.bounceIntensity = bounceIntensity.value;

        if (cookie.overrideState)
            light.cookie = cookie.value;

        if (cookieSize.overrideState)
            light.cookieSize = cookieSize.value;
    }
}

