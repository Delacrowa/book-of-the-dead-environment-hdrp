
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
public sealed class ExposedAutoFocusReferenceParameter : VolumeParameter<ExposedReference<DepthOfFieldAutoFocus>> {
    public ExposedAutoFocusReferenceParameter(ExposedReference<DepthOfFieldAutoFocus> value, bool overrideState = false)
        : base(value, overrideState) {
    }
}

// ExecuteInEditMode needed to run OnDisable() after DoF made us allocate the buffer in edit mode.
[DisallowMultipleComponent, ExecuteInEditMode]
public class DepthOfFieldAutoFocus : MonoBehaviour, IDepthOfFieldAutoFocus
{
    static class Uniforms
    {
        internal static readonly int _FocalLength = Shader.PropertyToID("_FocalLength");
        internal static readonly int _ApertureFilmHeightx2 = Shader.PropertyToID("_ApertureFilmHeightx2");
        internal static readonly int _SmoothTime = Shader.PropertyToID("_SmoothTime");
        internal static readonly int _MaxAdaptionSpeed = Shader.PropertyToID("_MaxAdaptionSpeed");
        internal static readonly int _DeltaTime = Shader.PropertyToID("_DeltaTime");
        internal static readonly int _VoteBias = Shader.PropertyToID("_VoteBias");
        internal static readonly int _DistanceOverride = Shader.PropertyToID("_DistanceOverride");
        internal static readonly int _DistanceOverrideWeight = Shader.PropertyToID("_DistanceOverrideWeight");
        internal static readonly int _FixedFocusDistance = Shader.PropertyToID("_FixedFocusDistance");
        internal static readonly int _Influence = Shader.PropertyToID("_Influence");
        internal const string _AutoFocusParams = "_AutoFocusParams";
        internal const string _AutoFocusOutput = "_AutoFocusOutput";
        internal const string AutoFocusKeyword = "AUTO_FOCUS";
    }

    [Header("AF Settings")]
    float m_Influence = 1.0f;
    public float influence { get{ return m_Influence; } set { m_Influence = value; } }
    [Tooltip("The time it takes to switch to the new focus distance, in seconds.")]
    public float m_AdaptationTime;
    float m_MaxAdaptionSpeed;

    [Tooltip("Stickier auto focus is more stable (less switching back and forth as tiny grass blades cross the camera), but requires looking at a bigger uniform-ish area to switch focus to it.")]
    [Range(0, 1)]
    public float m_Stickiness = 0.4f;

    [Header("DoF Settings")]
    [Tooltip("Above that walking speed, disable auto focus and fix it at whatever distance the DoF component is set to. When the player slows down, use the Adaptation Time setting to move the focus closer again, as usual.")]
    public float m_MaxPlayerVelocity = 3.0f;
    float m_CurrentSmoothedPlayerVelocity = 0.0f;
    bool m_ResetHistory = true;

    [UnityEngine.Rendering.PostProcessing.Min(0.1f)]
    public float focusDistanceWalk = 10f;
    [Range(0.05f, 32f)]
    public float apertureWalk = 5.6f;
    [UnityEngine.Rendering.PostProcessing.Min(0.1f)]
    public float focusDistanceRun = 10f;
    [Range(0.05f, 32f)]
    public float apertureRun = 5.6f;

    ComputeBuffer m_AutoFocusParamsCB;
    ComputeBuffer m_AutoFocusOutputCB;
    [HideInInspector]
    public ComputeShader m_Compute;

    [NonSerialized]
    float m_DistanceOverride = 0;
    public float distanceOverride
    {
        get { return m_DistanceOverride; }
        set { m_DistanceOverride = Mathf.Max(value, 0.0f); }
    }
    [NonSerialized]
    float m_DistanceOverrideWeight = 0;
    public float distanceOverrideWeight
    {
        get { return m_DistanceOverrideWeight; }
        set { m_DistanceOverrideWeight = Mathf.Clamp01(value); }
    }

    void Reset()
    {
        m_AdaptationTime = 0.2f;
        m_MaxAdaptionSpeed = 4.0f;
        m_DistanceOverrideWeight = 0;
    }

    void OnValidate()
    {
        m_AdaptationTime = Mathf.Max(m_AdaptationTime, 0.001f);
        m_MaxAdaptionSpeed = Mathf.Max(m_MaxAdaptionSpeed, 0.0f);
    }

