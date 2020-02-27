using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[HDRPCallback]
public class StaggeredCascade : MonoBehaviour {
	static public StaggeredCascade Instance { get; private set; }

	public enum Mode {
		Full0Alt12Fix3	= 0,
		Full01Alt23		= 1,
		Full012Fix3		= 2,
	}

	public Mode mode;

	public enum StaggerStage {
		Disabled,
		Updating,
		Enabled,
	}

	public StaggerStage staggerStage { get { return m_StaggerStage; } set { m_StaggerStage = value; } }

	StaggerStage				m_StaggerStage;

	Camera						m_ShadowCamera;
	CullResults					m_ShadowCullResults;
	ScriptableCullingParameters m_ShadowCullParams;

	ShaderPassName				m_ShaderPassName;
	DrawRendererSettings		m_DrawRendererSettings;
	FilterRenderersSettings		m_FilterRendererSettings;

	Matrix4x4[]					m_StaggeredViewMatrix = new Matrix4x4[4];
	Matrix4x4[]					m_StaggeredProjMatrix = new Matrix4x4[4];
	Vector3[]					m_StaggeredPosition = new Vector3[4];
	Vector3[]					m_StaggeredLightDir = new Vector3[4];
	Camera						m_PreparedCamera;

#if UNITY_EDITOR
	static bool					ms_SceneViewStagger;
#endif
	static uint					ms_RenderFrameNumber;

	[HDRPCallbackMethod]
	static void StaggeredCascadeSetup() {
#if UNITY_EDITOR
		var panel = DebugManager.instance.GetPanel("Scene View", true);
		var container = panel.children.Where(c => c.displayName == "Forest Custom").FirstOrDefault() as DebugUI.Container;
		if(container == null)
			container = new DebugUI.Container { displayName = "Forest Custom" };
		container.children.Add(new DebugUI.BoolField {
			displayName = "Stagger Last Cascade",
			getter = () => ms_SceneViewStagger,
			setter = value => ms_SceneViewStagger = value
		});
		if(!panel.children.Contains(container))
			panel.children.Add(container);
#endif
		
		ShadowAtlas.OnIsStaggered += OnIsStaggered;
		ShadowAtlas.OnShouldRenderShadows += OnShouldRenderShadows;
		ShadowAtlas.OnRenderShadows += OnRenderShadows;
		HDRenderPipeline.OnBeginCamera += OnBeginCamera;
	}

	static uint TweakSliceIdx(uint sliceIdx) {
		if(Instance && Instance.mode == Mode.Full01Alt23)
			return (uint)Mathf.Max(0, (int)sliceIdx - 1);
		else
			return sliceIdx;
	}
	
	void OnEnable() {
		Debug.Assert(Instance == null, "StaggeredCascade is expected to be a singleton");
		Instance = this;

		m_StaggerStage = StaggerStage.Updating;

		var go = new GameObject("ShadowCamera");
		go.hideFlags = HideFlags.DontSave;// | HideFlags.NotEditable;
		go.transform.SetParent(transform);
		m_ShadowCamera = go.AddComponent<Camera>();
		m_ShadowCamera.orthographic = true;
		m_ShadowCamera.enabled = false;

		m_ShaderPassName = new ShaderPassName("ShadowCaster");
		m_DrawRendererSettings = new DrawRendererSettings {
			rendererConfiguration = RendererConfiguration.None,
			sorting = { flags = SortFlags.QuantizedFrontToBack }
		};
		m_DrawRendererSettings.SetShaderPassName(0, m_ShaderPassName);
		m_FilterRendererSettings = new FilterRenderersSettings(true) {
			renderQueueRange = new RenderQueueRange {
				min = (int)UnityEngine.Rendering.RenderQueue.Geometry,
				max = (int)UnityEngine.Rendering.RenderQueue.GeometryLast
			},
		};
	}

	void OnDisable() {
		Instance = null;

		if(Application.isPlaying)
			Destroy(m_ShadowCamera.gameObject);
		else
			DestroyImmediate(m_ShadowCamera.gameObject);

		m_StaggerStage = StaggerStage.Disabled;
	}

	void OnDrawGizmosSelected() {
		if(!m_ShadowCamera)
			return;

		Gizmos.matrix = m_ShadowCamera.transform.localToWorldMatrix;
		Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
		Gizmos.DrawCube(Vector3.zero, Vector3.one);
	}

	static void OnBeginCamera(ScriptableRenderContext renderContext, Camera camera, FrameSettings settings, CommandBuffer cmd) {
		++ms_RenderFrameNumber;

		if(Instance) {
			Instance.m_PreparedCamera = camera;

			if(Instance.isActiveAndEnabled && Instance.m_StaggerStage == StaggerStage.Updating && CanBeStaggered())
				LayerCulling.ForceDisable = true;
		}
	}

	static bool CaresAboutSlice(uint sliceIdx, uint sliceCount) {
		return
			sliceCount == 4 && sliceIdx > 0 && (
				sliceIdx == 3
				|| ( (ms_RenderFrameNumber & 1) == (sliceIdx & 1) && Instance.mode != Mode.Full012Fix3)
			);
	}

	static bool CanBeStaggered() {
		if(!Instance)
			return false;

		if(Instance.m_PreparedCamera.cameraType != CameraType.SceneView && Instance.m_PreparedCamera.cameraType != CameraType.Game)
			return false;

#if UNITY_EDITOR
		if(Instance.m_PreparedCamera.cameraType == CameraType.SceneView && !ms_SceneViewStagger) {
			Instance.m_StaggerStage = StaggerStage.Disabled;
			return false;
		}
#endif

		if(Instance.isActiveAndEnabled && Instance.m_StaggerStage == StaggerStage.Disabled && Instance.m_PreparedCamera.cameraType == CameraType.Game) {
			Instance.m_StaggerStage = StaggerStage.Updating;
		}

		return true;
	}
	 
