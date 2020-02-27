using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

[ExecuteInEditMode]
public partial class GrassOcclusion : MonoBehaviour
{
    public static GrassOcclusion Instance { get; private set; }

    [Header("Baked Results")]
    [Tooltip("Baked results plus some realtime tweakable params")]
    public GrassOcclusionData m_Data;

    static class Uniforms
    {
        internal static readonly int _GrassOcclusion = Shader.PropertyToID("_GrassOcclusion");
        internal static readonly int _GrassOcclusionAmountTerrain = Shader.PropertyToID("_GrassOcclusionAmountTerrain");
        internal static readonly int _GrassOcclusionAmountGrass = Shader.PropertyToID("_GrassOcclusionAmountGrass");
        internal static readonly int _GrassOcclusionHeightFadeBottom = Shader.PropertyToID("_GrassOcclusionHeightFadeBottom");
        internal static readonly int _GrassOcclusionHeightFadeTop = Shader.PropertyToID("_GrassOcclusionHeightFadeTop");
        internal static readonly int _GrassOcclusionCullHeight = Shader.PropertyToID("_GrassOcclusionCullHeight");
        internal static readonly int _GrassOcclusionHeightRange = Shader.PropertyToID("_GrassOcclusionHeightRange");
        internal static readonly int _GrassOcclusionWorldToLocal = Shader.PropertyToID("_GrassOcclusionWorldToLocal");
        internal static readonly int _GrassOcclusionHeightmap = Shader.PropertyToID("_GrassOcclusionHeightmap");
    }

    [HDRPCallbackMethod]
    static void GrassOcclusionSetup()
    {
        HDRenderPipeline.OnPrepareCamera += SetupGPUData;
    }

    static public void SetupGPUData(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd)
    {
        if(Instance)
            Instance.SetShaderUniforms(cmd);
        else
            SetWhiteTexture(cmd);
    }
    
    void SetShaderUniforms(CommandBuffer cb)
    {
        if (!m_Data)
        {
            SetWhiteTexture(cb);
            return;
        }

        cb.SetGlobalMatrix(Uniforms._GrassOcclusionWorldToLocal, m_Data.worldToLocal);
        cb.SetGlobalTexture(Uniforms._GrassOcclusion, m_Data.occlusion);
        cb.SetGlobalFloat(Uniforms._GrassOcclusionAmountTerrain, m_Data.occlusionAmountTerrain);
        cb.SetGlobalFloat(Uniforms._GrassOcclusionAmountGrass, m_Data.occlusionAmountGrass);
        cb.SetGlobalFloat(Uniforms._GrassOcclusionHeightFadeBottom, Mathf.Min(m_Data.heightFadeBottom, m_Data.heightFadeTop) / m_Data.terrainHeight);
        cb.SetGlobalFloat(Uniforms._GrassOcclusionHeightFadeTop, m_Data.heightFadeTop / m_Data.terrainHeight);
        cb.SetGlobalFloat(Uniforms._GrassOcclusionCullHeight, m_Data.cullHeight / m_Data.terrainHeight);
        cb.SetGlobalFloat(Uniforms._GrassOcclusionHeightRange, m_Data.terrainHeightRange);
        cb.SetGlobalTexture(Uniforms._GrassOcclusionHeightmap, m_Data.heightmap);
    }

    void OnEnable()
    {
        if(Application.isPlaying)
            Debug.Assert(Instance == null);

        Instance = this;
    }

    void OnDisable()
    {
        if(Application.isPlaying)
            Debug.Assert(Instance == this);

        if(Instance == this)
            Instance = null;
    }

    void OnDestroy()
    {
        if(Application.isPlaying)
            Destroy(ms_White);
        else
            DestroyImmediate(ms_White);

        ms_White = null;
    }

    static Texture2D ms_White;

	static void InitWhiteTexture()
    {
        if (ms_White != null)
            return;
        
        ms_White = new Texture2D(1, 1, TextureFormat.Alpha8, false);
        ms_White.hideFlags = HideFlags.DontSave;
        ms_White.SetPixels32(new Color32[]{new Color32(255, 255, 255, 255)});
        ms_White.Apply();
    }

    static void SetWhiteTexture(CommandBuffer cb)
    {
        InitWhiteTexture();

        cb.SetGlobalTexture(Uniforms._GrassOcclusion, ms_White);
    }
}
