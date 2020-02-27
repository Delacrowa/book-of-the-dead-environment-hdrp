using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class HDIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
//forest-begin:
			return new[] { "Assets/_LocalPackages/botd.com.unity.render-pipelines.high-definition" };
#if false
            var paths = new string[1];
            paths[0] = Path.GetFullPath("Packages/com.unity.render-pipelines.high-definition");
            return paths;
#endif
//forest-end:
        }
    }
}
