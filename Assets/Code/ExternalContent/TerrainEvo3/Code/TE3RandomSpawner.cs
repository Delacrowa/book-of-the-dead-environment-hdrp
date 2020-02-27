/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

// This is editor only code, but needs to hold data in the scene so we keep it outside an 'Editor' folder.
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

public class TE3RandomSpawner : MonoBehaviour {
	public GameObject[] spawnCandidates = new GameObject[0];

	public GameObject GetRandomTemplate() {
        var spawnCount = spawnCandidates.Length;

		if(spawnCount == 0)
			return null;

		return spawnCandidates[Random.Range(0, spawnCount - 1)];
	}

	public GameObject SpawnRandomInstance() {
        return SpawnInstance(GetRandomTemplate());
	}

	public GameObject SpawnInstance(GameObject template) {
        if(!template)
            return null;

        var prefabType = PrefabUtility.GetPrefabType(template);
        var isPrefabTemplate = prefabType == PrefabType.ModelPrefab || prefabType == PrefabType.Prefab;
        if(isPrefabTemplate)
            return PrefabUtility.InstantiatePrefab(template) as GameObject;
        else
		    return GameObject.Instantiate(template);
	}
}


    }//ns TerrainEvo3
}//ns ShaggyDogStudios

#endif //UNITY_EDITOR
