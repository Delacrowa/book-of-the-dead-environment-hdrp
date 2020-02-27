using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        [MenuItem("CONTEXT/Camera/Remove HD Camera", false, 0)]
        static void RemoveLight(MenuCommand menuCommand)
        {
            GameObject go = ((Camera)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Undo.SetCurrentGroupName("Remove HD Camera");
            Undo.DestroyObjectImmediate(go.GetComponent<Camera>());
            Undo.DestroyObjectImmediate(go.GetComponent<HDAdditionalCameraData>());
        }

        SerializedHDCamera m_SerializedCamera;
        HDCameraUI m_UIState = new HDCameraUI();

        RenderTexture m_PreviewTexture;
        Camera m_PreviewCamera;
        HDAdditionalCameraData m_PreviewAdditionalCameraData;
        PostProcessLayer m_PreviewPostProcessLayer;

        void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(serializedObject);
            m_UIState.Reset(m_SerializedCamera, Repaint);

            m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();
            m_PreviewCamera.enabled = false;
            m_PreviewCamera.cameraType = CameraType.Preview; // Must be init before adding HDAdditionalCameraData
            m_PreviewAdditionalCameraData = m_PreviewCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            // Say that we are a camera editor preview and not just a regular preview
            m_PreviewAdditionalCameraData.isEditorCameraPreview = true;
            m_PreviewPostProcessLayer = m_PreviewCamera.gameObject.AddComponent<PostProcessLayer>();
        }

        void OnDisable()
        {
            if (m_PreviewTexture != null)
            {
                m_PreviewTexture.Release();
                m_PreviewTexture = null;
            }
            DestroyImmediate(m_PreviewCamera.gameObject);
            m_PreviewCamera = null;
        }

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedCamera;

            d.Update();
            s.Update();

            HDCameraUI.Inspector.Draw(s, d, this);

            d.Apply();
        }

        RenderTexture GetPreviewTextureWithSize(int width, int height)
        {
            if (m_PreviewTexture == null || m_PreviewTexture.width != width || m_PreviewTexture.height != height)
            {
                if (m_PreviewTexture != null)
                    m_PreviewTexture.Release();

                m_PreviewTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_PreviewTexture.enableRandomWrite = true;
                m_PreviewTexture.Create();
            }
            return m_PreviewTexture;
        }
    }
}