    void Init(float initialFocusDistance)
    {
        if (m_AutoFocusParamsCB == null)
        {
            m_AutoFocusParamsCB = new ComputeBuffer(1, 12);
            m_ResetHistory = true;
        }

        if (m_AutoFocusOutputCB == null)
            m_AutoFocusOutputCB = new ComputeBuffer(1, 8);

        if (m_ResetHistory)
        {
            // Init the buffer to have a sensible starting point and no blinking
            float currentVelocity = 0;
            m_AutoFocusParamsCB.SetData(new float[]{initialFocusDistance, currentVelocity, initialFocusDistance});
            m_ResetHistory = false;
        }
    }

    void GetDoFParams(out float focusDistance, out float aperture)
    {
        if (PlayerVelocityUnderRunThreshold())
        {
            focusDistance = focusDistanceWalk;
            aperture = apertureWalk;
        }
        else
        {
            focusDistance = focusDistanceRun;
            aperture = apertureRun;
        }
    }

    void Focus(CommandBuffer cmd, float focalLength, float filmHeight, Camera cam)
    {
        float focusDistance, aperture;
        GetDoFParams(out focusDistance, out aperture);
        Init(focusDistance);

        cmd.SetComputeFloatParam(m_Compute, Uniforms._FocalLength, focalLength);
        cmd.SetComputeFloatParam(m_Compute, Uniforms._ApertureFilmHeightx2, aperture * filmHeight * 2);
        cmd.SetComputeFloatParam(m_Compute, Uniforms._SmoothTime, m_AdaptationTime);
        cmd.SetComputeFloatParam(m_Compute, Uniforms._MaxAdaptionSpeed, m_MaxAdaptionSpeed);
        cmd.SetComputeFloatParam(m_Compute, Uniforms._DeltaTime, Time.deltaTime);

        // Calculate vote bias based on a [0,1] stickiness.
        // Stickiness 0 is vote bias 0 - new contender only needs one more vote.
        // Stickiness 1 is vote bias 15 - new contender needs all the votes.
        float voteBias = m_Stickiness * 15.0f;
        cmd.SetComputeFloatParam(m_Compute, Uniforms._VoteBias, voteBias);

        // Overrides
        cmd.SetComputeFloatParam(m_Compute, Uniforms._DistanceOverride, m_DistanceOverride);
        cmd.SetComputeFloatParam(m_Compute, Uniforms._DistanceOverrideWeight, m_DistanceOverrideWeight);
        cmd.SetComputeFloatParam(m_Compute, Uniforms._FixedFocusDistance, focusDistance);
        cmd.SetComputeFloatParam(m_Compute, Uniforms._Influence, PlayerVelocityUnderRunThreshold() ? m_Influence : 0);

        cmd.SetComputeBufferParam(m_Compute, 0, Uniforms._AutoFocusParams, m_AutoFocusParamsCB);
        cmd.SetComputeBufferParam(m_Compute, 0, Uniforms._AutoFocusOutput, m_AutoFocusOutputCB);
        cmd.DispatchCompute(m_Compute, 0, 1, 1, 1);

        cmd.SetGlobalBuffer(Uniforms._AutoFocusOutput, m_AutoFocusOutputCB);
    }

    public string GetAutoFocusKeyword() {
        return Uniforms.AutoFocusKeyword;
    }

    public void SetUpAutoFocusParams(CommandBuffer cmd, float focalLength /*in meters*/, float filmHeight, Camera cam, bool resetHistory)
    {
        if (!enabled)
        {
            cmd.DisableShaderKeyword(Uniforms.AutoFocusKeyword);
            ResetHistory();
            return;
        }

        if (resetHistory)
            ResetHistory();

        cmd.BeginSample("AutoFocus");
        cmd.EnableShaderKeyword(Uniforms.AutoFocusKeyword);
        Focus(cmd, focalLength, filmHeight, cam);
        cmd.EndSample("AutoFocus");
    }

    public void ResetHistory()
    {
        m_ResetHistory = true;
    }

    bool PlayerVelocityUnderRunThreshold()
    {
        return m_CurrentSmoothedPlayerVelocity < m_MaxPlayerVelocity;
    }

    void OnDisable()
    {
        if (m_AutoFocusParamsCB != null)
            m_AutoFocusParamsCB.Release();
        m_AutoFocusParamsCB = null;

        if (m_AutoFocusOutputCB != null)
            m_AutoFocusOutputCB.Release();
        m_AutoFocusOutputCB = null;
    }

    public void UpdateVelocity(float velocity)
    {
        // Smoothing with exponential moving average
        // TODO: this is framerate-sensitive
        m_CurrentSmoothedPlayerVelocity = Mathf.Lerp(m_CurrentSmoothedPlayerVelocity, velocity, 0.2f);
    }
}

