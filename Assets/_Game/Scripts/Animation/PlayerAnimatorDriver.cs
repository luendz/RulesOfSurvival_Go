using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Loot;
using ROS.Game.Parachute;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Animation
{
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private Health health;
        [SerializeField] private ParachuteController parachute;

        [Header("Smoothing")]
        [SerializeField] private float dampTime = 0.12f;

        private static readonly int MoveX =
            Animator.StringToHash("MoveX");

        private static readonly int MoveY =
            Animator.StringToHash("MoveY");

        private static readonly int Speed =
            Animator.StringToHash("Speed");

        private static readonly int Grounded =
            Animator.StringToHash("Grounded");

        private static readonly int Crouch =
            Animator.StringToHash("Crouch");

        private static readonly int Prone =
            Animator.StringToHash("Prone");

        private static readonly int Aim =
            Animator.StringToHash("Aim");

        private static readonly int VerticalVelocity =
            Animator.StringToHash("VerticalVelocity");

        private static readonly int HasRifle =
            Animator.StringToHash("HasRifle");

        private static readonly int Reloading =
            Animator.StringToHash("Reloading");

        private static readonly int Dead =
            Animator.StringToHash("Dead");

        private static readonly int PickupItem =
            Animator.StringToHash("PickupItem");

        private PlayerInteractor _interactor;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (motor == null)
            {
                motor = GetComponentInParent<PlayerMotor>();
            }

            if (input == null)
            {
                input = GetComponentInParent<PlayerInputReader>();
            }

            if (equipment == null)
            {
                equipment = GetComponentInParent<WeaponEquipmentController>();
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (parachute == null)
            {
                parachute = GetComponentInParent<ParachuteController>();
            }

            _interactor = GetComponentInParent<PlayerInteractor>();
            if (_interactor != null)
                _interactor.Interacted += OnInteracted;
        }

        private void OnDestroy()
        {
            if (_interactor != null)
                _interactor.Interacted -= OnInteracted;
        }

        private void OnInteracted(IInteractable interactable)
        {
            if (interactable is LootPickup && animator != null)
                animator.SetTrigger(PickupItem);
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            motor = GetComponentInParent<PlayerMotor>();
            input = GetComponentInParent<PlayerInputReader>();
            equipment = GetComponentInParent<WeaponEquipmentController>();
            health = GetComponentInParent<Health>();
            parachute = GetComponentInParent<ParachuteController>();
        }

        private void Update()
        {
            if (animator == null || motor == null || input == null)
            {
                return;
            }

            if (health != null && !health.IsAlive)
            {
                UpdateDeathState();
                return;
            }

            SetBoolIfPresent(Dead, false);

            UpdateMovement();
            UpdateStates();
        }

        private void UpdateDeathState()
        {
            animator.SetFloat(MoveX, 0f);
            animator.SetFloat(MoveY, 0f);
            animator.SetFloat(Speed, 0f);
            animator.SetBool(Aim, false);
            animator.SetBool(Reloading, false);
            SetBoolIfPresent(Dead, true);
        }

        private void SetBoolIfPresent(
            int parameterHash,
            bool value
        )
        {
            foreach (
                AnimatorControllerParameter parameter
                in animator.parameters
            )
            {
                if (
                    parameter.nameHash == parameterHash &&
                    parameter.type ==
                    AnimatorControllerParameterType.Bool
                )
                {
                    animator.SetBool(
                        parameterHash,
                        value
                    );

                    return;
                }
            }
        }

        private void UpdateMovement()
        {
            Vector2 move = Vector2.ClampMagnitude(
                input.Move,
                1f
            );

            animator.SetFloat(
                MoveX,
                move.x,
                dampTime,
                Time.deltaTime
            );

            animator.SetFloat(
                MoveY,
                move.y,
                dampTime,
                Time.deltaTime
            );

            float animationSpeed =
                ResolveAnimationSpeed(move);

            animator.SetFloat(
                Speed,
                animationSpeed,
                dampTime,
                Time.deltaTime
            );
        }

        private float ResolveAnimationSpeed(Vector2 move)
        {
            if (move.sqrMagnitude <= 0.01f)
            {
                return 0f;
            }

            if (motor.IsCrouching)
            {
                return 0.33f;
            }

            if (motor.IsProne)
            {
                return 0.33f;
            }

            if (IsAimActive())
            {
                return 0.33f;
            }

            if (
                input.SprintHeld &&
                move.y > 0.25f
            )
            {
                return 1f;
            }

            if (move.magnitude > 0.65f)
            {
                return 0.66f;
            }

            return 0.33f;
        }

        private bool IsAimActive()
        {
            if (input == null)
                return false;

            if (equipment != null)
            {
                return equipment.CombatState ==
                       PlayerCombatState.Aiming;
            }

            return input.AimHeld;
        }

        private bool IsReloading()
        {
            if (equipment == null)
                return false;

            return equipment.CombatState ==
                   PlayerCombatState.Reloading;
        }

        private void UpdateStates()
        {
            bool airborneDrop = parachute != null &&
                                parachute.IsAirbornePhase;

            animator.SetBool(Grounded, !airborneDrop && motor.IsGrounded);

            animator.SetBool(
                Crouch,
                motor.IsCrouching
            );

            animator.SetBool(
                Prone,
                motor.IsProne
            );

            animator.SetBool(
                Aim,
                IsAimActive()
            );

            animator.SetBool(
                HasRifle,
                equipment != null &&
                equipment.HasEquippedWeapon
            );

            animator.SetBool(
                Reloading,
                IsReloading()
            );

            animator.SetFloat(
                VerticalVelocity,
                airborneDrop
                    ? parachute.VerticalSpeed
                    : motor.Velocity.y
            );
        }
    }
}
