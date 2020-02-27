/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

[CanEditMultipleObjects]
[CustomEditor(typeof(TE3RandomSpawner))]
public class TE3RandomSpawnerEd : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		EditorGUILayout.Space();

		if(GUILayout.Button("Add Children")) {
            Undo.RecordObjects(targets, "Added children as spawn candidates");

            foreach(var tgt in targets) {
    			var t = tgt as TE3RandomSpawner;
                var candidates = new List<GameObject>(t.spawnCandidates);
                candidates.AddRange(t.GetComponentsInChildren<Renderer>().Select(xf => xf.gameObject).Where(go => go != t.gameObject));
                t.spawnCandidates = candidates.ToArray();
            }
		}

		if(GUILayout.Button("Add Terrain Prototypes")) {
            var terrain = Terrain.activeTerrain;
            if(!terrain) {
                Debug.LogWarning("No active terrain found!");
            } else {
                Undo.RecordObjects(targets, "Added terrain splat prototypes as spawn candidates");

                var treePrototypes = terrain.terrainData.treePrototypes;

                foreach(var tgt in targets) {
    		    	var t = tgt as TE3RandomSpawner;
                    var candidates = new List<GameObject>(t.spawnCandidates);

                    foreach(var proto in treePrototypes) {
                        if(candidates.Contains(proto.prefab))
                            continue;
                        else
                            candidates.Add(proto.prefab);
                    }

                    t.spawnCandidates = candidates.ToArray();
                }
            }
		}
	}
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios
