using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Gameplay;

public class MultiSceneController : MonoBehaviour
{
	[Serializable]
	public struct ScenePathList
	{
		public string 	groupName;
		public int		activeSceneIndex;
		public string[] scenePaths;
	}

    public bool benchmarkMode;

    public ScenePathList	mainScenePath;
	public ScenePathList[]	editorScenePaths;

    public GameObject[] 	deactivateAfterLoad;
	public string			centerSpawnPointName;

    public Slider			progressSlider;
    public Text				progressText;

	public ShaderVariantCollection[] shaderVariantCollections = new ShaderVariantCollection[0];

	IEnumerator Start()
    {
        AxelF.Zone.dontProbeZones = true;
        PlayerInput.ignore = true;

        foreach(var go in deactivateAfterLoad)
            go.SetActive(true);

		if(!Application.isEditor) {
			yield return null;

			var originalBackgroundLoadingPriority = Application.backgroundLoadingPriority;
			Application.backgroundLoadingPriority = ThreadPriority.High;

			for(int i = 1; i < mainScenePath.scenePaths.Length; ++i) {
				yield return StartCoroutine(LoadScene(i));
			}

			Application.backgroundLoadingPriority = originalBackgroundLoadingPriority;

			yield return null;

			progressText.text = "PREWARMING SHADERS";
			progressSlider.value = 0.9f;

#if !UNITY_EDITOR
	#if !UNITY_STANDALONE_OSX
			// There are some prewarming issues that can cause crashes with certain drivers, so try to avoid those here.
			if(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Vulkan && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
			    foreach(var svc in shaderVariantCollections)
				    svc.WarmUp();
	#endif
#endif

			yield return null;

			progressText.text = "STARTING";
			progressSlider.value = 1;
		}

        GameController gameController;
        do gameController = GameController.FindGameController();
        while (!gameController);

		if(!string.IsNullOrEmpty(centerSpawnPointName)) {
			yield return null;

			var spawnPoint = gameController.GetSpawnPoint(centerSpawnPointName);
			var stagger = StaggeredCascade.Instance;
			if(spawnPoint != null && stagger != null) {
				var cameraTransform = gameController.defaultCamera.transform;
				var spawnPointTransform = spawnPoint.transform;

                var prevPosition = cameraTransform.localPosition;

				cameraTransform.position = spawnPointTransform.position;
				cameraTransform.rotation = spawnPointTransform.rotation;

				yield return null;

				stagger.staggerStage = StaggeredCascade.StaggerStage.Updating;

				yield return null;
				yield return null;

                cameraTransform.localPosition = prevPosition;

				yield return null;
			}
		}

        gameController.SpawnPlayer(nextFrame: false);

        foreach (var go in deactivateAfterLoad)
            go.SetActive(false);

        AxelF.Zone.dontProbeZones = false;

        while (!gameController.debugContext)
            yield return null;

        if (benchmarkMode)
            ((DebugControls) gameController.debugContext).EnableBenchmarkMode();

        ((DebugControls) gameController.debugContext).AssumeControl();
    }

    IEnumerator LoadScene(int index)
    {
        var asyncOp = SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
        if (asyncOp != null)
        {
            progressText.text = "LOADING '" + mainScenePath.scenePaths[index] + "'";
            while (!asyncOp.isDone)
            {
                progressSlider.value = (((index - 1) + asyncOp.progress) / (mainScenePath.scenePaths.Length - 1)) * 0.8f;
                yield return null;
            }
			
			if(index == mainScenePath.activeSceneIndex)
				SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(index));
        }
    }
}

