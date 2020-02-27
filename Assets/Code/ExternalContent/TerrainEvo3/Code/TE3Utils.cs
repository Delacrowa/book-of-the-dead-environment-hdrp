/* TerrainEvo3 - Unity environment extension tools: http://www.shaggydog.se/terrainevo3
 * Copyright (c) 2014 - 2017, Shaggy Dog Studios. All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of Shaggy Dog Studios nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using UnityEngine;

namespace ShaggyDogStudios {
    namespace TerrainEvo3 {

static public class TE3Utils {
    static public int FindLODGroupIndex(Renderer renderer) {
        var lodGroup = renderer ? renderer.GetComponentInParent<LODGroup>() : null;
        if(lodGroup) {
            var lods = lodGroup.GetLODs();
            for(var i = 0; i < lods.Length; ++i)
                foreach(var r in lods[i].renderers)
                    if(r == renderer)
                        return i;
        }

        return -1;
    }

    static public Bounds TransformAABB(Matrix4x4 xform, Bounds localBounds) {
        var localSize = localBounds.extents;
        var worldCenter = xform.MultiplyPoint(localBounds.center);
        var worldExtents =
            Abs((Vector3)xform.GetRow(0) * localSize.x) +
            Abs((Vector3)xform.GetRow(1) * localSize.y) +
            Abs((Vector3)xform.GetRow(2) * localSize.z);

        return new Bounds(worldCenter, worldExtents);
    }

    static public Vector3 Abs(Vector3 v) {
        v.x = Mathf.Abs(v.x);
        v.y = Mathf.Abs(v.y);
        v.z = Mathf.Abs(v.z);
        return v;
    }

    static public Vector3 Vec3Random() {
        return new Vector3(Random.value, Random.value, Random.value);
    }

    static public Vector3 Vec3Lerp(Vector3 v0, Vector3 v1, Vector3 a) {
        return new Vector3(Mathf.Lerp(v0.x, v1.x, a.x), Mathf.Lerp(v0.y, v1.y, a.y), Mathf.Lerp(v0.z, v1.z, a.z));
    }

    static public float Vec3Max(Vector3 v0) {
        return Mathf.Max(v0.x, Mathf.Max(v0.y, v0.z));
    }

    static public float Vec3Min(Vector3 v0) {
        return Mathf.Min(v0.x, Mathf.Min(v0.y, v0.z));
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    static public void SetDirty(Object o) {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(o);
#endif
    }
}

    }//ns TerrainEvo3
}//ns ShaggyDogStudios
