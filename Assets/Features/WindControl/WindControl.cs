using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[HDRPCallback]
public class WindControl : MonoBehaviour {
    static public WindControl Instance { get; private set; }

    [Header("Global")]
    [Range(0f, 3f)]     public float windGlobalStrengthScale                = 1f;
    [Range(0f, 3f)]     public float windGlobalStrengthScale2               = 1f;
    [Range(0f, 360f)]   public float windDirection                          = 65f;
    [Range(0f, 30f)]    public float windDirectionVariance                  = 25f;
    [Range(0.01f, 20f)] public float windDirectionVariancePeriod            = 15f;
    [Range(0f, 5f)]     public float windZoneIntensityOffset                = 0.1f;
    [Range(0f, 5f)]     public float windZoneIntensityBaseScale             = 0.25f;
    [Range(0f, 5f)]     public float windZoneIntensityGustScale             = 0.5f;
                        public bool  windZoneIntensityFromGrass             = true;

    [Header("Grass Base")]
    [LinkedRange(0f, 75f, "windTreeBaseStrength", "linkWindBaseStrength")]                                  public float windBaseStrength                       = 15f;
    [LinkedRange(0f, 3f, "windTreeBaseStrengthOffset", "linkWindBaseStrengthOffset")]                       public float windBaseStrengthOffset                 = 0.25f;
    [LinkedRange(0f, 10f, "windTreeBaseStrengthPhase", "linkWindBaseStrengthPhase")]                        public float windBaseStrengthPhase                  = 3f;
    [LinkedRange(0f, 10f, "windTreeBaseStrengthPhase2", "linkWindBaseStrengthPhase2")]                      public float windBaseStrengthPhase2                 = 0f;
    [LinkedRange(0.01f, 20f, "windTreeBaseStrengthVariancePeriod", "linkWindBaseStrengthVariancePeriod")]   public float windBaseStrengthVariancePeriod         = 10f;

    [Header("Grass Gust")]
    [LinkedRange(0f, 75f, "windTreeGustStrength", "linkWindGustStrength")]                                  public float windGustStrength                       = 25f;
    [LinkedRange(0f, 5f, "windTreeGustStrengthOffset", "linkWindGustStrengthOffset")]                       public float windGustStrengthOffset                 = 1f;
    [LinkedRange(0f, 10f, "windTreeGustStrengthPhase", "linkWindGustStrengthPhase")]                        public float windGustStrengthPhase                  = 3f;
    [LinkedRange(0f, 10f, "windTreeGustStrengthPhase2", "linkWindGustStrengthPhase2")]                      public float windGustStrengthPhase2                 = 3f;
    [LinkedRange(0.01f, 10f, "windTreeGustStrengthVariancePeriod", "linkWindGustStrengthVariancePeriod")]   public float windGustStrengthVariancePeriod         = 2f;
    [LinkedRange(0f, 5f, "windTreeGustInnerCosScale", "linkWindGustInnerCosScale")]                         public float windGustInnerCosScale                  = 2f;
    public AnimationCurve                                                                                                windGustStrengthControl                = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(10f, 1f));

    [Header("Grass Flutter")]
    [LinkedRange(0f, 10f, "windTreeFlutterStrength", "linkWindFlutterStrength")]                             public float windFlutterStrength                    = 0.4f;
    [LinkedRange(0f, 10f, "windTreeFlutterGustStrength", "linkWindFlutterGustStrength")]                     public float windFlutterGustStrength                = 0.2f;
    [LinkedRange(0f, 75f, "windTreeFlutterGustStrengthOffset", "linkWindFlutterGustStrengthOffset")]        public float windFlutterGustStrengthOffset          = 50f;
    [LinkedRange(0f, 75f, "windTreeFlutterGustStrengthScale", "linkWindFlutterGustStrengthScale")]          public float windFlutterGustStrengthScale           = 75f;
    [LinkedRange(0.01f, 2f, "windTreeFlutterGustVariancePeriod", "linkWindFlutterGustVariancePeriod")]      public float windFlutterGustVariancePeriod          = 0.25f;

    [Header("Tree Base")]
    [LinkedRange(0f, 10f, "windBaseStrength", "linkWindBaseStrength")]                                      public float windTreeBaseStrength                   = 0.25f;
    [LinkedRange(0f, 5f, "windBaseStrengthOffset", "linkWindBaseStrengthOffset")]                           public float windTreeBaseStrengthOffset             = 1f;
    [LinkedRange(0f, 2f, "windBaseStrengthPhase", "linkWindBaseStrengthPhase")]                             public float windTreeBaseStrengthPhase              = 0.5f;
    [LinkedRange(0f, 2f, "windBaseStrengthPhase2", "linkWindBaseStrengthPhase2")]                           public float windTreeBaseStrengthPhase2             = 0f;
    [LinkedRange(0.01f, 20f, "windBaseStrengthVariancePeriod", "linkWindBaseStrengthVariancePeriod")]       public float windTreeBaseStrengthVariancePeriod     = 6f;

    [Header("Tree Gust")]
    [LinkedRange(0f, 10f, "windGustStrength", "linkWindGustStrength")]                                      public float windTreeGustStrength                   = 3f;
    [LinkedRange(0f, 5f, "windGustStrengthOffset", "linkWindGustStrengthOffset")]                           public float windTreeGustStrengthOffset             = 1f;
    [LinkedRange(0f, 10f, "windGustStrengthPhase", "linkWindGustStrengthPhase")]                            public float windTreeGustStrengthPhase              = 2f;
    [LinkedRange(0f, 10f, "windGustStrengthPhase2", "linkWindGustStrengthPhase2")]                          public float windTreeGustStrengthPhase2             = 3f;
    [LinkedRange(0.01f, 10f, "windGustStrengthVariancePeriod", "linkWindGustStrengthVariancePeriod")]       public float windTreeGustStrengthVariancePeriod     = 4f;
    [LinkedRange(0f, 5f, "windGustInnerCosScale", "linkWindGustInnerCosScale")]                             public float windTreeGustInnerCosScale              = 2f;
    public AnimationCurve                                                                                                windTreeGustStrengthControl            = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(10f, 1f));

    [Header("Tree Flutter")]
    [LinkedRange(0f, 5f, "windFlutterStrength", "linkWindFlutterStrength")]                                 public float windTreeFlutterStrength                = 0.1f;
    [LinkedRange(0f, 5f, "windFlutterGustStrength", "linkWindFlutterGustStrength")]                         public float windTreeFlutterGustStrength            = 0.5f;
    [LinkedRange(0f, 75f, "windFlutterGustStrengthOffset", "linkWindFlutterGustStrengthOffset")]            public float windTreeFlutterGustStrengthOffset      = 12.5f;
    [LinkedRange(0f, 75f, "windFlutterGustStrengthScale", "linkWindFlutterGustStrengthScale")]              public float windTreeFlutterGustStrengthScale       = 25f;
    [LinkedRange(0.01f, 2f, "windFlutterGustVariancePeriod", "linkWindFlutterGustVariancePeriod")]          public float windTreeFlutterGustVariancePeriod      = 0.1f;

#pragma warning disable 0414
	[HideInInspector][SerializeField] bool linkWindBaseStrength                       = false;
    [HideInInspector][SerializeField] bool linkWindBaseStrengthOffset                 = false;
    [HideInInspector][SerializeField] bool linkWindBaseStrengthPhase                  = false;
    [HideInInspector][SerializeField] bool linkWindBaseStrengthPhase2                 = false;
    [HideInInspector][SerializeField] bool linkWindBaseStrengthVariancePeriod         = false;

    [HideInInspector][SerializeField] bool linkWindGustStrength                       = false;
    [HideInInspector][SerializeField] bool linkWindGustStrengthOffset                 = false;
    [HideInInspector][SerializeField] bool linkWindGustStrengthPhase                  = false;
    [HideInInspector][SerializeField] bool linkWindGustStrengthPhase2                 = false;
    [HideInInspector][SerializeField] bool linkWindGustStrengthVariancePeriod         = false;
    [HideInInspector][SerializeField] bool linkWindGustInnerCosScale                  = false;

    [HideInInspector][SerializeField] bool linkWindFlutterStrength                    = false;
    [HideInInspector][SerializeField] bool linkWindFlutterGustStrength                = false;
    [HideInInspector][SerializeField] bool linkWindFlutterGustStrengthOffset          = false;
    [HideInInspector][SerializeField] bool linkWindFlutterGustStrengthScale           = false;
    [HideInInspector][SerializeField] bool linkWindFlutterGustVariancePeriod          = false;
