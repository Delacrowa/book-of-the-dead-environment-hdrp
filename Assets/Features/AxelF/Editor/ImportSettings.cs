
using System;
using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

public enum ImportTarget {
    Standalone,
    PS4,
    PSP2,
    XBoxOne,
    WSA,
    iOS,
    Android,
    WebGL,
}

public class ImportSettings : ScriptableObject {
    public static readonly string path = "Assets/Features/AxelF/Editor/ImportSettings.asset";

    public static ImportSettings instance {
        get {
            var s = AssetDatabase.LoadAssetAtPath<ImportSettings>(path);
            if (s == null) {
                s = ScriptableObject.CreateInstance<ImportSettings>();
                AssetDatabase.CreateAsset(s, path);
            }
            return s;
        }
    }

    [MenuItem("AxelF/Settings/Import Settings")]
    static void PingImportSettings() {
        EditorGUIUtility.PingObject(instance);
    }

    public string root = "Assets/Audio";

    [Serializable]
    public class Settings {
        public ImportTarget target = ImportTarget.Standalone;
        public AudioCompressionFormat compressionFormat = AudioCompressionFormat.ADPCM;
        public AudioClipLoadType loadType = AudioClipLoadType.CompressedInMemory;
        [Range(0f, 1f)] public float quality = 1f;
    }

    [Serializable]
    public class Override {
        public bool visible;
        public string filter;
        public Settings[] settings;
    }

    [Colorize]
    public Override[] overrides;
}

} // Editor
} // AxelF

