/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

static public class TE3BuildStripper {
	[PostProcessScene(10)]
	public static void OnPostprocessScene() {
		if(!BuildPipeline.isBuildingPlayer)
			return;

		for(var i = 0; i < EditorSceneManager.loadedSceneCount; ++i) {
			var scene = EditorSceneManager.GetSceneAt(i);
			var behaviours = new HashSet<MonoBehaviour>();

			CollectBehaviours<TE3AutoRandomSpawner>(scene, behaviours);
			CollectBehaviours<TE3LightmapProxy>(scene, behaviours);
			//CollectBehaviours<TE3LODGroupProxy>(scene, behaviours);
			CollectBehaviours<TE3RandomSpawner>(scene, behaviours);
			CollectBehaviours<TE3ScatterArea>(scene, behaviours);
			CollectBehaviours<TE3ScatterProxy>(scene, behaviours);
			//CollectBehaviours<TE3TerrainMaterialPicker>(scene, behaviours);
			//CollectBehaviours<TE3TerrainMaterialProjector>(scene, behaviours);
			
			var stripped = 0;
			foreach(var b in behaviours) {
				Object.DestroyImmediate(b);
				++stripped;
			}

			Debug.LogFormat("TE3 stripped out {0} editor-only behaviours from scene '{1}'.", stripped, scene.name);
		}
	}

	static void CollectBehaviours<T>(Scene scene, HashSet<MonoBehaviour> collected) where T : MonoBehaviour {
		foreach(var root in scene.GetRootGameObjects())
			foreach(var behaviour in root.transform.GetComponentsInChildren<T>(true))
					collected.Add(behaviour);
	}
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios
