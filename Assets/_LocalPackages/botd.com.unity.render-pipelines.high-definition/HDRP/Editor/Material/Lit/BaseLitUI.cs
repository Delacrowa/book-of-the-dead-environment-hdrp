using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // This is the purpose of BaseLitGUI. It contain all properties that are common to all Material based on Lit template.
    // For the default hand written Lit material see LitUI.cs that contain specific properties for our default implementation.
    public abstract class BaseLitGUI : BaseUnlitGUI
    {
        protected static class StylesBaseLit
        {
            public static GUIContent doubleSidedNormalModeText = new GUIContent("Normal mode", "This will modify the normal base on the selected mode. Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal");
            public static GUIContent depthOffsetEnableText = new GUIContent("Enable Depth Offset", "EnableDepthOffset on this shader (Use with heightmap)");

            // Displacement mapping (POM, tessellation, per vertex)
            //public static GUIContent enablePerPixelDisplacementText = new GUIContent("Enable Per Pixel Displacement", "");

            public static GUIContent displacementModeText = new GUIContent("Displacement mode", "Apply heightmap displacement to the selected element: Vertex, pixel or tessellated vertex. Pixel displacement must be use with flat surfaces, it is an expensive features and typical usage is paved road.");
            public static GUIContent lockWithObjectScaleText = new GUIContent("Lock with object scale", "Displacement mapping will take the absolute value of the scale of the object into account.");
            public static GUIContent lockWithTilingRateText = new GUIContent("Lock with height map tiling rate", "Displacement mapping will take the absolute value of the tiling rate of the height map into account.");

            // Material ID
            public static GUIContent materialIDText = new GUIContent("Material type", "Select a material feature to enable on top of regular material");
            public static GUIContent transmissionEnableText = new GUIContent("Enable Transmission", "Enable Transmission for getting  back lighting");

            // Per pixel displacement
            public static GUIContent ppdMinSamplesText = new GUIContent("Minimum steps", "Minimum steps (texture sample) to use with per pixel displacement mapping");
            public static GUIContent ppdMaxSamplesText = new GUIContent("Maximum steps", "Maximum steps (texture sample) to use with per pixel displacement mapping");
            public static GUIContent ppdLodThresholdText = new GUIContent("Fading mip level start", "Starting heightmap mipmap lod number where the parallax occlusion mapping effect start to disappear");
            public static GUIContent ppdPrimitiveLength = new GUIContent("Primitive length", "Dimensions of the primitive (with the scale of 1) to which the per-pixel displacement mapping is being applied. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");
            public static GUIContent ppdPrimitiveWidth = new GUIContent("Primitive width", "Dimensions of the primitive (with the scale of 1) to which the per-pixel displacement mapping is being applied. For example, the standard quad is 1 x 1 meter, while the standard plane is 10 x 10 meters.");

            // Tessellation
            public static string tessellationModeStr = "Tessellation Mode";
            public static readonly string[] tessellationModeNames = Enum.GetNames(typeof(TessellationMode));

            public static GUIContent tessellationText = new GUIContent("Tessellation options", "Tessellation options");
            public static GUIContent tessellationFactorText = new GUIContent("Tessellation factor", "This value is the tessellation factor use for tessellation, higher mean more tessellated. Above 15 is costly. Maximum tessellation factor is 15 on XBone / PS4");
            public static GUIContent tessellationFactorMinDistanceText = new GUIContent("Start fade distance", "Distance (in unity unit) at which the tessellation start to fade out. Must be inferior at Max distance");
            public static GUIContent tessellationFactorMaxDistanceText = new GUIContent("End fade distance", "Maximum distance (in unity unit) to the camera where triangle are tessellated");
            public static GUIContent tessellationFactorTriangleSizeText = new GUIContent("Triangle size", "Desired screen space sized of triangle (in pixel). Smaller value mean smaller triangle.");
            public static GUIContent tessellationShapeFactorText = new GUIContent("Shape factor", "Strength of Phong tessellation shape (lerp factor)");
            public static GUIContent tessellationBackFaceCullEpsilonText = new GUIContent("Triangle culling Epsilon", "If -1.0 back face culling is enabled for tessellation, higher number mean more aggressive culling and better performance");

            // Vertex animation
            public static string vertexAnimation = "Vertex animation";

            // Wind
//forest-begin: Added vertex animation
            public static GUIContent windText = new GUIContent("Wind Mode");
//forest-end:
            public static GUIContent windInitialBendText = new GUIContent("Initial Bend");
            public static GUIContent windStiffnessText = new GUIContent("Stiffness");
            public static GUIContent windDragText = new GUIContent("Drag");
            public static GUIContent windShiverDragText = new GUIContent("Shiver Drag");
            public static GUIContent windShiverDirectionalityText = new GUIContent("Shiver Directionality");

            public static GUIContent supportDBufferText = new GUIContent("Enable Decal", "Allow to specify if the material can receive decal or not");

            public static GUIContent enableGeometricSpecularAAText = new GUIContent("Enable geometric specular AA", "This reduce specular aliasing on highly dense mesh (Particularly useful when they don't use normal map)");
            public static GUIContent specularAAScreenSpaceVarianceText = new GUIContent("Screen space variance", "Allow to control the strength of the specular AA reduction. Higher mean more blurry result and less aliasing");
            public static GUIContent specularAAThresholdText = new GUIContent("Threshold", "Allow to limit the effect of specular AA reduction. 0 mean don't apply reduction, higher value mean allow higher reduction");
        }

        public enum DoubleSidedNormalMode
        {
            Flip,
            Mirror,
            None
        }

        public enum TessellationMode
        {
            None,
            Phong
        }

        public enum DisplacementMode
        {
            None,
            Vertex,
            Pixel,
            Tessellation
        }

        public enum MaterialId
        {
            LitSSS = 0,
            LitStandard = 1,
            LitAniso = 2,
            LitIridescence = 3,
            LitSpecular = 4,
            LitTranslucent = 5
        };

        public enum HeightmapParametrization
        {
            MinMax = 0,
            Amplitude = 1
        }

        protected MaterialProperty doubleSidedNormalMode = null;
        protected const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";
        protected MaterialProperty depthOffsetEnable = null;
        protected const string kDepthOffsetEnable = "_DepthOffsetEnable";

        // Properties
        // Material ID
        protected MaterialProperty materialID  = null;
        protected const string kMaterialID = "_MaterialID";
        protected MaterialProperty transmissionEnable = null;
        protected const string kTransmissionEnable = "_TransmissionEnable";

        protected const string kStencilRef = "_StencilRef";
        protected const string kStencilWriteMask = "_StencilWriteMask";
        protected const string kStencilRefMV = "_StencilRefMV";
        protected const string kStencilWriteMaskMV = "_StencilWriteMaskMV";

        protected MaterialProperty displacementMode = null;
        protected const string kDisplacementMode = "_DisplacementMode";
        protected MaterialProperty displacementLockObjectScale = null;
        protected const string kDisplacementLockObjectScale = "_DisplacementLockObjectScale";
        protected MaterialProperty displacementLockTilingScale = null;
        protected const string kDisplacementLockTilingScale = "_DisplacementLockTilingScale";

        // Per pixel displacement params
        protected MaterialProperty ppdMinSamples = null;
        protected const string kPpdMinSamples = "_PPDMinSamples";
        protected MaterialProperty ppdMaxSamples = null;
        protected const string kPpdMaxSamples = "_PPDMaxSamples";
        protected MaterialProperty ppdLodThreshold = null;
        protected const string kPpdLodThreshold = "_PPDLodThreshold";
        protected MaterialProperty ppdPrimitiveLength = null;
        protected const string kPpdPrimitiveLength = "_PPDPrimitiveLength";
        protected MaterialProperty ppdPrimitiveWidth = null;
        protected const string kPpdPrimitiveWidth = "_PPDPrimitiveWidth";
        protected MaterialProperty invPrimScale = null;
        protected const string kInvPrimScale = "_InvPrimScale";
//forest-begin: Tree occlusion
        protected MaterialProperty treeOcclusion;
        protected MaterialProperty treeAO;
        protected MaterialProperty treeAOBias;
        protected MaterialProperty treeAO2;
        protected MaterialProperty treeAOBias2;
        protected MaterialProperty treeDO;
        protected MaterialProperty treeDOBias;
        protected MaterialProperty treeDO2;
        protected MaterialProperty treeDOBias2;
        protected MaterialProperty tree12Width;
//forest-end:

        // Wind
        protected MaterialProperty windEnable = null;
        protected const string kWindEnabled = "_EnableWind";
        protected MaterialProperty windInitialBend = null;
        protected const string kWindInitialBend = "_InitialBend";
        protected MaterialProperty windStiffness = null;
        protected const string kWindStiffness = "_Stiffness";
        protected MaterialProperty windDrag = null;
        protected const string kWindDrag = "_Drag";
        protected MaterialProperty windShiverDrag = null;
        protected const string kWindShiverDrag = "_ShiverDrag";
        protected MaterialProperty windShiverDirectionality = null;
        protected const string kWindShiverDirectionality = "_ShiverDirectionality";
//forest-begin: Added vertex animation
        protected MaterialProperty  windHeightScale;
        protected const string      kWindHeightScale        = "_WindHeightScale";
        protected MaterialProperty  windHeightIntensity;
        protected const string      kWindHeightIntensity    = "_WindHeightIntensity";
        protected MaterialProperty  windHeightSpeedScale;
        protected const string      kWindHeightSpeedScale   = "_WindHeightSpeed";
        protected MaterialProperty  windInnerRadius;
        protected const string      kWindInnerRadius        = "_WindInnerRadius";
        protected MaterialProperty  windRangeRadius;
        protected const string      kWindRangeRadius        = "_WindRangeRadius";
        protected MaterialProperty  windRadiusIntensity;
        protected const string      kWindRadiusIntensity    = "_WindRadiusIntensity";
        protected MaterialProperty  windRadiusSpeedScale;
        protected const string      kWindRadiusSpeedScale   = "_WindRadiusSpeed";
        protected MaterialProperty  windElasticityLvlB;
        protected const string      kWindElasticityLvlB     = "_WindElasticityLvlB";
        protected MaterialProperty  windElasticityLvl0;
        protected const string      kWindElasticityLvl0     = "_WindElasticityLvl0";
        protected MaterialProperty  windElasticityLvl1;
        protected const string      kWindElasticityLvl1     = "_WindElasticityLvl1";
        protected MaterialProperty  windRangeLvlB;
        protected const string      kWindRangeLvlB          = "_WindRangeLvlB";
        protected MaterialProperty  windRangeLvl0;
        protected const string      kWindRangeLvl0          = "_WindRangeLvl0";
        protected MaterialProperty  windRangeLvl1;
        protected const string      kWindRangeLvl1          = "_WindRangeLvl1";
        protected MaterialProperty  windFakeSingleObjectPivot;
        protected const string      kWindFakeSingleObjectPivot= "_WindFakeSingleObjectPivot";
        protected MaterialProperty  windFlutterElasticity;
        protected const string      kWindFlutterElasticity  = "_WindFlutterElasticity";
        protected MaterialProperty  windFlutterScale;
        protected const string      kWindFlutterScale       = "_WindFlutterScale";
        protected MaterialProperty  windFlutterPeriodScale;
        protected const string      kWindFlutterPeriodScale = "_WindFlutterPeriodScale";
        protected MaterialProperty  windFlutterPhase;
        protected const string      kWindFlutterPhase       = "_WindFlutterPhase";
//forest-end:
//forest-begin: Wind flutter map
        protected MaterialProperty  windFlutterMap          = null;
        protected const string      kWindFlutterMap         = "_WindFlutterMap";
//forest-end:

        // tessellation params
        protected MaterialProperty tessellationMode = null;
        protected const string kTessellationMode = "_TessellationMode";
        protected MaterialProperty tessellationFactor = null;
        protected const string kTessellationFactor = "_TessellationFactor";
        protected MaterialProperty tessellationFactorMinDistance = null;
        protected const string kTessellationFactorMinDistance = "_TessellationFactorMinDistance";
        protected MaterialProperty tessellationFactorMaxDistance = null;
        protected const string kTessellationFactorMaxDistance = "_TessellationFactorMaxDistance";
        protected MaterialProperty tessellationFactorTriangleSize = null;
        protected const string kTessellationFactorTriangleSize = "_TessellationFactorTriangleSize";
        protected MaterialProperty tessellationShapeFactor = null;
        protected const string kTessellationShapeFactor = "_TessellationShapeFactor";
        protected MaterialProperty tessellationBackFaceCullEpsilon = null;
        protected const string kTessellationBackFaceCullEpsilon = "_TessellationBackFaceCullEpsilon";

        // Decal
        protected MaterialProperty supportDBuffer = null;
        protected const string kSupportDBuffer = "_SupportDBuffer";
        protected MaterialProperty enableGeometricSpecularAA = null;
        protected const string kEnableGeometricSpecularAA = "_EnableGeometricSpecularAA";
        protected MaterialProperty specularAAScreenSpaceVariance = null;
        protected const string kSpecularAAScreenSpaceVariance = "_SpecularAAScreenSpaceVariance";
        protected MaterialProperty specularAAThreshold = null;
        protected const string kSpecularAAThreshold = "_SpecularAAThreshold";

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);

            doubleSidedNormalMode = FindProperty(kDoubleSidedNormalMode, props);
            depthOffsetEnable = FindProperty(kDepthOffsetEnable, props);

            // MaterialID
            materialID = FindProperty(kMaterialID, props);
            transmissionEnable = FindProperty(kTransmissionEnable, props);

            displacementMode = FindProperty(kDisplacementMode, props);
            displacementLockObjectScale = FindProperty(kDisplacementLockObjectScale, props);
            displacementLockTilingScale = FindProperty(kDisplacementLockTilingScale, props);

            // Per pixel displacement
            ppdMinSamples = FindProperty(kPpdMinSamples, props);
            ppdMaxSamples = FindProperty(kPpdMaxSamples, props);
            ppdLodThreshold = FindProperty(kPpdLodThreshold, props);
            ppdPrimitiveLength = FindProperty(kPpdPrimitiveLength, props);
            ppdPrimitiveWidth  = FindProperty(kPpdPrimitiveWidth, props);
            invPrimScale = FindProperty(kInvPrimScale, props);

            // tessellation specific, silent if not found
            tessellationMode = FindProperty(kTessellationMode, props, false);
            tessellationFactor = FindProperty(kTessellationFactor, props, false);
            tessellationFactorMinDistance = FindProperty(kTessellationFactorMinDistance, props, false);
            tessellationFactorMaxDistance = FindProperty(kTessellationFactorMaxDistance, props, false);
            tessellationFactorTriangleSize = FindProperty(kTessellationFactorTriangleSize, props, false);
            tessellationShapeFactor = FindProperty(kTessellationShapeFactor, props, false);
            tessellationBackFaceCullEpsilon = FindProperty(kTessellationBackFaceCullEpsilon, props, false);

