using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    public class SerializedFrameSettings
    {
        public SerializedProperty root;

        public SerializedProperty enableShadow;
        public SerializedProperty enableContactShadow;
        public SerializedProperty enableSSR;
        public SerializedProperty enableSSAO;
        public SerializedProperty enableSubsurfaceScattering;
        public SerializedProperty enableTransmission;
        public SerializedProperty enableAtmosphericScattering;
        public SerializedProperty enableVolumetric;

        public SerializedProperty diffuseGlobalDimmer;
        public SerializedProperty specularGlobalDimmer;

        public SerializedProperty enableForwardRenderingOnly;
        public SerializedProperty enableDepthPrepassWithDeferredRendering;

        public SerializedProperty enableTransparentPrepass;
        public SerializedProperty enableMotionVectors;
        public SerializedProperty enableObjectMotionVectors;
//forest-begin: G-Buffer motion vectors
		public SerializedProperty enableGBufferMotionVectors;
//forest-end:
        public SerializedProperty enableDBuffer;
        public SerializedProperty enableRoughRefraction;
        public SerializedProperty enableTransparentPostpass;
        public SerializedProperty enableDistortion;
        public SerializedProperty enablePostprocess;

        public SerializedProperty enableStereo;
        public SerializedProperty enableAsyncCompute;

        public SerializedProperty enableOpaqueObjects;
        public SerializedProperty enableTransparentObjects;

        public SerializedProperty enableMSAA;

        public SerializedProperty enableShadowMask;
//forest-begin: Explicit reflection probe tracking
		public SerializedProperty disableReflectionProbeCulling;
//forest-end:

//forest-begin: customizable sorting flags
		public SerializedProperty sortFlagsDepthPrepass;
		public SerializedProperty sortFlagsGBuffer;
		public SerializedProperty sortFlagsForward;
		public SerializedProperty sortFlagsObjectMotionVectors;
//forest-end:

        public SerializedLightLoopSettings lightLoopSettings;


        public SerializedFrameSettings(SerializedProperty root)
        {
            this.root = root;

            enableShadow = root.Find((FrameSettings d) => d.enableShadow);
            enableContactShadow = root.Find((FrameSettings d) => d.enableContactShadows);
            enableSSR = root.Find((FrameSettings d) => d.enableSSR);
            enableSSAO = root.Find((FrameSettings d) => d.enableSSAO);
            enableSubsurfaceScattering = root.Find((FrameSettings d) => d.enableSubsurfaceScattering);
            enableTransmission = root.Find((FrameSettings d) => d.enableTransmission);
            enableAtmosphericScattering = root.Find((FrameSettings d) => d.enableAtmosphericScattering);
            enableVolumetric = root.Find((FrameSettings d) => d.enableVolumetrics);
            diffuseGlobalDimmer = root.Find((FrameSettings d) => d.diffuseGlobalDimmer);
            specularGlobalDimmer = root.Find((FrameSettings d) => d.specularGlobalDimmer);
            enableForwardRenderingOnly = root.Find((FrameSettings d) => d.enableForwardRenderingOnly);
            enableDepthPrepassWithDeferredRendering = root.Find((FrameSettings d) => d.enableDepthPrepassWithDeferredRendering);
            enableTransparentPrepass = root.Find((FrameSettings d) => d.enableTransparentPrepass);
            enableMotionVectors = root.Find((FrameSettings d) => d.enableMotionVectors);
            enableObjectMotionVectors = root.Find((FrameSettings d) => d.enableObjectMotionVectors);
//forest-begin: G-Buffer motion vectors
			enableGBufferMotionVectors = root.Find((FrameSettings d) => d.enableGBufferMotionVectors);
//forest-end:
            enableDBuffer = root.Find((FrameSettings d) => d.enableDBuffer);
            enableRoughRefraction = root.Find((FrameSettings d) => d.enableRoughRefraction);
            enableTransparentPostpass = root.Find((FrameSettings d) => d.enableTransparentPostpass);
            enableDistortion = root.Find((FrameSettings d) => d.enableDistortion);
            enablePostprocess = root.Find((FrameSettings d) => d.enablePostprocess);
            enableStereo = root.Find((FrameSettings d) => d.enableStereo);
            enableAsyncCompute = root.Find((FrameSettings d) => d.enableAsyncCompute);
            enableOpaqueObjects = root.Find((FrameSettings d) => d.enableOpaqueObjects);
            enableTransparentObjects = root.Find((FrameSettings d) => d.enableTransparentObjects);
            enableMSAA = root.Find((FrameSettings d) => d.enableMSAA);
            enableShadowMask = root.Find((FrameSettings d) => d.enableShadowMask);
//forest-begin: Explicit reflection probe tracking
			disableReflectionProbeCulling = root.Find((FrameSettings d) => d.disableReflectionProbeCulling);
//forest-end:

//forest-begin: customizable sorting flags
			sortFlagsDepthPrepass = root.Find((FrameSettings d) => d.sortFlagsDepthPrepass);
			sortFlagsGBuffer = root.Find((FrameSettings d) => d.sortFlagsGBuffer);
			sortFlagsForward = root.Find((FrameSettings d) => d.sortFlagsForward);
			sortFlagsObjectMotionVectors = root.Find((FrameSettings d) => d.sortFlagsObjectMotionVectors);
//forest-end:

            lightLoopSettings = new SerializedLightLoopSettings(root.Find((FrameSettings d) => d.lightLoopSettings));
        }
    }
}
