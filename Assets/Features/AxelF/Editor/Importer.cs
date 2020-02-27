
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

public class Importer : AssetPostprocessor {
    protected void OnPreprocessAudio() {
        int overrideIndex;

        if (!ImportSettingsEditor.overridesTable.TryGetValue(assetPath, out overrideIndex))
            return;

        var importSettings = AssetDatabase.LoadAssetAtPath<ImportSettings>(ImportSettings.path);
        var @override = importSettings.overrides[overrideIndex];

        var importer = (AudioImporter) assetImporter;

        var targets = Enum.GetNames(typeof(ImportTarget));
        var values = (int[]) Enum.GetValues(typeof(ImportTarget));

        for (int i = 0, n = targets.Length; i < n; ++i) {
            importer.ClearSampleSettingOverride(targets[i]);

            foreach (var settings in @override.settings)
                if ((int) settings.target == values[i]) {
                    var sampleSettings = new AudioImporterSampleSettings {
                        compressionFormat = settings.compressionFormat,
                        loadType = settings.loadType,
                        quality = settings.quality,
                        sampleRateOverride = 44100
                    };

                    if (settings.target == ImportTarget.Standalone)
                        importer.defaultSampleSettings = sampleSettings;
                    else
                        importer.SetOverrideSampleSettings(targets[i], sampleSettings);
                }
        }
    }
}

} // Editor
} // AxelF

