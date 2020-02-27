
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[Serializable]
public sealed class CullingParameter : VolumeParameter<ExposedReference<LayerCulling>> {
	public CullingParameter(ExposedReference<LayerCulling> value, bool overrideState = false)
		: base(value, overrideState) {
	}
}

[Serializable]
public class CullingProperties : PropertyVolumeComponent<CullingProperties>
{
    public CullingParameter target =
        new CullingParameter(default(ExposedReference<LayerCulling>));

	public MinFloatParameter environmentSmallCullDistance = new MinFloatParameter(40f, 0.1f);
	public MinFloatParameter environmentSmallShadowCullDistance = new MinFloatParameter(32f, 0.1f);
	public MinFloatParameter environmentLargeCullDistance = new MinFloatParameter(150f, 0.1f);
	public MinFloatParameter environmentLargeShadowCullDistance = new MinFloatParameter(150f, 0.1f);
	public MinFloatParameter undergrowthSmallCullDistance = new MinFloatParameter(26f, 0.1f);
	public MinFloatParameter undergrowthSmallShadowCullDistance = new MinFloatParameter(14f, 0.1f);
	public MinFloatParameter undergrowthMediumCullDistance = new MinFloatParameter(45f, 0.1f);
	public MinFloatParameter undergrowthMediumShadowCullDistance = new MinFloatParameter(30f, 0.1f);
	public MinFloatParameter undergrowthLargeCullDistance = new MinFloatParameter(46f, 0.1f);
	public MinFloatParameter undergrowthLargeShadowCullDistance = new MinFloatParameter(34f, 0.1f);

	public override void OverrideProperties(PropertyMaster master)
    {
        var lc = target.value.Resolve(master);
        if (!lc)
            return;

		if(environmentSmallCullDistance.overrideState)
			lc.SetCullDistance(LayerCulling.Layer.EnvironmentSmall, environmentSmallCullDistance.value);

		if(environmentSmallShadowCullDistance.overrideState)
			lc.SetShadowCullDistance(LayerCulling.Layer.EnvironmentSmall, environmentSmallShadowCullDistance.value);

		if(environmentLargeCullDistance.overrideState)
			lc.SetCullDistance(LayerCulling.Layer.EnvironmentLarge, environmentLargeCullDistance.value);

		if(environmentLargeShadowCullDistance.overrideState)
			lc.SetShadowCullDistance(LayerCulling.Layer.EnvironmentLarge, environmentLargeShadowCullDistance.value);

		if(undergrowthSmallCullDistance.overrideState)
			lc.SetCullDistance(LayerCulling.Layer.UndergrowthSmall, undergrowthSmallCullDistance.value);

		if(undergrowthSmallShadowCullDistance.overrideState)
			lc.SetShadowCullDistance(LayerCulling.Layer.UndergrowthSmall, undergrowthSmallShadowCullDistance.value);

		if(undergrowthMediumCullDistance.overrideState)
			lc.SetCullDistance(LayerCulling.Layer.UndergrowthMedium, undergrowthMediumCullDistance.value);

		if(undergrowthMediumShadowCullDistance.overrideState)
			lc.SetShadowCullDistance(LayerCulling.Layer.UndergrowthMedium, undergrowthMediumShadowCullDistance.value);

		if(undergrowthLargeCullDistance.overrideState)
			lc.SetCullDistance(LayerCulling.Layer.UndergrowthLarge, undergrowthLargeCullDistance.value);

		if(undergrowthLargeShadowCullDistance.overrideState)
			lc.SetShadowCullDistance(LayerCulling.Layer.UndergrowthLarge, undergrowthLargeShadowCullDistance.value);

	}
}

