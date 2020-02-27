using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[CustomEditor(typeof(MultiSceneController))]
public class MultiSceneControllerEd : Editor {
    [MenuItem("Tools/Load Book of the Dead: Environment", priority = 0)]
    public static void LoadForestScenes() { DoLoadForestScenes(0); }

	[MenuItem("Tools/Load Asset Library", priority = 1)]
	public static void LoadAssetLibraryScene() {
		EditorSceneManager.OpenScene("Assets/Scenes/AssetLibrary.unity", OpenSceneMode.Single);
	}

	static public Scene LoadMasterScene(bool force = false) {
		var masterScene = EditorSceneManager.GetSceneByPath("Assets/Scenes/Master.unity");

		if(force || !masterScene.IsValid())
			masterScene = EditorSceneManager.OpenScene("Assets/Scenes/Master.unity", OpenSceneMode.Single);

		return masterScene;
	}

    static void DoLoadForestScenes(int sceneSet) {
		LoadMasterScene(true);

        var instance = Object.FindObjectOfType<MultiSceneController>();

		if(sceneSet == 0)
			LoadEditorScenes(instance.mainScenePath);
		else
			LoadEditorScenes(instance.editorScenePaths[sceneSet - 1]);
    }

	public override void OnInspectorGUI() {
		var t = target as MultiSceneController;

		DrawDefaultInspector();
		EditorGUILayout.Space();

		GUI.enabled = !Application.isPlaying && t.mainScenePath.scenePaths != null && t.mainScenePath.scenePaths.Length > 0;
		if(GUILayout.Button("Load Default Scenes"))
		{
		    LoadEditorScenes(t.mainScenePath);
		}
	}

    public static void LoadEditorScenes(MultiSceneController.ScenePathList scenePathList)
    {
		var scenePaths = scenePathList.scenePaths;
        var scenes = new List<SceneSetup>(scenePaths.Length);
        for (int i = 0; i < scenePaths.Length; ++i)
        {
			if(string.IsNullOrEmpty(scenePaths[i]))
				continue;

			var scene = new SceneSetup();
            scene.path = scenePaths[i];
            scene.isActive = i == scenePathList.activeSceneIndex;
            scene.isLoaded = true;
			scenes.Add(scene);
        }

        Debug.LogFormat("Restoring {0} editor scenes.", scenes.Count);
        EditorSceneManager.RestoreSceneManagerSetup(scenes.ToArray());
    }
}
