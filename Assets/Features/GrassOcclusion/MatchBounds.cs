#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MatchBounds : MonoBehaviour
{
	public Transform m_MatchThis;
	public Renderer m_ToThat;
	
	public void Match()
	{
		Bounds bounds = m_ToThat.bounds;
		m_MatchThis.position = new Vector3(m_ToThat.transform.position.x, m_MatchThis.position.y, m_ToThat.transform.position.z);
		float size = bounds.size.x;
		m_MatchThis.localScale = new Vector3(size, m_MatchThis.localScale.y, size);
	}
}

[CustomEditor(typeof(MatchBounds))]
public class MatchBoundsEditor : Editor
{
	override public void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUILayout.Space();

		if(GUILayout.Button("Match"))
		{
			((MatchBounds)target).Match();
			SceneView.RepaintAll();
		}

		EditorGUILayout.Space();
	}
}
#endif