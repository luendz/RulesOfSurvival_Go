using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Character
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private WeaponEquipmentController equipment;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.3f;
        [SerializeField] private float runSpeed = 5.2f;
        [SerializeField] private float sprintSpeed = 7.2f;
        [SerializeField] private float crouchSpeed = 2.2f;
        [SerializeField] private float proneSpeed = 1.25f;
        [SerializeField] private float rotationSharpness = 10f;
        [SerializeField] private float aimRotationSharpness = 16f;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float groundedGravity = -2f;

        [Header("Stances")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.25f;
        [SerializeField] private float proneHeight = 0.65f;

        public PlayerMovementState MovementState { get; private set; } =
            PlayerMovementState.Idle;

        public bool IsCrouching { get; private set; }

        public bool IsProne { get; private set; }

        public bool IsGrounded
        {
            get
            {
                EnsureReferences();

                return _controller != null &&
                       _controller.isGrounded;
            }
        }

        public bool IsAiming
        {
            get
            {
                EnsureReferences();

                if (_input == null)
                    return false;

                if (equipment != null)
                {
                    return equipment.CombatState ==
                           PlayerCombatState.Aiming;
                }

                return _input.AimHeld;
            }
        }

        public Vector3 Velocity => _velocity;

        public Vector2 MoveInput
        {
            get
            {
                EnsureReferences();

                return _input != null
                    ? _input.Move
                    : Vector2.zero;
            }
        }

        private CharacterController _controller;
        private PlayerInputReader _input;
        private Vector3 _velocity;

        private void Awake()
        {
            EnsureReferences();

            if (cameraTransform == null &&
                Camera.main != null)
            {
                cameraTransform =
                    Camera.main.transform;
            }

            if (_controller != null)
            {
                ApplyStanceHeightImmediate(
                    standingHeight
                );
            }
        }

        private void EnsureReferences()
        {
            if (_controller == null)
            {
                _controller =
                    GetComponent<CharacterController>();
            }

            if (_input == null)
            {
                _input =
                    GetComponent<PlayerInputReader>();
            }

            if (equipment == null)
            {
                equipment =
                    GetComponent<WeaponEquipmentController>();
            }
        }

        public void SetCamera(Transform targetCamera)
        {
            cameraTransform = targetCamera;
        }

        private void Update()
        {
            EnsureReferences();

            if (_controller == null ||
                _input == null)
            {
                return;
            }

            HandleStance();
            HandleMovement();
        }

        private bool IsReloading()
        {
            EnsureReferences();

            return equipment != null &&
                   equipment.CombatState ==
                   PlayerCombatState.Reloading;
        }

        private void HandleStance()
        {
            // Durante la recarga mantenemos la postura
            // con la que comenzó:
            // Standing o Crouch.
            if (IsReloading())
            {
                return;
            }

            if (_input.PronePressed)
            {
                IsProne = !IsProne;

                if (IsProne)
                {
                    IsCrouching = false;
                }
            }
            else if (_input.CrouchPressed)
            {
                IsCrouching = !IsCrouching;

                if (IsCrouching)
                {
                    IsProne = false;
                }
            }

            float targetHeight =
                IsProne
                    ? proneHeight
                    : IsCrouching
                        ? crouchingHeight
                        : standingHeight;

            ApplyStanceHeight(
                targetHeight
            );
        }

        private void ApplyStanceHeight(
            float height
        )
        {
            if (_controller == null)
            {
                return;
            }

            _controller.height =
                Mathf.Lerp(
                    _controller.height,
                    height,
                    15f * Time.deltaTime
                );

            _controller.center =
                Vector3.up *
                (_controller.height * 0.5f);

            if (visualRoot != null)
            {
                visualRoot.localPosition =
                    Vector3.zero;
            }
        }

        private void ApplyStanceHeightImmediate(
            float height
        )
        {
            if (_controller == null)
            {
                return;
            }

            _controller.height = height;

            _controller.center =
                Vector3.up *
                (height * 0.5f);

            if (visualRoot != null)
            {
                visualRoot.localPosition =
                    Vector3.zero;
            }
        }

        private void HandleMovement()
        {
            if (IsReloading())
            {
                HandleReloadMovementLock();
                return;
            }

            Vector2 input =
                Vector2.ClampMagnitude(
                    _input.Move,
                    1f
                );

            Vector3 camForward =
                cameraTransform != null
                    ? cameraTransform.forward
                    : transform.forward;

            Vector3 camRight =
                cameraTransform != null
                    ? cameraTransform.right
                    : transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 move;

            if (_input.FreeLookHeld)
            {
                move =
                    ResolveFreeLookMovement(
                        input
                    );
            }
            else
            {
                move =
                    (
                        camForward * input.y +
                        camRight * input.x
                    ).normalized;
            }

            float speed =
                ResolveSpeed(input);

            HandleRotation(
                move,
                camForward
            );

            HandleGravityAndJump();

            Vector3 finalVelocity =
                move * speed +
                Vector3.up * _velocity.y;

            _controller.Move(
                finalVelocity *
                Time.deltaTime
            );

            UpdateMovementState(
                input,
                speed
            );
        }

        private void HandleReloadMovementLock()
        {
            // Durante la recarga:
            // - sin desplazamiento
            // - sin sprint
            // - sin salto
            // - sin cambiar crouch/prone
            //
            // La gravedad sigue activa para mantener
            // al CharacterController correctamente
            // apoyado sobre el suelo.

            if (_controller.isGrounded &&
                _velocity.y < 0f)
            {
                _velocity.y =
                    groundedGravity;
            }

            _velocity.y +=
                gravity *
                Time.deltaTime;

            _controller.Move(
                Vector3.up *
                _velocity.y *
                Time.deltaTime
            );

            MovementState =
                IsCrouching
                    ? PlayerMovementState.Crouching
                    : PlayerMovementState.Idle;
        }

        private Vector3 ResolveFreeLookMovement(
            Vector2 input
        )
        {
            Vector3 characterForward =
                transform.forward;

            Vector3 characterRight =
                transform.right;

            characterForward.y = 0f;
            characterRight.y = 0f;

            characterForward.Normalize();
            characterRight.Normalize();

            return (
                characterForward * input.y +
                characterRight * input.x
            ).normalized;
        }

        private void HandleRotation(
            Vector3 move,
            Vector3 camForward
        )
        {
            if (_input.FreeLookHeld)
            {
                return;
            }

            if (IsAiming)
            {
                RotateTowardCamera(
                    camForward
                );

                return;
            }

            RotateTowardMovement(
                move
            );
        }

        private void RotateTowardMovement(
            Vector3 move
        )
        {
            if (move.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    move,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSharpness *
                    Time.deltaTime
                );
        }

        private void RotateTowardCamera(
            Vector3 camForward
        )
        {
            if (camForward.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    camForward,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    aimRotationSharpness *
                    Time.deltaTime
                );
        }

        private void HandleGravityAndJump()
        {
            if (_controller.isGrounded &&
                _velocity.y < 0f)
            {
                _velocity.y =
                    groundedGravity;
            }

            if (_input.JumpPressed &&
                _controller.isGrounded &&
                !IsProne)
            {
                _velocity.y =
                    Mathf.Sqrt(
                        jumpHeight *
                        -2f *
                        gravity
                    );
            }

            _velocity.y +=
                gravity *
                Time.deltaTime;
        }

        private float ResolveSpeed(
            Vector2 input
        )
        {
            if (IsProne)
            {
                return proneSpeed;
            }

            if (IsCrouching)
            {
                return crouchSpeed;
            }

            if (_input.SprintHeld &&
                input.y > 0.25f &&
                !IsAiming)
            {
                return sprintSpeed;
            }

            return input.magnitude > 0.65f
                ? runSpeed
                : walkSpeed;
        }

        private void UpdateMovementState(
            Vector2 input,
            float speed
        )
        {
            if (!_controller.isGrounded)
            {
                MovementState =
                    _velocity.y > 0f
                        ? PlayerMovementState.Jumping
                        : PlayerMovementState.Falling;

                return;
            }

            if (IsProne)
            {
                MovementState =
                    PlayerMovementState.Prone;

                return;
            }

            if (IsCrouching)
            {
                MovementState =
                    PlayerMovementState.Crouching;

                return;
            }

            if (input.sqrMagnitude < 0.01f)
            {
                MovementState =
                    PlayerMovementState.Idle;

                return;
            }

            if (Mathf.Approximately(
                    speed,
                    sprintSpeed))
            {
                MovementState =
                    PlayerMovementState.Sprinting;
            }
            else if (Mathf.Approximately(
                         speed,
                         runSpeed))
            {
                MovementState =
                    PlayerMovementState.Running;
            }
            else
            {
                MovementState =
                    PlayerMovementState.Walking;
            }
        }
    }
}