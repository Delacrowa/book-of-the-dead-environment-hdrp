using UnityEngine;
using UnityEngine.Rendering;

static public class EnableSRPBatcher {
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void EnableBatcher() {
		GraphicsSettings.useScriptableRenderPipelineBatching = true;
	}
}
