
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using System.Collections.Generic;
using UnityEngine;

namespace Hapki.Spatial {

sealed public class Octree<T> {
    public struct Data {
        public Bounds bounds;
        public T value;

        public static bool operator==(Data x, Data y) {
            return x.GetHashCode() == y.GetHashCode();
        }

        public static bool operator!=(Data x, Data y) {
            return !(x == y);
        }

        public override bool Equals(object o) {
            return o is Data && this == (Data) o;
        }

        public override int GetHashCode() {
            return bounds.GetHashCode() ^ value.GetHashCode();
        }
    }

    public static Octree<T> Create(Bounds bounds) {
        return new Octree<T>(bounds.center, Vector3.one * MaxComponent(bounds.size));
    }

    static float MaxComponent(Vector3 v) {
        return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
    }
    
    static Octree<T>[] Create8(Bounds bounds) {
        var ext = bounds.extents;
        var mid = bounds.min + ext * 0.5f;
        return new Octree<T>[8] {
            new Octree<T>(mid, ext),
            new Octree<T>(mid + new Vector3(ext.x, 0f, 0f), ext),
            new Octree<T>(mid + new Vector3(0f, 0f, ext.z), ext),
            new Octree<T>(mid + new Vector3(ext.x, 0f, ext.z), ext),
            new Octree<T>(mid + new Vector3(0f, ext.y, 0f), ext),
            new Octree<T>(mid + new Vector3(ext.x, ext.y, 0f), ext),
            new Octree<T>(mid + new Vector3(0f, ext.y, ext.z), ext),
            new Octree<T>(mid + ext, ext),
        };
    }

    Bounds _bounds;
    List<Data> _data;
    Octree<T>[] _children;

    Octree(Vector3 position, Vector3 size) {
        _bounds = new Bounds(position, size);
    }

    public bool Add(Data data, int maxDepth = 6, int capacity = 50) {
        return Add(data, maxDepth, capacity, 0);
    }

    bool Add(Data data, int maxDepth, int capacity, int depth) {
        if (_bounds.Contains(data.bounds.center)) {
            if (depth < maxDepth) {
                if (_children == null)
                    _children = Create8(_bounds);
                for (int i = 0; i < 8; ++i)
                    if (_children[i].Add(data, maxDepth, capacity, depth + 1))
                        return true;
            }
            if (_data == null)
                _data = new List<Data>(capacity);
            _data.Add(data);
            return true;
        }
        return false;
    }

    public void Remove(Data data) {
        if (_bounds.Contains(data.bounds.center)) {
            if (_data != null)
                for (int i = 0, n = _data.Count; i < n; ++i)
                    if (_data[i] == data) {
                        _data[i] = _data[n - 1];
                        _data.RemoveAt(n - 1);
                        return;
                    }
            if (_children != null)
                for (int i = 0; i < 8; ++i)
                    _children[i].Remove(data);
        }
    }

    public void QueryIntersecting(Bounds bounds, List<Data> results) {
        if (_bounds.Intersects(bounds)) {
            if (_data != null)
                foreach (var i in _data)
                    if (i.bounds.Intersects(bounds))
                        results.Add(i);
            if (_children != null)
                for (int i = 0; i < 8; ++i)
                    _children[i].QueryIntersecting(bounds, results);
        }
    }

    public void RemoveIntersecting(Bounds bounds, List<Data> results) {
        if (_bounds.Intersects(bounds)) {
            if (_data != null)
                for (int n = _data.Count, i = n - 1; i >= 0; --i) {
                    var data = _data[i];
                    if (data.bounds.Intersects(bounds)) {
                        results.Add(data);
                        _data[i] = _data[n - 1];
                        _data.RemoveAt(n - 1);
                    }
                }
            if (_children != null)
                for (int i = 0; i < 8; ++i)
                    _children[i].RemoveIntersecting(bounds, results);
        }
    }

    public struct GizmoColors {
        public Color nodeWithDataColor;
        public Color nodeWithoutDataColor;
        public Color nodeDataColor;
    }

    public static GizmoColors gizmoColors = new GizmoColors {
        nodeWithDataColor = new Color(0.2f, 0.8f, 0.2f, 0.2f),
        nodeWithoutDataColor = new Color(0.2f, 0.2f, 0.8f, 0.2f),
        nodeDataColor = new Color(1f, 0.2f, 1f, 0.4f)
    };

    public void DrawGizmos(int drawDepth = -1) {
        DrawGizmos(drawDepth, 0);
    }

    void DrawGizmos(int drawDepth, int depth) {
        if (drawDepth < 0 || drawDepth == depth) {
            if (_data != null && _data.Count > 0)
                Gizmos.color = gizmoColors.nodeWithDataColor;
            else
                Gizmos.color = gizmoColors.nodeWithoutDataColor;
            Gizmos.DrawWireCube(_bounds.center, _bounds.size);
            if (_data != null) {
                Gizmos.color = gizmoColors.nodeDataColor;
                for (int i = 0, n = _data.Count; i < n; ++i) {
                    Gizmos.DrawCube(_data[i].bounds.center, _data[i].bounds.size);
                    Gizmos.DrawWireCube(_data[i].bounds.center, _data[i].bounds.size);
                }
            }
        }
        if (_children != null)
            for (int i = 0; i < 8; ++i)
                _children[i].DrawGizmos(drawDepth, depth + 1);
    }
}

} // Hapki.Spatial

