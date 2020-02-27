
#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif
using UnityEngine;

namespace TerrainFoley {

public class TerrainFoleyManager : MonoBehaviour {
    public static TerrainFoleyManager current { get; private set; }

    public VegetationLayers vegetationLayers;

    public bool visualizeOctree = false;
    public int visualizeOctreeLevel = -1;

    [HideInInspector] public VegetationMap vegetationMap;
    public SplatMap splatMap;

#if UNITY_EDITOR
    [PostProcessScene]
    static void OnPostProcess() {
        var manager = Object.FindObjectOfType<TerrainFoleyManager>();
        if (manager) {
            var terrain = manager.GetCurrentTerrain();
            if (terrain)
                manager.vegetationMap.Bake(terrain, manager.vegetationLayers);
        }
    }
#endif

    public Terrain GetCurrentTerrain() {
        return Terrain.activeTerrain ?? GetComponent<Terrain>();
    }

    protected void Start() {
        var terrain = GetCurrentTerrain();
        if (terrain) {
            splatMap.Initialize(terrain);
            vegetationMap.Initialize();
            current = this;
        }
    }

    protected void OnDestroy() {
        if (current == this)
            current = null;
    }

    public bool QueryVegetation(Bounds bounds, out Vector3 outPosition, out VegetationType outType) {
        return vegetationMap.Query(bounds, out outPosition, out outType);
    }

#if UNITY_EDITOR
    protected void OnDrawGizmos() {
        vegetationMap.DrawGizmos(visualizeOctree, visualizeOctreeLevel);
    }
#endif
}

} // TerrainFoley

