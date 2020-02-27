/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

[CustomEditor(typeof(TE3ScatterArea))]
public class TE3ScatterAreaEd : Editor {
    const int kIterationCount = 50 * 15;

    SerializedProperty m_spRingAreas;
    SerializedProperty m_spRectAreas;
    SerializedProperty m_spSplineAreas;
    SerializedProperty m_spBakedLodFactor;
    SerializedProperty m_spRandomSeed;

    void OnEnable() {
        m_spRingAreas = serializedObject.FindProperty("ringAreas");
        m_spRectAreas = serializedObject.FindProperty("rectAreas");
        m_spSplineAreas = serializedObject.FindProperty("splineAreas");
        m_spBakedLodFactor = serializedObject.FindProperty("bakedLodFactor");
        m_spRandomSeed = serializedObject.FindProperty("randomSeed");
    }

	public override void OnInspectorGUI() {
        base.OnInspectorGUI();

		GUI.enabled = m_iterationsLeft <= 0;

        var t = target as TE3ScatterArea;

        EditorGUILayout.PropertyField(m_spRingAreas, true);
        if(GUILayout.Button("Add RingArea")) {
            Undo.RecordObject(t, "Add RingArea");
            var a = new TE3ScatterArea.RingArea();
            a.SetDefaults();
            t.ringAreas.Add(a);
            serializedObject.Update();
        }

		EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_spRectAreas, true);
        if(GUILayout.Button("Add RectArea")) {
            Undo.RecordObject(t, "Add RectArea");
            var a = new TE3ScatterArea.RectArea();
            a.SetDefaults();
            t.rectAreas.Add(a);
            serializedObject.Update();
        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_spSplineAreas, true);
        if(GUILayout.Button("Add SplineArea")) {
            Undo.RecordObject(t, "Add SplineArea");
            var a = new TE3ScatterArea.SplineArea();
            a.SetDefaults();
            t.splineAreas.Add(a);
            serializedObject.Update();
        }

        if(GUI.changed) {
            var hadNew = false;
            foreach(var sa in t.CollectAllScatterAreas()) {
                foreach(var tmpl in sa.templates) {
                    if(!tmpl.initialized) { 
                        tmpl.SetDefaults();
                        hadNew = true;
                    }
                }
            }

            if(hadNew)
                serializedObject.Update();
        }

		EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_spBakedLodFactor);
        EditorGUILayout.PropertyField(m_spRandomSeed);

		EditorGUILayout.Space();

        GUI.enabled = true;

		if(m_iterationsLeft <= 0) {
		    if(GUILayout.Button("Simulate"))
                if(t.GeneratedRoot)
    			    SetupSimulate(new[] { t.GeneratedRoot });

            GUI.color = Color.yellow;

		    if(GUILayout.Button("Simulate All"))
			    SetupSimulate(FindAllScatterAreas().Where(sa => sa.GeneratedRoot).Select(sa => sa.GeneratedRoot).ToArray());

            GUI.color = Color.white;
        } else {
            GUI.color = Color.red;

		    if(GUILayout.Button("Stop Simulating"))
                m_iterationsLeft = 0;

            GUI.color = Color.white;
        }

		GUI.enabled = m_iterationsLeft <= 0;

        EditorGUILayout.Space();

		if(GUILayout.Button("Bake"))
            if(t.GeneratedRoot)
                Bake(t, t.GeneratedRoot);

        GUI.color = Color.yellow;

		if(GUILayout.Button("Bake All"))
            foreach(var sa in FindAllScatterAreas())
                if(sa.GeneratedRoot)
                    Bake(sa, sa.GeneratedRoot);

        GUI.color = Color.white;

		EditorGUILayout.Space();

		if(GUILayout.Button("Force Clear Cache"))
            t.ForceClearCache(true);

        GUI.color = Color.yellow;

		if(GUILayout.Button("Force Clear Cache All"))
            foreach(var sa in FindAllScatterAreas())
                sa.ForceClearCache(true);

        GUI.color = Color.white;
	}

	int				m_undoGroup;
	int				m_iterationsLeft;
	List<Collider>	m_addedColliders;
	List<Rigidbody>	m_addedBodies;

    static TE3ScatterArea[] FindAllScatterAreas() {
        return GameObject.FindObjectsOfType<TE3ScatterArea>();
    }

	void SetupSimulate(GameObject[] roots) {
		m_undoGroup = Undo.GetCurrentGroup();
		Undo.SetCurrentGroupName("TE3 Scatter Simulate");

		m_addedColliders = new List<Collider>();
		m_addedBodies = new List<Rigidbody>();

        foreach(var root in roots) {
		    foreach(Transform child in root.transform) {
			    Undo.RecordObject(child, string.Empty);

                var proxy = child.GetComponent<TE3ScatterProxy>();
                if(!proxy.simulatesPhysics)
                    continue;

			    var collider = child.GetComponentInChildren<Collider>();
                if(!collider) {
                    var meshCollider = child.gameObject.AddComponent<MeshCollider>();
			        meshCollider.sharedMesh = child.GetComponent<MeshFilter>().sharedMesh;
                    if (proxy.inflateConvexHull) {
                        meshCollider.inflateMesh = true;
                        meshCollider.skinWidth = 0.001f;
                    }
			        meshCollider.convex = true;
			        m_addedColliders.Add(meshCollider);
                }

			    var body = child.GetComponent<Rigidbody>();
                if(!body) {
			        body = child.gameObject.AddComponent<Rigidbody>();
			        body.sleepThreshold = 0.005f * 3f;
			        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
			        m_addedBodies.Add(body);
                }

                body.AddForce(new Vector3(Random.value * 2f - 1, 10f, Random.value * 2f - 1f) * 5f, ForceMode.Force);
		    }
        }

        Physics.autoSimulation = false;

		m_iterationsLeft = kIterationCount;
		EditorApplication.update += StepSimulate;
	}

	void CleanupSimulate() {
		EditorApplication.update -= StepSimulate;

        Physics.autoSimulation = true;

		foreach(var body in m_addedBodies)
			Object.DestroyImmediate(body);

		foreach(var collider in m_addedColliders)
			Object.DestroyImmediate(collider);

		m_addedBodies.Clear();
		m_addedColliders.Clear();

		Undo.CollapseUndoOperations(m_undoGroup);

		SceneView.RepaintAll();
		Repaint();
	}

	void StepSimulate() {
		if(m_iterationsLeft-- > 0) {
			Physics.Simulate(1f / 50f);

            if(m_iterationsLeft < kIterationCount / 2) {
			    var hasActiveBody = false;
			    foreach(var body in m_addedBodies) {
			    	if(!body.IsSleeping()) {
			    		hasActiveBody = true;
			    		break;
			    	}
			    }

			    if(!hasActiveBody)
			    	m_iterationsLeft = 0;
            }

            if(EditorApplication.isCompiling)
                m_iterationsLeft = 0;
		}
		
		if(m_iterationsLeft <= 0)
			CleanupSimulate();
	}

    static void Bake(TE3ScatterArea target, GameObject root) {
        var bakedSources = new List<GameObject>();
        var bakedOutputs = new List<GameObject>();

        var mat2mesh = new Dictionary<Material, MeshCollector>();
        foreach(Transform child in root.transform) {
            var childProxy = child.GetComponent<TE3ScatterProxy>();
            if(!childProxy.bakeMesh)
                continue;

            var childMR = child.GetComponent<MeshRenderer>();
            var childMF = child.GetComponent<MeshFilter>();
            var childMat = childMR.sharedMaterial;
            MeshCollector childCol = null;
            if(!mat2mesh.TryGetValue(childMat, out childCol)) {
                childCol = new MeshCollector();
                mat2mesh[childMat] = childCol;
            }

            if(childProxy.wrapEnvironment == 0f) {
                childCol.PushMesh(childMF.sharedMesh, Matrix4x4.TRS(child.transform.localPosition, child.transform.localRotation, child.transform.localScale), -1);
            } else {
                Debug.Log("wrap: " + childProxy.wrapEnvironment);
                var vtxColStart = childCol.VertexCount;
                var vtxCount = childMF.sharedMesh.vertexCount;
                childCol.PushMesh(childMF.sharedMesh, Matrix4x4.TRS(child.transform.localPosition, child.transform.localRotation, child.transform.localScale), -1);

                var indices = childMF.sharedMesh.triangles;
                for(int i = 0, n = indices.Length; i < n; i += 3) {
                    var p0 = childCol.GetPosition(vtxColStart + i + 0);
                    var p1 = childCol.GetPosition(vtxColStart + i + 1);
                    var p2 = childCol.GetPosition(vtxColStart + i + 2);
                    var n0 = childCol.GetNormal(vtxColStart + i + 0);
                    var n1 = childCol.GetNormal(vtxColStart + i + 1);
                    var n2 = childCol.GetNormal(vtxColStart + i + 2);

                    RaycastHit rhiPC0;
                    if (!Physics.Raycast(p0 - n0 * 0.01f, -n0, out rhiPC0, 1f))
                        rhiPC0.point = p0;
                    RaycastHit rhiPC1;
                    if (!Physics.Raycast(p1 - n1 * 0.01f, -n1, out rhiPC1, 1f))
                        rhiPC1.point = p1;
                    RaycastHit rhiPC2;
                    if (!Physics.Raycast(p2 - n2 * 0.01f, -n2, out rhiPC2, 1f))
                        rhiPC2.point = p2;

                    childCol.SetPosition(vtxColStart + i + 0, rhiPC0.point);
                    childCol.SetPosition(vtxColStart + i + 1, rhiPC1.point);
                    childCol.SetPosition(vtxColStart + i + 2, rhiPC2.point);
                }
            }

            bakedSources.Add(child.gameObject);
        }

        foreach(var kvp in mat2mesh) {
            var go = new GameObject("Baked_" + kvp.Key.name);
            bakedOutputs.Add(go);

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = kvp.Key;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = new Mesh();
            kvp.Value.WriteMesh(mf.sharedMesh, 2);

            mf.sharedMesh.RecalculateBounds();
        }

        target.SetBakedData(bakedSources, bakedOutputs);
    }

	class MeshCollector {
		List<Vector3> m_positions = new List<Vector3>();
		List<Vector3> m_normals = new List<Vector3>();
		List<Vector4> m_tangents = new List<Vector4>();
		List<Color> m_colors = new List<Color>();
		List<Vector2> m_uv0s = new List<Vector2>();
		List<Vector2> m_uv1s = new List<Vector2>();
		List<Vector4> m_uv2s = new List<Vector4>();
		List<Vector4> m_uv3s = new List<Vector4>();
		List<int> m_indices = new List<int>();

		public int VertexCount { get { return m_positions.Count; } }

		public Vector3 GetPosition(int idx) { return m_positions[idx]; }
		public void    SetPosition(int idx, Vector3 pos) { m_positions[idx] = pos; }
		public Vector3 GetNormal(int idx) { return m_normals[idx]; }
		public void    GetNormal(int idx, Vector3 nrm) { m_normals[idx] = nrm; }

		public void PushPosition(Vector3 pos, Matrix4x4 xform) { m_positions.Add(xform.MultiplyPoint(pos)); }
		public void PushNormal(Vector3 nrm, Matrix4x4 xform) { m_normals.Add(xform.MultiplyVector(nrm)); }
		public void PushTangent(Vector4 tan, Matrix4x4 xform) {
			var t = xform.MultiplyVector((Vector3)tan);
			m_tangents.Add(new Vector4(t.x, t.y, t.z, tan.w));
		}
		public void PushColor(Color col) { m_colors.Add(col); }
		public void PushUV0(Vector2 uv) { m_uv0s.Add(uv); }
		public void PushUV1(Vector2 uv) { m_uv1s.Add(uv); }
		public void PushUV2(Vector4 uv) { m_uv2s.Add(uv); }
		public void PushUV3(Vector4 uv) { m_uv3s.Add(uv); }
		public void PushIndex(int idx, int baseVertex) {
            baseVertex = baseVertex >= 0 ? baseVertex : VertexCount;
            m_indices.Add(idx + baseVertex);
        }

		public void PushPositions(Vector3[] pos, int start, int count, Matrix4x4 xform) {
			if(pos == null || pos.Length == 0) return;
			if(count == 0) count = pos.Length;
			for(int i = start, end = start + count; i < end; ++i)
				m_positions.Add(xform.MultiplyPoint(pos[i]));
		}
		public void PushNormals(Vector3[] nrm, int start, int count, Matrix4x4 xform) {
			if(nrm == null || nrm.Length == 0) return;
			if(count == 0) count = nrm.Length;
			for(int i = start, end = start + count; i < end; ++i)
				m_normals.Add(xform.MultiplyVector(nrm[i]));
		}
		public void PushTangents(Vector4[] tan, int start, int count, Matrix4x4 xform) {
			if(tan == null || tan.Length == 0) return;
			if(count == 0) count = tan.Length;
			for(int i = start, end = start + count; i < end; ++i)
				PushTangent(tan[i], xform);
		}
		public void PushColors(Color[] col, int start, int count) {
			if(col == null || col.Length == 0) return;
			if(count == 0) count = col.Length;
			for(int i = start, end = start + count; i < end; ++i)
				m_colors.Add(col[i]);
		}
		public void PushUV0s(Vector2[] uv, int start, int count) {
			if(uv == null || uv.Length == 0) return;
			if(count == 0) count = uv.Length;
			for(int i = start, end = start + count; i < end; ++i)
				m_uv0s.Add(uv[i]);
		}
		public void PushUV1s(Vector2[] uv, int start, int count) {
			if(uv == null || uv.Length == 0) return;
			if(count == 0) count = uv.Length;
			for(int i = start, end = start + count; i < end; ++i)
				m_uv1s.Add(uv[i]);
		}
		public void PushUV2s(Vector4[] uv, int start, int count) {
			if(uv == null || uv.Length == 0) return;
			if(count == 0) count = uv.Length;
			for(int i = start, end = start + count; i < end; ++i)
				m_uv2s.Add(uv[i]);
		}
		public void PushUV3s(Vector4[] uv, int start, int count) {
			if(uv == null || uv.Length == 0) return;
			if(count == 0) count = uv.Length;
			for(int i = start, end = start + count; i < end; ++i)
				m_uv3s.Add(uv[i]);
		}
		public void PushIndices(int[] indices, int start, int count, int baseVertex) {
            baseVertex = baseVertex >= 0 ? baseVertex : VertexCount;
			if(indices == null || indices.Length == 0) return;
			if(count == 0) count = indices.Length;
			for(int i = start, end = start + count; i < end; ++i)
				m_indices.Add(indices[i] + baseVertex);
		}

		public void PushMesh(Mesh m, Matrix4x4 xform, int baseVertex) {
            baseVertex = baseVertex >= 0 ? baseVertex : VertexCount;
			PushPositions(m.vertices, 0, 0, xform);
			PushNormals(m.normals, 0, 0, xform);
			PushTangents(m.tangents, 0, 0, xform);
			PushColors(m.colors, 0, 0);
			PushUV0s(m.uv, 0, 0);
			PushUV1s(m.uv2, 0, 0);
			PushIndices(m.triangles, 0, 0, baseVertex);
		}

		public int PushSubMesh(Mesh m, int subMesh, Matrix4x4 xform, int baseVertex) {
            baseVertex = baseVertex >= 0 ? baseVertex : VertexCount;
			var vertices = m.vertices;
			var subIndices = m.GetTriangles(subMesh);
			int minVtx = int.MaxValue, maxVtx = int.MinValue;
			for(int i = 0, n = subIndices.Length; i < n; ++i) {
				var idx = subIndices[i];
				if(idx < minVtx)
					minVtx = idx;
				if(idx > maxVtx)
					maxVtx = idx;
			}
			var vtxCount = maxVtx - minVtx + 1;
			 
			PushPositions(vertices, minVtx, vtxCount, xform);
			PushNormals(m.normals, minVtx, vtxCount, xform);
			PushTangents(m.tangents, minVtx, vtxCount, xform);
			PushColors(m.colors, minVtx, vtxCount);
			PushUV0s(m.uv, minVtx, vtxCount);
			PushUV1s(m.uv2, minVtx, vtxCount);
			PushIndices(subIndices, 0, 0, baseVertex - minVtx);

			return vtxCount;
		}

		public void Append(MeshCollector o) {
			var baseVertex = VertexCount;

			for(int i = 0; i < o.VertexCount; ++i) {
				PushPosition(o.m_positions[i], Matrix4x4.identity);
				if(o.m_normals.Count > i)
					PushNormal(o.m_normals[i], Matrix4x4.identity);
				if(o.m_tangents.Count > i)
					PushTangent(o.m_tangents[i], Matrix4x4.identity);
				if(o.m_colors.Count > i)
					PushColor(o.m_colors[i]);
				if(o.m_uv0s.Count > i)
					PushUV0(o.m_uv0s[i]);
				if(o.m_uv1s.Count > i)
					PushUV1(o.m_uv1s[i]);
				if(o.m_uv2s.Count > i)
					PushUV2(o.m_uv2s[i]);
				if(o.m_uv3s.Count > i)
					PushUV3(o.m_uv3s[i]);
			}

			for(int i = 0; i < o.m_indices.Count; ++i)
				PushIndex(o.m_indices[i], baseVertex);
		}

		public void Transform(Matrix4x4 xform) {
			for(int i = 0, n = m_positions.Count; i < n; ++i)
				m_positions[i] = xform.MultiplyPoint(m_positions[i]);

			for(int i = 0, n = m_normals.Count; i < n; ++i)
				m_normals[i] = xform.MultiplyVector(m_normals[i]);

			for(int i = 0, n = m_tangents.Count; i < n; ++i) {
				var tan = m_tangents[i];
				var t = xform.MultiplyVector((Vector3)tan);
				m_tangents[i] = new Vector4(t.x, t.y, t.z, tan.w);
			}
		}

		public void WriteMesh(Mesh mesh, int uv3Dim = 4) {
			mesh.Clear();
			mesh.SetVertices(m_positions);
			mesh.SetNormals(m_normals);
			mesh.SetTangents(m_tangents);
			mesh.SetColors(m_colors);
			mesh.SetUVs(0, m_uv0s);
			mesh.SetUVs(1, m_uv1s);
			mesh.SetUVs(2, m_uv2s);
			if(uv3Dim == 4)
				mesh.SetUVs(3, m_uv3s);
			else if(uv3Dim == 3)
				mesh.SetUVs(3, m_uv3s.Select(uv => (Vector3)uv).ToList());
			else
				mesh.SetUVs(3, m_uv3s.Select(uv => (Vector2)uv).ToList());
			mesh.SetTriangles(m_indices, 0);
		}

		public void Reset() {
			m_positions.Clear();
			m_normals.Clear();
			m_tangents.Clear();
			m_colors.Clear();
			m_uv0s.Clear();
			m_uv1s.Clear();
			m_uv2s.Clear();
			m_uv3s.Clear();
			m_indices.Clear();
		}
	}
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios
