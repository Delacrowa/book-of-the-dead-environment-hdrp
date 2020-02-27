using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

[ExecuteInEditMode]
public class AtmosphericScatteringSun : MonoBehaviour {
	public static AtmosphericScatteringSun instance;
	
	new public Transform	transform { get; private set; }
	new public Light		light { get { return m_light; } }

	Light m_light;

	void OnEnable() {
		//Debug.LogFormat("OnEnable: {0}: {1} / {2}", m_light ? m_light.commandBufferCount : -1, GetInstanceID(), name);

		if(instance) {
			Debug.LogErrorFormat("Not setting 'AtmosphericScatteringSun.instance' because '{0}' is already active!", instance.name);
			return;
		}

		this.transform = base.transform;
		m_light = GetComponent<Light>();
		instance = this;
	}

	void OnDisable() {
		if(instance == null) {
			Debug.LogErrorFormat("'AtmosphericScatteringSun.instance' is already null when disabling '{0}'!", this.name);
			return;
		}

		if(instance != this) {
			Debug.LogErrorFormat("Not UNsetting 'AtmosphericScatteringSun.instance' because it points to someone else '{0}'!", instance.name);
			return;
		}

		instance = null;
	}
}
