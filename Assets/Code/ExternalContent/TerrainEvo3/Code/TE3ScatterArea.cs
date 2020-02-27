/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

[SelectionBase]
[ExecuteInEditMode]
public class TE3ScatterArea : MonoBehaviour, TE3GUIDMgr.ITE3UniqueInstance, TE3ScatterMgr.ITE3ScatterModificationReceiver {
	[System.Serializable]
	public class Template {
        [HideInInspector]
        public bool             initialized;

        public bool             mute;
		public GameObject		asset;
		public float			innerRotation;
		public float			outerRotation;
		public Vector2			innerMinMaxScale;
		public Vector2			outerMinMaxScale;
		public Vector2			innerOuterHeightOffset;
		public Vector2			normalInnerOuterOffset;
		public bool				orientHitNormal;
		[Range(0f, 90f)]
		public float			slopeLimitAngle;
		[Range(0, 1)]
		public float			probability;
		public bool             bakeMesh;
		[Range(0, 1)]
		public float			wrapEnvironment;
		public bool             simulatePhysics;
        public bool             inflateConvexHull;
		public bool             useGrowsOnPhysics;
		public PhysicMaterial[]	growsOnPhysics;
		public bool             useGrowsOnTexture;
		public Texture2D[]	    growsOnTexture;

        public void SetDefaults() {
            initialized             = true;
            mute                    = false;
            asset                   = null;
            innerRotation           = 0f;
            outerRotation           = 0f;
            innerMinMaxScale        = Vector2.one;
            outerMinMaxScale        = Vector2.one;
		    innerOuterHeightOffset  = Vector2.one;
		    normalInnerOuterOffset  = Vector2.one;
            orientHitNormal         = false;
            slopeLimitAngle         = 45f;
            probability             = 1f;
            bakeMesh                = true;
            wrapEnvironment         = 0f;
            simulatePhysics         = false;
            inflateConvexHull       = false;
            useGrowsOnPhysics       = false;
            growsOnPhysics          = new PhysicMaterial[0];
            useGrowsOnTexture       = false;
            growsOnTexture          = new Texture2D[0];
        }
	}

    [System.Serializable]
	public abstract class ScatterArea {
		public string		    name;
	    public bool			    showDebugGizmo;
		public float		    rayStartAbove;
		public float		    rayDownwardLength;
        [ReadOnlyGUI]
		public float		    clearance;
		[Range(0, 500)]
		public int			    density;
		public List<Template>   templates;

        public virtual void SetDefaults() {
            name                = "<not set>";
            showDebugGizmo      = false;
            rayStartAbove       = 2f;
            rayDownwardLength   = 4f;
            clearance           = 0f;
            density             = 30;
            templates           = new List<Template>();
        }

        public abstract Vector3     GetSpawnPositionCandidate(Transform transform);
        public abstract Vector3     GetSpawnPositionOffset(Transform transform, Template template, Vector3 hitNormal);
        public abstract Quaternion  GetSpawnRotation(Transform transform, Template template, GameObject desiredAsset, Vector3 hitNormal);
        public abstract Vector3     GetSpawnScale(Transform transform, Template template, GameObject desiredAsset);
        public abstract void        DrawDebugGizmos(Transform transform);
	}

	[System.Serializable]
	public class RingArea : ScatterArea {
        [Range(.33f, 20f)]
		public float		innerRadius;
        [Range(1f, 20f)]
		public float		outerRadius;
        [Range(0.2f, 5f)]
		public float		distributionPower;
		public Vector3      offset;

        Vector3 m_Dir3D;
        float   m_Strength;

        public override void SetDefaults() {
            base.SetDefaults();

            innerRadius         = 1f;
            outerRadius         = 2f;
            distributionPower   = 1.5f;
        }

        public override Vector3 GetSpawnPositionCandidate(Transform transform) {
		    // Get a random direction
		    var dir2D = Random.insideUnitCircle;
		    m_Dir3D = new Vector3(dir2D.x, 0f, dir2D.y).normalized;

		    // Get distributed strength (focused on inner radius)
		    m_Strength = Mathf.Pow(Random.value, distributionPower);

		    // Get distributed distance
            var scale = TE3Utils.Vec3Max(transform.lossyScale);
		    var dist = Mathf.Lerp(innerRadius * scale, outerRadius * scale, m_Strength);

		    // Point candidate
		    var pnt = transform.position + offset + m_Dir3D * dist;

            return pnt;
        }

