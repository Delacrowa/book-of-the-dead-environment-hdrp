
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

[CanEditMultipleObjects]
[CustomEditor(typeof(AudioEmitter))]
public class AudioEmitterEditor : UnityEditor.Editor {
    [MenuItem("AxelF/GameObject/Audio Emitter")]
    static void CreateAudioEmitter() {
        var o = new GameObject("Audio Emitter");
        o.AddComponent<AudioEmitter>();

        var p = Selection.activeGameObject;
        if (p != null && !AssetDatabase.IsMainAsset(p) && !AssetDatabase.IsSubAsset(p))
            o.transform.parent = p.transform;

        EditorGUIUtility.PingObject(o);
    }

    public override void OnInspectorGUI() {
        ColorizeDrawer.Reset();
        var oldColor = GUI.color;
        GUI.color = ColorizeDrawer.GetColor("");
        DrawDefaultInspector();
        GUI.color = oldColor;
    }

    protected void OnSceneGUI() {
        var t = ((AudioEmitter) target).transform;
        AudioZone z = null;
        for (; t != null; t = t.transform.parent)
            if ((z = t.GetComponent<AudioZone>()) != null)
                break;
        if (z != null) {
            var v = z.transform.position;
            AudioZoneEditor.DrawZoneLabelStatic(z, v); // XXX this is drawn multiple times because zone
            var q = z.transform.parent;
            if (q != null)
                for (int i = 0, n = q.childCount; i < n; ++i) {
                    var u = q.GetChild(i).GetComponents<AudioZone>();
                    for (int j = 0, m = u.Length; j < m; ++j)
                        if (u[j] && u[j] != z)
                            AudioZoneEditor.DrawZoneLabelStatic(
                                u[j], u[j].transform.position);
                }
        }
    }
}

} // Editor
} // AxelF
