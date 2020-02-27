using UnityEngine;
using UnityEngine.Serialization;

public class GrassOcclusionData : ScriptableObject
{
	[Header("Realtime Tweaks")]
	[Range(0, 1)]
	public float occlusionAmountTerrain = 1.0f;
	[Range(0, 1)]
	public float occlusionAmountGrass = 1.0f;
	// [MinValue(0)]
	public float heightFadeBottom = 0.14f;
	// [MinValue(0)]
	public float heightFadeTop = 0.5f;
	public float cullHeight = 0.5f;

	[Header("Baked Results")]
	public Matrix4x4 worldToLocal;
	public Texture2D occlusion;
	public Texture2D heightmap;
	[HideInInspector]
	public float terrainHeight;
	[HideInInspector]
	public float terrainHeightRange;
}