        public override Vector3 GetSpawnPositionOffset(Transform transform, Template template, Vector3 hitNormal) {
            var offset = Vector3.zero;

            // Offset Up
            offset.y += Mathf.Lerp(template.innerOuterHeightOffset.x, template.innerOuterHeightOffset.y, m_Strength);

            // Offset along 'up' (either world up or oriented up)
            offset += (template.orientHitNormal ? hitNormal : Vector3.up) * Mathf.Lerp(template.normalInnerOuterOffset.x, template.normalInnerOuterOffset.y, m_Strength);

            return offset;
        }

        public override Quaternion GetSpawnRotation(Transform transform, Template template, GameObject desiredAsset, Vector3 hitNormal) {        
		    // Get rotation (bend outward * random yaw * template rotation)
		    var bendAxis = Vector3.Cross(Vector3.up, m_Dir3D);
		    var bendRotation = Mathf.Lerp(template.innerRotation, template.outerRotation, m_Strength);

            Quaternion rot = Quaternion.identity;

		    // Orient to ground hit?
		    if(template.orientHitNormal)
			    rot *= Quaternion.FromToRotation(Vector3.up, hitNormal);

		    rot *= Quaternion.AngleAxis(bendRotation, bendAxis);
		    rot *= Quaternion.Euler(0f, Random.value * 360f - 180f, 0f);

            rot *= desiredAsset.transform.rotation;

            return rot;
        }

        public override Vector3 GetSpawnScale(Transform transform, Template template, GameObject desiredAsset) {
		    // Calculate scale
		    var minMaxScale = Vector2.Lerp(template.innerMinMaxScale, template.outerMinMaxScale, m_Strength);
		    var scale = desiredAsset.transform.lossyScale * Mathf.Lerp(minMaxScale.x, minMaxScale.y, Random.value);
            return scale;
        }

        public override void DrawDebugGizmos(Transform transform) {
            if(!showDebugGizmo)
                return;
            
            var scale = TE3Utils.Vec3Max(transform.lossyScale);
            var or = outerRadius * scale;
            var ir = innerRadius * scale;
			var radius = or - ir;
			var steps = Mathf.RoundToInt(radius * 10f);
			var stepSize = radius / (float)(steps - 1);
			for(int i = 0; i < steps; ++i) {
				var strength = Mathf.Pow(1f -  i / (float)steps, distributionPower);
				Handles.color = new Color(1f, 1f, 1f, strength);
				Handles.DrawSolidDisc(transform.position + offset, Vector3.up, ir + stepSize * i);
			}

			Handles.color = Color.gray;
			Handles.DrawSolidDisc(transform.position, Vector3.up, ir);
		}
	}

    [System.Serializable]
	public class RectArea : ScatterArea {
        [Range(1f, 15f)]
		public Vector2		innerDimensions;
        [Range(1f, 15f)]
		public Vector2		outerDimensions;
        [Range(0.2f, 5f)]
		public Vector2		distributionPowers;
		public Vector3      offset;

        public override void SetDefaults() {
            base.SetDefaults();

            innerDimensions     = Vector2.one;
            outerDimensions     = Vector2.one * 2f;
            distributionPowers  = Vector2.one * 1.5f;
        }

        public override Vector3 GetSpawnPositionCandidate(Transform transform) {
            return Vector3.zero;
        }

        public override Vector3 GetSpawnPositionOffset(Transform transform, Template template, Vector3 hitNormal) {
            return Vector3.zero;
        }

        public override Quaternion GetSpawnRotation(Transform transform, Template template, GameObject desiredAsset, Vector3 hitNormal) {
            return Quaternion.identity;
        }

        public override Vector3 GetSpawnScale(Transform transform, Template template, GameObject desiredAsset) {
            return Vector3.one;
        }

        public override void DrawDebugGizmos(Transform transform) {
        }
	}

    [System.Serializable]
	public class SplineArea : ScatterArea {
        public /*TE3BaseSpline*/Object    baseSpline;

        public override void SetDefaults() {
            base.SetDefaults();

            baseSpline  = null;
        }

        public override Vector3 GetSpawnPositionCandidate(Transform transform) {
            return Vector3.zero;
        }

        public override Vector3 GetSpawnPositionOffset(Transform transform, Template template, Vector3 hitNormal) {
            return Vector3.zero;
        }

        public override Quaternion GetSpawnRotation(Transform transform, Template template, GameObject desiredAsset, Vector3 hitNormal) {
            return Quaternion.identity;
        }

        public override Vector3 GetSpawnScale(Transform transform, Template template, GameObject desiredAsset) {
            return Vector3.one;
        }

