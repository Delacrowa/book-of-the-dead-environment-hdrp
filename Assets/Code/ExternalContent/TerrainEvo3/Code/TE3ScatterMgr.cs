/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

// This is editor only code, but behaviours interact with it so we keep it outside any 'Editor' folder.
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

[ExecuteInEditMode]
public class TE3ScatterMgr : TE3SceneModificationMgr.ITE3SceneModificationReceiver {
	public interface ITE3ScatterModificationReceiver {
		void ZoneAreaModified(Component area);
		void ZoneTargetModified(Component target);
		void ScatterAreaModified(Component area);
	}

    static TE3ScatterMgr ms_Instance;

    static public TE3ScatterMgr Instance { get {
        if(ms_Instance == null) {
            ms_Instance = new TE3ScatterMgr();
		    TE3SceneModificationMgr.RegisterModificationReceiver(ms_Instance);
        }

        return ms_Instance;
    }}

	//List<ITE3ScatterModificationReceiver>	    m_ZoneAreas     = new List<ITE3ScatterModificationReceiver>();
	//List<ITE3ScatterModificationReceiver>	    m_ZoneTargets   = new List<ITE3ScatterModificationReceiver>();
	List<TE3ScatterArea>    m_ScatterAreas  = new List<TE3ScatterArea>();

	public UndoPropertyModification ProcessModification(UndoPropertyModification modification) {
		var targetComponent = modification.currentValue.target as Component;
		//var isZoneAreaModification = typeof(TE3ZoneArea).IsAssignableFrom(targetComponent.GetType()) || targetComponent.GetComponent<TE3ZoneArea>() || targetComponent.GetComponent<TE3VolumeZoneArea>();
		//var isZoneTargetModification = typeof(TE3ZoneTarget).IsAssignableFrom(targetComponent.GetType()) || targetComponent.GetComponent<TE3ZoneTarget>() || targetComponent.GetComponent<TE3VolumeZoneTarget>();
		var isScatterAreaModification = typeof(TE3ScatterArea).IsAssignableFrom(targetComponent.GetType()) || targetComponent.GetComponent<TE3ScatterArea>();
		//Debug.Log("isZoneAreaModification: " + isZoneAreaModification);
		//Debug.Log("isZoneTargetModification: " + isZoneTargetModification);
		//Debug.Log("isScatterAreaModification: " + isScatterAreaModification);

		//if(isZoneAreaModification) {
		//	foreach(var za in m_ZoneAreas)
		//		za.ZoneAreaModified(targetComponent);
		//	foreach(var zt in m_ZoneTargets)
		//		zt.ZoneAreaModified(targetComponent);
		//}

		//if(isZoneTargetModification) {
		//	foreach(var za in m_ZoneAreas)
		//		za.ZoneTargetModified(targetComponent);
		//	foreach(var zt in m_ZoneTargets)
		//		zt.ZoneTargetModified(targetComponent);
		//}

		if(isScatterAreaModification) {
			foreach(var sa in m_ScatterAreas)
                if(sa == targetComponent || sa.transform == targetComponent)
				    (sa as ITE3ScatterModificationReceiver).ScatterAreaModified(targetComponent);
		}

		return modification;
	}

	//public void RegisterZoneArea(TE3ZoneArea area) {
	//	m_ZoneAreas.Add(area);

	//	TE3SceneModificationMgr.RegisterModificationTarget(this, area);
	//	TE3SceneModificationMgr.RegisterModificationTarget(this, area.transform);
	//}

	//public void UnregisterZoneArea(TE3ZoneArea area) {
	//	m_ZoneAreas.Remove(area);

	//	TE3SceneModificationMgr.UnregisterModificationTarget(this, area);
	//	TE3SceneModificationMgr.UnregisterModificationTarget(this, area.transform);
	//}

	//public void RegisterZoneTarget(TE3ZoneTarget target) {
	//	m_ZoneTargets.Add(target);

	//	TE3SceneModificationMgr.RegisterModificationTarget(this, target);
	//	TE3SceneModificationMgr.RegisterModificationTarget(this, target.transform);
	//}

	//public void UnregisterZoneTarget(TE3ZoneTarget target) {
	//	m_ZoneTargets.Remove(target);

	//	TE3SceneModificationMgr.UnregisterModificationTarget(this, target);
	//	TE3SceneModificationMgr.UnregisterModificationTarget(this, target.transform);
	//}

	public void RegisterScatterArea(TE3ScatterArea area) {
		m_ScatterAreas.Add(area);

		TE3SceneModificationMgr.RegisterModificationTarget(this, area);
		TE3SceneModificationMgr.RegisterModificationTarget(this, area.transform);
	}

	public void UnregisterScatterArea(TE3ScatterArea area) {
		m_ScatterAreas.Remove(area);

		TE3SceneModificationMgr.UnregisterModificationTarget(this, area);
		TE3SceneModificationMgr.UnregisterModificationTarget(this, area.transform);
	}

}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios

#endif //UNITY_EDITOR
