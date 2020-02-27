using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

[ExecuteInEditMode]
public class LightmapOcclusion : MonoBehaviour
{
	public Vector3 m_Luminance = new Vector3(0.3f, 0.35f, 0.4f);
	[Range(1f, 8f)]
	public float m_Scale = 3f;
	[Range(1f, 8f)]
	public float m_Power = 2f;
	[Range(0f, 1f)]
	public float m_ReflectionStrength = 1f;
	[Range(0f, 1f)]
	public float m_SpecularStrength = 1f;
	public enum LightmapOcclusionMode { Normal, Reflection }
	public LightmapOcclusionMode m_Mode = LightmapOcclusionMode.Reflection;

	public static LightmapOcclusion Instance { get; private set; }

	static class Uniforms
    {
        internal static readonly int _LightmapOcclusionLuminanceMode = Shader.PropertyToID("_LightmapOcclusionLuminanceMode");
		internal static readonly int _LightmapOcclusionScalePowerReflStrengthSpecStrength = Shader.PropertyToID("_LightmapOcclusionScalePowerReflStrengthSpecStrength");
	}

	[HDRPCallbackMethod]
	static void LightmapOcclusionSetup()
	{
		HDRenderPipeline.OnPrepareCamera += SetShaderUniforms;
	}

	static public void SetShaderUniforms(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd)
	{
		if(!Instance)
		{
			cmd.SetGlobalVector(Uniforms._LightmapOcclusionLuminanceMode, Vector4.zero);
			cmd.SetGlobalVector(Uniforms._LightmapOcclusionScalePowerReflStrengthSpecStrength, Vector4.zero);
			return;
		}

		Instance.SetShaderUniforms(cmd);
	}

	void SetShaderUniforms(CommandBuffer cmd)
	{
		var lightmapOcclusionLuminanceMode = (Vector4)m_Luminance.normalized;
		lightmapOcclusionLuminanceMode.w = (float)(int)m_Mode;
		cmd.SetGlobalVector(Uniforms._LightmapOcclusionLuminanceMode, lightmapOcclusionLuminanceMode);
		cmd.SetGlobalVector(Uniforms._LightmapOcclusionScalePowerReflStrengthSpecStrength,
		new Vector4(m_Scale, m_Power, m_ReflectionStrength, m_SpecularStrength));
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

}
