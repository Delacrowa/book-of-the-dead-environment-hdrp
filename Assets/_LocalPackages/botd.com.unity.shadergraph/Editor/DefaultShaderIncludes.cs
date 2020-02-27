using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    internal static class DefaultShaderIncludes
    {
        public static string GetAssetsPackagePath()
        {
//forest-begin:
			var packageDirectories = Directory.GetDirectories(Application.dataPath, "botd.com.unity.shadergraph", SearchOption.AllDirectories);
//forest-end:
			return packageDirectories.Length == 0 ? null : Path.GetFullPath(packageDirectories.First());
        }

        public static string GetRepositoryPath()
        {
            var path = GetAssetsPackagePath();
            if (path == null)
                return null;
            return Path.GetFullPath(Directory.GetParent(path).ToString());
        }

        public static string GetDebugOutputPath()
        {
            var path = GetRepositoryPath();
            if (path == null)
                return null;
            path = Path.Combine(path, "DebugOutput");
            return Directory.Exists(path) ? path : null;
        }

        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            return new[]
            {
                GetAssetsPackagePath() ?? Path.GetFullPath("Packages/com.unity.shadergraph")
            };
        }
    }
}
