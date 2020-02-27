using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderPipelineSettings define settings that can't be change during runtime. It is equivalent to the GraphicsSettings of Unity (Tiers + shader variant removal).
    // This allow to allocate resource or not for a given feature.
    // FrameSettings control within a frame what is enable or not(enableShadow, enableStereo, enableDistortion...).
    // HDRenderPipelineAsset reference the current RenderPipelineSettings used, there is one per supported platform(Currently this feature is not implemented and only one GlobalFrameSettings is available).
    // A Camera with HDAdditionalData has one FrameSettings that configures how it will render. For example a camera used for reflection will disable distortion and post-process.
    // Additionally, on a Camera there is another FrameSettings called ActiveFrameSettings that is created on the fly based on FrameSettings and allows modifications for debugging purpose at runtime without being serialized on disk.
    // The ActiveFrameSettings is registered in the debug windows at the creation of the camera.
    // A Camera with HDAdditionalData has a RenderPath that defines if it uses a "Default" FrameSettings, a preset of FrameSettings or a custom one.
    // HDRenderPipelineAsset contains a "Default" FrameSettings that can be referenced by any camera with RenderPath.Defaut or when the camera doesn't have HDAdditionalData like the camera of the Editor.
    // It also contains a DefaultActiveFrameSettings

    // RenderPipelineSettings represents settings that are immutable at runtime.
    // There is a dedicated RenderPipelineSettings for each platform
    [Serializable]
    public class RenderPipelineSettings
    {
        // Lighting
        public bool supportShadowMask = true;
        public bool supportSSR = true;
        public bool supportSSAO = true;
        public bool supportSubsurfaceScattering = true;
        [FormerlySerializedAs("supportForwardOnly")]
        public bool supportOnlyForward = false;
        public bool increaseSssSampleCount = false;
        public bool supportVolumetrics = true;
        public bool increaseResolutionOfVolumetrics = false;

        // reduce variant
        public bool supportRuntimeDebugDisplay = true;
        public bool supportDitheringCrossFade = true;

        // Engine
        public bool         supportDBuffer = false;
        public bool         supportMSAA = false;
        public MSAASamples  msaaSampleCount = MSAASamples.None;
        public bool         supportMotionVectors = true;
        public bool         supportStereo = false;

        public GlobalLightLoopSettings  lightLoopSettings = new GlobalLightLoopSettings();
        public ShadowInitParameters     shadowInitParams = new ShadowInitParameters();
        public GlobalDecalSettings      decalSettings = new GlobalDecalSettings();
    }
}
