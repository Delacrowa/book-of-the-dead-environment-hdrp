
using UnityEngine.Experimental.Rendering;

public abstract class PropertyVolumeComponent<X> : PropertyVolumeComponentBase
        where X : PropertyVolumeComponent<X> {
    static PropertyVolumeComponent() {
        PropertyMaster.componentTypes.Add(typeof(X));
    }
}

