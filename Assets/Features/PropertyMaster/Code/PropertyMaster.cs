
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

[ExecuteInEditMode]
public class PropertyMaster : MonoBehaviour, IExposedPropertyTable, ISerializationCallbackReceiver {
    internal static readonly HashSet<Type> componentTypes = new HashSet<Type>();

    public enum UpdateMode {
        Automatic,
        Manual
    }

    public UpdateMode updateMode;

    [Space(9)]
    public bool updateVolumes;
    public Transform volumeTrigger;
    public LayerMask volumeLayerMask = 0;

    [Serializable]
    struct ExposedReferenceData {
        public PropertyName name;
        public Object value;
    }

    [SerializeField, HideInInspector]
    List<ExposedReferenceData> _exposedReferenceList = new List<ExposedReferenceData>();
    Dictionary<PropertyName, Object> _exposedReferenceTable = new Dictionary<PropertyName, Object>();

    protected void OnEnable() {
		HDRenderPipeline.OnBeforeCameraCull += OnBeforeCameraCull;
    }

    protected void OnDisable() {
		HDRenderPipeline.OnBeforeCameraCull -= OnBeforeCameraCull;
    }

    void OnBeforeCameraCull(ScriptableRenderContext context, HDCamera hdCamera, FrameSettings settings, CommandBuffer cmd) {
        if (updateMode == UpdateMode.Automatic && (hdCamera.camera.cameraType == CameraType.SceneView || hdCamera.camera.cameraType == CameraType.Game))
            UpdateProperties();
    }

    public void UpdateProperties() {
        var manager = VolumeManager.instance;
        var stack = manager.stack;

        if (updateVolumes && volumeTrigger && volumeLayerMask != 0)
            manager.Update(volumeTrigger, volumeLayerMask);

        foreach (var type in componentTypes) {
            var component = (PropertyVolumeComponentBase) stack.GetComponent(type);

            if (component.active)
                component.OverrideProperties(this);
        }
    }

    public void OnBeforeSerialize() {
        _exposedReferenceList = new List<ExposedReferenceData>();

        foreach (var i in _exposedReferenceTable)
            _exposedReferenceList.Add(new ExposedReferenceData {name = i.Key, value = i.Value});
    }

    public void OnAfterDeserialize() {
        _exposedReferenceTable = new Dictionary<PropertyName, Object>();

        foreach (var i in _exposedReferenceList)
            _exposedReferenceTable.Add(i.name, i.value);
    }

    public void ClearReferenceValue(PropertyName name) {
        _exposedReferenceTable.Remove(name);
    }

    public Object GetReferenceValue(PropertyName name, out bool valid) {
        Object value;
        valid = _exposedReferenceTable.TryGetValue(name, out value);
        return value;
    }

    public void SetReferenceValue(PropertyName name, Object value) {
        _exposedReferenceTable[name] = value;
    }
}

