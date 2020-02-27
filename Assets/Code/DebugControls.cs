
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Playables;
using Gameplay;

[DisallowMultipleComponent]
public class DebugControls : MonoBehaviour {
    public PlayableDirector playableDirector;
    public Transform animatedTransform;
    [Tooltip("Spin camera (for performance testing)")]
    public bool rotateCamera = false;
    public float angularSpeed = 45f;
    public float scrubSpeed = 5f;

    public enum EditorOptionsMode {
        Default, // same behaviour as player build
        Latent, // hidden on start but can be shown by user
        Disabled // completely disabled
    }

    public EditorOptionsMode editorOptionsMode = EditorOptionsMode.Latent;

    public bool emulateGameViewFocus = true;
    bool _emulatingGameViewFocus;

    public bool startInTimelineMode = false;
    bool _startedInTimelineMode;

    public enum Control {
        None,
        Options,
        Gameplay,
        Timeline
    }

    public Control control { get; set; }
    GameController _gameController;

    protected IEnumerator Start() {
        do _gameController = GameController.FindGameController();
        while (!_gameController);

        _gameController.debugContext = this;
        yield break;
    }

    protected void OnDestroy() {
        if (_gameController && _gameController.debugContext == this)
            _gameController.debugContext = null;
    }

    public void AssumeControl() {
        if (Application.isEditor && editorOptionsMode != EditorOptionsMode.Default) {
            control = DebugControls.Control.Gameplay;
            UIController.view = UIView.Gameplay;
        } else {
            control = DebugControls.Control.Options;
            UIController.view = UIView.Options;
        }
    }

    public void EnableBenchmarkMode() {
        startInTimelineMode = true;
        rotateCamera = true;
    }

    bool _oldToggle;
    bool _oldSpawn;

    float _selectTimer;

