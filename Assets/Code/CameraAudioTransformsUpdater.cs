
using UnityEngine;
using AxelF;
using Gameplay;

public class CameraAudioTransformsUpdater : MonoBehaviour {
    protected void Awake() {
        var gameController = GameObject.FindWithTag("GameController").GetComponent<GameController>();
        if (gameController)
            gameController.audioTransformsUpdater = (player, eye) => {
                Heartbeat.playerTransform = player;
                Heartbeat.listenerTransform = eye;
            };
    }
}

