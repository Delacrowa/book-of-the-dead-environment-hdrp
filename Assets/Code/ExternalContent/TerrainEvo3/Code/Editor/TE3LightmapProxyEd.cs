/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using UnityEngine;
using UnityEditor;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

[CustomEditor(typeof(TE3LightmapProxy))]
class TE3LightmapProxyEd : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        if(GUILayout.Button("Force Sync")) {
            var t = target as TE3LightmapProxy;
            t.Sync();
        }

        EditorGUILayout.Space();
        GUI.color = Color.yellow;

        if(GUILayout.Button("Force Sync All")) {
            foreach(var proxy in FindObjectsOfType<TE3LightmapProxy>())
                if(proxy.gameObject.scene.IsValid() && proxy.gameObject.scene.isLoaded)
                    proxy.Sync();
        }
    }
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios
