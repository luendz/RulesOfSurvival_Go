using System;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Gameplay;
using ROS.Game.Input;
using ROS.Game.Parachute;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Animation
{
    [DefaultExecutionOrder(-20)]
    public sealed class PlayerGestureController : MonoBehaviour
    {
        public const string GestureLayerName = "Gestures";
        public const string GestureIdleState = "GestureIdle";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private Health health;
        [SerializeField] private ParachuteController parachute;
        [SerializeField] private ConsumableController consumable;

        [Header("Gesture")]
        [SerializeField, Min(0f)]
        private float transitionDuration = 0.12f;

        [SerializeField, Min(0f)]
        private float movementCancelThreshold = 0.18f;

        public bool IsPlaying { get; private set; }
        public string CurrentGesture { get; private set; }

        public bool CanPlayGesture
        {
            get
            {
                EnsureReferences();

                if (IsPlaying || animator == null || motor == null)
                    return false;

                if (health != null && !health.IsAlive)
                    return false;

                if (!motor.IsGrounded || motor.IsCrouching || motor.IsProne)
                    return false;

                if (motor.ExternalMovementLocked)
                    return false;

                if (parachute != null && parachute.IsAirbornePhase)
                    return false;

                if (consumable != null && consumable.IsUsing)
                    return false;

                if (equipment != null &&
                    (equipment.IsSwitchingWeapon ||
                     equipment.CombatState == PlayerCombatState.Reloading))
                {
                    return false;
                }

                return ResolveGestureLayer();
            }
        }

        public event Action<string> GestureStarted;
        public event Action<string> GestureEnded;

        private int _gestureLayerIndex = -1;
        private float _ignoreIdleUntil;
        private bool _warnedMissingLayer;

        private static readonly int GestureIdleHash =
            Animator.StringToHash(GestureIdleState);

        private void Awake()
        {
            EnsureReferences();
            ResolveGestureLayer();
        }

        private void OnDisable()
        {
            if (IsPlaying)
            {
                FinishGesture();
            }
        }

        private void Update()
        {
            EnsureReferences();

            if (!IsPlaying)
                return;

            if (!CanContinueGesture())
            {
                CancelGesture();
                return;
            }

            if (HasCancelInput())
            {
                CancelGesture();
                return;
            }

            if (animator == null || !ResolveGestureLayer())
            {
                FinishGesture();
                return;
            }

            if (Time.unscaledTime < _ignoreIdleUntil)
                return;

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(_gestureLayerIndex);

            if (!animator.IsInTransition(_gestureLayerIndex) &&
                state.shortNameHash == GestureIdleHash)
            {
                FinishGesture();
            }
        }

        public bool TryPlayGesture(
            string animatorStateName,
            string displayName = null
        )
        {
            if (string.IsNullOrWhiteSpace(animatorStateName) ||
                !CanPlayGesture)
            {
                return false;
            }

            int stateHash = Animator.StringToHash(
                $"{GestureLayerName}.{animatorStateName}"
            );

            if (!animator.HasState(_gestureLayerIndex, stateHash))
            {
                Debug.LogWarning(
                    $"Gesture state '{animatorStateName}' was not found in layer '{GestureLayerName}'.",
                    this
                );
                return false;
            }

            motor.SetExternalMovementLocked(true);
            animator.SetLayerWeight(_gestureLayerIndex, 1f);
            animator.CrossFade(
                stateHash,
                transitionDuration,
                _gestureLayerIndex,
                0f
            );

            IsPlaying = true;
            CurrentGesture = string.IsNullOrWhiteSpace(displayName)
                ? animatorStateName
                : displayName;

            _ignoreIdleUntil =
                Time.unscaledTime + Mathf.Max(0.08f, transitionDuration);

            GestureStarted?.Invoke(CurrentGesture);
            return true;
        }

        public void CancelGesture()
        {
            if (!IsPlaying)
                return;

            if (animator != null && ResolveGestureLayer())
            {
                int idleHash = Animator.StringToHash(
                    $"{GestureLayerName}.{GestureIdleState}"
                );

                if (animator.HasState(_gestureLayerIndex, idleHash))
                {
                    animator.CrossFade(
                        idleHash,
                        transitionDuration,
                        _gestureLayerIndex,
                        0f
                    );
                }
            }

            FinishGesture();
        }

        private bool CanContinueGesture()
        {
            if (health != null && !health.IsAlive)
                return false;

            if (motor == null || !motor.IsGrounded)
                return false;

            if (parachute != null && parachute.IsAirbornePhase)
                return false;

            if (consumable != null && consumable.IsUsing)
                return false;

            if (equipment != null &&
                (equipment.IsSwitchingWeapon ||
                 equipment.CombatState == PlayerCombatState.Reloading))
            {
                return false;
            }

            return true;
        }

        private bool HasCancelInput()
        {
            if (input == null)
                return false;

            float thresholdSqr =
                movementCancelThreshold * movementCancelThreshold;

            if (input.Move.sqrMagnitude > thresholdSqr)
                return true;

            return input.JumpPressed ||
                   input.CrouchPressed ||
                   input.PronePressed ||
                   input.ReloadPressed ||
                   input.InteractPressed ||
                   input.AimHeld ||
                   input.FireHeld;
        }

        private void FinishGesture()
        {
            string finishedGesture = CurrentGesture;

            IsPlaying = false;
            CurrentGesture = null;

            if (motor != null)
            {
                motor.SetExternalMovementLocked(false);
            }

            if (!string.IsNullOrWhiteSpace(finishedGesture))
            {
                GestureEnded?.Invoke(finishedGesture);
            }
        }

        private bool ResolveGestureLayer()
        {
            if (animator == null)
                return false;

            if (_gestureLayerIndex >= 0 &&
                _gestureLayerIndex < animator.layerCount &&
                animator.GetLayerName(_gestureLayerIndex) == GestureLayerName)
            {
                return true;
            }

            _gestureLayerIndex =
                animator.GetLayerIndex(GestureLayerName);

            if (_gestureLayerIndex >= 0)
            {
                _warnedMissingLayer = false;
                return true;
            }

            if (!_warnedMissingLayer)
            {
                Debug.LogWarning(
                    $"Animator layer '{GestureLayerName}' is missing. " +
                    "Open the project in Unity so GestureAnimatorConfigurator can configure it.",
                    this
                );
                _warnedMissingLayer = true;
            }

            return false;
        }

        private void EnsureReferences()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>();

            if (motor == null)
                motor = GetComponent<PlayerMotor>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (equipment == null)
                equipment = GetComponent<WeaponEquipmentController>();

            if (health == null)
                health = GetComponent<Health>();

            if (parachute == null)
                parachute = GetComponent<ParachuteController>();

            if (consumable == null)
                consumable = GetComponent<ConsumableController>();
        }
    }
}
