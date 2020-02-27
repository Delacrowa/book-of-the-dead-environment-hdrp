using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

[ExecuteInEditMode]
public class QualitySelector : MonoBehaviour {
	public enum Platform { Desktop, PS4, PS4Pro, XB1, XB1X };

	[System.Serializable]
	public struct Quality {
		public Platform				platform;
		public RenderPipelineAsset	renderPipeline;
		public LayerCulling			layerCulling;
		public AdditionalShadowData clampedShadowResolutionTarget;
	}

	public Platform		overrideEditorPlatform	= Platform.Desktop;
	public Quality[]	qualitySettings			= new Quality[0];

	void OnValidate() {
		ApplyQuality(false);	
	}

#if UNITY_EDITOR
	void Awake() {
		// Reset override
		if(!Application.isPlaying && overrideEditorPlatform != Platform.Desktop)
			overrideEditorPlatform = Platform.Desktop;
	}
#endif

	void OnEnable() {
		ApplyQuality(true);
	}

	void ApplyQuality(bool verbose) {
		var platform = GetPlatform();
		foreach(var quality in qualitySettings) {
			if(quality.platform == platform) {
				ApplyQuality(quality, verbose);
				break;
			}
		}
	}

	void ApplyQuality(Quality quality, bool verbose) {
		if(Application.isPlaying && verbose)
			Debug.LogFormat("Applying quality: {0}", quality.platform);

		if(quality.renderPipeline && quality.renderPipeline != GraphicsSettings.renderPipelineAsset)
			GraphicsSettings.renderPipelineAsset = quality.renderPipeline;

		if(quality.layerCulling && quality.layerCulling != LayerCulling.Instance) {
			if(LayerCulling.Instance)
				LayerCulling.Instance.enabled = false;

			quality.layerCulling.enabled = true;

			if(!quality.layerCulling.isActiveAndEnabled)
				quality.layerCulling.gameObject.SetActive(true);
		}

		if(quality.clampedShadowResolutionTarget && quality.renderPipeline) {
			var hdrp = quality.renderPipeline as HDRenderPipelineAsset;
			if(hdrp) {
				var cascadeCount = quality.clampedShadowResolutionTarget.cascadeCount;
				var atlasWidth = hdrp.GetRenderPipelineSettings().shadowInitParams.shadowAtlasWidth;
				var atlasHeight = hdrp.GetRenderPipelineSettings().shadowInitParams.shadowAtlasHeight;
				if(cascadeCount != 4 || atlasWidth != atlasHeight) {
					Debug.LogWarning("Unable to automatically restrict shadow resolution if shadow map atlas width != height or shadow cascades count != 4.");
				} else {
					quality.clampedShadowResolutionTarget.shadowResolution = atlasWidth / 2;
				}
			}
		}
	}

	Platform GetPlatform() {
#if UNITY_EDITOR
		return overrideEditorPlatform;
#elif UNITY_PS4
		return UnityEngine.PS4.Utility.neoMode ? Platform.PS4Pro : Platform.PS4;
#elif UNITY_XBOXONE
		var hwVer = UnityEngine.XboxOne.Hardware.version; 
		return hwVer == UnityEngine.XboxOne.HardwareVersion.XboxOne || hwVer == UnityEngine.XboxOne.HardwareVersion.XboxOneS ? Platform.XB1 : Platform.XB1X;
#else
		return Platform.Desktop;
#endif
	}
}
