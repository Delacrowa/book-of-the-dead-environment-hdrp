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

[ExecuteInEditMode]
[AddComponentMenu("Shaggy Dog Studios/Terrain Evo 3/Lightmap Proxy")]
public class TE3LightmapProxy : MonoBehaviour {
	public Renderer source;

    void Reset() {
        var self = GetComponent<Renderer>();
        var lodGroupIdx = TE3Utils.FindLODGroupIndex(self);

        // Bail if no renderer, no lodgroup, or we're in lod0.
        if(lodGroupIdx < 1)
            return;

        // If we're in a LOD group, assume we want to source from LOD0.
        // (only allowed if there's a single renderer in LOD0)
        var lods = GetComponentInParent<LODGroup>().GetLODs();
        if(lods[0].renderers.Length == 1)
            source = lods[0].renderers[0];
    }

    void OnEnable() {
        Sync();

        if(!Application.isPlaying)
            Lightmapping.completed += Sync;
    }

    void OnDisable() {
        if(!Application.isPlaying)
            Lightmapping.completed -= Sync;
    }

    public void Sync() {
        var self = GetComponent<Renderer>();

        // Bail if not fully populated
        if(!source || !self)
            return;

        // Transfer lightmap lookup
        self.lightmapIndex = source.lightmapIndex;
        self.lightmapScaleOffset = source.lightmapScaleOffset;
    }
}


    }//ns TerrainEvo3
}//ns ShaggyDogStudios

#endif //UNITY_EDITOR
