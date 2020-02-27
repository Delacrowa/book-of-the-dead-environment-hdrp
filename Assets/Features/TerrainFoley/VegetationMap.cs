
using System;
using System.Collections.Generic;
using UnityEngine;
using Hapki.Spatial;

namespace TerrainFoley {

public enum VegetationType {
    None,
    Undergrowth,
}

[Serializable]
public struct VegetationLayers {
    public LayerMask undergrowthLayerMask;
}

[Serializable]
public struct VegetationMap {
    static bool GetVegetationInfo(
            GameObject @object, VegetationLayers layers, out VegetationType type, out Bounds bounds) {
        if (@object) {
            if ((layers.undergrowthLayerMask & (1 << @object.layer)) != 0)
                type = VegetationType.Undergrowth;
            else
                goto bail;

            var renderer = @object.GetComponentInChildren<Renderer>();
            if (!renderer)
                goto bail;

            bounds = renderer.bounds;
            return true;
        }

    bail:
        type = VegetationType.None;
        bounds = default(Bounds);
        return false;
    }

    public Bounds rootBounds;
    public List<Bounds> dataBounds;
    public List<VegetationType> dataTypes;

    Octree<VegetationType> _octree;
    List<Octree<VegetationType>.Data> _results;

    public void Bake(Terrain terrain, VegetationLayers layers) {
        var sizeFuzzyFudgeFactor = new Vector3(0.5f, 1.667f, 0.5f);

        var data = terrain.terrainData;
        var protos = data.treePrototypes;
        var origin = terrain.GetPosition();

        rootBounds = new Bounds();
        dataBounds = new List<Bounds>(1000);
        dataTypes = new List<VegetationType>(1000);

        for (int i = 0, n = data.treeInstanceCount; i < n; ++i) {
            var tree = data.GetTreeInstance(i);
            VegetationType type;
            Bounds bounds;

            if (GetVegetationInfo(protos[tree.prototypeIndex].prefab, layers, out type, out bounds)) {
                bounds.center = origin + Vector3.Scale(data.size, tree.position);
                bounds.size = Vector3.Scale(bounds.size, sizeFuzzyFudgeFactor);

                rootBounds.SetMinMax(
                    Vector3.Min(rootBounds.min, bounds.min),
                    Vector3.Max(rootBounds.max, bounds.max));

                dataBounds.Add(bounds);
                dataTypes.Add(type);
            }
        }
    }

    public void Initialize() {
        _octree = Octree<VegetationType>.Create(rootBounds);
        _results = new List<Octree<VegetationType>.Data>(100);

        for (int i = 0, n = dataBounds.Count; i < n; ++i)
            if (!_octree.Add(new Octree<VegetationType>.Data {
                        bounds = dataBounds[i],
                        value = dataTypes[i]
                    }))
                Debug.LogWarningFormat("Failed to map vegetation {0}/{1}", i, n);
    }

    public bool Query(Bounds bounds, out Vector3 outPosition, out VegetationType outType) {
        if (_octree != null) {
            _results.Clear();
            _octree.QueryIntersecting(bounds, _results);

            var position = bounds.center;
            position.y = bounds.min.y; // foot approximation

            float minDistance = Mathf.Infinity;
            int index = -1;
            for (int i = 0, n = _results.Count; i < n; ++i) {
                var sqrDistance = (_results[i].bounds.ClosestPoint(position) - position).sqrMagnitude;
                if (minDistance > sqrDistance) {
                    minDistance = sqrDistance;
                    index = i;
                }
            }

            const float sqrTouchingDistance = 0.01f;
            if (index >= 0 && minDistance <= sqrTouchingDistance) {
                outPosition = _results[index].bounds.center;
                outType = _results[index].value;
                return true;
            }
        }

        outPosition = Vector3.zero;
        outType = VegetationType.None;
        return false;
    }

    public void DrawGizmos(bool visualizeOctree, int visualizeOctreeLevel) {
        if (visualizeOctree && _octree != null)
            _octree.DrawGizmos(visualizeOctreeLevel);
    }
}

} // TerrainFoley

