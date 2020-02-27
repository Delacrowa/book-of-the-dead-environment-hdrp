
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AxelF.Editor {

public static class Tools {
    [MenuItem("AxelF/Tools/Find Patches Without AudioClips")]
    static void FindPatchWithoutAudioClips() {
        var root = "Assets/Audio";
        var guids = AssetDatabase.FindAssets("t:Object", new string[]{root});
        int count = 0;

        Debug.Log("[Find Patches Without AudioClips]: Searching for patches in " + root);

        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var patch = asset as Patch;

            if (patch) {
                bool noClips = false;
                if (patch.program.clips == null || patch.program.clips.Length == 0)
                    noClips = true;
                else
                    foreach (var clip in patch.program.clips)
                        if (!clip.clip)
                            noClips = true;
                if (noClips)
                    Debug.LogWarning("[Find Patches Without AudioClips]: Found " + path, asset);
                ++count;
            }
        }

        Debug.Log(
            "[Find Patches Without AudioClips]: All done, checked " +
            count + (count == 1 ? " patch" : " patches"));
    }

    [MenuItem("AxelF/Tools/Find Unused AudioClips")]
    static void FindUnusedAudioClips() {
        var root = "Assets/Audio";
        var guids = AssetDatabase.FindAssets("t:Object", new string[]{root});
        var clips = new HashSet<string>();
        int patchCount = 0;
        int clipCount = 0;

        Debug.Log("[Find Unused AudioClips]: Searching for patches in " + root);

        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var patch = asset as Patch;

            if (patch) {
                if (patch.program.clips != null)
                    foreach (var clip in patch.program.clips)
                        if (clip.clip)
                            clips.Add(AssetDatabase.GetAssetPath(clip.clip));
                ++patchCount;
            }
        }

        guids = AssetDatabase.FindAssets("t:AudioClip", new string[]{root});
        clipCount = guids.Length;

        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!clips.Contains(path))
                Debug.LogWarning("[Find Unused AudioClips]: Found " + path, AssetDatabase.LoadMainAssetAtPath(path));
        }

        Debug.Log(
            "[Find Unused AudioClips]: All done, checked " +
            patchCount + (patchCount == 1 ? " patch " : " patches ") + "and " +
            clipCount + (clipCount == 1 ? " AudioClip " : " AudioClips"));
    }
}

} // AxelF.Editor

