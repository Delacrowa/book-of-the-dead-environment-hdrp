
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace Gameplay {

public class GameController : MonoBehaviour {
    public static GameController FindGameController() {
        return GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
    }

    public SpawnPoint startPoint;
    public SpawnPoint lastPlayerSpawnPoint { get; private set; }

    public PlayerController playerPrefab;
    public PlayerCamera playerCameraPrefab;

    public PlayerController playerController { get; set; }
    public PlayerCamera playerCamera { get; set; }
    public Camera defaultCamera { get; set; }

    public Object debugContext { get; set; }

    public delegate void AudioTransformsUpdater(Transform root, Transform eye);
    public AudioTransformsUpdater audioTransformsUpdater;

    protected void Start() {
        defaultCamera = Camera.main;

        var cameraTransform = defaultCamera.transform;
        cameraTransform.localPosition = Vector3.zero;
        cameraTransform.localRotation = Quaternion.identity;
        cameraTransform.localScale = Vector3.one;

        PlayerInput.SelectInputMapping();
    }

    public void DespawnPlayer() {
        if (playerController)
            Destroy(playerController.gameObject);

        if (playerCamera)
            Destroy(playerCamera.gameObject);

        playerController = null;
        playerCamera = null;
    }

    public SpawnPoint[] GetAllSpawnPoints() {
        return
            (from a in GameAgent.GetAgents()
                where a is SpawnPoint
                select a as SpawnPoint).ToArray();
    }

	public SpawnPoint GetSpawnPoint(string name) {
        return SpawnPoint.Find(name);
    }

    public SpawnPoint GetNextPlayerSpawnPoint() {
        var spawnPoints = GetAllSpawnPoints();
        Array.Sort(spawnPoints, (x, y) => string.Compare(x.agentIdentifier, y.agentIdentifier));
        int index = Array.IndexOf(spawnPoints, lastPlayerSpawnPoint);
        return spawnPoints[(index + 1) % spawnPoints.Length];
    }

    public void SpawnPlayer(bool reset = true, bool nextFrame = true) {
        SpawnPlayer(startPoint, reset, nextFrame);
    }

    public void SpawnPlayer(SpawnPoint spawnPoint, bool reset = true, bool nextFrame = true) {
        StartCoroutine(SpawnPlayerCo(spawnPoint, reset, nextFrame));
    }

    IEnumerator SpawnPlayerCo(SpawnPoint spawnPoint, bool reset, bool nextFrame) {
        if (nextFrame)
            yield return null;

        Profiler.BeginSample("SpawnPlayerCo");

        lastPlayerSpawnPoint = spawnPoint;

        if (!playerPrefab) {
            Debug.LogError("Missing player prefab");
            yield break;
        }

        if (!playerController)
            playerController = Instantiate(playerPrefab);

        if (!playerCamera)
            playerCamera = Instantiate(playerCameraPrefab);

        for (var i = GameAgent.GetEnumerator(); i.MoveNext();)
            if (i.Current)
                i.Current.OnBeforeSpawnPlayer(reset);

        var camera = spawnPoint.camera ? spawnPoint.camera : defaultCamera;
        var cameraTransform = camera.transform;
        cameraTransform.parent =
            playerCamera.eyeTransform ? playerCamera.eyeTransform : playerCamera.transform;

        cameraTransform.localPosition = Vector3.zero;
        cameraTransform.localRotation = Quaternion.identity;
        cameraTransform.localScale = Vector3.one;

        playerCamera.camera = camera;
        playerController.playerCamera = playerCamera;

        if (audioTransformsUpdater != null)
            audioTransformsUpdater(playerController.transform, camera.transform);

        spawnPoint.Spawn(playerController, reset);

        for (var i = GameAgent.GetEnumerator(); i.MoveNext();)
            if (i.Current)
                i.Current.OnAfterSpawnPlayer(spawnPoint, reset);

        Profiler.EndSample();
    }
}

} // Gameplay

