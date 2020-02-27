
#define AXELF_DEVELOPER

using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

public static class Developer {
    public static readonly string[] paths = new string[] {
        "Assets/Features/AxelF",
        "Assets/Gizmos"
    };

#if AXELF_DEVELOPER
    [MenuItem("AxelF/Export Package", false, 9000)]
    static void ExportPackage() {
        AssetDatabase.ExportPackage(paths, "AxelF.unitypackage", ExportPackageOptions.Recurse);
    }
#endif
}

} // Editor
} // AxelF

