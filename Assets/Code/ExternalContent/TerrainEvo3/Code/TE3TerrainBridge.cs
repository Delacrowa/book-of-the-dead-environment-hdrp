/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

public interface ITE3BridgeProvider {
    bool    ProvidesDataAt(float x, float z);
    Bounds  GetBounds();
}

public interface ITE3HeightProvider : ITE3BridgeProvider {
    float   GetHeightAt(float x, float z);
    Vector3 GetNormalAt(float x, float z);
    float   GetHeightAndNormalAt(float x, float z, out Vector3 normal);
    float   GetDataResolution();
    //void  Release();
}

public struct TE3MaterialInfo {
    public Texture2D    albedo;
    public Texture2D    normal;
    public float        uvScale;
}

public interface ITE3MaterialProvider : ITE3BridgeProvider {
    TE3MaterialInfo GetMaterialAt(float x, float z);
    float           GetDataResolution();
    //void          Release();
}

public class TE3TerrainBridge {
    static TE3TerrainBridge ms_Instance;

    static public TE3TerrainBridge Instance { get {
        if(ms_Instance == null)
            ms_Instance = new TE3TerrainBridge();

        return ms_Instance;
    }}

    List<TE3UnityTerrainProvider> m_UnityTerrainProviders;

    public TE3TerrainBridge() {
        m_UnityTerrainProviders = new List<TE3UnityTerrainProvider>();
        EnsureProviders();
    }

    void EnsureProviders() {
        for(var i = 0; i < m_UnityTerrainProviders.Count;) {
            var p = m_UnityTerrainProviders[i];

            if(!p.terrain)
                m_UnityTerrainProviders.RemoveAt(i);
            else
                ++i;
        }

        foreach(var terrain in Terrain.activeTerrains)
            if(!m_UnityTerrainProviders.Select(x => x.terrain).Contains(terrain))
                m_UnityTerrainProviders.Add(new TE3UnityTerrainProvider(terrain));    
    }

    public ITE3HeightProvider GetHeightProviderAt(float x, float z, float r) {
        return FindProvider<ITE3HeightProvider>(x, z, r);
    }

    public ITE3MaterialProvider GetMaterialProviderAt(float x, float z, float r) {
        return FindProvider<ITE3MaterialProvider>(x, z, r);
    }

    T FindProvider<T>(float x, float z, float r) where T : class {
        EnsureProviders();

        foreach(ITE3BridgeProvider p in m_UnityTerrainProviders)
            if(p is T && p.ProvidesDataAt(x, z))
                return p as T;

        if(r > 0f)
            foreach(ITE3BridgeProvider p in m_UnityTerrainProviders)
                if(p is T)
                    if(p.ProvidesDataAt(x - r, z - r) || p.ProvidesDataAt(x + r, z - r) || p.ProvidesDataAt(x + r, z + r) || p.ProvidesDataAt(x - r, z + r))
                        return p as T;

        return null;
    }
}

class TE3UnityTerrainProvider : ITE3HeightProvider, ITE3MaterialProvider {
    //int         m_heightRefCount;
    //int         m_materialRefCount;

    internal Terrain terrain { get { return m_Terrain; } }

    Terrain     m_Terrain;
    TerrainData m_TerrainData;

    Vector3     m_TerrainSize;
    Vector3     m_TerrainSizeRcp;
    Vector3     m_TerrainBasePos;
    Vector3     m_TerrainMaxPos;

    Bounds      m_TerrainBounds;
    float       m_TerrainHeightResolution;
    float       m_TerrainMaterialResolution;

    public TE3UnityTerrainProvider(Terrain terrain) {
        //m_heightRefCount = m_materialRefCount = 0;

        m_Terrain = terrain;
        m_TerrainData = m_Terrain.terrainData;

        m_TerrainSize = m_TerrainData.size;
        m_TerrainSizeRcp = new Vector3(1f / m_TerrainSize.x, 1f / m_TerrainSize.y, 1f / m_TerrainSize.z);
        m_TerrainBasePos = m_Terrain.transform.position;
        m_TerrainMaxPos = m_TerrainBasePos + m_TerrainSize;

        m_TerrainBounds.SetMinMax(m_TerrainBasePos, m_TerrainMaxPos);
        m_TerrainHeightResolution = m_TerrainData.heightmapWidth * m_TerrainSizeRcp.x;
        m_TerrainMaterialResolution = m_TerrainData.alphamapWidth * m_TerrainSizeRcp.x;
    }