    protected void Update() {
        if (!playableDirector || !_gameController)
            return;

        bool intercepted = DebugManager.instance.displayRuntimeUI;
        bool toggle = false;
        bool play = false;
        bool pause = false;
        bool spawn = false;
        bool start = false;
        float scalar = 0f;

#if UNITY_EDITOR
        if (emulateGameViewFocus) {
            if (Input.GetKeyDown(KeyCode.Escape))
                _emulatingGameViewFocus = true;

            if (Input.GetMouseButtonDown(0))
                _emulatingGameViewFocus = false;
        }
#endif

        if (intercepted) {
            // do nothing
        } else if (PlayerInput.mapping == PlayerInputMapping.MouseAndKeyboard) {
            toggle = Input.GetKeyDown(KeyCode.Tab);
            play = Input.GetKeyDown(KeyCode.Space);
            pause = play;
            spawn = Input.GetKeyDown(KeyCode.Return);
            start = Input.GetKeyDown(KeyCode.Escape);
            scalar =
                Input.GetKey(KeyCode.LeftArrow) ? +1f :
                Input.GetKey(KeyCode.RightArrow) ? -1f : 0f;
        } else if (PlayerInput.mapping == PlayerInputMapping.PlayStation) {
            toggle = Input.GetAxis("PSDPadY") >= 1f;
            play = Input.GetButtonDown("PSCross");
            pause = Input.GetButtonDown("PSCircle");
            spawn = Input.GetAxis("PSDPadY") <= -1f;
            start = Input.GetButtonDown("PSOptions");
            scalar = Input.GetAxis("PSRStickY");
        } else if (PlayerInput.mapping == PlayerInputMapping.PlayStationForWindows) {
            toggle = Input.GetAxis("PSWinDPadY") >= 1f;
            play = Input.GetButtonDown("PSWinCross");
            pause = Input.GetButtonDown("PSWinCircle");
            spawn = Input.GetAxis("PSWinDPadY") <= -1f;
            start = Input.GetButtonDown("PSWinOptions");
            scalar = Input.GetAxis("PSWinRStickY");
        } else if (PlayerInput.mapping == PlayerInputMapping.Xbox) {
            toggle = Input.GetAxis("XboxDPadY") >= 1f;
            play = Input.GetButtonDown("XboxButtonA");
            pause = Input.GetButtonDown("XboxButtonB");
            spawn = Input.GetAxis("XboxDPadY") <= -1f;
            start = Input.GetButtonDown("XboxStart");
            scalar = Input.GetAxis("XboxRStickY");
        } else if (PlayerInput.mapping == PlayerInputMapping.XboxForWindows) {
            toggle = Input.GetAxis("XboxWinDPadY") >= 1f;
            play = Input.GetButtonDown("XboxWinButtonA");
            pause = Input.GetButtonDown("XboxWinButtonB");
            spawn = Input.GetAxis("XboxWinDPadY") <= -1f;
            start = Input.GetButtonDown("XboxWinStart");
            scalar = Input.GetAxis("XboxWinRStickY");
        } else if (PlayerInput.mapping == PlayerInputMapping.XboxForMac) {
            toggle = Input.GetButtonDown("XboxMacDPadUp");
            play = Input.GetButtonDown("XboxMacButtonA");
            pause = Input.GetButtonDown("XboxMacButtonB");
            spawn = Input.GetButtonDown("XboxMacDPadDown");
            start = Input.GetButtonDown("XboxMacStart");
            scalar = Input.GetAxis("XboxMacRStickY");
        } else if (PlayerInput.mapping == PlayerInputMapping.Vive) {
            toggle = Input.GetButtonDown("ViveRThumb");
            play = Input.GetButtonDown("ViveRGrip");
            pause = Input.GetButtonDown("ViveLGrip");
            spawn = Input.GetButtonDown("ViveLThumb");
            scalar = Input.GetAxis("ViveRThumbX");
        }

        bool newToggle = toggle & !_oldToggle;
        _oldToggle = toggle;
        toggle = newToggle;

        bool newSpawn = spawn & !_oldSpawn;
        _oldSpawn = spawn;
        spawn = newSpawn;

        if (!PlayerInput.forceMapping) {
            if (PlayerInput.mapping != PlayerInputMapping.MouseAndKeyboard)
                if (Input.GetKeyDown(KeyCode.Space))
                    PlayerInput.SelectInputMapping(PlayerInputMapping.MouseAndKeyboard, force: true);
        } else if (control == Control.Options) {
            if (Input.GetKeyDown(KeyCode.Space))
                PlayerInput.SelectInputMapping();
        }

        if (control == Control.Options) {
            if (!PlayerInput.forceMapping)
                if ((_selectTimer -= Time.deltaTime) <= 0f) {
                    _selectTimer += 2f;
                    PlayerInput.SelectInputMapping();
                }

            if (start) {
                control = Control.Gameplay;
                UIController.view = UIView.Gameplay;
            }

            return;
        } else if (control >= Control.Gameplay) {
            if (start && (!Application.isEditor || editorOptionsMode != EditorOptionsMode.Disabled)) {
                control = Control.Options;
                UIController.view = UIView.Options;
            }
        }

        if (control != Control.None)
            if (startInTimelineMode && !_startedInTimelineMode)
                toggle = _startedInTimelineMode = true;

        if (toggle) {
            if (control == Control.Gameplay) {
                control = Control.Timeline;

                var cameraTransform = _gameController.defaultCamera.transform;
                cameraTransform.parent = animatedTransform;
                cameraTransform.localPosition = Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;
                cameraTransform.localScale = Vector3.one;

                _gameController.DespawnPlayer();
                playableDirector.Play();

                AxelF.Heartbeat.playerTransform = cameraTransform;
                AxelF.Heartbeat.listenerTransform = cameraTransform;
            } else if (control == Control.Timeline) {
                control = Control.Gameplay;

                _gameController.SpawnPlayer();
                playableDirector.Stop();
            }
        }

        PlayerInput.ignore = (_emulatingGameViewFocus || intercepted || control < Control.Gameplay);
        Cursor.lockState = !PlayerInput.ignore ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);

        if (control == Control.Gameplay)
            if (spawn) {
                var spawnPoint = _gameController.GetNextPlayerSpawnPoint();
                _gameController.SpawnPlayer(spawnPoint);
            }

        if (control == Control.Timeline)
            if (play && playableDirector.state == PlayState.Paused)
                playableDirector.Play();
            else if (pause && playableDirector.state != PlayState.Paused)
                playableDirector.Pause();
            else {
                if (!Mathf.Approximately(scalar, 0f)) {
                    playableDirector.time -= (scalar * scrubSpeed) * (1f/30f);

                    if (playableDirector.time < 0f)
                        playableDirector.time += playableDirector.duration;
                    else if (playableDirector.time > playableDirector.duration)
                        playableDirector.time -= playableDirector.duration;

                    if (playableDirector.state != PlayState.Playing)
                        playableDirector.Evaluate();
                }

                if (rotateCamera) {
                    var cameraTransform = _gameController.defaultCamera.transform;
                    cameraTransform.rotation *= Quaternion.Euler(0f, angularSpeed * Time.deltaTime, 0f);
                }
            }
    }
}

