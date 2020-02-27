/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

// This is editor only code, but needs to hold data in the scene so we keep it outside an 'Editor' folder.
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

[SelectionBase]
[ExecuteInEditMode]
public class TE3AutoRandomSpawner : MonoBehaviour, TE3GUIDMgr.ITE3UniqueInstance {
    public Vector3  minEulerRotation        = new Vector3(-5f, -180f, -5f);
    public Vector3  maxEulerRotation        = new Vector3( 5f,  180f,  5f);
    public Vector3  minScale                = new Vector3(0.95f, 0.90f, 0.95f);
    public Vector3  maxScale                = new Vector3(1.05f, 1.10f, 1.05f);
    public bool     preserveTemplateScale   = true;
    public bool     preserveTemplateRotation= true;

	[HideInInspector]
    [SerializeField]
    string      m_GUID;

    [ReadOnlyGUI]
    [SerializeField]
    GameObject  m_SpawnedInstance;

    string TE3GUIDMgr.ITE3UniqueInstance.GetGUID() {
        return m_GUID;
    }
      
    void TE3GUIDMgr.ITE3UniqueInstance.SetGUID(string newGUID) {
        m_GUID = newGUID;
    }

    void TE3GUIDMgr.ITE3UniqueInstance.MakeUnique(string newGUID) {
        m_GUID = newGUID;

        AutoSpawn();
    }

    // Implicitly handled by uniqueness
    //void OnValidate() {
    //    // Spawn when added to scene
    //    if(m_SpawnedInstance == null && gameObject.scene.IsValid() && gameObject.scene.isLoaded)
    //        AutoSpawn();
    //}

    void OnEnable() {
        TE3GUIDMgr.Register(this);
	}

	void OnDisable() {
        TE3GUIDMgr.Unregister(this);
	}

    public void AutoSpawn(GameObject forceTemplate = null) {
        var spawner = GetComponent<TE3RandomSpawner>();
        var template = forceTemplate ? forceTemplate : spawner.GetRandomTemplate();
        var spawned = spawner.SpawnInstance(template);
        spawned.transform.SetParent(transform);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.rotation = Quaternion.Euler(TE3Utils.Vec3Lerp(minEulerRotation, maxEulerRotation, TE3Utils.Vec3Random()))
            * (preserveTemplateRotation ? template.transform.rotation : Quaternion.identity);
        spawned.transform.localScale = Vector3.Scale(
            TE3Utils.Vec3Lerp(minScale, maxScale, TE3Utils.Vec3Random()),
            preserveTemplateScale ? template.transform.lossyScale : Vector3.one
        );

        if(m_SpawnedInstance)
            Undo.DestroyObjectImmediate(m_SpawnedInstance);
        Undo.RegisterCreatedObjectUndo(spawned, "Auto Spawned");
        Undo.RecordObject(this, "Auto Spawned");

        m_SpawnedInstance = spawned;
        Debug.Log("AutoSpawn2: " + spawned);
    }
}


    }//ns TerrainEvo3
}//ns ShaggyDogStudios

#endif //UNITY_EDITOR
