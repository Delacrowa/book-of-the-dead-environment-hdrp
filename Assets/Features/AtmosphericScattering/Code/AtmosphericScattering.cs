using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

[ExecuteInEditMode]
[HDRPCallback]
public class AtmosphericScattering : MonoBehaviour {
	public enum OcclusionDownscale { x1 = 1, x2 = 2, x3 = 3, x4 = 4 }
	public enum OcclusionSamples { x24 = 0, x64 = 1, x80 = 2, x96 = 3, x164 = 4 }
	public enum ScatterDebugMode { None, Scattering, Occlusion, OccludedScattering, Rayleigh, Mie, Height }
	public enum DepthTexture { Enable, Disable, Ignore }

	[Header("World Components")]
	public Gradient	worldRayleighColorRamp				= null;
	public float	worldRayleighColorIntensity			= 1f;
	public float	worldRayleighDensity				= 10f;
	public float	worldRayleighExtinctionFactor		= 1.1f;
	public float	worldRayleighIndirectScatter		= 0.33f;
	public Gradient	worldMieColorRamp					= null;
	public float	worldMieColorIntensity				= 1f;
	public float	worldMieDensity						= 15f;
	public float	worldMieExtinctionFactor			= 0f;
	public float	worldMiePhaseAnisotropy				= 0.9f;
	public float	worldNearScatterPush				= 0f;
	public float	worldNormalDistance					= 1000f;

	[Header("Height Components")]
	public Color	heightRayleighColor					= Color.white;
	public float	heightRayleighIntensity				= 1f;
	public float	heightRayleighDensity				= 10f;
	public float	heightMieDensity					= 0f;
	public float	heightExtinctionFactor				= 1.1f;
	public float	heightSeaLevel						= 0f;
	public float	heightDistance						= 50f;
	public Vector3	heightPlaneShift					= Vector3.zero;
	public float	heightNearScatterPush				= 0f;
	public float	heightNormalDistance				= 1000f;

	[Header("Sky Dome")]
	public Vector3		skyDomeScale					= new Vector3(1f, 0.1f, 1f);
	public Vector3		skyDomeRotation					= Vector3.zero;
	public Transform	skyDomeTrackedYawRotation		= null;
	public bool			skyDomeVerticalFlip				= false;
	public Cubemap		skyDomeCube						= null;
	public float		skyDomeExposure					= 1f;
	public Color		skyDomeTint						= Color.white;
	[HideInInspector] public Vector3 skyDomeOffset		= Vector3.zero;

	[Header("Scatter Occlusion")]
	public bool					useOcclusion			= false;
	public bool					occlusionJitter			= false;
	public bool					occlusionJitterALU		= false;
	public bool					occlusionUpsample		= false;
	[Range(0.0f, 4.0f)]
	public float				occlusionUpsampleRadius	= 1.5f;
	[Range(0.0f, 1.0f)]
	public float				occlusionUpsampleNoiseL	= 0.0f;
	public bool					occlusionResolve		= true;
	[Range(0.0f, 1.0f)]
	public float				occlusionResolveFactor	= 0.95f;
	public bool					occlusionResolveCheap	= false;
	public Texture2D			occlusionNoiseTex		= null;
	public float				occlusionBias			= 0f;
	public float				occlusionBiasIndirect	= 0.6f;
	public float				occlusionBiasClouds		= 0.3f;
	public OcclusionDownscale	occlusionDownscale		= OcclusionDownscale.x2;
	public OcclusionSamples		occlusionSamples		= OcclusionSamples.x64;
	public bool					occlusionDepthFixup		= true;
	public float				occlusionDepthThreshold	= 25f;
	public bool					occlusionFullSky		= false;
	public float				occlusionBiasSkyRayleigh= 0.2f;
	public float				occlusionBiasSkyMie		= 0.4f;
	
	[Header("Other")]
	public float			worldScaleExponent			= 1.0f;
	public bool				forcePerPixel				= false;
	public bool				forcePostEffect				= false;
	[Tooltip("Soft clouds need depth values. Ignore means externally controlled.")]
	public DepthTexture		depthTexture				= DepthTexture.Enable;
	public ScatterDebugMode	debugMode					= ScatterDebugMode.None;
	
	[HideInInspector] public Shader occlusionShader;

	bool			m_isAwake;
	Material		m_occlusionMaterial;
	
    public static AtmosphericScattering instance { get; private set; }
    
