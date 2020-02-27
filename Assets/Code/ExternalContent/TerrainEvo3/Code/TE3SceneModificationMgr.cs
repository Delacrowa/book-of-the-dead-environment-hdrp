/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

// This is editor only code, but behaviours need to interact with it so we keep it outside an 'Editor' folder.
#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

[InitializeOnLoad]
public class TE3SceneModificationMgr : UnityEditor.AssetModificationProcessor {
	static Dictionary<ITE3SceneModificationReceiver, List<Component>> ms_ModificationReceiverTargets;
	static Dictionary<Component, List<ITE3SceneModificationReceiver>> ms_TargetToReceiverMap;
    static event ScenesSaved ms_ScenesSavedDelegates;

	public interface ITE3SceneModificationReceiver {
		UndoPropertyModification ProcessModification(UndoPropertyModification modification);
	}

    public delegate void ScenesSaved();

	static TE3SceneModificationMgr() {
		Undo.postprocessModifications -= PostprocessModifications;
        Undo.postprocessModifications += PostprocessModifications;

		ms_ModificationReceiverTargets = new Dictionary<ITE3SceneModificationReceiver, List<Component>>();
		ms_TargetToReceiverMap = new Dictionary<Component, List<ITE3SceneModificationReceiver>>();
	}

	static UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications) {
        if(modifications == null)
            return modifications;

		foreach(var mod in modifications) {
            if(mod.currentValue == null)
                continue;

			var modTarget = mod.currentValue.target as Component;
			if(!modTarget)
				continue;

			List<ITE3SceneModificationReceiver> receivers;
			if(ms_TargetToReceiverMap.TryGetValue(modTarget, out receivers)) {
				foreach(var receiver in receivers) {
                    var o = receiver as Object;
                    if(o == null || o)
    				    receiver.ProcessModification(mod); // TODO possibly remove mod?
                }
			}
		}
		return modifications;
	}

    static string[] OnWillSaveAssets(string[] paths) {
        EditorApplication.delayCall += () => {
            if(ms_ScenesSavedDelegates != null)
                ms_ScenesSavedDelegates.Invoke();
        };

        return paths;
    }

    static public void RegisterModificationReceiver(ITE3SceneModificationReceiver receiver) {
		Debug.Assert(!ms_ModificationReceiverTargets.ContainsKey(receiver));

		ms_ModificationReceiverTargets.Add(receiver, new List<Component>());
		foreach(var receivers in ms_TargetToReceiverMap.Values)
			receivers.Remove(receiver);
	}

	static public void UnregisterModificationReceiver(ITE3SceneModificationReceiver receiver) {
		Debug.Assert(ms_ModificationReceiverTargets.ContainsKey(receiver));

		ms_ModificationReceiverTargets.Remove(receiver);
	}

    static public void RegisterModificationTargetTransformHierarchy(ITE3SceneModificationReceiver receiver, Transform target) {
        for(var xform = target; xform; xform = xform.parent)
            RegisterModificationTarget(receiver, xform);
    }

    static public void RegisterModificationTarget(ITE3SceneModificationReceiver receiver, Component target) {
		Debug.Assert(ms_ModificationReceiverTargets.ContainsKey(receiver));

		ms_ModificationReceiverTargets[receiver].Add(target);

		List<ITE3SceneModificationReceiver> receivers;
		if(!ms_TargetToReceiverMap.TryGetValue(target, out receivers))
			receivers = ms_TargetToReceiverMap[target] = new List<ITE3SceneModificationReceiver>();
		receivers.Add(receiver);
	}

	static public void UnregisterModificationTarget(ITE3SceneModificationReceiver receiver, Component target) {
		Debug.Assert(ms_ModificationReceiverTargets.ContainsKey(receiver));
		Debug.Assert(ms_TargetToReceiverMap.ContainsKey(target));

		ms_ModificationReceiverTargets[receiver].Remove(target);
		var receivers = ms_TargetToReceiverMap[target];
		receivers.Remove(receiver);
		if(receivers.Count == 0)
			ms_TargetToReceiverMap.Remove(target);
	}

    static public void RegisterScenesSavedDelegate(ScenesSaved cb) {
		ms_ScenesSavedDelegates += cb;
	}

	static public void UnregisterScenesSavedDelegate(ScenesSaved cb) {
		ms_ScenesSavedDelegates -= cb;
	}
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios

#endif //UNITY_EDITOR
