
using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace TerrainFoley {

public struct FoleyMap {
    public int[] lookup;

    public void Initialize(string[] names, SplatMap splatMap) {
        if (names == null || splatMap.data == null) {
            Debug.LogWarning("Failed to initialize foley map");
            return;
        }

        lookup = new int[splatMap.count];

        for (int i = 0, n = splatMap.count; i < n; ++i) {
            var texture = splatMap.data.splatPrototypes[i].texture;
            bool found = false;

            for (int j = 0, m = names.Length; j < m; ++j)
                if (!string.IsNullOrEmpty(names[j]))
                    if (texture.name.IndexOf(names[j], StringComparison.OrdinalIgnoreCase) >= 0) {
                        // Debug.Log("map: " + names[j] + " -- " + texture.name);
                        lookup[i] = j;
                        found = true;
                        break;
                    }

            if (!found)
                Debug.LogWarningFormat("Failed to bind footstep foley to terrain texture '{0}'", texture.name);
        }
    }

    public int GetFoleyIndexAtPosition(Vector3 position, SplatMap splatMap) {
        if (lookup == null)
            return -1;

        int index = splatMap.GetSplatIndexAtPosition(position);
        if (index < 0 || index >= lookup.Length)
            return -1;

        return lookup[index];
    }
}

public struct SplatMap {
    public TerrainData data;
    public float[,,] lookup;
    public Vector3 origin;
    public int width;
    public int height;
    public int count;

    public void Initialize(Terrain terrain) {
        if (terrain != null)
            data = terrain.terrainData;

        if (lookup == null && data != null) {
            width = data.alphamapWidth;
            height = data.alphamapHeight;
            lookup = data.GetAlphamaps(0, 0, width, height);
            origin = terrain.transform.position;
            count = lookup.Length / (width * height);
        } else
            Debug.LogWarning("Failed to initialize splat map");
    }

    public int GetSplatIndexAtPosition(Vector3 position) {
        if (data == null || lookup == null)
            return -1;

        int x = Mathf.FloorToInt((position.x - origin.x) / data.size.x * width);
        int z = Mathf.FloorToInt((position.z - origin.z) / data.size.z * height);
        int j = 0;
        float k = 0f;

        for (int i = 0, n = count; i < n; ++i) {
            float l = lookup[z, x, i];
            if (k < l) {
                j = i;
                k = l;
            }
        }

        return j;
    }
}

} // TerrainFoley

