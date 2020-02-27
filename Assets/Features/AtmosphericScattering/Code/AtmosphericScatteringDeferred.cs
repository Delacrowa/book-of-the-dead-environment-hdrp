using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

[ImageEffectAllowedInSceneView]
[ExecuteInEditMode]
[HDRPCallback]
public class AtmosphericScatteringDeferred : MonoBehaviour {
	[HideInInspector] public Shader deferredFogShader = null;

	Material    m_fogMaterial;
	
    void OnEnable() {		
		if(!deferredFogShader)
			deferredFogShader = Shader.Find("Hidden/AtmosphericScattering_Deferred");

		m_fogMaterial = new Material(deferredFogShader);
        m_fogMaterial.hideFlags = HideFlags.DontSave;
	}

	void OnDisable() {
		if(!Application.isPlaying)
            Object.DestroyImmediate(m_fogMaterial);
    }

	[HDRPCallbackMethod]
	static void AtmosphericScatteringDeferredSetup() {
		HDRenderPipeline.OnBeforeForwardOpaque += DoRender;
	}

	static void DoRender(ScriptableRenderContext renderContext, HDCamera hdCamera, PostProcessLayer ppLayer, RTHandleSystem.RTHandle depthHandle, RTHandleSystem.RTHandle colorHandle, CommandBuffer cmd) {
		var atmosphericDeferred = hdCamera.camera.GetComponent<global::AtmosphericScatteringDeferred>();
		if(atmosphericDeferred)
			atmosphericDeferred.RenderAtmosphericsPost(renderContext, hdCamera, ppLayer, depthHandle, colorHandle, cmd);
	}

	void RenderAtmosphericsPost(ScriptableRenderContext renderContext, HDCamera hdCamera, PostProcessLayer ppLayer, RTHandleSystem.RTHandle depthHandle, RTHandleSystem.RTHandle colorHandle, CommandBuffer cmd) {
		// Maybe we don't need this anymore?
		renderContext.ExecuteCommandBuffer(cmd);
		cmd.Clear();

		using(new ProfilingSample(cmd, "Atmospherics")) {
			// TODO: These custom effects need to go into the PP chain. As standalone, each of them have to do an additional blit.

			// Hack patch in main camera if looking for scattering when rendering reflection probe.
			var camera = hdCamera.camera;
			var realCamera = camera;
			Vector3 pos = default(Vector3), scale = default(Vector3);
			Quaternion rot = default(Quaternion);
			if(camera.cameraType == CameraType.Reflection) {
				camera = Camera.main;
				camera = camera ? camera : realCamera;
				pos = camera.transform.position;
				rot = camera.transform.rotation;
				scale = camera.transform.localScale;
				camera.transform.position = realCamera.transform.position;
				camera.transform.rotation = Quaternion.LookRotation(realCamera.worldToCameraMatrix.GetColumn(2), realCamera.worldToCameraMatrix.GetColumn(1));
				camera.transform.localScale = realCamera.transform.localScale;
				realCamera.transform.rotation = Quaternion.LookRotation(realCamera.worldToCameraMatrix.GetColumn(2), realCamera.worldToCameraMatrix.GetColumn(1));
			}

			// Render atmospherics if any
			var atmosphericScattering = global::AtmosphericScattering.instance;
			var atmosphericDeferred = camera.GetComponent<global::AtmosphericScatteringDeferred>();
			if(atmosphericScattering && atmosphericDeferred && atmosphericDeferred.enabled) {
				atmosphericScattering.RenderOcclusion(cmd, realCamera, hdCamera);
				RenderScattering(cmd, hdCamera, realCamera, depthHandle, colorHandle, colorHandle);
			}

			// Restore actual camera
			if(camera != realCamera) {
				realCamera.transform.rotation = Quaternion.identity;
				camera.transform.position = pos;
				camera.transform.rotation = rot;
				camera.transform.localScale = scale;
				camera = realCamera;
			}
		}
	}

	void RenderScattering(CommandBuffer cb, HDCamera hdCamera, Camera cam, RTHandleSystem.RTHandle depthHandle, RTHandleSystem.RTHandle srcHandle, RTHandleSystem.RTHandle dstHandle) {
        cb.BeginSample("AtmosphericScatteringBlit");
        
        CommandBufferBlit(cb, hdCamera, cam, depthHandle, srcHandle, dstHandle);

        cb.EndSample("AtmosphericScatteringBlit");
	}

	void CommandBufferBlit(CommandBuffer cb, HDCamera hdCamera, Camera cam, RTHandleSystem.RTHandle depthHandle, RTHandleSystem.RTHandle srcHandle, RTHandleSystem.RTHandle dstHandle) {
		if(AtmosphericScattering.instance && AtmosphericScattering.instance.useOcclusion && AtmosphericScattering.instance.debugMode == AtmosphericScattering.ScatterDebugMode.None) {
			cb.SetGlobalTexture("_CameraDepthTexture", depthHandle.rt);
			HDUtils.DrawFullScreen(cb, hdCamera, m_fogMaterial, dstHandle, null, 1);
		} else {
			var pongID = Shader.PropertyToID("_AtmosphericPong");
			cb.GetTemporaryRT(pongID, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf);
			cb.SetGlobalTexture("_MainTex", srcHandle.rt);
			cb.SetGlobalTexture("_CameraDepthTexture", depthHandle.rt);
			cb.SetRenderTarget(pongID);
			HDUtils.DrawFullScreen(cb, hdCamera, m_fogMaterial, pongID, null, 0);

			HDUtils.SetRenderTarget(cb, hdCamera, dstHandle);
			cb.SetGlobalTexture(HDShaderIDs._BlitTexture, pongID);
			cb.SetGlobalVector(HDShaderIDs._BlitScaleBias, new Vector4(1, 1, 0, 0));
			cb.SetGlobalFloat(HDShaderIDs._BlitMipLevel, 0);
			cb.DrawProcedural(Matrix4x4.identity, HDUtils.GetBlitMaterial(), 0, MeshTopology.Triangles, 3, 1);
		}
	}
}