	void Awake() {
		if(occlusionShader == null)
			occlusionShader = Shader.Find("Hidden/AtmosphericScattering_Occlusion");

		m_occlusionMaterial = new Material(occlusionShader);
		m_occlusionMaterial.hideFlags = HideFlags.HideAndDontSave;

		if(worldRayleighColorRamp == null) {
			worldRayleighColorRamp = new Gradient();
			worldRayleighColorRamp.SetKeys(
				new[]{ new GradientColorKey(new Color(0.3f, 0.4f, 0.6f), 0f), new GradientColorKey(new Color(0.5f, 0.6f, 0.8f), 1f) },
			new[]{ new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
			);
		}
		if(worldMieColorRamp == null) {
			worldMieColorRamp = new Gradient();
			worldMieColorRamp.SetKeys(
				new[]{ new GradientColorKey(new Color(0.95f, 0.75f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.9f, 8.0f), 1f) },
			new[]{ new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
			);
		}

		m_isAwake = true;
	}

	void OnEnable() {
		if(instance && instance != this)
			Debug.LogErrorFormat("Unexpected: AtmosphericScattering.instance already set (to: {0}). Still overriding with: {1}.", instance.name, name);
		
		instance = this;
	}

    void OnDisable() {
		if(instance != this) {
			if(instance)
				Debug.LogErrorFormat("Unexpected: AtmosphericScattering.instance set to: {0}, not to: {1}. Leaving alone.", instance.name, name);
		} else {
			instance = null;
		}
	}

	void OnValidate() {
		if(!m_isAwake)
			return;

		occlusionBias = Mathf.Clamp01(occlusionBias);
		occlusionBiasIndirect = Mathf.Clamp01(occlusionBiasIndirect);
		occlusionBiasClouds = Mathf.Clamp01(occlusionBiasClouds);
		occlusionBiasSkyRayleigh = Mathf.Clamp01(occlusionBiasSkyRayleigh);
		occlusionBiasSkyMie = Mathf.Clamp01(occlusionBiasSkyMie);
		worldScaleExponent = Mathf.Clamp(worldScaleExponent, 1f, 2f);
		worldNormalDistance = Mathf.Clamp(worldNormalDistance, 1f, 10000f);
		worldNearScatterPush = Mathf.Clamp(worldNearScatterPush, -200f, 300f);
		worldRayleighDensity = Mathf.Clamp(worldRayleighDensity, 0, 1000f);
		worldMieDensity = Mathf.Clamp(worldMieDensity, 0f, 1000f);
		worldRayleighIndirectScatter = Mathf.Clamp(worldRayleighIndirectScatter, 0, 1f);

		heightNormalDistance = Mathf.Clamp(heightNormalDistance, 1f, 10000f);
		heightNearScatterPush = Mathf.Clamp(heightNearScatterPush, -200f, 300f);
		heightRayleighDensity = Mathf.Clamp(heightRayleighDensity, 0, 1000f);
		heightMieDensity = Mathf.Clamp(heightMieDensity, 0, 1000f);
		
		worldMiePhaseAnisotropy = Mathf.Clamp01(worldMiePhaseAnisotropy);
		skyDomeExposure = Mathf.Clamp(skyDomeExposure, 0f, 8f);

#if UNITY_EDITOR
		UnityEditor.SceneView.RepaintAll();
#endif
	}

	[HDRPCallbackMethod]
	static void AtmosphericScatteringSetup() {
		HDRenderPipeline.OnPrepareCamera += SetupGPUData;
	}

	static void SetupGPUData(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd) {
#if DETAILED_TRANSITION_TRACKING
        Debug.LogFormat("{0:X4}: A CamPos: {1}  CamRot: {2}  CamFov: {3}  CamNear: {4}  CamFar: {5}", Time.frameCount, cam.transform.position, cam.transform.rotation, cam.fieldOfView, cam.nearClipPlane, cam.farClipPlane);
#endif

        if(instance)
            instance.UpdateShadingParams(cmd, hdCamera.camera);
    }

    private int occlusionHistoryIndex = 0;
    private RenderTexture[] occlusionHistoryBuffers = new RenderTexture[2];
    private RenderTargetIdentifier[] occlusionHistoryIds = new RenderTargetIdentifier[2];

    private bool occlusionHistorySunMotion = false;
    private const float occlusionHistorySunMotionThreshold = (Mathf.PI / 180.0f) * 0.001f;
    private Quaternion occlusionHistorySunRotation = Quaternion.identity;

    private bool EnsureOcclusonHistory(int width, int height)
    {
        bool continuous = true;

        for (int i = 0; i != occlusionHistoryBuffers.Length; i++)
        {
            var rt = occlusionHistoryBuffers[i];
            if (rt != null && (rt.width != width || rt.height != height || rt.IsCreated() == false))
            {
                RenderTexture.ReleaseTemporary(rt);
                rt = null;
            }

            if (rt == null)
            {
                rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);
                rt.filterMode = FilterMode.Bilinear;
                rt.wrapMode = TextureWrapMode.Clamp;

                occlusionHistoryBuffers[i] = rt;
                occlusionHistoryIds[i] = new RenderTargetIdentifier(rt);

                continuous = false;
            }
        }

        return continuous;
    }

	public void RenderOcclusion(CommandBuffer cb, Camera cam, HDCamera hdCamera) {
#if DETAILED_TRANSITION_TRACKING
        Debug.LogFormat("{0:X4}: B CamPos: {1}  CamRot: {2}  CamFov: {3}  CamNear: {4}  CamFar: {5}", Time.frameCount, cam.transform.position, cam.transform.rotation, cam.fieldOfView, cam.nearClipPlane, cam.farClipPlane);
#endif

		var activeSun = AtmosphericScatteringSun.instance;
		if(!activeSun) {
            // When there's no primary light, mie scattering and occlusion will be disabled, so there's nothing for us schedule.
            occlusionHistorySunMotion = false;
            occlusionHistorySunRotation = Quaternion.identity;
            return;
		}

        var camSupportResolve = (Camera.main == cam);
        if (camSupportResolve)
        {
            var activeSunRotation = activeSun.transform.rotation;
            occlusionHistorySunMotion = (Quaternion.Angle(activeSunRotation, occlusionHistorySunRotation) > occlusionHistorySunMotionThreshold);
            occlusionHistorySunRotation = activeSunRotation;
        }

		if(useOcclusion) {
            cb.BeginSample("Scatter Occlusion");

			var camRgt = cam.transform.right;
			var camUp = cam.transform.up;
			var camFwd = cam.transform.forward;
				
			var dy = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
			var dx = dy * cam.aspect;
				
			var vpCenter = camFwd * cam.farClipPlane;
			var vpRight = camRgt * dx * cam.farClipPlane;
			var vpUp = camUp * dy * cam.farClipPlane;

            cb.SetGlobalVector("u_CameraPosition", cam.transform.position);

			// TODO: No longer need these camera props (but leave them alone for now to reduce risk of accidental breakage)
			cb.SetGlobalVector("u_ViewportCorner", vpCenter - vpRight - vpUp);
			cb.SetGlobalVector("u_ViewportRight", vpRight * 2f);
			cb.SetGlobalVector("u_ViewportUp", vpUp * 2f);
			var farDist = cam.farClipPlane;
			var refDist = (Mathf.Min(farDist, QualitySettings.shadowDistance) - 1f) / farDist;
			cb.SetGlobalFloat("u_OcclusionSkyRefDistance", refDist);
			var fDownscale = (float)(int)occlusionDownscale;
			var srcRect = cam.pixelRect;
			var occDownscale = 1f / fDownscale;
			var occWidth = Mathf.RoundToInt(srcRect.width * occDownscale);
			var occHeight = Mathf.RoundToInt(srcRect.height * occDownscale);
			cb.SetGlobalFloat("u_Downscale", fDownscale);
			cb.SetGlobalVector("u_DownscaledScreenSize", new Vector4(1f / occWidth, 1f / occHeight, 0, 0));

            cb.SetGlobalFloat("u_JitterPhase", Mathf.Sin(Time.realtimeSinceStartup));
            cb.SetGlobalFloat("u_JitterScale", occlusionJitter ? 1.0f : 0.0f);

            cb.SetGlobalFloat("u_UpsampleRadius", occlusionUpsampleRadius);
            cb.SetGlobalFloat("u_UpsampleNoiseL", occlusionUpsampleNoiseL);

            cb.SetGlobalTexture("u_NoiseTex", occlusionNoiseTex);
            cb.SetGlobalFloat("u_NoisePhase", Mathf.Sin(Time.realtimeSinceStartup));

            var occlusionId = Shader.PropertyToID("u_OcclusionTexture");

            var upsampleFrom = occlusionUpsample ? occlusionDownscale : OcclusionDownscale.x1;
            var upsamplePass = System.Enum.GetNames(typeof(OcclusionSamples)).Length;

            switch (upsampleFrom)
            {
                case OcclusionDownscale.x4:
                    {
                        var occlusionId_x1 = Shader.PropertyToID("u_OcclusionTexture_x1");
                        var occlusionId_x2 = Shader.PropertyToID("u_OcclusionTexture_x2");
                        var occlusionId_x4 = occlusionId;

                        cb.GetTemporaryRT(occlusionId_x1, occWidth * 1, occHeight * 1, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);
                        cb.GetTemporaryRT(occlusionId_x2, occWidth * 2, occHeight * 2, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);
                        cb.GetTemporaryRT(occlusionId_x4, occWidth * 4, occHeight * 4, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);

                        HDUtils.DrawFullScreen(cb, hdCamera, m_occlusionMaterial, occlusionId_x1, null, (int)occlusionSamples);

                        cb.SetGlobalTexture(occlusionId, occlusionId_x1);
                        HDUtils.DrawFullScreen(cb, hdCamera, m_occlusionMaterial, occlusionId_x2, null, upsamplePass);

                        cb.SetGlobalTexture(occlusionId, occlusionId_x2);
                        HDUtils.DrawFullScreen(cb, hdCamera, m_occlusionMaterial, occlusionId_x4, null, upsamplePass);
                    }
                    break;

                case OcclusionDownscale.x2:
                    {
                        var occlusionId_x1 = Shader.PropertyToID("u_OcclusionTexture_x1");
                        var occlusionId_x2 = occlusionId;

                        cb.GetTemporaryRT(occlusionId_x1, occWidth * 1, occHeight * 1, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);
                        cb.GetTemporaryRT(occlusionId_x2, occWidth * 2, occHeight * 2, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);

                        HDUtils.DrawFullScreen(cb, hdCamera, m_occlusionMaterial, occlusionId_x1, null, (int)occlusionSamples);

                        cb.SetGlobalTexture(occlusionId, occlusionId_x1);
                        HDUtils.DrawFullScreen(cb, hdCamera, m_occlusionMaterial, occlusionId_x2, null, upsamplePass);
                    }
                    break;

                default:
                    {
                        cb.GetTemporaryRT(occlusionId, occWidth, occHeight, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);
                        HDUtils.DrawFullScreen(cb, hdCamera, m_occlusionMaterial, occlusionId, null, (int)occlusionSamples);
                    }
                    break;
            }

            cb.SetGlobalTexture(occlusionId, occlusionId);

            if (occlusionJitter && occlusionResolve && camSupportResolve)
            {
                int resolvePass = upsamplePass + 1;
                int resolveInput = occlusionHistoryIndex;
                int resolveOutput = (occlusionHistoryIndex + 1) & 1;

                float historyWeight = EnsureOcclusonHistory(cam.pixelWidth, cam.pixelHeight) ? occlusionResolveFactor : 0.0f;

                cb.SetGlobalFloat("u_OcclusionHistorySunMotion", occlusionHistorySunMotion ? 1.0f : 0.0f);
                cb.SetGlobalFloat("u_OcclusionHistoryWeight", historyWeight);
                cb.SetGlobalTexture("u_OcclusionHistory", occlusionHistoryIds[resolveInput]);
                HDUtils.DrawFullScreen(cb, hdCamera, m_occlusionMaterial, occlusionHistoryIds[resolveOutput], null, resolvePass);

                occlusionHistoryIndex = resolveOutput;

                cb.SetGlobalTexture(occlusionId, occlusionHistoryIds[resolveOutput]);
            }

            cb.EndSample("Scatter Occlusion");
        }
    }

    void UpdateShadingParams(CommandBuffer cb, Camera cam) {
#if DETAILED_TRANSITION_TRACKING
        Debug.LogFormat(this, "{0:X4} AtmosphericScattering UpdateShadingParams: {1} (ppix: {2}  rs: {3}, ms: {4}, hrs: {5}, hms: {6})", Time.frameCount, this, forcePerPixel, worldRayleighDensity, worldMieDensity, heightRayleighDensity, heightMieDensity);
#endif

        UpdateKeywords(cb);
        UpdateStaticUniforms(cb);
        UpdateDynamicUniforms(cb, cam);
    }

    void UpdateKeywords(CommandBuffer cb) {
        cb.DisableShaderKeyword("ATMOSPHERICS");
        cb.DisableShaderKeyword("ATMOSPHERICS_PER_PIXEL");
        cb.DisableShaderKeyword("ATMOSPHERICS_OCCLUSION");
        cb.DisableShaderKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");
        cb.DisableShaderKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");
        cb.DisableShaderKeyword("ATMOSPHERICS_OCCLUSION_JITTER_ALU");
        cb.DisableShaderKeyword("ATMOSPHERICS_OCCLUSION_UPSAMPLE_NOISE_L");
        cb.DisableShaderKeyword("ATMOSPHERICS_OCCLUSION_RESOLVE_CHEAP");
        cb.DisableShaderKeyword("ATMOSPHERICS_SUNRAYS");
        cb.DisableShaderKeyword("ATMOSPHERICS_DEBUG");

        if(!forcePerPixel)
            cb.EnableShaderKeyword("ATMOSPHERICS");
        else
            cb.EnableShaderKeyword("ATMOSPHERICS_PER_PIXEL");

        if(useOcclusion) {
            cb.EnableShaderKeyword("ATMOSPHERICS_OCCLUSION");

            if(occlusionDepthFixup && occlusionDownscale != OcclusionDownscale.x1)
                cb.EnableShaderKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");

            if(occlusionFullSky)
                cb.EnableShaderKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");

            if (occlusionJitterALU)
                cb.EnableShaderKeyword("ATMOSPHERICS_OCCLUSION_JITTER_ALU");

            if (occlusionUpsampleNoiseL > float.Epsilon)
                cb.EnableShaderKeyword("ATMOSPHERICS_OCCLUSION_UPSAMPLE_NOISE_L");

            if (occlusionResolveCheap)
                cb.EnableShaderKeyword("ATMOSPHERICS_OCCLUSION_RESOLVE_CHEAP");
        }

        if(debugMode != ScatterDebugMode.None)
            cb.EnableShaderKeyword("ATMOSPHERICS_DEBUG");
    }

    void UpdateStaticUniforms(CommandBuffer cb) {
		cb.SetGlobalVector("u_SkyDomeOffset", skyDomeOffset);
		cb.SetGlobalVector("u_SkyDomeScale", skyDomeScale);
		cb.SetGlobalTexture("u_SkyDomeCube", skyDomeCube);
		cb.SetGlobalFloat("u_SkyDomeExposure", Mathf.GammaToLinearSpace(skyDomeExposure));
		cb.SetGlobalColor("u_SkyDomeTint", skyDomeTint.linear);

		cb.SetGlobalFloat("u_ShadowBias", useOcclusion ? occlusionBias : 1f);
		cb.SetGlobalFloat("u_ShadowBiasIndirect", useOcclusion ? occlusionBiasIndirect : 1f);
		cb.SetGlobalFloat("u_ShadowBiasClouds", useOcclusion ? occlusionBiasClouds : 1f);
		cb.SetGlobalVector("u_ShadowBiasSkyRayleighMie", useOcclusion ? new Vector4(occlusionBiasSkyRayleigh, occlusionBiasSkyMie, 0f, 0f) : Vector4.zero);
		cb.SetGlobalFloat("u_OcclusionDepthThreshold", occlusionDepthThreshold);

		cb.SetGlobalFloat("u_WorldScaleExponent", worldScaleExponent);
		
		cb.SetGlobalFloat("u_WorldNormalDistanceRcp", 1f/worldNormalDistance);
		cb.SetGlobalFloat("u_WorldNearScatterPush", -Mathf.Pow(Mathf.Abs(worldNearScatterPush), worldScaleExponent) * Mathf.Sign(worldNearScatterPush));
		
		cb.SetGlobalFloat("u_WorldRayleighDensity", -worldRayleighDensity / 100000f);
		cb.SetGlobalFloat("u_MiePhaseAnisotropy", worldMiePhaseAnisotropy);
		cb.SetGlobalVector("u_RayleighInScatterPct", new Vector4(1f - worldRayleighIndirectScatter, worldRayleighIndirectScatter, 0f, 0f));
		
		cb.SetGlobalFloat("u_HeightNormalDistanceRcp", 1f/heightNormalDistance);
		cb.SetGlobalFloat("u_HeightNearScatterPush", -Mathf.Pow(Mathf.Abs(heightNearScatterPush), worldScaleExponent) * Mathf.Sign(heightNearScatterPush));
		cb.SetGlobalFloat("u_HeightRayleighDensity", -heightRayleighDensity / 100000f);
		
		cb.SetGlobalFloat("u_HeightSeaLevel", heightSeaLevel);
		cb.SetGlobalFloat("u_HeightDistanceRcp", 1f/heightDistance);
		cb.SetGlobalVector("u_HeightPlaneShift", heightPlaneShift);
		cb.SetGlobalVector("u_HeightRayleighColor", (Vector4)heightRayleighColor * heightRayleighIntensity);
		cb.SetGlobalFloat("u_HeightExtinctionFactor", heightExtinctionFactor);
		cb.SetGlobalFloat("u_RayleighExtinctionFactor", worldRayleighExtinctionFactor);
		cb.SetGlobalFloat("u_MieExtinctionFactor", worldMieExtinctionFactor);
		
		var rayleighColorM20 = worldRayleighColorRamp.Evaluate(0.00f);
		var rayleighColorM10 = worldRayleighColorRamp.Evaluate(0.25f);
		var rayleighColorO00 = worldRayleighColorRamp.Evaluate(0.50f);
		var rayleighColorP10 = worldRayleighColorRamp.Evaluate(0.75f);
		var rayleighColorP20 = worldRayleighColorRamp.Evaluate(1.00f);
		
		var mieColorM20 = worldMieColorRamp.Evaluate(0.00f);
		var mieColorO00 = worldMieColorRamp.Evaluate(0.50f);
		var mieColorP20 = worldMieColorRamp.Evaluate(1.00f);
		
		cb.SetGlobalVector("u_RayleighColorM20", (Vector4)rayleighColorM20 * worldRayleighColorIntensity);
		cb.SetGlobalVector("u_RayleighColorM10", (Vector4)rayleighColorM10 * worldRayleighColorIntensity);
		cb.SetGlobalVector("u_RayleighColorO00", (Vector4)rayleighColorO00 * worldRayleighColorIntensity);
		cb.SetGlobalVector("u_RayleighColorP10", (Vector4)rayleighColorP10 * worldRayleighColorIntensity);
		cb.SetGlobalVector("u_RayleighColorP20", (Vector4)rayleighColorP20 * worldRayleighColorIntensity);
		
		cb.SetGlobalVector("u_MieColorM20", (Vector4)mieColorM20 * worldMieColorIntensity);
		cb.SetGlobalVector("u_MieColorO00", (Vector4)mieColorO00 * worldMieColorIntensity);
		cb.SetGlobalVector("u_MieColorP20", (Vector4)mieColorP20 * worldMieColorIntensity);

		cb.SetGlobalFloat("u_AtmosphericsDebugMode", (int)debugMode);
	}

	void UpdateDynamicUniforms(CommandBuffer cb, Camera cam) {
		var activeSun = AtmosphericScatteringSun.instance;
		bool hasSun = !!activeSun;

		var trackedYaw = skyDomeTrackedYawRotation ? skyDomeTrackedYawRotation.eulerAngles.y : 0f;
		cb.SetGlobalMatrix("u_SkyDomeRotation",
           Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(skyDomeRotation.x, 0f, 0f), Vector3.one)
           * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, skyDomeRotation.y - trackedYaw, 0f), Vector3.one)
           * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, skyDomeVerticalFlip ? -1f : 1f, 1f))                   
		);

        cb.SetGlobalVector("u_CameraPosition", cam.transform.position);

        cb.SetGlobalVector("u_SunDirection", hasSun ? -activeSun.transform.forward : Vector3.down);	
		cb.SetGlobalFloat("u_WorldMieDensity", hasSun ? -worldMieDensity / 100000f : 0f);
		cb.SetGlobalFloat("u_HeightMieDensity", hasSun ? -heightMieDensity / 100000f : 0f);

		var pixelRect = cam.pixelRect;
		var scale = (float)(int)occlusionDownscale;
		var depthTextureScaledTexelSize = new Vector4(scale / pixelRect.width, scale / pixelRect.height, -scale / pixelRect.width, -scale / pixelRect.height);
		cb.SetGlobalVector("u_DepthTextureScaledTexelSize", depthTextureScaledTexelSize);

        cb.SetGlobalVector("_AtmosphericScatteringSunVector", hasSun ? activeSun.transform.forward : Vector3.zero);
    }
}