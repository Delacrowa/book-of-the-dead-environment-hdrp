
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using System;
using UnityEditor;
using UnityEngine;

namespace Hapki.Editor {

[CustomPropertyDrawer(typeof(SerializedEnumAttribute))]
public class SerializedEnumDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        try {
            var type = ((SerializedEnumAttribute) attribute).type;
            var name = property.stringValue;
            var value = default(Enum);
            if (string.IsNullOrEmpty(name)) {
                var values = Enum.GetValues(type);
                if (values != null && values.Length > 0)
                    value = (Enum) values.GetValue(0);
            } else
                value = (Enum) Enum.Parse(type, name);
            var newValue = EditorGUI.EnumPopup(position, label, value);
            property.stringValue = newValue.ToString();
        } catch {
        }
    }
}

} // Hapki.Editor

