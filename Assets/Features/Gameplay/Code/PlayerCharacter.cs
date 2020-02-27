
using System;
using UnityEngine;
using Gameplay.Utilities;

namespace Gameplay {

[Serializable]
public struct PlayerLooking {
    [Range(0.1f, 2f), Tooltip("[0.1, 2]")] public float lookSpeed;
    [Range(0.1f, 2f), Tooltip("[0.1, 2]")] public float runLookSpeed;
    [Range(-360f, 360f), Tooltip("[-360, 360]")] public float pitchLimitMin;
    [Range(-360f, 360f), Tooltip("[-360, 360]")] public float pitchLimitMax;
}

[Flags]
public enum PlayerFeet {
    None = 0,
    Left = 1,
    Right = 2,
    Both = 3
}

[Serializable]
public struct PlayerFootPlanting {
    public LayerMask floorLayers;
    [Range(0, 10f), Tooltip("[0, 10]")] public float walkStepDistance;
    [Range(0, 10f), Tooltip("[0, 10]")] public float runStepDistance;
    [Range(0, 10f), Tooltip("[0, 10]")] public float stopSpeedThreshold;
}

[Serializable]
public struct PlayerJumping {
    [Range(0f, 10f), Tooltip("[0, 10]")] public float force;
    [Range(0f, 20f), Tooltip("[0, 20]")] public float dampSpeed;
    [Range(0f, 10f), Tooltip("[0, 10]")] public float gravityFactor;
}

[Serializable]
public struct PlayerLocomotion {
    [Range(0f, 10f), Tooltip("[0, 10]")] public float walkSpeed;
    [Range(0f, 10f), Tooltip("[0, 10]")] public float runSpeed;
}

[DisallowMultipleComponent]
public class PlayerCharacter : MonoBehaviour {
    public PlayerLooking looking = new PlayerLooking {
        lookSpeed = 0.5f,
        runLookSpeed = 1f,
        pitchLimitMin = -45f,
        pitchLimitMax = 90f
    };

    public PlayerFootPlanting footPlanting = new PlayerFootPlanting {
        floorLayers = 1,
        walkStepDistance = 1.2f,
        runStepDistance = 2.5f,
        stopSpeedThreshold = 0f
    };

    public PlayerJumping jumping = new PlayerJumping {
        force = 1.15f,
        dampSpeed = 5f,
        gravityFactor = 1f
    };

    public PlayerLocomotion locomotion = new PlayerLocomotion {
        walkSpeed = 2f,
        runSpeed = 6f
    };

    Vector2 _lookingAngles;
    Vector2 _movingSpeed;
    float _jumpSpeedScalar;
    float _jumpingScalar;

    Vector3 _lastFootPlantPosition;
    Vector3 _lastVegetationPosition;
    int _feetPlanted;

    public void OnSpawn(Vector3 angles, bool reset, CharacterController characterController) {
        var position = transform.position;

        _lookingAngles.x = angles.x;
        _lookingAngles.y = angles.y;
        _movingSpeed = Vector2.zero;

        _jumpingScalar = jumping.gravityFactor;
        _jumpSpeedScalar = 0f;

        _lastFootPlantPosition = new Vector3(position.x, 0f, position.z);
        _lastVegetationPosition = _lastFootPlantPosition;
        _feetPlanted = 8 + 2;

        // wiggle into place
        UpdateLooking(transform, 0.1f);
        UpdateMoving(transform, 0.1f, characterController);
    }

    public void GetLookPitchAndYaw(out float pitch, out float yaw) {
        pitch = _lookingAngles.x;
        yaw = _lookingAngles.y;
    }

