
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

[CustomPropertyDrawer(typeof(ColorizeAttribute))]
public class ColorizeDrawer : PropertyDrawer {
    static Dictionary<object, int> _lookup = new Dictionary<object, int>();

    static readonly Color _disabledColor = new Color(0.75f, 0.75f, 0.75f);

    static readonly Color[] _colors = new Color[] {
        new Color(0.85f, 1.00f, 1.00f),
        new Color(0.85f, 1.00f, 0.85f),
        new Color(0.95f, 1.00f, 0.75f),
        new Color(1.00f, 0.75f, 0.65f),
        new Color(1.00f, 0.75f, 0.95f),
        new Color(0.75f, 0.75f, 1.00f),
        new Color(0.75f, 0.85f, 1.00f)
    };

    static int _index;

    public static Color disabledColor {
        get { return _disabledColor; }
    }

    public static void Reset() {
        _lookup.Clear();
        _index = 0;
    }

    public static Color GetColor(int _index) {
        return _colors[_index % _colors.Length];
    }

    public static Color GetColor(string path) {
        int i, j;
        while ((i = path.IndexOf('[')) > 0) {
            j = path.IndexOf(']');
            path = path.Substring(0, i) + path.Substring(j + 1);
        }
        if (!_lookup.TryGetValue(path, out i)) {
            i = _index;
            _lookup[path] = i;
            _index = i + 1;
        }
        return GetColor(i);
    }

    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) {
        return EditorGUI.GetPropertyHeight(prop, label, true);
    }

    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label) {
        var oldColor = GUI.color;
        GUI.color = GetColor(prop.propertyPath);
        EditorGUI.PropertyField(pos, prop, label, true);
        GUI.color = oldColor;
    }
}

} // Editor
} // AxelF