        public override void DrawDebugGizmos(Transform transform) {
        }
    }

	public List<RingArea>	ringAreas;
	public List<RectArea>	rectAreas;
	public List<SplineArea>	splineAreas;
    [Range(0f, 1f)]
    public float            bakedLodFactor;
	public int			    randomSeed;

	[HideInInspector]
    [SerializeField]
    string                  m_GUID;

	[HideInInspector]
	[SerializeField]
	GameObject			    m_GeneratedRoot;

	[HideInInspector]
	[SerializeField]
	GameObject			    m_BakedRoot;
	
    GameObject			    m_CacheRoot;
    List<Transform>         m_WorkingCache = new List<Transform>();

	public GameObject GeneratedRoot { get { return m_GeneratedRoot; } }
	
    public void SetBakedData(List<GameObject> bakedSources, List<GameObject> bakedOutputs) {
        foreach(var bs in bakedSources)
            Object.DestroyImmediate(bs);

        if(m_GeneratedRoot.transform.childCount == 0) {
            Object.DestroyImmediate(m_GeneratedRoot);
            m_GeneratedRoot = null;
        }
        Object.DestroyImmediate(m_CacheRoot);
        m_CacheRoot = null;

        m_BakedRoot = new GameObject(string.Format("Baked Root ({0})", name));
        m_BakedRoot.transform.parent = transform;
		m_BakedRoot.transform.localPosition = Vector3.zero;
		m_BakedRoot.transform.localRotation = Quaternion.identity;
		m_BakedRoot.transform.localScale = Vector3.one;

        foreach(var bo in bakedOutputs) {
            bo.transform.SetParent(m_BakedRoot.transform);
		    bo.transform.localPosition = Vector3.zero;
		    bo.transform.localRotation = Quaternion.identity;
		    bo.transform.localScale = Vector3.one;
        }

        if(bakedLodFactor < 1f) {
            var lg = m_BakedRoot.AddComponent<LODGroup>();
            lg.fadeMode = LODFadeMode.CrossFade;
            lg.animateCrossFading = false;

            var lod = new LOD(bakedLodFactor, m_BakedRoot.GetComponentsInChildren<MeshRenderer>());
            lod.fadeTransitionWidth = 0.1f;

            lg.SetLODs(new[] { lod });
        }
    }

    public void ForceClearCache(bool updateScatter = false) {
		Object.DestroyImmediate(m_GeneratedRoot);
		Object.DestroyImmediate(m_CacheRoot);
		m_GeneratedRoot = null;
		m_CacheRoot = null;

        if(updateScatter)
            (this as TE3ScatterMgr.ITE3ScatterModificationReceiver).ScatterAreaModified(null);
    }

	void Reset() {
		ringAreas = new List<RingArea>();
		rectAreas = new List<RectArea>();
        splineAreas = new List<SplineArea>();
        bakedLodFactor = 0.3f;
		randomSeed = Random.Range(int.MinValue, int.MaxValue);	 
	}

    string TE3GUIDMgr.ITE3UniqueInstance.GetGUID() {
        return m_GUID;
    }
      
    void TE3GUIDMgr.ITE3UniqueInstance.SetGUID(string newGUID) {
        m_GUID = newGUID;
    }

    void TE3GUIDMgr.ITE3UniqueInstance.MakeUnique(string newGUID) {
        m_GUID = newGUID;

        ForceClearCache();
        (this as TE3ScatterMgr.ITE3ScatterModificationReceiver).ScatterAreaModified(null);
    }

