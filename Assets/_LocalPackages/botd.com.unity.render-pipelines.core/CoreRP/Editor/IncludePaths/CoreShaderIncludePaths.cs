using System.Linq;
using UnityEngine;
using System.IO;

namespace UnityEditor.Experimental.Rendering
{
    static class CoreShaderIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
//forest-begin:
			return new[] { "Assets/_LocalPackages/botd.com.unity.render-pipelines.core" };
#if false
            var paths = new string[1];
            paths[0] = Path.GetFullPath("Packages/com.unity.render-pipelines.core");
            return paths;
#endif
//forest-end:
        }
    }
}
