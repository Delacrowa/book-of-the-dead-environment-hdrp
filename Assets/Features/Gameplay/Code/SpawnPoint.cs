
using UnityEngine;

namespace Gameplay {

public class SpawnPoint : GameAgent {
    public static new SpawnPoint Find(string id) {
        return Find<SpawnPoint>(id);
    }

    public bool randomizePosition;
    public LayerMask snapLayers = (1 << 0) | (1 << 15);
    public new Camera camera;

    public void Spawn(GameAgent agent, bool reset) {
        if (reset)
            agent.gameObject.SetActive(false);

        SetPositionAndRotation(agent.transform);

        if (reset)
            agent.gameObject.SetActive(true);

        agent.OnSpawn(this, reset);
    }

    bool SetPositionAndRotation(Transform agentTransform) {
        var offset =
            randomizePosition ?
            Random.insideUnitCircle * transform.localScale.y :
            Vector2.zero;

        if (SnapToFloor(agentTransform, offset)) {
            agentTransform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, 0f);
            return true;
        }

        agentTransform.localRotation = transform.localRotation;
        return false;
    }

    bool SnapToFloor(Transform agentTransform, Vector3 offset) {
        var position = transform.localPosition + new Vector3(offset.x, 2f, offset.y);
        RaycastHit hit;

        if (Physics.Raycast(position, Vector3.down, out hit, 4f, snapLayers)) {
            agentTransform.localPosition = hit.point;
            return true;
        }

        Debug.LogWarning("SnapToFloor: Failed to to snap " + agentTransform + " at " + this, this);
        agentTransform.localPosition = position;
        return false;
    }
}

} // Gameplay

