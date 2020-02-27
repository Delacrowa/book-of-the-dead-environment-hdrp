
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using UnityEditor;
using UnityEngine;

namespace Hapki.Editor {

[CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
public class EnumFlagsDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        property.intValue = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
    }
}

} // Hapki.Editor