#pragma warning restore 0414

	WindZone m_WindZone;

    protected float WindDirectionAngle { get {
        var time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        return windDirection + Mathf.Cos(Mathf.PI * 2f * time / windDirectionVariancePeriod) * windDirectionVariance;
    }}

    protected Vector3 WindDirectionVector { get {
        return Quaternion.Euler(0f, WindDirectionAngle, 0f) * Vector3.forward;
    }}

    protected Vector3 WindDirectionStableVector { get {
        return Quaternion.Euler(0f, windDirection, 0f) * Vector3.forward;
    }}

    protected float RealTime { get { return Application.isPlaying ? Time.time : Time.realtimeSinceStartup; } }

	[HDRPCallbackMethod]
	static void WindControlSetup() {
		HDRenderPipeline.OnPrepareCamera += SetupGPUData;
	}

	static void SetupGPUData(ScriptableRenderContext renderContext, HDCamera hdCamera, CommandBuffer cmd) {
        if(Instance)
            Instance.SetShaderUniforms(cmd);
        else
            SetDisabledData(cmd);
    }

    void Reset() {
        windGustStrengthControl.postWrapMode = WrapMode.Loop;
        windTreeGustStrengthControl.postWrapMode = WrapMode.Loop;
    }

    protected virtual void OnEnable() {
        if(Application.isPlaying)
            Debug.Assert(Instance == null);

        m_WindZone = GetComponent<WindZone>();

        Instance = this;

#if DETAILED_TRANSITION_TRACKING
        Debug.LogFormat("{0:X4} Setting new WindControl2 Instance {1}/{2}.", Time.frameCount, GetInstanceID(), name);
#endif
    }

    protected virtual void OnDisable() {
        if(Application.isPlaying)
            Debug.Assert(Instance == this);

        m_WindZone = null;

        Instance = null;

#if DETAILED_TRANSITION_TRACKING
        Debug.LogFormat("{0:X4} CLEARING WindControl2 Instance {1}/{2}.", Time.frameCount, GetInstanceID(), name);
#endif
    }

    protected virtual void SetShaderUniforms(CommandBuffer cb) {
        var gustStrength = windGustStrengthControl.Evaluate(RealTime);
        var treeGustStrength = windTreeGustStrengthControl.Evaluate(RealTime);

		var _WindData_0_0 = (Vector4)WindDirectionVector; _WindData_0_0.w = 1f;
		var _WindData_0_1 = (Vector4)WindDirectionStableVector; _WindData_0_1.w = RealTime;
		//cb.SetGlobalVector("_WindData_0_0", _WindData_0_0);
		//cb.SetGlobalVector("_WindData_0_1", _WindData_0_1);
		var _WindData_0 = new Matrix4x4(_WindData_0_0, _WindData_0_1, Vector4.zero, Vector4.zero);
		cb.SetGlobalMatrix("_WindData_0", _WindData_0.transpose);
		//cb.SetGlobalVector("_WindDirection", WindDirectionVector);
		//cb.SetGlobalFloat("_WindEnabled", 1f);
		//cb.SetGlobalVector("_WindDirectionStable", WindDirectionStableVector);
		//cb.SetGlobalFloat("_WindTime", RealTime);

		var _WindData_1_0 = new Vector4(windBaseStrength * windGlobalStrengthScale * windGlobalStrengthScale2, windBaseStrengthOffset, windBaseStrengthPhase, windBaseStrengthPhase2);
		var _WindData_1_1 = new Vector4(windBaseStrengthVariancePeriod, windGustStrength * gustStrength * windGlobalStrengthScale * windGlobalStrengthScale2, windGustStrengthOffset, windGustStrengthPhase);
		var _WindData_1_2 = new Vector4(windGustStrengthPhase2, windGustStrengthVariancePeriod, windGustInnerCosScale, windFlutterStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		var _WindData_1_3 = new Vector4(windFlutterGustStrength * windGlobalStrengthScale * windGlobalStrengthScale2, windFlutterGustStrengthOffset, windFlutterGustStrengthScale, windFlutterGustVariancePeriod);
		//cb.SetGlobalVector("_WindData_1_0", _WindData_1_0);
		//cb.SetGlobalVector("_WindData_1_1", _WindData_1_1);
		//cb.SetGlobalVector("_WindData_1_2", _WindData_1_2);
		//cb.SetGlobalVector("_WindData_1_3", _WindData_1_3);
		var _WindData_1 = new Matrix4x4(_WindData_1_0, _WindData_1_1, _WindData_1_2, _WindData_1_3);
		cb.SetGlobalMatrix("_WindData_1", _WindData_1.transpose);
		//cb.SetGlobalFloat("_WindBaseStrength", windBaseStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		//cb.SetGlobalFloat("_WindBaseStrengthOffset", windBaseStrengthOffset);
		//cb.SetGlobalFloat("_WindBaseStrengthPhase", windBaseStrengthPhase);
		//cb.SetGlobalFloat("_WindBaseStrengthPhase2", windBaseStrengthPhase2);
		//cb.SetGlobalFloat("_WindBaseStrengthVariancePeriod", windBaseStrengthVariancePeriod);
		//cb.SetGlobalFloat("_WindGustStrength", windGustStrength * gustStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		//cb.SetGlobalFloat("_WindGustStrengthOffset", windGustStrengthOffset);
		//cb.SetGlobalFloat("_WindGustStrengthPhase", windGustStrengthPhase);
		//cb.SetGlobalFloat("_WindGustStrengthPhase2", windGustStrengthPhase2);
		//cb.SetGlobalFloat("_WindGustStrengthVariancePeriod", windGustStrengthVariancePeriod);
		//cb.SetGlobalFloat("_WindGustInnerCosScale", windGustInnerCosScale);
		//cb.SetGlobalFloat("_WindFlutterStrength", windFlutterStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		//cb.SetGlobalFloat("_WindFlutterGustStrength", windFlutterGustStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		//cb.SetGlobalFloat("_WindFlutterGustStrengthOffset", windFlutterGustStrengthOffset);
		//cb.SetGlobalFloat("_WindFlutterGustStrengthScale", windFlutterGustStrengthScale);
		//cb.SetGlobalFloat("_WindFlutterGustVariancePeriod", windFlutterGustVariancePeriod);

		var _WindData_2_0 = new Vector4(windTreeBaseStrength * windGlobalStrengthScale * windGlobalStrengthScale2, windTreeBaseStrengthOffset, windTreeBaseStrengthPhase, windTreeBaseStrengthPhase2);
		var _WindData_2_1 = new Vector4(windTreeBaseStrengthVariancePeriod, windTreeGustStrength * treeGustStrength * windGlobalStrengthScale * windGlobalStrengthScale2, windTreeGustStrengthOffset, windTreeGustStrengthPhase);
		var _WindData_2_2 = new Vector4(windTreeGustStrengthPhase2, windTreeGustStrengthVariancePeriod, windTreeGustInnerCosScale, windTreeFlutterStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		var _WindData_2_3 = new Vector4(windTreeFlutterGustStrength * windGlobalStrengthScale * windGlobalStrengthScale2, windTreeFlutterGustStrengthOffset, windTreeFlutterGustStrengthScale, windTreeFlutterGustVariancePeriod);
		//cb.SetGlobalVector("_WindData_2_0", _WindData_2_0);
		//cb.SetGlobalVector("_WindData_2_1", _WindData_2_1);
		//cb.SetGlobalVector("_WindData_2_2", _WindData_2_2);
		//cb.SetGlobalVector("_WindData_2_3", _WindData_2_3);
		var _WindData_2 = new Matrix4x4(_WindData_2_0, _WindData_2_1, _WindData_2_2, _WindData_2_3);
		cb.SetGlobalMatrix("_WindData_2", _WindData_2.transpose);
		//cb.SetGlobalFloat("_WindTreeBaseStrength",                  windTreeBaseStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		//cb.SetGlobalFloat("_WindTreeBaseStrengthOffset",            windTreeBaseStrengthOffset);
		//cb.SetGlobalFloat("_WindTreeBaseStrengthPhase",             windTreeBaseStrengthPhase);
		//cb.SetGlobalFloat("_WindTreeBaseStrengthPhase2",            windTreeBaseStrengthPhase2);
		//cb.SetGlobalFloat("_WindTreeBaseStrengthVariancePeriod",    windTreeBaseStrengthVariancePeriod);
		//cb.SetGlobalFloat("_WindTreeGustStrength",                  windTreeGustStrength * treeGustStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		//cb.SetGlobalFloat("_WindTreeGustStrengthOffset",            windTreeGustStrengthOffset);
		//cb.SetGlobalFloat("_WindTreeGustStrengthPhase",             windTreeGustStrengthPhase);
		//cb.SetGlobalFloat("_WindTreeGustStrengthPhase2",            windTreeGustStrengthPhase2);
		//cb.SetGlobalFloat("_WindTreeGustStrengthVariancePeriod",    windTreeGustStrengthVariancePeriod);
		//cb.SetGlobalFloat("_WindTreeGustInnerCosScale",             windTreeGustInnerCosScale);
		//cb.SetGlobalFloat("_WindTreeFlutterStrength",               windTreeFlutterStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		//cb.SetGlobalFloat("_WindTreeFlutterGustStrength",           windTreeFlutterGustStrength * windGlobalStrengthScale * windGlobalStrengthScale2);
		//cb.SetGlobalFloat("_WindTreeFlutterGustStrengthOffset",     windTreeFlutterGustStrengthOffset);
		//cb.SetGlobalFloat("_WindTreeFlutterGustStrengthScale",      windTreeFlutterGustStrengthScale);
		//cb.SetGlobalFloat("_WindTreeFlutterGustVariancePeriod",     windTreeFlutterGustVariancePeriod);

		if(m_WindZone) {
            m_WindZone.transform.rotation = Quaternion.LookRotation(WindDirectionVector);
            var wind = windZoneIntensityFromGrass ? GetBaseGustWind(RealTime, gustStrength) : GetTreeBaseGustWind(RealTime, treeGustStrength);
            m_WindZone.windMain = (windZoneIntensityOffset + wind.x * windZoneIntensityBaseScale + wind.y * windZoneIntensityGustScale) / 100f;
        }
		
#if DETAILED_TRANSITION_TRACKING
        Debug.LogFormat("{0:X4} Setting wind GPU data from {1}/{2}.", Time.frameCount, GetInstanceID(), name);
#endif
    }
    Vector3 GetBaseGustWind(float time, float gustStrength) {
        var objectBasePhase = Vector3.Dot(transform.position, Vector3.one * windBaseStrengthPhase) + Vector3.Dot(transform.position, WindDirectionStableVector) * -windBaseStrengthPhase2;
        var objectGustPhase = Vector3.Dot(transform.position, Vector3.one * windGustStrengthPhase) + Vector3.Dot(transform.position, WindDirectionStableVector) * -windGustStrengthPhase2;

        float windBaseCosInner = Mathf.PI * 2f * (time + objectBasePhase) / windBaseStrengthVariancePeriod;
        float windBase = (Mathf.Cos(windBaseCosInner) * 0.5f + windBaseStrengthOffset) * windBaseStrength * windGlobalStrengthScale * windGlobalStrengthScale2;
        float windGustCosInner = Mathf.PI * 2f * (time + objectGustPhase) / windGustStrengthVariancePeriod;
        float windGust = (Mathf.Cos(windGustCosInner + Mathf.Cos(windGustCosInner * 2f)) * 0.5f + windGustStrengthOffset) * windGustStrength * gustStrength * windGlobalStrengthScale * windGlobalStrengthScale2;

        return new Vector3(windBase, windGust, windBase + windGust);
    }

    Vector3 GetTreeBaseGustWind(float time, float treeGustStrength) {
        var objectBasePhase = Vector3.Dot(transform.position, Vector3.one * windTreeBaseStrengthPhase) + Vector3.Dot(transform.position, WindDirectionStableVector) * -windTreeBaseStrengthPhase2;
        var objectGustPhase = Vector3.Dot(transform.position, Vector3.one * windTreeGustStrengthPhase) + Vector3.Dot(transform.position, WindDirectionStableVector) * -windTreeGustStrengthPhase2;

        float windBaseCosInner = Mathf.PI * 2f * (time + objectBasePhase) / windTreeBaseStrengthVariancePeriod;
        float windBase = (Mathf.Cos(windBaseCosInner) * 0.5f + windTreeBaseStrengthOffset) * windTreeBaseStrength * windGlobalStrengthScale * windGlobalStrengthScale2;
        float windGustCosInner = Mathf.PI * 2f * (time + objectGustPhase) / windTreeGustStrengthVariancePeriod;
        float windGust = (Mathf.Cos(windGustCosInner + Mathf.Cos(windGustCosInner * 2f)) * 0.5f + windTreeGustStrengthOffset) * windTreeGustStrength * treeGustStrength * windGlobalStrengthScale * windGlobalStrengthScale2;

        return new Vector3(windBase, windGust, windBase + windGust);
    }

    static void SetDisabledData(CommandBuffer cb) {
        cb.SetGlobalFloat("_WindEnabled", 0f);

#if DETAILED_TRANSITION_TRACKING
        Debug.LogFormat("{0:X4} Setting disabled wind data.", Time.frameCount);
#endif
    }

    static public void VisualizeWindData(CommandBuffer cb) {
        if(!Instance || !Instance.enabled)
            return;

        Instance.DoVisualizeWindData(cb);
    }

    protected virtual void DoVisualizeWindData(CommandBuffer cb) {}

    void OnDrawGizmos() {
        var size = 2f;
        var forward = Quaternion.Euler(0f, WindDirectionAngle, 0f) * Vector3.forward * size;
        var right = Quaternion.Euler(0f, WindDirectionAngle, 0f) * Vector3.right * size * 0.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position - forward, transform.position + forward);
        Gizmos.DrawLine(transform.position + forward, transform.position + forward * 0.5f + right);
        Gizmos.DrawLine(transform.position + forward, transform.position + forward * 0.5f - right);
    }
}
