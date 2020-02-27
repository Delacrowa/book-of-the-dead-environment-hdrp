
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay {

public abstract class GameAgent : MonoBehaviour {
    static readonly HashSet<GameAgent> agents = new HashSet<GameAgent>();
    static readonly Dictionary<string, GameAgent> lookup = new Dictionary<string, GameAgent>();

    public static IEnumerable<GameAgent> GetAgents() {
        return agents;
    }

    public static HashSet<GameAgent>.Enumerator GetEnumerator() {
        return agents.GetEnumerator();
    }

    public static GameAgent Find(string id) {
        GameAgent agent = null;
        if (!string.IsNullOrEmpty(id)) {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                foreach (var i in FindObjectsOfType<GameAgent>())
                    if (i.agentIdentifier == id)
                        return i;
#endif
            lookup.TryGetValue(id, out agent);
        }
        return agent;
    }

    public static T Find<T>(string id) where T : GameAgent {
        return (T) Find(id);
    }

    public string agentIdentifier;

    protected void Awake() {
        agents.Add(this);

        if (!string.IsNullOrEmpty(agentIdentifier)) {
            try {
                lookup.Add(agentIdentifier, this);
            } catch (ArgumentException e) {
                if (!lookup[agentIdentifier])
                    lookup[agentIdentifier] = this;
                else
                    Debug.LogException(e);
            }
        }
    }

    protected void OnDestroy() {
        agents.Remove(this);

        if (!string.IsNullOrEmpty(agentIdentifier))
            lookup.Remove(agentIdentifier);
    }

    public virtual void OnBeforeSpawnPlayer(bool reset) {
    }

    public virtual void OnAfterSpawnPlayer(SpawnPoint point, bool reset) {
    }

    public virtual void OnSpawn(SpawnPoint spawnPoint, bool reset) {
    }

    public override string ToString() {
        if (!string.IsNullOrEmpty(agentIdentifier))
            return string.Format("{0} '{1}'", base.ToString(), agentIdentifier);
        return base.ToString();
    }
}

} // Gameplay