	static ShadowAtlas.StaggeredOutput OnIsStaggered(uint sliceIdx, uint sliceCount, Matrix4x4 view, Matrix4x4 proj, Vector3 pos, Vector3 lightDir) {
		if(!Instance)
			return new ShadowAtlas.StaggeredOutput { valid = false };

		sliceIdx = TweakSliceIdx(sliceIdx);

		if(sliceCount == 4) {
			if(sliceIdx == 3) {
				if(Instance.m_StaggerStage != StaggerStage.Enabled) {
					Instance.m_StaggeredViewMatrix[sliceIdx] = view;
					Instance.m_StaggeredProjMatrix[sliceIdx] = proj;
					Instance.m_StaggeredPosition[sliceIdx] = pos;
					Instance.m_StaggeredLightDir[sliceIdx] = lightDir;
				}
			} else if( (ms_RenderFrameNumber & 1) != (sliceIdx & 1) && Instance.mode != Mode.Full012Fix3) {
				Instance.m_StaggeredViewMatrix[sliceIdx] = view;
				Instance.m_StaggeredProjMatrix[sliceIdx] = proj;
				Instance.m_StaggeredPosition[sliceIdx] = pos;
				Instance.m_StaggeredLightDir[sliceIdx] = lightDir;
			}
		}

		if(CaresAboutSlice(sliceIdx, sliceCount) && CanBeStaggered() && Instance.m_StaggerStage == StaggerStage.Enabled)
			return new ShadowAtlas.StaggeredOutput { valid = true, view = Instance.m_StaggeredViewMatrix[sliceIdx], proj = Instance.m_StaggeredProjMatrix[sliceIdx], position = Instance.m_StaggeredPosition[sliceIdx] };

		return new ShadowAtlas.StaggeredOutput { valid = false };
	}

	static bool OnShouldRenderShadows(uint sliceIdx, uint sliceCount) {
		sliceIdx = TweakSliceIdx(sliceIdx);

		if(!Instance || !CaresAboutSlice(sliceIdx, sliceCount) || !CanBeStaggered() || Instance.m_StaggerStage != StaggerStage.Enabled) {
			return true;
		} else {
			return false;
		}
	}

	static bool OnRenderShadows(ScriptableRenderContext renderContext, CullResults cullResults, uint sliceIdx, uint sliceCount) {
#if !UNITY_EDITOR        
#if UNITY_XBOXONE
        if (Instance)
        {
            if ( Instance.mode == Mode.Full012Fix3 )
            {
                // On base H/W, force the shadows to alternate cascades 2/3 for performance
                UnityEngine.XboxOne.HardwareVersion hwVersion = UnityEngine.XboxOne.Hardware.version; 
                if ((hwVersion == UnityEngine.XboxOne.HardwareVersion.XboxOne ) || (hwVersion == UnityEngine.XboxOne.HardwareVersion.XboxOneS ))
                {
                    Instance.mode = Mode.Full0Alt12Fix3;
                }
            }
        }
#endif      
#endif  
		var preTweakedSliceIdx = sliceIdx;
		sliceIdx = TweakSliceIdx(sliceIdx);

		if(!Instance || !CaresAboutSlice(sliceIdx, sliceCount) || !CanBeStaggered())
			return false;

		if(sliceIdx < 3) {
			if(Instance.mode == Mode.Full01Alt23 && preTweakedSliceIdx == 3 && Instance.m_StaggerStage == StaggerStage.Updating)
				Instance.m_StaggerStage = StaggerStage.Enabled;

			return false;
		}

		return Instance.DoRenderShadows(renderContext, cullResults, sliceIdx, sliceCount);
	}
	
	bool DoRenderShadows(ScriptableRenderContext renderContext, CullResults cullResults, uint sliceIdx, uint sliceCount) {
		if(m_StaggerStage == StaggerStage.Enabled)
			return true;

		var scales = transform.localScale;
		m_ShadowCamera.transform.position = transform.position - m_StaggeredLightDir[sliceIdx] * scales.y / 2f;
		m_ShadowCamera.transform.rotation = Quaternion.LookRotation(m_StaggeredLightDir[sliceIdx], Vector3.up);
		m_ShadowCamera.orthographicSize = scales.z / 2f;
		m_ShadowCamera.aspect = scales.x / scales.z;
		m_ShadowCamera.nearClipPlane = 0f;
		m_ShadowCamera.farClipPlane = scales.y;

		CullResults.GetCullingParameters(m_ShadowCamera, out m_ShadowCullParams);
		var lodParams = m_ShadowCullParams.lodParameters;
		lodParams.cameraPosition = m_StaggeredPosition[sliceIdx];
		m_ShadowCullParams.lodParameters = lodParams;
		m_ShadowCullParams.shadowDistance = 0f;

		var lodBias = QualitySettings.lodBias;
		QualitySettings.lodBias = 100f;
		CullResults.Cull(ref m_ShadowCullParams, renderContext, ref m_ShadowCullResults);
		QualitySettings.lodBias = lodBias;

		m_DrawRendererSettings.sorting.cameraPosition = m_StaggeredPosition[sliceIdx];
		m_DrawRendererSettings.sorting.worldToCameraMatrix = m_StaggeredViewMatrix[sliceIdx];

		renderContext.DrawRenderers(m_ShadowCullResults.visibleRenderers, ref m_DrawRendererSettings, m_FilterRendererSettings);

		m_StaggerStage = StaggerStage.Enabled;

		return true;
	}
}
