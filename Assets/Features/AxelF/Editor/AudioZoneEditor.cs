
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

[CustomEditor(typeof(AudioZone))]
public class AudioZoneEditor : UnityEditor.Editor {
    protected static StringBuilder builder = new StringBuilder();

    [MenuItem("AxelF/GameObject/Audio Zone")]
    static void CreateAudioZone() {
        var o = new GameObject("Audio Zone");
        o.AddComponent<AudioZone>();

        var p = Selection.activeGameObject;
        if (p != null && !AssetDatabase.IsMainAsset(p) && !AssetDatabase.IsSubAsset(p))
            o.transform.parent = p.transform;

        EditorGUIUtility.PingObject(o);
    }

    public override void OnInspectorGUI() {
        ColorizeDrawer.Reset();
        var oldColor = GUI.color;
        GUI.color = ColorizeDrawer.GetColor("");

        serializedObject.Update();
        var prop = serializedObject.GetIterator();
        var targ = (AudioZone) serializedObject.targetObject;
        bool disabled = false;
        if (prop.NextVisible(true))
            do {
                disabled = !(prop.name != "m_Script"
                        && (prop.name != "radius" || targ.trigger == null)
                        && (prop.name != "layerMask" || targ.trigger != null));
                EditorGUI.BeginDisabledGroup(disabled);
                EditorGUILayout.PropertyField(prop);
                EditorGUI.EndDisabledGroup();
            } while (prop.NextVisible(false));
        serializedObject.ApplyModifiedProperties();

        GUI.color = oldColor;
    }

    protected static string GetZoneCaption(Zone z) {
        return string.Format("{0} ({1:N2})", z.name, z.GetRadius());
    }

    protected virtual void DrawZoneLabel(Zone z, Vector3 p) {
        DrawZoneLabelStatic(z, p);
    }

    public static void DrawZoneLabelStatic(Zone z, Vector3 p) {
        if (z is AudioZone) {
            var e = AudioZone.FindEmitters((AudioZone) z);
            if (e.Length > 0) {
                bool first = true;
                foreach (var i in e)
                    if (i.patches != null)
                        for (int j = 0, k = i.patches.Length; j < k; ++j)
                            if (i.patches[j]) {
                                if (first) {
                                    first = false;
                                    builder.Append('\n');
                                }
                                builder.Append('\n');
                                builder.Append(i.patches[j].name);
                            }
            }
        }
        var s = builder.ToString();
        builder.Length = 0;

        Handles.BeginGUI();
        var c = GUI.color;
        GUI.color = Color.white;
        var x = new GUIContent(GetZoneCaption(z));
        var y = new GUIContent(s);
        var l = HandleUtility.WorldPointToSizedRect(p, x, EditorStyles.boldLabel);
        var m = HandleUtility.WorldPointToSizedRect(p, y, EditorStyles.helpBox);
        float n = Mathf.Max(l.width, m.width);
        l.width = n;
        m.width = n;
        l.x -= m.width * 1.2f;
        l.y -= m.height * 0.5f;
        m.x = l.x - Mathf.Max(0f, m.width - l.width);
        m.y = l.y + 1f;
        EditorGUI.HelpBox(m, y.text, MessageType.None);
        EditorGUI.DropShadowLabel(l, x);
        GUI.color = c;
        Handles.EndGUI();

        if (z.trigger == null) {
            EditorGUI.BeginChangeCheck();
            float f = Handles.RadiusHandle(z.transform.rotation, p, z.radius);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(z, "Changed Zone Radius");
                z.radius = f;
            }
        }
    }

    protected void OnSceneGUI() {
        var z = (Zone) target;
        var p = z.transform.position;

        DrawZoneLabel(z, p);

        var u = z.FindParentZone();
        if (u != null) {
            var v = u.transform.position;
            if (u is AudioZone)
                DrawZoneLabel((AudioZone) u, v);
        }

        var q = z.transform.parent;
        if (q != null)
            for (int i = 0, n = q.childCount; i < n; ++i) {
                var v = q.GetChild(i).GetComponents<AudioZone>();
                for (int j = 0, m = v.Length; j < m; ++j) {
                    if (v[j] && v[j] != z)
                        DrawZoneLabel(v[j], v[j].transform.position);
                }
            }
    }
}

} // Editor
} // AxelF

