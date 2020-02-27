
using UnityEngine;

namespace Gameplay {

public abstract class PlayerCamera : MonoBehaviour {
    public Transform eyeTransform;

    public new Camera camera {
        get { return _camera; }
        set {
            _camera = value;
            autoFocus = value.GetComponent<DepthOfFieldAutoFocus>();
        }
    }

    Camera _camera;

    public DepthOfFieldAutoFocus autoFocus { get; private set; }

    public virtual void OnSpawn(SpawnPoint spawnPoint) {
    }

    public virtual void Simulate(Vector3 playerPosition, Vector3 playerAngles, float deltaTime) {
    }

    public virtual void Warp(Vector3 position, Vector3 angles) {
    }
}

} // Gameplay

