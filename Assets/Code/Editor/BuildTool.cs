using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;

static public class BuildTool {
	static readonly string kMasterPath = "Assets/Scenes/Master.unity";


	[MenuItem("Tools/Build Player/Win64 Dev Player (run)", priority = 200)]
	public static void BuildRunDevPlayerWin64() { DoBuildPlayer("BotD_Env_Dev_x64.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.AutoRunPlayer); }

	[MenuItem("Tools/Build Player/Win64 NonDev Player (run)", priority = 210)]
	public static void BuildRunNonDevPlayerWin64() { DoBuildPlayer("BotD_Env_x64.exe", BuildTarget.StandaloneWindows64, BuildOptions.None | BuildOptions.AutoRunPlayer); }

	[MenuItem("Tools/Build Player/Win64 Dev Player", priority = 220)]
	public static void BuildDevPlayerWin64() { DoBuildPlayer("BotD_Env_Dev_x64.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.ShowBuiltPlayer); }

	[MenuItem("Tools/Build Player/Win64 NonDev Player", priority = 230)]
	public static void BuildNonDevPlayerWin64() { DoBuildPlayer("BotD_Env_x64.exe", BuildTarget.StandaloneWindows64, BuildOptions.None | BuildOptions.ShowBuiltPlayer); }


	[MenuItem("Tools/Build Player/Mac Dev Player (run)", priority = 300)]
	public static void BuildRunDevPlayerOSX() { DoBuildPlayer("BotD_Env_Dev.app", BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.AutoRunPlayer); }

	[MenuItem("Tools/Build Player/Mac NonDev Player (run)", priority = 310)]
	public static void BuildRunNonDevPlayerOSX() { DoBuildPlayer("BotD_Env.app", BuildTarget.StandaloneOSX, BuildOptions.None | BuildOptions.AutoRunPlayer); }

	[MenuItem("Tools/Build Player/Mac Dev Player", priority = 320)]
	public static void BuildDevPlayerOSX() { DoBuildPlayer("BotD_Env_Dev.app", BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.ShowBuiltPlayer); }

	[MenuItem("Tools/Build Player/Mac NonDev Player", priority = 330)]
	public static void BuildNonDevPlayerOSX() { DoBuildPlayer("BotD_Env.app", BuildTarget.StandaloneOSX, BuildOptions.None | BuildOptions.ShowBuiltPlayer); }


	[MenuItem("Tools/Build Player/PS4 Dev Player (run)", priority = 400)]
	public static void BuildRunDevPlayerPS4() { DoBuildPlayer("BotD_Env_Dev_PS4", BuildTarget.PS4, BuildOptions.Development | BuildOptions.AutoRunPlayer); }

	[MenuItem("Tools/Build Player/PS4 NonDev Player (run)", priority = 410)]
	public static void BuildRunNonDevPlayerPS4() { DoBuildPlayer("BotD_Env_PS4", BuildTarget.PS4, BuildOptions.None | BuildOptions.AutoRunPlayer); }

	[MenuItem("Tools/Build Player/PS4 Dev Player", priority = 420)]
	public static void BuildDevPlayerPS4() { DoBuildPlayer("BotD_Env_Dev_PS4", BuildTarget.PS4, BuildOptions.Development | BuildOptions.ShowBuiltPlayer); }

	[MenuItem("Tools/Build Player/PS4 NonDev Player", priority = 430)]
	public static void BuildNonDevPlayerPS4() { DoBuildPlayer("BotD_Env_PS4", BuildTarget.PS4, BuildOptions.None | BuildOptions.ShowBuiltPlayer); }


	[MenuItem("Tools/Build Player/XboxOne Dev Player (run)", priority = 500)]
	public static void BuildRunDevPlayerXboxOne() { DoBuildPlayer("BotD_Env_Dev_XboxOne", BuildTarget.XboxOne, BuildOptions.Development | BuildOptions.AutoRunPlayer); }

	[MenuItem("Tools/Build Player/XboxOne NonDev Player (run)", priority = 510)]
	public static void BuildRunNonDevPlayerXboxOne() { DoBuildPlayer("BotD_Env_XboxOne", BuildTarget.XboxOne, BuildOptions.None | BuildOptions.AutoRunPlayer); }

	[MenuItem("Tools/Build Player/XboxOne Dev Player", priority = 520)]
	public static void BuildDevPlayerXboxOne() { DoBuildPlayer("BotD_Env_Dev_XboxOne", BuildTarget.XboxOne, BuildOptions.Development | BuildOptions.ShowBuiltPlayer); }

	[MenuItem("Tools/Build Player/XboxOne NonDev Player", priority = 530)]
	public static void BuildNonDevPlayerXboxOne() { DoBuildPlayer("BotD_Env_XboxOne", BuildTarget.XboxOne, BuildOptions.None | BuildOptions.ShowBuiltPlayer); }
    

	static void DoBuildPlayer(string name, BuildTarget target, BuildOptions flags) {
		var masterScene = EditorSceneManager.GetSceneByPath(kMasterPath);
		if(!masterScene.IsValid())
			masterScene = EditorSceneManager.OpenScene(kMasterPath);

		var msc = Object.FindObjectOfType<MultiSceneController>();
		BuildPipeline.BuildPlayer(msc.mainScenePath.scenePaths, string.Format("Builds/{0}", name), target, flags);
	}

	[PostProcessScene]
	public static void OnPostprocessScene() {
		if(BuildPipeline.isBuildingPlayer) {
			var scene = EditorSceneManager.GetActiveScene();
			var behaviours = new HashSet<MonoBehaviour>();

			foreach(var root in scene.GetRootGameObjects()) {
				foreach(var proxy in root.transform.GetComponentsInChildren<OcclusionProbesDetail>(true)) behaviours.Add(proxy);
			}
			
			var stripped = 0;
			foreach(var b in behaviours) {
				Object.DestroyImmediate(b);
				++stripped;
			}

			Debug.LogFormat("Stripped {0} editor-only behaviours from scene '{1}'.", stripped, scene.name);
		}
	}
}