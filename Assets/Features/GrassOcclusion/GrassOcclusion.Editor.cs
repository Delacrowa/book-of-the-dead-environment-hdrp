#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public partial class GrassOcclusion : MonoBehaviour
{
	[System.Serializable]
	public class GrassPrototype
	{
		public bool m_Use = true;
		public GameObject m_Prefab;
		public Texture2D m_Occlusion;
		[Range(-1, 1)]
		public float m_Bias = 0.0f;
	}

	struct GrassInstance
	{
		public Vector2 position;
		public float scale;
		public float rotation;
	}

	[Header("Bake Settings")]
	public int m_Resolution = 2048;
	public Terrain m_Terrain;
	public GrassPrototype[] m_GrassPrototypes;
	public bool m_NonTerrainInstances = true;

	[HideInInspector]
	public Shader m_Shader;
	Material m_Material;

	static class UniformsBake
	{
		internal static readonly int _Instances = Shader.PropertyToID("_Instances");
		internal static readonly int _Verts = Shader.PropertyToID("_Verts");
		internal static readonly int _Bias = Shader.PropertyToID("_Bias");
		internal static readonly int _Occlusion = Shader.PropertyToID("_Occlusion");
	}

	public void Bake()
	{
		string dataPath = SceneToGrassOcclusionDataPath(gameObject.scene);
        // We don't care where was the old asset we were referencing. The new one has to be at the
        // canonical path. So we check if it's there already.
        m_Data = AssetDatabase.LoadMainAssetAtPath(dataPath) as GrassOcclusionData;

        if (m_Data == null)
        {
            // Assigning a new asset, dirty the scene that contains it, so that the user knows to save it.
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            m_Data = ScriptableObject.CreateInstance<GrassOcclusionData>();
            AssetDatabase.CreateAsset(m_Data, dataPath);
        }
        else
        {
            // Clean up the old textures
            DestroyImmediate(m_Data.occlusion, true);
        }

		if (m_Data.occlusion == null || m_Data.occlusion.width != m_Resolution || m_Data.occlusion.height != m_Resolution)
		{
			DestroyImmediate(m_Data.occlusion);
			m_Data.occlusion = new Texture2D(m_Resolution, m_Resolution, TextureFormat.Alpha8, false, true);
			m_Data.occlusion.name = "Grass Occlusion";
			AssetDatabase.AddObjectToAsset(m_Data.occlusion, m_Data);
		}

		if (m_Material == null)
		{
			m_Material = new Material(m_Shader);
			m_Material.hideFlags = HideFlags.HideAndDontSave;
		}

		TreeInstance[] instances = m_Terrain.terrainData.treeInstances;
		TreePrototype[] prototypes = m_Terrain.terrainData.treePrototypes;

		float magicalScaleConstant = 41.5f; //yea, I know
		float terrainScale = magicalScaleConstant / m_Terrain.terrainData.size.x;
		Matrix4x4 worldToLocal = GetTerrainWorldToLocal(m_Terrain);

		RenderTexture rt = RenderTexture.GetTemporary(m_Resolution, m_Resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		Graphics.SetRenderTarget(rt);
		GL.Clear(false, true, Color.white);

		foreach(GrassPrototype p in m_GrassPrototypes)
			SplatOcclusion(p, instances, prototypes, m_Material, terrainScale, m_NonTerrainInstances, worldToLocal);

		// Can't read pixels of an Alpha8 texture, so gotta go through a 32b format :/
		Texture2D temp = new Texture2D(m_Resolution, m_Resolution, TextureFormat.ARGB32, false, true);
		temp.ReadPixels(new Rect(0, 0, m_Resolution, m_Resolution), 0, 0, false);

		RenderTexture.ReleaseTemporary(rt);

		byte[] data32 = temp.GetRawTextureData();
		byte[] data8 = new byte[data32.Length/4];

		// Copy from a 4-channel layout into 1-channel
		for (int j = 0; j < data8.Length; j++)
			data8[j] = (byte)data32[j * 4 + 1];

		DestroyImmediate(temp);

		m_Data.occlusion.LoadRawTextureData(data8);
		m_Data.occlusion.Apply();

		m_Data.worldToLocal = worldToLocal;

		DestroyImmediate(m_Data.heightmap, true);
		m_Data.heightmap = GetTerrainHeightmap(m_Terrain, out m_Data.terrainHeightRange);
		AssetDatabase.AddObjectToAsset(m_Data.heightmap, m_Data);
		m_Data.terrainHeight = m_Terrain.terrainData.size.y;

		AssetDatabase.SaveAssets();
	}

	static void SplatOcclusion(GrassPrototype grassPrototype, TreeInstance[] instances, TreePrototype[] prototypes, Material material, float terrainScale, bool nonTerrainInstances, Matrix4x4 worldToLocal)
	{
		if (!grassPrototype.m_Use)
			return;

		int instanceCount = 0;
		foreach(TreeInstance ti in instances)
			if (prototypes[ti.prototypeIndex].prefab == grassPrototype.m_Prefab)
				instanceCount++;

		List<GameObject> manualInstances = null;
		if (nonTerrainInstances)
		{
			manualInstances = FindAllPrefabInstances(grassPrototype.m_Prefab);
			instanceCount += manualInstances.Count;
		}

		if (instanceCount == 0)
			return;

		GrassInstance[] grassInstances = new GrassInstance[instanceCount];

		Bounds bounds = grassPrototype.m_Prefab.GetComponent<LODGroup>().GetLODs()[0].renderers[0].GetComponent<MeshFilter>().sharedMesh.bounds;
		float sizex = bounds.extents.x + Mathf.Abs(bounds.center.x);
		float sizez = bounds.extents.z + Mathf.Abs(bounds.center.z);
		float size = Mathf.Max(sizex * grassPrototype.m_Prefab.transform.localScale.x, sizez * grassPrototype.m_Prefab.transform.localScale.z);
		size *= terrainScale;

		int i = 0;
		foreach(TreeInstance inst in instances)
		{
			//TreePrototype prototype = prototypes[ti.prototypeIndex];
			if (prototypes[inst.prototypeIndex].prefab != grassPrototype.m_Prefab)
				continue;

			GrassInstance grassInstance = new GrassInstance();

			Vector3 pos = inst.position;
			grassInstance.position = new Vector2(pos.x, pos.z);
			
			grassInstance.scale = inst.widthScale * size;

			grassInstance.rotation = inst.rotation;

			grassInstances[i] = grassInstance;

			i++;
		}

		if (nonTerrainInstances && manualInstances != null && manualInstances.Count > 0)
		{
			foreach(GameObject inst in manualInstances)
			{
				GrassInstance grassInstance = new GrassInstance();

				Vector3 pos = inst.transform.position;
				pos = worldToLocal.MultiplyPoint3x4(pos);
				grassInstance.position = new Vector2(pos.x, pos.z);
				
				grassInstance.scale = inst.transform.localScale.x * size;

				grassInstance.rotation = inst.transform.rotation.eulerAngles.y * Mathf.Deg2Rad;

				grassInstances[i] = grassInstance;

				i++;
			}
		}

		ComputeBuffer grassInstancesCB = new ComputeBuffer(instanceCount, 16);
		grassInstancesCB.SetData(grassInstances);
		material.SetBuffer(UniformsBake._Instances, grassInstancesCB);

		Vector2[] verts = new Vector2[]{new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, 1), new Vector2(-1, 1)};
		int vertCount = verts.Length;
		ComputeBuffer vertsCB = new ComputeBuffer(vertCount, 8);
		vertsCB.SetData(verts);
		material.SetBuffer(UniformsBake._Verts, vertsCB);

		material.SetTexture(UniformsBake._Occlusion, grassPrototype.m_Occlusion);
		material.SetFloat(UniformsBake._Bias, grassPrototype.m_Bias);
		material.SetPass(0);

		Graphics.DrawProcedural(MeshTopology.Triangles, vertCount, instanceCount);
		vertsCB.Release();
	}

	static Matrix4x4 GetTerrainWorldToLocal(Terrain terrain)
	{
		Vector3 size = terrain.terrainData.size;
		Matrix4x4 localToWorld = Matrix4x4.TRS(terrain.transform.position, Quaternion.identity, new Vector3(size.x, size.y, size.z));
		return localToWorld.inverse;
	}

	static Texture2D GetTerrainHeightmap(Terrain terrain, out float maxHeight)
	{
		int res = terrain.terrainData.heightmapResolution;
		float[,] heights = terrain.terrainData.GetHeights(0, 0, res, res);

		maxHeight = 0;
		for (int i = 0; i < res; i++)
			for (int j = 0; j < res; j++)
				maxHeight = Mathf.Max(maxHeight, heights[i, j]);

		float encodeBytesAndRange = 255 / maxHeight;

		byte[] bheights = new byte[res*res];
		for (int i = 0; i < res; i++)
			for (int j = 0; j < res; j++)
				bheights[i * res + j] = (byte)(encodeBytesAndRange * heights[i, j]);

		// TODO: res is always POT+1, because that's how terrain works; make it POT and adjust the matrix to componsate
		Texture2D heightmap = new Texture2D(res, res, TextureFormat.Alpha8, false);
		heightmap.LoadRawTextureData(bheights);

		heightmap.name = "Terrain Heightmap";
		heightmap.wrapMode = TextureWrapMode.Clamp;
		heightmap.Apply();
		return heightmap;
	}

	string SceneToGrassOcclusionDataPath(Scene scene)
    {
        // Scene path: "Assets/Folder/Scene.unity"
        // We want: "Assets/Folder/Scene/GrassOcclusionData.asset"
        int suffixLength = 6;
        string path = scene.path;

        if (path.Substring(path.Length - suffixLength) != ".unity")
            Debug.LogError("Something's wrong with the path to the scene", this);
        
        return path.Substring(0, path.Length - suffixLength) + "/GrassOcclusionData.asset";
    }

	static List<GameObject> FindAllPrefabInstances(UnityEngine.Object prefab)
	{
		List<GameObject> instances = new List<GameObject>();
		GameObject[] gameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];

		foreach(GameObject go in gameObjects)
			if (PrefabUtility.GetPrefabType(go) == PrefabType.PrefabInstance && PrefabUtility.GetCorrespondingObjectFromSource(go) == prefab)
				instances.Add(go);

		return instances;
	}

}
#endif