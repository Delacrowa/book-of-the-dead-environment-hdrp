#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveOcclusionToTexture : MonoBehaviour
{
	public GameObject m_UseMyName;
	public OcclusionProbeData m_Data;

	public void Save()
	{
		Texture3D src = m_Data.occlusion;
		int width = src.width;
		int height = src.depth;
		Color32[] srcData = src.GetPixels32();

		int countx = src.width;
		int county = src.height;
		int countz = src.depth;

		byte[] dstData = new byte[width * height];

		for(int x = 0; x < countx; x++)
		{
			// for(int y = 0; y < county; y++)
			int y = 0;
			{
				for(int z = 0; z < countz; z++)
				{
					dstData[x + z * countx] = srcData[x + y * countx + z * countx * county].a;
				}
			}
		}

		Texture2D dst = new Texture2D(width, height, TextureFormat.Alpha8, false);
		dst.LoadRawTextureData(dstData);
		dst.Apply();

		dst.wrapMode = TextureWrapMode.Clamp;

		string dataPath = SceneToOcclusionProbeDataPath(gameObject.scene, m_UseMyName.name);
		AssetDatabase.CreateAsset(dst, dataPath);

		AssetDatabase.SaveAssets();
	}

	string SceneToOcclusionProbeDataPath(Scene scene, string name)
    {
        // Scene path: "Assets/Folder/Scene.unity"
        // We want: "Assets/Folder/Scene/name-OcclusionSlice.asset"
        int suffixLength = 6;
        string path = scene.path;

        if (path.Substring(path.Length - suffixLength) != ".unity")
            Debug.LogError("Something's wrong with the path to the scene", this);
        
        return path.Substring(0, path.Length - suffixLength) + "/" + name + "-OcclusionSlice.asset";
    }
}

[CustomEditor(typeof(SaveOcclusionToTexture))]
public class SaveOcclusionToTextureEditor : Editor
{
	override public void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUILayout.Space();

		if(GUILayout.Button("Save"))
		{
			((SaveOcclusionToTexture)target).Save();
			SceneView.RepaintAll();
		}

		EditorGUILayout.Space();
	}
}
#endif