//forest-begin: Tree occlusion
		    treeOcclusion = FindProperty("_UseTreeOcclusion", props, false);
		    treeAO = FindProperty("_TreeAO", props, false);
		    treeAOBias = FindProperty("_TreeAOBias", props, false);
		    treeAO2 = FindProperty("_TreeAO2", props, false);
		    treeAOBias2 = FindProperty("_TreeAOBias2", props, false);
		    treeDO = FindProperty("_TreeDO", props, false);
		    treeDOBias = FindProperty("_TreeDOBias", props, false);
		    treeDO2 = FindProperty("_TreeDO2", props, false);
		    treeDOBias2 = FindProperty("_TreeDOBias2", props, false);
		    tree12Width = FindProperty("_Tree12Width", props, false);
//forest-end:

            // Wind
            windEnable = FindProperty(kWindEnabled, props);
            windInitialBend = FindProperty(kWindInitialBend, props);
            windStiffness = FindProperty(kWindStiffness, props);
            windDrag = FindProperty(kWindDrag, props);
            windShiverDrag = FindProperty(kWindShiverDrag, props);
            windShiverDirectionality = FindProperty(kWindShiverDirectionality, props);

//forest-begin: Added vertex animation
            windHeightScale         = FindProperty(kWindHeightScale,        props, false);
            windHeightIntensity     = FindProperty(kWindHeightIntensity,    props, false);
            windHeightSpeedScale    = FindProperty(kWindHeightSpeedScale,   props, false);
            windInnerRadius         = FindProperty(kWindInnerRadius,        props, false);
            windRangeRadius         = FindProperty(kWindRangeRadius,        props, false);
            windRadiusIntensity     = FindProperty(kWindRadiusIntensity,    props, false);
            windRadiusSpeedScale    = FindProperty(kWindRadiusSpeedScale,   props, false);
            windElasticityLvlB      = FindProperty(kWindElasticityLvlB,     props, false);
            windElasticityLvl0      = FindProperty(kWindElasticityLvl0,     props, false);
            windElasticityLvl1      = FindProperty(kWindElasticityLvl1,     props, false);
            windRangeLvlB           = FindProperty(kWindRangeLvlB,          props, false);
            windRangeLvl0           = FindProperty(kWindRangeLvl0,          props, false);
            windRangeLvl1           = FindProperty(kWindRangeLvl1, props, false);
            windFakeSingleObjectPivot= FindProperty(kWindFakeSingleObjectPivot,props, false);
            windFlutterElasticity   = FindProperty(kWindFlutterElasticity,  props, false);
            windFlutterScale        = FindProperty(kWindFlutterScale,       props, false);
            windFlutterPeriodScale  = FindProperty(kWindFlutterPeriodScale, props, false);
            windFlutterPhase        = FindProperty(kWindFlutterPhase,       props, false);
//forest-end:
//forest-begin: Wind flutter map
            windFlutterMap          = FindProperty(kWindFlutterMap, props, false);
//forest-end:

            // Decal
            supportDBuffer = FindProperty(kSupportDBuffer, props);

            // specular AA
            enableGeometricSpecularAA = FindProperty(kEnableGeometricSpecularAA, props, false);
            specularAAScreenSpaceVariance = FindProperty(kSpecularAAScreenSpaceVariance, props, false);
            specularAAThreshold = FindProperty(kSpecularAAThreshold, props, false);
        }

        void TessellationModePopup()
        {
            EditorGUI.showMixedValue = tessellationMode.hasMixedValue;
            var mode = (TessellationMode)tessellationMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (TessellationMode)EditorGUILayout.Popup(StylesBaseLit.tessellationModeStr, (int)mode, StylesBaseLit.tessellationModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Tessellation Mode");
                tessellationMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        protected abstract void UpdateDisplacement();

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();

            EditorGUI.indentLevel++;

            // This follow double sided option
            if (doubleSidedEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(doubleSidedNormalMode, StylesBaseLit.doubleSidedNormalModeText);
                EditorGUI.indentLevel--;
            }

            m_MaterialEditor.ShaderProperty(materialID, StylesBaseLit.materialIDText);

            if ((int)materialID.floatValue == (int)BaseLitGUI.MaterialId.LitSSS)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(transmissionEnable, StylesBaseLit.transmissionEnableText);
                EditorGUI.indentLevel--;
            }

            m_MaterialEditor.ShaderProperty(supportDBuffer, StylesBaseLit.supportDBufferText);

            m_MaterialEditor.ShaderProperty(enableGeometricSpecularAA, StylesBaseLit.enableGeometricSpecularAAText);

            if (enableGeometricSpecularAA.floatValue > 0.0)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(specularAAScreenSpaceVariance, StylesBaseLit.specularAAScreenSpaceVarianceText);
                m_MaterialEditor.ShaderProperty(specularAAThreshold, StylesBaseLit.specularAAThresholdText);
                EditorGUI.indentLevel--;
            }

            m_MaterialEditor.ShaderProperty(enableMotionVectorForVertexAnimation, StylesBaseUnlit.enableMotionVectorForVertexAnimationText);

            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.ShaderProperty(displacementMode, StylesBaseLit.displacementModeText);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateDisplacement();
            }

            if ((DisplacementMode)displacementMode.floatValue != DisplacementMode.None)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(displacementLockObjectScale, StylesBaseLit.lockWithObjectScaleText);
                m_MaterialEditor.ShaderProperty(displacementLockTilingScale, StylesBaseLit.lockWithTilingRateText);
                EditorGUI.indentLevel--;
            }

            if ((DisplacementMode)displacementMode.floatValue == DisplacementMode.Pixel)
            {
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(ppdMinSamples, StylesBaseLit.ppdMinSamplesText);
                m_MaterialEditor.ShaderProperty(ppdMaxSamples, StylesBaseLit.ppdMaxSamplesText);
                ppdMinSamples.floatValue = Mathf.Min(ppdMinSamples.floatValue, ppdMaxSamples.floatValue);
                m_MaterialEditor.ShaderProperty(ppdLodThreshold, StylesBaseLit.ppdLodThresholdText);
                m_MaterialEditor.ShaderProperty(ppdPrimitiveLength, StylesBaseLit.ppdPrimitiveLength);
                ppdPrimitiveLength.floatValue = Mathf.Max(0.01f, ppdPrimitiveLength.floatValue);
                m_MaterialEditor.ShaderProperty(ppdPrimitiveWidth, StylesBaseLit.ppdPrimitiveWidth);
                ppdPrimitiveWidth.floatValue = Mathf.Max(0.01f, ppdPrimitiveWidth.floatValue);
                invPrimScale.vectorValue = new Vector4(1.0f / ppdPrimitiveLength.floatValue, 1.0f / ppdPrimitiveWidth.floatValue); // Precompute
                m_MaterialEditor.ShaderProperty(depthOffsetEnable, StylesBaseLit.depthOffsetEnableText);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            // Display tessellation option if it exist
            if (tessellationMode != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(StylesBaseLit.tessellationText, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                TessellationModePopup();
                m_MaterialEditor.ShaderProperty(tessellationFactor, StylesBaseLit.tessellationFactorText);
                m_MaterialEditor.ShaderProperty(tessellationFactorMinDistance, StylesBaseLit.tessellationFactorMinDistanceText);
                m_MaterialEditor.ShaderProperty(tessellationFactorMaxDistance, StylesBaseLit.tessellationFactorMaxDistanceText);
                // clamp min distance to be below max distance
                tessellationFactorMinDistance.floatValue = Math.Min(tessellationFactorMaxDistance.floatValue, tessellationFactorMinDistance.floatValue);
                m_MaterialEditor.ShaderProperty(tessellationFactorTriangleSize, StylesBaseLit.tessellationFactorTriangleSizeText);
                if ((TessellationMode)tessellationMode.floatValue == TessellationMode.Phong)
                {
                    m_MaterialEditor.ShaderProperty(tessellationShapeFactor, StylesBaseLit.tessellationShapeFactorText);
                }
                if (doubleSidedEnable.floatValue == 0.0)
                {
                    m_MaterialEditor.ShaderProperty(tessellationBackFaceCullEpsilon, StylesBaseLit.tessellationBackFaceCullEpsilonText);
                }
                EditorGUI.indentLevel--;
            }
        }

        protected override void VertexAnimationPropertiesGUI()
        {
            EditorGUILayout.LabelField(StylesBaseLit.vertexAnimation, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            m_MaterialEditor.ShaderProperty(windEnable, StylesBaseLit.windText);
            if (!windEnable.hasMixedValue && windEnable.floatValue > 0.0f)
            {
                EditorGUI.indentLevel++;
//forest-begin: Added vertex animation
                if(windEnable.floatValue < 1.5f) {
                    m_MaterialEditor.ShaderProperty(windInitialBend, StylesBaseLit.windInitialBendText);
                    m_MaterialEditor.ShaderProperty(windStiffness, StylesBaseLit.windStiffnessText);
                    m_MaterialEditor.ShaderProperty(windDrag, StylesBaseLit.windDragText);
                    m_MaterialEditor.ShaderProperty(windShiverDrag, StylesBaseLit.windShiverDragText);
                    m_MaterialEditor.ShaderProperty(windShiverDirectionality, StylesBaseLit.windShiverDirectionalityText);
                } else if(windHeightScale != null) {
                    if(windEnable.floatValue > 1.5f && windEnable.floatValue < 4.5f) {
                        EditorGUILayout.Space();
                        m_MaterialEditor.ShaderProperty(windElasticityLvlB, windElasticityLvlB.displayName);
                        m_MaterialEditor.ShaderProperty(windRangeLvlB, windRangeLvlB.displayName);
                    }

                    if(windEnable.floatValue == 4f) {
                        m_MaterialEditor.ShaderProperty(windElasticityLvl0, windElasticityLvl0.displayName);
                        m_MaterialEditor.ShaderProperty(windRangeLvl0, windRangeLvl0.displayName);
                        m_MaterialEditor.ShaderProperty(windElasticityLvl1, windElasticityLvl1.displayName);
                        m_MaterialEditor.ShaderProperty(windRangeLvl1, windRangeLvl1.displayName);
                    }

                    if(windEnable.floatValue == 6f)
                        m_MaterialEditor.ShaderProperty(windFakeSingleObjectPivot, windFakeSingleObjectPivot.displayName);

                    if(windInnerRadius != null && windEnable.floatValue > 5.5f) {
                        EditorGUILayout.Space();
                        m_MaterialEditor.ShaderProperty(windElasticityLvlB, windElasticityLvlB.displayName);
                        m_MaterialEditor.ShaderProperty(windRangeLvlB, windRangeLvlB.displayName);
                    }

                    EditorGUILayout.Space();
                    m_MaterialEditor.ShaderProperty(windFlutterElasticity, windFlutterElasticity.displayName);
                    m_MaterialEditor.ShaderProperty(windFlutterPhase, windFlutterPhase.displayName);
                    m_MaterialEditor.ShaderProperty(windFlutterScale, windFlutterScale.displayName);
                    m_MaterialEditor.ShaderProperty(windFlutterPeriodScale, windFlutterPeriodScale.displayName);
                }
                //forest-end:

                //forest-begin: Tree occlusion
                if(windEnable.floatValue > 1.5f && windEnable.floatValue < 4.5f) {
                    if(treeOcclusion != null) {
                        EditorGUILayout.Space();
                        var useTreeOcclusion = treeOcclusion.floatValue > 0.5f;
                        var newUseTreeOcclusion = EditorGUILayout.Toggle("Tree Occlusion", useTreeOcclusion);
                        if(newUseTreeOcclusion != useTreeOcclusion)
                            treeOcclusion.floatValue = newUseTreeOcclusion ? 1f : 0f;

                        if(useTreeOcclusion) {
                            ++EditorGUI.indentLevel;
                            m_MaterialEditor.ShaderProperty(treeAO, treeAO.displayName);
                            m_MaterialEditor.ShaderProperty(treeAOBias, treeAOBias.displayName);
                            m_MaterialEditor.ShaderProperty(treeAO2, treeAO2.displayName);
                            m_MaterialEditor.ShaderProperty(treeAOBias2, treeAOBias2.displayName);
                            m_MaterialEditor.ShaderProperty(treeDO, treeDO.displayName);
                            m_MaterialEditor.ShaderProperty(treeDOBias, treeDOBias.displayName);
                            m_MaterialEditor.ShaderProperty(treeDO2, treeDO2.displayName);
                            m_MaterialEditor.ShaderProperty(treeDOBias2, treeDOBias2.displayName);
                            m_MaterialEditor.ShaderProperty(tree12Width, tree12Width.displayName);
                            --EditorGUI.indentLevel;
                        }
                    }
                }
//forest-end:
                EditorGUI.indentLevel--;
            }
//forest-begin: Wind flutter map
            if(windFlutterMap != null)
                m_MaterialEditor.TexturePropertySingleLine(new GUIContent(windFlutterMap.displayName), windFlutterMap);
//forest-end:

            EditorGUI.indentLevel--;
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupBaseLitKeywords(Material material)
        {
            SetupBaseUnlitKeywords(material);

            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;

            if (doubleSidedEnable)
            {
                DoubleSidedNormalMode doubleSidedNormalMode = (DoubleSidedNormalMode)material.GetFloat(kDoubleSidedNormalMode);
                switch (doubleSidedNormalMode)
                {
                    case DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.None: // None mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;
                }
            }

            // Set the reference value for the stencil test.
            int stencilRef = (int)StencilLightingUsage.RegularLighting;
            if ((int)material.GetFloat(kMaterialID) == (int)BaseLitGUI.MaterialId.LitSSS)
            {
                stencilRef = (int)StencilLightingUsage.SplitLighting;
            }
            // As we tag both during velocity pass and Gbuffer pass we need a separate state and we need to use the write mask
            material.SetInt(kStencilRef, stencilRef);
            material.SetInt(kStencilWriteMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
            material.SetInt(kStencilRefMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);
            material.SetInt(kStencilWriteMaskMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);

            bool enableDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) != DisplacementMode.None;
            bool enableVertexDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Vertex;
            bool enablePixelDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Pixel;
            bool enableTessellationDisplacement = ((DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Tessellation) && material.HasProperty(kTessellationMode);

            CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT", enableVertexDisplacement);
            CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT", enablePixelDisplacement);
            // Only set if tessellation exist
            CoreUtils.SetKeyword(material, "_TESSELLATION_DISPLACEMENT", enableTessellationDisplacement);

            bool displacementLockObjectScale = material.GetFloat(kDisplacementLockObjectScale) > 0.0;
            bool displacementLockTilingScale = material.GetFloat(kDisplacementLockTilingScale) > 0.0;
            // Tessellation reuse vertex flag.
            CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && (enableVertexDisplacement || enableTessellationDisplacement));
            CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && enablePixelDisplacement);
            CoreUtils.SetKeyword(material, "_DISPLACEMENT_LOCK_TILING_SCALE", displacementLockTilingScale && enableDisplacement);

//forest-begin: Added vertex animation
			if (material.HasProperty(kWindEnabled))
			{
				var windMode = material.GetFloat(kWindEnabled);
				CoreUtils.SetKeyword(material, "_ANIM_SINGLE_PIVOT_COLOR",   windMode > 2.5f && windMode < 3.5f);
				CoreUtils.SetKeyword(material, "_ANIM_HIERARCHY_PIVOT",      windMode > 3.5f && windMode < 4.5f);
	            CoreUtils.SetKeyword(material, "_ANIM_PROCEDURAL_BRANCH",    windMode > 5.5f && windMode < 6.5f);

//forest-begin: G-Buffer motion vectors
				if(windMode > 2.5f) {
					var hdrpa = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
					if(hdrpa && hdrpa.GetFrameSettings().enableGBufferMotionVectors) {
						material.SetInt(kStencilRef, stencilRef | (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);
						material.SetInt(kStencilWriteMask, (int)HDRenderPipeline.StencilBitMask.LightingMask | (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);
					}
				}
//forest-end:
			}
//forest-end:


            // Depth offset is only enabled if per pixel displacement is
            bool depthOffsetEnable = (material.GetFloat(kDepthOffsetEnable) > 0.0f) && enablePixelDisplacement;
            CoreUtils.SetKeyword(material, "_DEPTHOFFSET_ON", depthOffsetEnable);

            if (material.HasProperty(kTessellationMode))
            {
                TessellationMode tessMode = (TessellationMode)material.GetFloat(kTessellationMode);
                CoreUtils.SetKeyword(material, "_TESSELLATION_PHONG", tessMode == TessellationMode.Phong);
            }

            SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor", material);


            // Use negation so we don't create keyword by default
            CoreUtils.SetKeyword(material, "_DISABLE_DBUFFER", material.GetFloat(kSupportDBuffer) == 0.0);

            CoreUtils.SetKeyword(material, "_ENABLE_GEOMETRIC_SPECULAR_AA", material.GetFloat(kEnableGeometricSpecularAA) == 1.0);
        }

        static public void SetupBaseLitMaterialPass(Material material)
        {
            SetupBaseUnlitMaterialPass(material);
        }
    }
} // namespace UnityEditor