    bool ITE3BridgeProvider.ProvidesDataAt(float x, float z) {
        return x >= m_TerrainBasePos.x && x <= m_TerrainMaxPos.x && z >= m_TerrainBasePos.z && z <= m_TerrainMaxPos.z;
    }

    Bounds ITE3BridgeProvider.GetBounds() {
        return m_TerrainBounds;        
    }

    float ITE3HeightProvider.GetHeightAndNormalAt(float x, float z, out Vector3 normal) {
        x = (x - m_TerrainBasePos.x) * m_TerrainSizeRcp.x;
        z = (z - m_TerrainBasePos.z) * m_TerrainSizeRcp.z;
        normal = m_TerrainData.GetInterpolatedNormal(x, z);
        return m_TerrainData.GetInterpolatedHeight(x, z) + m_TerrainBasePos.y; // GetInterpolatedHeight is already scaled to size, but not offset by transform.
    }

    float ITE3HeightProvider.GetHeightAt(float x, float z) {
        x = (x - m_TerrainBasePos.x) * m_TerrainSizeRcp.x;
        z = (z - m_TerrainBasePos.z) * m_TerrainSizeRcp.z;
        return m_TerrainData.GetInterpolatedHeight(x, z) + m_TerrainBasePos.y; // GetInterpolatedHeight is already scaled to size, but not offset by transform.
    }

    Vector3 ITE3HeightProvider.GetNormalAt(float x, float z) {
        x = (x - m_TerrainBasePos.x) * m_TerrainSizeRcp.x;
        z = (z - m_TerrainBasePos.z) * m_TerrainSizeRcp.z;
        return m_TerrainData.GetInterpolatedNormal(x, z);
    }

    float ITE3HeightProvider.GetDataResolution() {
        return m_TerrainHeightResolution;
    }

    //void ITE3HeightProvider.Release() {
    //    Debug.Assert(m_heightRefCount > 0);
    //    --m_heightRefCount;
    //}

    TE3MaterialInfo ITE3MaterialProvider.GetMaterialAt(float x, float z) {
        x = (x - m_TerrainBasePos.x) * m_TerrainSizeRcp.x;
        z = (z - m_TerrainBasePos.z) * m_TerrainSizeRcp.z;
        var ix = Mathf.Clamp(Mathf.RoundToInt(x * m_TerrainData.alphamapWidth), 0, m_TerrainData.alphamapWidth - 1);
        var iz = Mathf.Clamp(Mathf.RoundToInt(z * m_TerrainData.alphamapWidth), 0, m_TerrainData.alphamapWidth - 1);
        var alphas = m_TerrainData.GetAlphamaps(ix, iz, 1, 1);

        if(alphas.GetLength(2) == 0)
            return default(TE3MaterialInfo);

        var maxWeight = alphas[0, 0, 0];
        var index = 0;
        for(int i = index, n = m_TerrainData.alphamapLayers ; i < n; ++i) {
            var w = alphas[0, 0, i];
            if(w > maxWeight) {
                maxWeight = w;
                index = i;
            }
        }

        var splat = m_TerrainData.splatPrototypes[index];
        return new TE3MaterialInfo { albedo = splat.texture, normal = splat.normalMap, uvScale = splat.tileSize.x };
    }

    float ITE3MaterialProvider.GetDataResolution() {
        return m_TerrainMaterialResolution;
    }

    //void ITE3MaterialProvider.Release() {
    //    Debug.Assert(m_materialRefCount > 0);

    //    if(--m_materialRefCount == 0) {

    //    }
    //}

}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios

#endif //UNITY_EDITOR
