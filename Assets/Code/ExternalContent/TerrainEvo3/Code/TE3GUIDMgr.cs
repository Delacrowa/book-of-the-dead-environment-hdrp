/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

// This is editor only code, but behaviours need to interact with it so we keep it outside an 'Editor' folder.
using System.Collections.Generic;
using UnityEngine;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
static public class TE3GUIDMgr {
	public interface ITE3UniqueInstance {
        string  GetGUID();
        void    SetGUID(string newGUID);

        void    MakeUnique(string newGUID);
	}

#if UNITY_EDITOR
    static Dictionary<string, ITE3UniqueInstance> ms_InstanceMap;

    static TE3GUIDMgr() {
        ms_InstanceMap = new Dictionary<string, ITE3UniqueInstance>();
    }

    static string NewGUID() {
        var guid = System.Guid.NewGuid().ToString("N");
        while(ms_InstanceMap.ContainsKey(guid))
            guid = System.Guid.NewGuid().ToString("N");
        return guid;
    }
#endif

    static public void Register(ITE3UniqueInstance instance) {
#if UNITY_EDITOR
        if(string.IsNullOrEmpty(instance.GetGUID()) || instance.GetGUID() == System.Guid.Empty.ToString("N"))
            instance.SetGUID(NewGUID());

        if(ms_InstanceMap.ContainsKey(instance.GetGUID())) {
            Debug.LogFormat(instance as Object, "Making instance {0}, unique.", instance);
            instance.MakeUnique(NewGUID());
        }

        ms_InstanceMap[instance.GetGUID()] = instance;
#endif
    }

    static public void Unregister(ITE3UniqueInstance instance) {
#if UNITY_EDITOR
        if(!ms_InstanceMap.ContainsKey(instance.GetGUID())) {
            Debug.LogErrorFormat("Trying to unregister unknown guid: {0}", instance.GetGUID());
            return;
        }

        if(ms_InstanceMap[instance.GetGUID()] != instance) {
            Debug.LogErrorFormat("Trying to unregister guid {0} associated with {1}, however guid was registered by {2}.", instance.GetGUID(), instance, ms_InstanceMap[instance.GetGUID()]);
            return;
        }

        ms_InstanceMap.Remove(instance.GetGUID());
  #endif
    }
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios
