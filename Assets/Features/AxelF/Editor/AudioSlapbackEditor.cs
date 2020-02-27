
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

[CustomEditor(typeof(AudioSlapback))]
public class AudioSlapbackEditor : AudioZoneEditor {
    [MenuItem("AxelF/GameObject/Audio Slapback")]
    static void CreateAudioSlapback() {
        var o = new GameObject("Audio Slapback");
        o.AddComponent<AudioSlapback>();

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

    protected override void DrawZoneLabel(Zone z, Vector3 p) {
        if (!(z is AudioSlapback)) {
            base.DrawZoneLabel(z, p);
            return;
        }

        Handles.BeginGUI();
        var c = GUI.color;
        GUI.color = Color.white;
        var x = new GUIContent(z.name);
        var y = GUIContent.none;
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
                Undo.RecordObject(z, "Changed Slapback Radius");
                z.radius = f;
            }
        }
    }
}

} // Editor
} // AxelF
