
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Array = System.Array;

namespace AxelF {
namespace Editor {

public static class Factory {
    public delegate void Initializer<X,Y>(X x, Y y);

    public static void Create<X,Y>(Initializer<X,Y> init)
            where X : ScriptableObject
            where Y : Object {
        var p = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(p))
            p = ImportSettings.instance.root;
        if (!Directory.Exists(p)) {
            p = Path.GetDirectoryName(p);
            if (!Directory.Exists(p))
                p = "Assets";
        }
        var l = new List<Y>();
        foreach (var i in Selection.objects)
            if (i is Y && (AssetDatabase.IsMainAsset(i) || AssetDatabase.IsSubAsset(i)))
                l.Add((Y) i);
        l.Sort((a, b) => string.Compare(a.name, b.name));
        var r = "";
        foreach (var i in l)
            if (r == "")
                r = i.name;
            else if (i.name.IndexOf(r) != 0) {
                for (int n = r.Length - 1; n > 0; --n) {
                    var s = r.Substring(0, n);
                    if (i.name.IndexOf(s) == 0) {
                        r = s;
                        break;
                    }
                }
            }
        if (r != "" &&
                (r[r.Length - 1] >= '0' && r[r.Length - 1] <= '9' ||
                    r[r.Length - 1] == '_' || r[r.Length - 1] == '-')) {
            if (r.Contains("_"))
                r = r.Substring(0, r.LastIndexOf("_"));
            else if (r.Contains("-"))
                r = r.Substring(0, r.LastIndexOf("-"));
        }
        p = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(p, (r != "" ? r : string.Format("New {0}.asset", typeof(X).Name))));
        p = EditorUtility.SaveFilePanel(
            string.Format("Save {0} Asset", typeof(X).Name),
            Path.GetDirectoryName(p), Path.GetFileName(p), "asset");
        if (!string.IsNullOrEmpty(p)) {
            p = p.Substring(p.IndexOf("Assets"));
            var x = ScriptableObject.CreateInstance<X>();
            foreach (var i in l)
                init(x, i);
            AssetDatabase.CreateAsset(x, p);
            EditorGUIUtility.PingObject(x);
        }
    }

    [MenuItem("Assets/Create/AxelF Patch")]
    static void CreateAudioProgram() {
        Create<Patch, AudioClip>((a, c) => {
            if (a.program == null)
                a.program = new AudioProgram();
            Array.Resize(ref a.program.clips, a.program.clips != null ? a.program.clips.Length + 1 : 1);
            a.program.clips[a.program.clips.Length - 1] = new AudioProgram.AudioClipParams {
                clip = c
            };
        });
    }
}

} // Editor
} // AxelF

