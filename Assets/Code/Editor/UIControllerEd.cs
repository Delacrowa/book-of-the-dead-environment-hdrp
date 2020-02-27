
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIController))]
public class UIControllerEd : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        GUI.enabled = !Application.isPlaying;
        GUILayout.Space(5);
        GUILayout.Label("Edit Mode", EditorStyles.boldLabel);

        UIController.targetView = (UIView)
            EditorGUILayout.EnumPopup("Edit View", UIController.targetView);

        var uiController = (UIController) target;


        uiController.editController = (UIController.EditController)
            EditorGUILayout.EnumPopup("Edit Controller", uiController.editController);

        uiController.ForceUpdate(true);
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}

