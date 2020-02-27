using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using Gameplay;

public static class GameViewTracker
{
    [MenuItem(k_MenuName, true)]
    public static bool ToggleGameViewTrackingValidate()
    {
        Menu.SetChecked(k_MenuName, s_Enabled);
        return true;
    }

    [MenuItem(k_MenuName, priority = 1050)]
    public static void ToggleGameViewTracking()
    {
        SetEnabled(!s_Enabled);
    }

    static void SetEnabled(bool enabled)
    {
        if (enabled && !s_Enabled)
        {
            SceneView.onSceneGUIDelegate += sceneGUICallback;
            s_Enabled = true;
        }
        else if (!enabled && s_Enabled)
        {
            SceneView.onSceneGUIDelegate -= sceneGUICallback;
            s_Enabled = false;

            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localEulerAngles = Vector3.zero;
        }
    }

    static void sceneGUICallback(SceneView s)
    {
        if (Camera.main == null)
            return;

        if(!s.camera.orthographic)
            Camera.main.transform.SetPositionAndRotation(s.camera.transform.position - 0.1f * s.camera.transform.forward, s.camera.transform.rotation);
    }

    static bool s_Enabled;
    const string k_MenuName = "Tools/Toggle GameView tracking %T";
}


[InitializeOnLoad]
public class Startup_PreviewTextureCache
{
    //Set PreviewTextureCache to 128, as we have many tree assets used by our terrain,
    static Startup_PreviewTextureCache()
    {
        AssetPreview.SetPreviewTextureCacheSize(128);

    }
}

[InitializeOnLoad]
public class Startup_ResetMainCamera
{
    //Reset main camera to position (0,0,0)
    
    static Startup_ResetMainCamera()
    {
        var camera = Camera.main;

        if (camera) {
            //Debug.Log("ResetMainCamera: " + camera, camera);

            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
        }
    }
}

static class VolumeToggle {
	[MenuItem("Tools/Toggle Volume Colliders &#v", priority = 1030)]
	static void ToggleVolumeColliders() {
		var volumes = Object.FindObjectsOfType(typeof(Volume));
		var ppVolumes = Object.FindObjectsOfType(typeof(UnityEngine.Rendering.PostProcessing.PostProcessVolume));
		bool hide = false;

		foreach(Volume volume in volumes)
			if(!volume.hideColliderGizmos) {
				hide = true;
				break;
			}

		foreach(UnityEngine.Rendering.PostProcessing.PostProcessVolume volume in ppVolumes)
			if(!volume.hideColliderGizmos) {
				hide = true;
				break;
			}

		foreach(Volume volume in volumes)
			volume.hideColliderGizmos = hide;

		foreach(UnityEngine.Rendering.PostProcessing.PostProcessVolume volume in ppVolumes)
			volume.hideColliderGizmos = hide;
	}
}

static class FreeCameraToggle {
    [MenuItem("Tools/Toggle FreeCamera", true, 1040)]
    static bool CanToggleFreeCamera() {
        return Application.isPlaying;
    }

    [MenuItem("Tools/Toggle FreeCamera", priority = 1040)]
    static void ToggleFreeCamera() {
        var camera = Camera.main;
        var freeCamera = camera.GetComponent<FreeCamera>();
        var gameController = GameController.FindGameController();

        if (freeCamera && gameController) {
            var debugControls = (DebugControls) gameController.debugContext;

            if (freeCamera.enabled) {
                camera.transform.parent = gameController.playerCamera.eyeTransform;
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localRotation = Quaternion.identity;
                freeCamera.enabled = false;
                debugControls.enabled = true;
            } else {
                camera.transform.parent = null;
                freeCamera.enabled = true;
                debugControls.enabled = false;
            }
        }
    }
}

