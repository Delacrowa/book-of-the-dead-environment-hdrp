/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy
 * of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#if UNITY_EDITOR

using UnityEngine;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

public class TE3ScatterProxy : MonoBehaviour {
    public GameObject   sourceAsset;
    public bool         bakeMesh;
    public float        wrapEnvironment;
    public bool         simulatesPhysics;
    public bool         inflateConvexHull;
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios

#endif //UNITY_EDITOR
