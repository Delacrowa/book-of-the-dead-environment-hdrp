using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(LinkedRangeAttribute))]
internal sealed class LinkedRangeDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var mainPosition = new Rect(position.x, position.y, position.width - 24, position.height);
        var lPosition = new Rect(mainPosition.width + 4, position.y, position.width - mainPosition.width - 4, position.height);

        var rangeAttribute = (LinkedRangeAttribute)attribute;

        if(property.propertyType == SerializedPropertyType.Float) {
            EditorGUI.Slider(mainPosition, property, rangeAttribute.min, rangeAttribute.max, label);
        } else if(property.propertyType == SerializedPropertyType.Integer) {
            EditorGUI.IntSlider(mainPosition, property, (int)rangeAttribute.min, (int)rangeAttribute.max, label);
        } else {
            EditorGUI.LabelField(mainPosition, label.text, "Use Range with float or int.");
        }

        var spLink = property.serializedObject.FindProperty(rangeAttribute.linkField);
        var spLinked = property.serializedObject.FindProperty(rangeAttribute.linkedField);

        EditorGUI.BeginChangeCheck();
        spLink.boolValue = EditorGUI.Toggle(lPosition, GUIContent.none, spLink.boolValue, "button");
        if(EditorGUI.EndChangeCheck() && spLink.boolValue) {
            if(property.propertyType == SerializedPropertyType.Float && spLinked.floatValue != property.floatValue)
                spLinked.floatValue = property.floatValue = (spLinked.floatValue + property.floatValue) / 2f;
            else if(property.propertyType == SerializedPropertyType.Integer && spLinked.intValue != property.intValue)
                spLinked.intValue = property.intValue = (spLinked.intValue + property.intValue) / 2;
        }

        if(spLink.boolValue) {
            if(property.propertyType == SerializedPropertyType.Float && spLinked.floatValue != property.floatValue)
                spLinked.floatValue = property.floatValue;
            else if(property.propertyType == SerializedPropertyType.Integer && spLinked.intValue != property.intValue)
                spLinked.intValue = property.intValue;
        }
    }
}