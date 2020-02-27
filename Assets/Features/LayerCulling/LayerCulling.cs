using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using System.Linq;

[ExecuteInEditMode]
[HDRPCallback]
public class LayerCulling : MonoBehaviour {
	static public LayerCulling Instance { get; private set; }
	static public bool ForceDisable { private get; set; }

	public enum Layer {
		Default				= 0,
		Terrain				= 15,
		EnvironmentSmall	= 16,
		EnvironmentLarge	= 17,
		ScatterTiny			= 18,
		ScatterSmall		= 19,
		UndergrowthSmall	= 20,
		UndergrowthMedium	= 21,
		UndergrowthLarge	= 22,
	}

    [System.Serializable]
    public class CullDistance {
        public Layer	layer;
        public float	distance;
    }

	[Header("Camera Culling")]
	public bool				enableCameraCulling	= true;
	public CullDistance[]   cullDistances		= new CullDistance[0];
	[Range(0.01f, 2f)]
	public float			globalDistanceScale = 1f;
	public float			globalMaxDistance = 0f;

	[Header("Shadow Culling")]
	public bool				enableShadowCulling = true;
	public Light			shadowCullTarget;
	public CullDistance[]	shadowCullDistances = new CullDistance[0];
	[Range(0.01f, 2f)]
	public float			globalShadowDistanceScale = 1f;
	public float			globalShadowMaxDistance = 0f;

	[Header("Shared")]
	public bool sphericalCulling = false;

#if UNITY_EDITOR
	static bool ms_SceneViewCulling;
#else
	const bool ms_SceneViewCulling = false;
#endif

	public void SetCullDistance(LayerCulling.Layer layer, float distance) {
		foreach(var cd in cullDistances.Where(cd => cd.layer == layer))
			cd.distance = distance;
	}

	public void SetShadowCullDistance(LayerCulling.Layer layer, float distance) {
		foreach(var cd in shadowCullDistances.Where(cd => cd.layer == layer))
			cd.distance = distance;
	}

	[HDRPCallbackMethod]
	static void LayerCullingSetup() {
#if UNITY_EDITOR
		var panel = DebugManager.instance.GetPanel("Scene View", true);
		var container = panel.children.Where(c => c.displayName == "Forest Custom").FirstOrDefault() as DebugUI.Container;
		if(container == null)
			container = new DebugUI.Container { displayName = "Forest Custom" };
		container.children.Add(new DebugUI.BoolField {
			displayName = "Use Layer Culling",
			getter = () => ms_SceneViewCulling,
			setter = value => ms_SceneViewCulling = value
		});
		if(!panel.children.Contains(container))
			panel.children.Add(container);
#endif

		HDRenderPipeline.OnBeforeCameraCull += UpdateLayerCulling;
	}

	static void UpdateLayerCulling(ScriptableRenderContext context, HDCamera hdCamera, FrameSettings settings, CommandBuffer cmd) {
		var isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
		if(!ForceDisable && Instance && Instance.isActiveAndEnabled && (!isSceneView || ms_SceneViewCulling)) {
			AssignCulling(hdCamera.camera, Instance);
		} else {
			ClearLayerCulling(hdCamera.camera, Instance ? Instance.shadowCullTarget : null);
		}

		ForceDisable = false;
	}

    void OnEnable() {
		//Debug.Assert(Instance == null, "LayerCulling is expected to be a singleton");
		Instance = this;
    }

    void OnDisable() {
		Instance = null;

		ClearLayerCulling(null, shadowCullTarget);
	}

	static void AssignCulling(Camera cameraCullTarget, LayerCulling culling) {
		// Spherical distance is shared between camera and shadow culling (since the distances are combined internally)
		if(cameraCullTarget)
			cameraCullTarget.layerCullSpherical = culling.sphericalCulling;

		if(cameraCullTarget) {
			if(culling.enableCameraCulling)
				cameraCullTarget.layerCullDistances = CalculateLayers(culling.globalDistanceScale, culling.globalMaxDistance, culling.cullDistances);
			else
				cameraCullTarget.layerCullDistances = new float[32];
		}

		if(culling.shadowCullTarget) {
			if(culling.enableShadowCulling)
				culling.shadowCullTarget.layerShadowCullDistances = CalculateLayers(culling.globalShadowDistanceScale, culling.globalShadowMaxDistance, culling.shadowCullDistances);
			else
				culling.shadowCullTarget.layerShadowCullDistances = null;
		}
	}

	static void ClearLayerCulling(Camera cullTarget, Light shadowCullTarget) {
		if(cullTarget)
			cullTarget.layerCullSpherical = false;

		if(cullTarget)
			cullTarget.layerCullDistances = new float[32];

		if(shadowCullTarget)
			shadowCullTarget.layerShadowCullDistances = null;
	}

	static float[] CalculateLayers(float scale, float max, CullDistance[] distances) {
		var distanceArray = new float[32];
		for(var i = 0; i < distanceArray.Length; ++i)
			distanceArray[i] = max;

		foreach(var distance in distances) {
			var layer = (int)distance.layer;
			if(layer >= 0 && layer < 32) {
				var scaledDist = scale * (float)distance.distance;

				if(scaledDist > 0f) {
					if(distanceArray[layer] > 0f)
						distanceArray[layer] = Mathf.Min(distanceArray[layer], scaledDist);
					else
						distanceArray[layer] = scaledDist;
				}
			}
		}

		return distanceArray;
	}
}
