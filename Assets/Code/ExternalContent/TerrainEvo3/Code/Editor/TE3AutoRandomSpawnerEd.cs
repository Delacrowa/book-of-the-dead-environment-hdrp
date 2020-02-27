/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

[CanEditMultipleObjects]
[CustomEditor(typeof(TE3AutoRandomSpawner))]
public class TE3AutoRandomSpawnerEd : Editor {
    SerializedProperty m_spSpawnedInstance;

    void OnEnable() {
        m_spSpawnedInstance = serializedObject.FindProperty("m_SpawnedInstance");
    }

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		EditorGUILayout.Space();

        var t = target as TE3AutoRandomSpawner;

        var targetSpawner = t.GetComponent<TE3RandomSpawner>();
        if(targetSpawner) {
            EditorGUI.BeginChangeCheck();
            var templateNames = targetSpawner.spawnCandidates.Select(go => go.name).ToArray();
            var forceSpawnTemplate = EditorGUILayout.Popup("Force Template", -1, templateNames);
            if(EditorGUI.EndChangeCheck() && forceSpawnTemplate != -1)
                t.AutoSpawn(targetSpawner.spawnCandidates[forceSpawnTemplate]);

		    if(GUILayout.Button("Re-Roll"))
                t.AutoSpawn();
        }

        if(m_spSpawnedInstance.objectReferenceValue && !t.gameObject.scene.IsValid()) {
    		if(GUILayout.Button("Clean Prefab")) {
                Object.DestroyImmediate(m_spSpawnedInstance.objectReferenceValue);
                m_spSpawnedInstance.objectReferenceValue = null;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
	}
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios
