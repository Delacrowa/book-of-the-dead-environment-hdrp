using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<PlanarReflectionProbeUI, SerializedPlanarReflectionProbe>;

    partial class PlanarReflectionProbeUI
    {
        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionProbeModeSettings;
        public static readonly CED.IDrawer ProxyVolumeSettings = CED.FoldoutGroup(
                "Proxy Volume",
                (s, d, o) => s.isSectionExpendedProxyVolume,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionProxySettings)
                );
        public static readonly CED.IDrawer SectionProbeModeBakedSettings = CED.noop;
        public static readonly CED.IDrawer SectionProbeModeCustomSettings = CED.Action(Drawer_SectionProbeModeCustomSettings);
        public static readonly CED.IDrawer SectionProbeModeRealtimeSettings = CED.Action(Drawer_SectionProbeModeRealtimeSettings);
        public static readonly CED.IDrawer SectionBakeButton = CED.Action(Drawer_SectionBakeButton);

        public static readonly CED.IDrawer SectionFoldoutAdditionalSettings = CED.FoldoutGroup(
                "Artistic Settings",
                (s, d, o) => s.isSectionExpendedAdditionalSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionInfluenceSettings)
                );

        public static readonly CED.IDrawer SectionFoldoutCaptureSettings;

        public static readonly CED.IDrawer SectionCaptureMirrorSettings = CED.Action(Drawer_SectionCaptureMirror);
        public static readonly CED.IDrawer SectionCaptureStaticSettings = CED.Action(Drawer_SectionCaptureStatic);

        static PlanarReflectionProbeUI()
        {
            SectionFoldoutCaptureSettings = CED.FoldoutGroup(
                    "Capture Settings",
                    (s, d, o) => s.isSectionExpandedCaptureSettings,
                    FoldoutOption.Indent,
                    CED.Action(Drawer_SectionCaptureSettings),
                    CED.FadeGroup(
                        (s, d, o, i) =>
                        {
                            switch (i)
                            {
                                default:
                                case 0: return s.isSectionExpandedCaptureMirrorSettings;
                                case 1: return s.isSectionExpandedCaptureStaticSettings;
                            }
                        },
                        FadeOption.None,
                        SectionCaptureMirrorSettings,
                        SectionCaptureStaticSettings)
                    );

            SectionProbeModeSettings = CED.Group(
                    CED.Action(Drawer_FieldCaptureType),
                    CED.FadeGroup(
                        (s, d, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
                        FadeOption.Indent,
                        SectionProbeModeBakedSettings,
                        SectionProbeModeRealtimeSettings,
                        SectionProbeModeCustomSettings
                        )
                    );

            Inspector = CED.Group(
                    CED.Action(Drawer_Toolbar),
                    CED.space,
                    ProxyVolumeSettings,
                    CED.Select(
                        (s, d, o) => s.influenceVolume,
                        (s, d, o) => d.influenceVolume,
                        InfluenceVolumeUI.SectionFoldoutShape
                        ),
                    CED.Action(Drawer_DifferentShapeError),
                    SectionFoldoutCaptureSettings,
                    SectionFoldoutAdditionalSettings,
                    CED.Select(
                        (s, d, o) => s.frameSettings,
                        (s, d, o) => d.frameSettings,
                        FrameSettingsUI.Inspector
                        ),
                    CED.space,
                    CED.Action(Drawer_SectionBakeButton)
                    );
        }

        const EditMode.SceneViewEditMode EditBaseShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        const EditMode.SceneViewEditMode EditInfluenceShape = EditMode.SceneViewEditMode.GridBox;
        const EditMode.SceneViewEditMode EditInfluenceNormalShape = EditMode.SceneViewEditMode.Collider;
        const EditMode.SceneViewEditMode EditCenter = EditMode.SceneViewEditMode.ReflectionProbeOrigin;
        const EditMode.SceneViewEditMode EditMirrorPosition = EditMode.SceneViewEditMode.GridMove;
        const EditMode.SceneViewEditMode EditMirrorRotation = EditMode.SceneViewEditMode.GridSelect;

        static void Drawer_SectionCaptureStatic(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.captureLocalPosition, _.GetContent("Capture Local Position"));

            _.DrawMultipleFields(
                "Clipping Planes",
                new[] { d.captureNearPlane, d.captureFarPlane },
                new[] { _.GetContent("Near|The closest point relative to the camera that drawing will occur."), _.GetContent("Far|The furthest point relative to the camera that drawing will occur.\n") });
        }

        static void Drawer_SectionCaptureMirror(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            // EditorGUILayout.PropertyField(d.captureMirrorPlaneLocalPosition, _.GetContent("Plane Position"));
            // EditorGUILayout.PropertyField(d.captureMirrorPlaneLocalNormal, _.GetContent("Plane Normal"));
        }

        static void Drawer_DifferentShapeError(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            var proxy = d.proxyVolumeReference.objectReferenceValue as ReflectionProxyVolumeComponent;
            if (proxy != null && (int)proxy.proxyVolume.shapeType != d.influenceVolume.shapeType.enumValueIndex)
            {
                EditorGUILayout.HelpBox(
                    "Proxy volume and influence volume have different shape types, this is not supported.",
                    MessageType.Error,
                    true
                    );
            }
        }

        static void Drawer_SectionCaptureSettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            GUI.enabled = false;
            EditorGUILayout.LabelField(
                _.GetContent("Probe Texture Size (Set By HDRP)"),
                _.GetContent(hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize.ToString()),
                EditorStyles.label);
            EditorGUILayout.Toggle(
                _.GetContent("Probe Compression (Set By HDRP)"),
                hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(d.overrideFieldOfView, _.GetContent("Override FOV"));
            if (d.overrideFieldOfView.boolValue)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.fieldOfViewOverride, _.GetContent("Field Of View"));
                --EditorGUI.indentLevel;
            }
        }

        static void Drawer_SectionProbeModeCustomSettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            d.customTexture.objectReferenceValue = EditorGUILayout.ObjectField(_.GetContent("Capture"), d.customTexture.objectReferenceValue, typeof(Texture), false);
            var texture = d.customTexture.objectReferenceValue as Texture;
            if (texture != null && texture.dimension != TextureDimension.Tex2D)
                EditorGUILayout.HelpBox("Provided Texture is not a 2D Texture, it will be ignored", MessageType.Warning);
        }

        static void Drawer_SectionBakeButton(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)d.mode.intValue, d.target);
        }

        static void Drawer_SectionProbeModeRealtimeSettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(d.refreshMode, _.GetContent("Refresh Mode"));
            EditorGUILayout.PropertyField(d.capturePositionMode, _.GetContent("Capture Position Mode"));
            GUI.enabled = true;
        }

        static void Drawer_SectionProxySettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.proxyVolumeReference, _.GetContent("Reference"));

            if (d.proxyVolumeReference.objectReferenceValue != null)
            {
                var proxy = (ReflectionProxyVolumeComponent)d.proxyVolumeReference.objectReferenceValue;
                if ((int)proxy.proxyVolume.shapeType != d.influenceVolume.shapeType.enumValueIndex)
                    EditorGUILayout.HelpBox(
                        "Proxy volume and influence volume have different shape types, this is not supported.",
                        MessageType.Error,
                        true
                        );
            }
            else
            {
                EditorGUILayout.HelpBox(
                        "When no Proxy setted, Influence shape will be used as Proxy shape too.",
                        MessageType.Info,
                        true
                        );
            }
        }

        static void Drawer_SectionInfluenceSettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.weight, _.GetContent("Weight"));


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(d.multiplier, _.GetContent("Multiplier"));
            if (EditorGUI.EndChangeCheck())
                d.multiplier.floatValue = Mathf.Max(0.0f, d.multiplier.floatValue);
        }

        static void Drawer_FieldCaptureType(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(d.mode, _.GetContent("Type"));
            GUI.enabled = true;
        }

        static readonly EditMode.SceneViewEditMode[] k_Toolbar_SceneViewEditModes =
        {
            EditBaseShape,
            EditInfluenceShape,
            EditInfluenceNormalShape,
            //EditCenter
        };

        static readonly EditMode.SceneViewEditMode[] k_Toolbar_Static_SceneViewEditModes =
        {
            //EditCenter  //offset have no meanings with planar
        };
        static readonly EditMode.SceneViewEditMode[] k_Toolbar_Mirror_SceneViewEditModes =
        {
            //EditMirrorPosition,  //offset have no meanings with planar
            EditMirrorRotation
        };
        static GUIContent[] s_Toolbar_Contents = null;
        static GUIContent[] toolbar_Contents
        {
            get
            {
                return s_Toolbar_Contents ?? (s_Toolbar_Contents = new[]
                {
                    EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                    EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)"),
                    EditorGUIUtility.IconContent("SceneViewOrtho", "|Modify the influence normal volume. (SHIFT+3)"),
                });
            }
        }

        static GUIContent[] s_Toolbar_Static_Contents = null;
        static GUIContent[] toolbar_Static_Contents
        {
            get
            {
                return s_Toolbar_Static_Contents ?? (s_Toolbar_Static_Contents = new GUIContent[]
                {
                    //EditorGUIUtility.IconContent("MoveTool", "|Move the capture position.")   //offset have no meanings with planar
                });
            }
        }

        static GUIContent[] s_Toolbar_Mirror_Contents = null;
        static GUIContent[] toolbar_Mirror_Contents
        {
            get
            {
                return s_Toolbar_Mirror_Contents ?? (s_Toolbar_Mirror_Contents = new[]
                {
                    //EditorGUIUtility.IconContent("MoveTool", "|Move the mirror plane."),   //offset have no meanings with planar
                    EditorGUIUtility.IconContent("RotateTool", "|Rotate the mirror plane.")
                });
            }
        }

        static void Drawer_Toolbar(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;

            EditMode.DoInspectorToolbar(k_Toolbar_SceneViewEditModes, toolbar_Contents, GetBoundsGetter(o), o);

            if (d.isMirrored)
                EditMode.DoInspectorToolbar(k_Toolbar_Mirror_SceneViewEditModes, toolbar_Mirror_Contents, GetBoundsGetter(o), o);
            else
                EditMode.DoInspectorToolbar(k_Toolbar_Static_SceneViewEditModes, toolbar_Static_Contents, GetBoundsGetter(o), o);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static public void Drawer_ToolBarButton(int buttonIndex, Editor owner, params GUILayoutOption[] styles)
        {
            if (GUILayout.Button(toolbar_Contents[buttonIndex], styles))
            {
                EditMode.ChangeEditMode(k_Toolbar_SceneViewEditModes[buttonIndex], GetBoundsGetter(owner)(), owner);
            }
        }

        static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in o.targets)
                    {
                        var rp = targetObject.transform;
                        var b = rp.position;
                        bounds.Encapsulate(b);
                    }
                    return bounds;
                };
        }
    }
}
