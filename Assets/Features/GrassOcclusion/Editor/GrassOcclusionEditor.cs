using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrassOcclusion))]
[CanEditMultipleObjects]
public class GrassOcclusionEditor : Editor
{
	override public void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUILayout.Space();

		if(GUILayout.Button("Bake"))
		{
			((GrassOcclusion)target).Bake();
			SceneView.RepaintAll();
		}

		EditorGUILayout.Space();
	}
}
