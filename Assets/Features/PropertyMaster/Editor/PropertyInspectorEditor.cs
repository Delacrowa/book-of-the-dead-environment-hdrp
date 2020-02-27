
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CustomEditor(typeof(PropertyInspector))]
public class PropertyInspectorEditor : Editor {
    struct Info {
        public string order;
        public Volume volume;
        public string weight;
        public string factor;
        public bool active;
    }

    readonly List<Info> _info = new List<Info>();
    readonly List<Collider> _colliders = new List<Collider>();
    int _numActiveInfo;

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        var camera = Camera.main;
        var inspector = target as PropertyInspector;

        _info.Clear();
        _numActiveInfo = 0;

        if (camera && inspector.layerMask != 0)
            GrabVolumeInfo(camera.transform, inspector.layerMask);

        DrawVolumeInfo();
    }

    void DrawVolumeInfo() {
        var width = EditorGUIUtility.currentViewWidth;

        if (_numActiveInfo == 0) {
            GUI.color = new Color(0.5f, 1f, 0.8f, 1f);
            EditorGUILayout.HelpBox(new GUIContent("No active volumes"), true);
        } else {
            GUI.color = new Color(1f, 1f, 0.5f, 1f);

            if (_numActiveInfo == 1)
                EditorGUILayout.HelpBox(
                    new GUIContent("Warning: 1 active volume is overriding properties"), true);
            else
                EditorGUILayout.HelpBox(
                    new GUIContent("Warning: " + _numActiveInfo + " active volumes are overriding properties"),
                    true);
        }

        GUI.color = Color.white;

        foreach (var i in _info) {
            GUI.enabled = i.active;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(i.order, GUILayout.Width(width * 0.1f));
            EditorGUILayout.ObjectField(i.volume, typeof(Volume), true, GUILayout.Width(width * 0.5f));
            GUILayout.Label(i.weight, GUILayout.Width(width * 0.1f));
            GUILayout.Label(i.factor, GUILayout.Width(width * 0.3f));
            EditorGUILayout.EndHorizontal();
        }

        GUI.enabled = true;
    }

    void GrabVolumeInfo(Transform trigger, LayerMask layerMask) {
        var volumes = VolumeManager.instance.GrabVolumes(layerMask);
        var triggerPos = trigger.position;

        foreach (var volume in volumes)
            if (!volume.enabled)
                AddInfo(volume, false, "Disabled");
            else if (volume.isGlobal)
                AddInfo(volume, true, "Global");
            else {
                _colliders.Clear();
                volume.GetComponents(_colliders);

                float interpFactor = GetInterpFactor(volume, _colliders, triggerPos);

                if (interpFactor < 0f)
                    AddInfo(volume, false, "Out of Range");
                else
                    AddInfo(volume, true, interpFactor);
            }
    }

    float GetInterpFactor(Volume volume, List<Collider> colliders, Vector3 triggerPos) {
            float closestDistanceSqr = float.PositiveInfinity;

            foreach (var collider in colliders) {
                if (!collider.enabled)
                    continue;

                var closestPoint = collider.ClosestPoint(triggerPos);
                var d = (closestPoint - triggerPos).sqrMagnitude;

                if (d < closestDistanceSqr)
                    closestDistanceSqr = d;
            }

            float blendDistSqr = volume.blendDistance * volume.blendDistance;

            if (closestDistanceSqr > blendDistSqr)
                return -1f;

            float interpFactor = 1f;

            if (blendDistSqr > 0f)
                interpFactor = Mathf.SmoothStep(1f, 0f, closestDistanceSqr / blendDistSqr);

            return interpFactor;
    }

    void AddInfo(Volume volume, bool active, string factor) {
        _info.Add(new Info {
            order = (_info.Count + 1).ToString("X2"),
            volume = volume,
            weight = volume.weight.ToString("N3"),
            factor = factor,
            active = active
        });

        if (active)
            ++_numActiveInfo;
    }

    void AddInfo(Volume volume, bool active, float factor) {
        AddInfo(volume, active, factor.ToString("N3"));
    }
}

