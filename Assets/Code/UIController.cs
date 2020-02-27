
using UnityEngine;
using Gameplay;

public enum UIView {
    None,
    Options,
    Gameplay
}

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class UIController : MonoBehaviour {
    public static UIView view {
        get { return currentView; }
        set { targetView = value; }
    }

    public static UIView targetView;
    public static UIView currentView;

    [Header("Options View")]
    public GameObject options;
    public GameObject pressSpace;
    public GameObject keyboard;
    public GameObject PS4Controller;
    public GameObject xboxController;

    [Header("Watermark View")]
    public GameObject watermark;
    public bool showWatermark = true;

    public enum EditController {
        Keyboard,
        PS4Controller,
        XboxController
    }

    public EditController editController { get; set; }

    protected void OnEnable() {
        if (options)
            options.SetActive(false);

        if (watermark)
            watermark.SetActive(false);
    }

    protected void LateUpdate() {
        ForceUpdate(false);
    }

    public void ForceUpdate(bool force) {
        bool isOptionsView = (targetView == UIView.Options);
        bool isGameplayView = (targetView == UIView.Gameplay);

        if (currentView != targetView || force) {
            currentView = targetView;

            if (options)
                options.SetActive(isOptionsView);

            if (watermark)
                watermark.SetActive(isGameplayView && showWatermark);

            if (Application.isPlaying) {
                var gameController = GameController.FindGameController();
                var autoFocus = gameController.defaultCamera.GetComponent<DepthOfFieldAutoFocus>();

                if (autoFocus) {
                    autoFocus.distanceOverride = 0f;
                    autoFocus.distanceOverrideWeight = isOptionsView ? 1f : 0f;
                }
            }
        }

        if (isOptionsView) {
            bool showKeyboard = false;
            bool showPS4Controller = false;
            bool showXboxController = false;
            bool showPressSpace = false;

            if (Application.isPlaying) {
                switch (PlayerInput.mapping) {
                case PlayerInputMapping.MouseAndKeyboard:
                    showKeyboard = isOptionsView;
                    break;
                case PlayerInputMapping.PlayStation:
                case PlayerInputMapping.PlayStationForWindows:
                    showPS4Controller = isOptionsView;
                    break;
                case PlayerInputMapping.Xbox:
                case PlayerInputMapping.XboxForWindows:
                case PlayerInputMapping.XboxForMac:
                    showXboxController = isOptionsView;
                    break;
                }

                switch (PlayerInput.mapping) {
                case PlayerInputMapping.PlayStationForWindows:
                case PlayerInputMapping.XboxForWindows:
                case PlayerInputMapping.XboxForMac:
                    showPressSpace = isOptionsView;
                    break;
                }
            } else {
                showKeyboard = (editController == EditController.Keyboard);
                showPS4Controller = (editController == EditController.PS4Controller);
                showXboxController = (editController == EditController.XboxController);
                showPressSpace = (editController != EditController.Keyboard);
            }

            keyboard.SetActive(showKeyboard);
            PS4Controller.SetActive(showPS4Controller);
            xboxController.SetActive(showXboxController);
            pressSpace.SetActive(showPressSpace);
        }
    }
}