	void TE3ScatterMgr.ITE3ScatterModificationReceiver.ZoneAreaModified(Component area) { }
	void TE3ScatterMgr.ITE3ScatterModificationReceiver.ZoneTargetModified(Component target) { }
	void TE3ScatterMgr.ITE3ScatterModificationReceiver.ScatterAreaModified(Component area) {    
        Object.DestroyImmediate(m_BakedRoot);
        m_BakedRoot = null;

try {
        var materialProvider = TE3TerrainBridge.Instance.GetMaterialProviderAt(transform.position.x, transform.position.z, 2f /*should be radius of all areas, probably doens't matter much*/);

        if(!m_GeneratedRoot) {
		    m_GeneratedRoot = new GameObject(string.Format("Generated Root ({0})", name));
		    m_GeneratedRoot.transform.parent = transform;
		    m_GeneratedRoot.transform.localPosition = Vector3.zero;
		    m_GeneratedRoot.transform.localRotation = Quaternion.identity;
		    m_GeneratedRoot.transform.localScale = Vector3.one;
        }

        if(!m_CacheRoot) {
            m_CacheRoot = new GameObject(string.Format("Cache Root ({0})", name));
            m_CacheRoot.SetActive(false);
            m_CacheRoot.transform.parent = transform;
		    m_CacheRoot.transform.localPosition = Vector3.zero;
		    m_CacheRoot.transform.localRotation = Quaternion.identity;
		    m_CacheRoot.transform.localScale = Vector3.one;
            m_CacheRoot.hideFlags = HideFlags.DontSaveInBuild;
        }

        foreach(Transform t in m_GeneratedRoot.transform)
            m_WorkingCache.Add(t);
        
        //Debug.LogFormat("Area {0} at {1}", this.name, transform.position.ToString("G5"));

		Random.InitState(randomSeed);

        var scatterAreas = CollectAllScatterAreas();

        foreach(var scatterArea in scatterAreas) {
			// Grab a consistent seed per area
			var nextSeed = Random.Range(int.MinValue, int.MaxValue);
            //Debug.LogFormat("Next Area Seed: {0}", nextSeed);

			// Grab a consistent seed per template
			var templateSeeds = new int[scatterArea.templates.Count];
			for(var i = 0; i < templateSeeds.Length; ++i)
                if(!scatterArea.templates[i].mute)
				    templateSeeds[i] = Random.Range(int.MinValue, int.MaxValue);

			//if(ra.density < 0 || ra.density > 100)
			//	ra.density = 100;

			//var totalProbability = 0f;
			//foreach(var t in ra.templates)
			//	totalProbability += t.probability;

			// Run two passes:
			//	- first deal with smaller / non-physical templates
			//	- then try to place templates that can't intersect too much
			for(int ip = 0; ip < 2; ++ip) {
				var physicalPass = ip == 1;
				for(int it = 0, nt = scatterArea.templates.Count; it < nt; ++it) {
					var template = scatterArea.templates[it];

                    if(template.mute)
                        continue;

                    // Pass decision based on collider or not in template asset
					if(template.asset.GetComponent<Collider>() == physicalPass)
						continue;

					// Set per template seed
					Random.InitState(templateSeeds[it]);
                    //Debug.LogFormat("Template {1} Seed: {0}", nextSeed, template.asset.name);

                    for(int id = 0, nextInstanceSeed = 0; id < scatterArea.density; ++id) {
                        // Need this in case we bail out before reaching the end of this scope
                        if(id > 0)
                            Random.InitState(nextInstanceSeed);
                        nextInstanceSeed = Random.Range(int.MinValue, int.MaxValue);
                        //Debug.LogFormat("Instance {1} Next Seed: {0}", nextInstanceSeed, id);

						// Check against probability
						if(template.probability < Random.value)
							continue;

                        // Get spawn pos candidate
                        var pnt = scatterArea.GetSpawnPositionCandidate(transform);

                        // Normal candidate
                        var nrm = Vector3.up;

						// Sweep for space (but use ray(s) because overlapping is slow)
						//var colliders = Physics.OverlapSphere(pnt, ra.clearance);
						//if(colliders.Length > 0)
						//	continue;

                        //Debug.Log("Raycasting!");
						// Adjust ground height by raycast
						var rhis = Physics.RaycastAll(pnt + Vector3.up * scatterArea.rayStartAbove, Vector3.down, scatterArea.rayDownwardLength);
                        if(rhis.Length > 0) {
                            var minDist = float.MaxValue;
                            foreach(var rhi in rhis) {
                                // Only care about closest hit
                                if(rhi.distance > minDist)
                                    continue;

    							// Bail if hit was too steep
	    						if(rhi.normal.y < Mathf.Sin(Mathf.Deg2Rad * template.slopeLimitAngle))
		    						continue;

                                // Ignore collisions of cached artifacts not yet placed
                                var rhiT = rhi.collider.transform;
                                var rhiPT = rhiT.parent;

                                if(m_WorkingCache.Contains(rhiT) || m_WorkingCache.Contains(rhiPT))
                                    continue;

						        // Check that our template grows on ground material
                                if(template.useGrowsOnPhysics && !(rhi.collider is TerrainCollider)) { // assume terrain collider
                                    var hitPhysMat = rhi.collider.sharedMaterial;
						            if(/*hitPhysMat != null &&*/ !template.growsOnPhysics.Contains(hitPhysMat))
						    	        continue;
                                }

                                // Check that our template grows on ground material
                                if(template.useGrowsOnTexture && materialProvider != null) {
                                    if(rhi.collider is TerrainCollider) { // assume terrain collider
                                        var mi = materialProvider.GetMaterialAt(pnt.x, pnt.z);
    						            if(!template.growsOnTexture.Contains(mi.albedo))
	    					    	        continue;
                                    }
                                }

                                // Valid hit
                                minDist = rhi.distance;
							    pnt.y = rhi.point.y;
                                nrm = rhi.normal;
                            }
						} else {
							continue;
						}

						// Get actual spawner (might be going through random proxy, needed for rotation preserving)
						var randomSpawner = template.asset.GetComponent<TE3RandomSpawner>();
						var desiredAsset = randomSpawner ? randomSpawner.GetRandomTemplate() : template.asset;

						// Get spawn rotation (bend outward * random yaw * template rotation)
                        var rot = scatterArea.GetSpawnRotation(transform, template, desiredAsset, nrm);

                        // Offset along 'up' (either world up or oriented up)
                        pnt += scatterArea.GetSpawnPositionOffset(transform, template, nrm);

						// Get spawn scale
                        var scale = scatterArea.GetSpawnScale(transform, template, desiredAsset);

						// Finally spawn (or recycle) an instance
						var spawnedTransform = m_WorkingCache.FirstOrDefault(t => PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) == desiredAsset);
                        if(spawnedTransform) {
                            //Debug.Log("Working cache hit: " + spawnedTransform.name);
                            m_WorkingCache.Remove(spawnedTransform);
                        } else { 
                            foreach(Transform t in m_CacheRoot.transform) { 
						        if(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) != desiredAsset)
                                    continue;

                                spawnedTransform = t;
        						spawnedTransform.SetParent(m_GeneratedRoot.transform);
                                //Debug.Log("Root cache hit: " + spawnedTransform.name);
                                break;
                            }
                        }

						if(!spawnedTransform) {
							var spawnedGO = PrefabUtility.InstantiatePrefab(desiredAsset, m_GeneratedRoot.scene) as GameObject;
                            var spawnedGOParent = spawnedGO.transform.parent;

							spawnedTransform = spawnedGO.transform;
                            spawnedTransform.gameObject.AddComponent<TE3ScatterProxy>();
							spawnedTransform.name = desiredAsset.name;
						    spawnedTransform.SetParent(m_GeneratedRoot.transform);

                            if(randomSpawner && desiredAsset.transform.parent == template.asset.transform && spawnedGOParent)
                                DestroyImmediate(spawnedGOParent.gameObject);

                            //Debug.Log("New instance spawned: " + spawnedTransform.name);
						}

                        var proxy = spawnedTransform.GetComponent<TE3ScatterProxy>();
						proxy.sourceAsset = desiredAsset;
                        proxy.bakeMesh = template.bakeMesh;
                        proxy.wrapEnvironment = template.wrapEnvironment;
                        proxy.simulatesPhysics = template.simulatePhysics;
                        proxy.inflateConvexHull = template.inflateConvexHull;
						spawnedTransform.position = pnt;
						spawnedTransform.rotation = rot;
						spawnedTransform.localScale = scale;
					}//density
				}//templates
			}//passes

			Random.InitState(nextSeed);
		}//ringAreas
} finally {
        foreach(var t in m_WorkingCache)
            t.SetParent(m_CacheRoot.transform);
        //Debug.LogFormat("{0} items moved to cache.", m_WorkingCache.Count);
        m_WorkingCache.Clear();
}
	}

    public List<ScatterArea> CollectAllScatterAreas() {
        var scatterAreas = new List<ScatterArea>(ringAreas.Count + rectAreas.Count + splineAreas.Count);

		foreach(var a in ringAreas)
            scatterAreas.Add(a);
        foreach(var a in rectAreas)
            scatterAreas.Add(a);
		foreach(var a in splineAreas)
            scatterAreas.Add(a);

        return scatterAreas;
    }

	void OnEnable() {
        TE3GUIDMgr.Register(this);
		TE3ScatterMgr.Instance.RegisterScatterArea(this);
	}

	void OnDisable() {
		TE3ScatterMgr.Instance.UnregisterScatterArea(this);
        TE3GUIDMgr.Unregister(this);
	}

	void OnDrawGizmosSelected() {
		Handles.matrix = Matrix4x4.Translate(Vector3.up * 0.05f);
		Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

		foreach(var scatterArea in CollectAllScatterAreas())
            scatterArea.DrawDebugGizmos(transform);
	}
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios

#endif //UNITY_EDITOR