    public void Simulate(
            CharacterController characterController, PlayerFoley playerFoley, PlayerInput input) {
        var transform = this.transform;
        float deltaTime = Time.deltaTime;

        bool isJumping = !characterController.isGrounded;
        bool jumpStart = false;
        if (!isJumping && input.jump) {
            _jumpingScalar = -jumping.force;
            jumpStart = true;
        } else if ((_jumpingScalar += deltaTime * jumping.dampSpeed) >= jumping.gravityFactor)
            _jumpingScalar = jumping.gravityFactor;

        float frictionFactor = isJumping ? 0.99f : 0.1f;
        var absMove = new Vector2(Mathf.Abs(input.move.x), Mathf.Abs(input.move.y));
        var signMove = new Vector2(Mathf.Sign(input.move.x), Mathf.Sign(input.move.y));

        var wantSpeed = Vector2.one;
        wantSpeed *= Mathf.Lerp(locomotion.walkSpeed, locomotion.runSpeed, input.run);
        wantSpeed = Vector2.Scale(wantSpeed, signMove);
        wantSpeed = Vector2.Lerp(_movingSpeed, wantSpeed, isJumping ? 0f : deltaTime * 10f);
        wantSpeed = Vector2.Scale(
            wantSpeed,
            Vector2.Lerp(
                absMove,
                new Vector2(
                    Mathf.Approximately(input.move.x, 0f) ? 0f : 1f,
                    Mathf.Approximately(input.move.y, 0f) ? 0f : 1f),
                input.run));
        _movingSpeed = Vector2.Lerp(wantSpeed, wantSpeed * frictionFactor, !isJumping ? 0f : deltaTime);

        float angularSpeed = 360f * deltaTime * Mathf.Lerp(looking.lookSpeed, looking.runLookSpeed, input.run);
        float lookX = input.look.x;
        float lookY = input.look.y * Mathf.Sign(input.move.y);
        _lookingAngles.x = _lookingAngles.x + lookX * angularSpeed;
        _lookingAngles.y = _lookingAngles.y + lookY * angularSpeed;

        _lookingAngles.x = Angles.Unwind(
            Mathf.Clamp(
                Angles.ToRelative(_lookingAngles.x),
                looking.pitchLimitMin,
                looking.pitchLimitMax));

        UpdateLooking(transform, deltaTime);
        UpdateMoving(transform, deltaTime, characterController);

        float walkSpeedSqr = locomotion.walkSpeed * locomotion.walkSpeed;
        float runSpeedSqr = locomotion.runSpeed * locomotion.runSpeed;
        float speedSqr = _movingSpeed.sqrMagnitude;
        float speedScalar = (speedSqr - walkSpeedSqr) / (runSpeedSqr - walkSpeedSqr);

        if (speedScalar < Mathf.Epsilon)
            speedScalar = 0f;

        int hadFeetPlanted = _feetPlanted;
        isJumping = !characterController.isGrounded; // check again after moving

        if (jumpStart)
            _jumpSpeedScalar = speedScalar;

        UpdateFootPlanting(transform, playerFoley, speedSqr, speedScalar, isJumping);

        if (jumpStart)
            playerFoley.OnJump();
        else if (hadFeetPlanted == 0 && _feetPlanted != 0)
            playerFoley.OnLand();

        playerFoley.breathingIntensity = isJumping ? 1f : speedScalar;
        playerFoley.playerJumping = jumpStart || _feetPlanted == 0;
    }

    void UpdateLooking(Transform transform, float deltaTime) {
        transform.localRotation = Quaternion.Euler(0f, _lookingAngles.y, 0f);
    }

    void UpdateMoving(Transform transform, float deltaTime, CharacterController characterController) {
        var moveVector = _jumpingScalar * Physics.gravity;
        moveVector.x += _movingSpeed.x;
        moveVector.z += _movingSpeed.y;

        characterController.Move(transform.TransformVector(moveVector * deltaTime));
    }

    void UpdateFootPlanting(
            Transform transform, PlayerFoley playerFoley, float speedSqr, float speedScalar, bool isJumping) {
        if (isJumping) {
            if ((_feetPlanted & 8) == 0)
                _feetPlanted = 0;
        } else {
            var position = transform.position;
            var footPlantPosition = new Vector3(position.x, 0f, position.z);

            float walkStepSqr = footPlanting.walkStepDistance * footPlanting.walkStepDistance;
            float runStepSqr = footPlanting.runStepDistance * footPlanting.runStepDistance;
            float stepSqr = (footPlantPosition - _lastFootPlantPosition).sqrMagnitude;
            float needStepSqr = Mathf.Lerp(walkStepSqr, runStepSqr, speedScalar);

            _feetPlanted &= ~8;

            if (_feetPlanted == 0) {
                _feetPlanted = 1;
                _lastFootPlantPosition = footPlantPosition;
                _lastVegetationPosition = footPlantPosition;

                Vector3 normal;
                PhysicMaterial physicalMaterial;
                FindFootPlacement(ref position, out normal, out physicalMaterial);
                playerFoley.PlayFootstep(
                    transform, position, normal, physicalMaterial, _jumpSpeedScalar, landing: true);
            } else if (speedSqr <= footPlanting.stopSpeedThreshold * footPlanting.stopSpeedThreshold) {
                if (_feetPlanted != 2) {
                    _feetPlanted = 2;
                    _lastFootPlantPosition = footPlantPosition;

                    Vector3 normal;
                    PhysicMaterial physicalMaterial;
                    FindFootPlacement(ref position, out normal, out physicalMaterial);
                    playerFoley.PlayFootstep(transform, position, normal, physicalMaterial, speedScalar: 0f);
                }
            } else if (stepSqr >= needStepSqr) {
                _feetPlanted = 1;
                _lastFootPlantPosition = footPlantPosition;

                Vector3 normal;
                PhysicMaterial physicalMaterial;
                FindFootPlacement(ref position, out normal, out physicalMaterial);
                playerFoley.PlayFootstep(transform, position, normal, physicalMaterial, speedScalar);
            } else if (playerFoley.intersectingVegetation) {
                var vegDistSqr = (footPlantPosition - _lastVegetationPosition).sqrMagnitude;

                if (vegDistSqr >= needStepSqr * 0.3f) {
                    _lastVegetationPosition = footPlantPosition;

                    Vector3 normal;
                    PhysicMaterial physicalMaterial;
                    FindFootPlacement(ref position, out normal, out physicalMaterial);
                    playerFoley.PlayVegetation(transform, position, speedScalar);
                }
            }
        }
    }

    bool FindFootPlacement(ref Vector3 position, out Vector3 normal, out PhysicMaterial physicalMaterial) {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 2f, footPlanting.floorLayers)) {
            position = hit.point;
            normal = hit.normal;
            physicalMaterial = hit.collider.sharedMaterial;
            return true;
        }
        normal = Vector3.up;
        physicalMaterial = null;
        return false;
    }
}

} // Gameplay

