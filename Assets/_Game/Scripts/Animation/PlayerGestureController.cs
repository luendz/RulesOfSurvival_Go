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
        // Compatibilidad: los gestos full-body viven ahora en FullBodyOverride.
        public const string GestureLayerName = PlayerAnimationCoordinator.FullBodyOverrideLayerName;
        public const string UpperBodyGestureLayerName = PlayerAnimationCoordinator.UpperBodyActionsLayerName;
        public const string FullBodyGestureLayerName = PlayerAnimationCoordinator.FullBodyOverrideLayerName;
        public const string GestureIdleState = "Empty";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private Health health;
        [SerializeField] private ParachuteController parachute;
        [SerializeField] private ConsumableController consumable;

        [Header("Gesture")]
        [SerializeField, Min(0f)] private float transitionDuration = 0.12f;
        [SerializeField, Min(0f)] private float aimCancelTransitionDuration = 0.02f;
        [SerializeField, Min(0f)] private float movementCancelThreshold = 0.18f;
        [SerializeField, Min(0f)] private float combatInputGraceTime = 0.18f;

        public bool IsPlaying { get; private set; }
        public bool IsFullBodyGesture { get; private set; }
        public string CurrentGesture { get; private set; }
        public string CurrentGestureLayerName { get; private set; }

        public bool CanPlayGesture
        {
            get
            {
                EnsureReferences();

                if (animator == null || motor == null)
                    return false;

                if (health != null && !health.IsAlive)
                    return false;

                if (!motor.IsGrounded || motor.IsCrouching || motor.IsProne)
                    return false;

                if (motor.ExternalMovementLocked && !IsPlaying)
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

                return ResolveLayer(UpperBodyGestureLayerName) >= 0 ||
                       ResolveLayer(FullBodyGestureLayerName) >= 0;
            }
        }

        public event Action<string> GestureStarted;
        public event Action<string> GestureEnded;

        private int _activeLayerIndex = -1;
        private float _ignoreIdleUntil;
        private float _ignoreCombatInputUntil;
        private bool _warnedMissingLayer;
        private bool _movementLockedByGesture;
        private int _weaponSlotBeforeGesture;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
            if (IsPlaying)
                FinishGesture();
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

            // Apuntar tiene prioridad absoluta y corta el gesto casi al instante.
            if (HasAimCancelInput())
            {
                CancelGesture(aimCancelTransitionDuration);
                return;
            }

            if (HasCancelInput())
            {
                CancelGesture();
                return;
            }

            if (animator == null || !ResolveActiveLayer())
            {
                FinishGesture();
                return;
            }

            if (Time.unscaledTime < _ignoreIdleUntil)
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(_activeLayerIndex);
            int emptyHash = Animator.StringToHash(GestureIdleState);

            if (!animator.IsInTransition(_activeLayerIndex) &&
                state.shortNameHash == emptyHash)
            {
                FinishGesture();
            }
        }

        public bool TryPlayGesture(string animatorStateName, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(animatorStateName) || !CanPlayGesture)
                return false;

            bool fullBody = IsFullBodyGestureState(animatorStateName);
            string targetLayerName = fullBody
                ? FullBodyGestureLayerName
                : UpperBodyGestureLayerName;

            int targetLayer = ResolveLayer(targetLayerName);
            if (targetLayer < 0)
            {
                WarnMissingLayer(targetLayerName);
                return false;
            }

            int stateHash = Animator.StringToHash($"{targetLayerName}.{animatorStateName}");
            if (!animator.HasState(targetLayer, stateHash))
            {
                Debug.LogWarning(
                    $"Gesture state '{animatorStateName}' was not found in layer '{targetLayerName}'.",
                    this
                );
                return false;
            }

            string previousGesture = CurrentGesture;
            bool switchingGesture = IsPlaying;

            if (!switchingGesture)
                StoreAndHolsterWeapon();

            UpdateMovementLock(fullBody);

            _activeLayerIndex = targetLayer;
            CurrentGestureLayerName = targetLayerName;
            IsFullBodyGesture = fullBody;
            IsPlaying = true;
            CurrentGesture = string.IsNullOrWhiteSpace(displayName)
                ? animatorStateName
                : displayName;

            // PlayerAnimationCoordinator es quien decide el LayerWeight.
            animator.CrossFade(
                stateHash,
                transitionDuration,
                targetLayer,
                0f
            );

            _ignoreIdleUntil =
                Time.unscaledTime + Mathf.Max(0.08f, transitionDuration);

            // Protege el clic izquierdo usado para confirmar el menú radial.
            // Aim no usa esta gracia porque siempre tiene prioridad.
            _ignoreCombatInputUntil =
                Time.unscaledTime + Mathf.Max(0.08f, combatInputGraceTime);

            if (switchingGesture && !string.IsNullOrWhiteSpace(previousGesture))
                GestureEnded?.Invoke(previousGesture);

            GestureStarted?.Invoke(CurrentGesture);
            return true;
        }

        public void CancelGesture()
        {
            CancelGesture(transitionDuration);
        }

        private void CancelGesture(float cancelTransitionDuration)
        {
            if (!IsPlaying)
                return;

            if (animator != null && ResolveActiveLayer())
            {
                int idleHash = Animator.StringToHash(
                    $"{CurrentGestureLayerName}.{GestureIdleState}"
                );

                if (animator.HasState(_activeLayerIndex, idleHash))
                {
                    animator.CrossFade(
                        idleHash,
                        Mathf.Max(0f, cancelTransitionDuration),
                        _activeLayerIndex,
                        0f
                    );
                }
            }

            FinishGesture();
        }

        private static bool IsFullBodyGestureState(string animatorStateName)
        {
            // Estos tres clips pueden componerse con las piernas en locomoción.
            switch (animatorStateName)
            {
                case "Gesture_Salute":
                case "Gesture_Talking_On_Phone":
                case "Gesture_Waving_Gesture":
                    return false;
                default:
                    return true;
            }
        }

        private void StoreAndHolsterWeapon()
        {
            _weaponSlotBeforeGesture = 0;

            if (equipment == null ||
                !equipment.HasEquippedWeapon ||
                equipment.EquippedSlot <= 0)
            {
                return;
            }

            _weaponSlotBeforeGesture = equipment.EquippedSlot;
            equipment.HolsterCurrentWeapon();
        }

        private void RestoreWeapon()
        {
            if (equipment == null || _weaponSlotBeforeGesture <= 0)
                return;

            int slotToRestore = _weaponSlotBeforeGesture;
            _weaponSlotBeforeGesture = 0;

            if (equipment.HasWeaponInSlot(slotToRestore))
                equipment.EquipSlot(slotToRestore);
        }

        private void UpdateMovementLock(bool shouldLock)
        {
            if (motor == null)
                return;

            if (_movementLockedByGesture == shouldLock)
                return;

            motor.SetExternalMovementLocked(shouldLock);
            _movementLockedByGesture = shouldLock;
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

            if (equipment != null && equipment.IsSwitchingWeapon)
                return false;

            return true;
        }

        private bool HasAimCancelInput()
        {
            return input != null && input.AimHeld;
        }

        private bool HasCancelInput()
        {
            if (input == null)
                return false;

            // Solo los gestos full-body cancelan por movimiento. Los gestos de
            // torso pueden convivir con Walk/Run y dejan las piernas a Locomotion.
            if (IsFullBodyGesture)
            {
                float thresholdSqr = movementCancelThreshold * movementCancelThreshold;
                if (input.Move.sqrMagnitude > thresholdSqr)
                    return true;
            }

            if (input.JumpPressed ||
                input.CrouchPressed ||
                input.PronePressed ||
                input.ReloadPressed ||
                input.InteractPressed)
            {
                return true;
            }

            if (Time.unscaledTime < _ignoreCombatInputUntil)
                return false;

            return input.FireHeld;
        }

        private void FinishGesture()
        {
            string finishedGesture = CurrentGesture;

            IsPlaying = false;
            IsFullBodyGesture = false;
            CurrentGesture = null;
            CurrentGestureLayerName = null;
            _activeLayerIndex = -1;

            if (_movementLockedByGesture && motor != null)
                motor.SetExternalMovementLocked(false);
            _movementLockedByGesture = false;

            RestoreWeapon();

            if (!string.IsNullOrWhiteSpace(finishedGesture))
                GestureEnded?.Invoke(finishedGesture);
        }

        private bool ResolveActiveLayer()
        {
            if (animator == null || string.IsNullOrWhiteSpace(CurrentGestureLayerName))
                return false;

            if (_activeLayerIndex >= 0 &&
                _activeLayerIndex < animator.layerCount &&
                animator.GetLayerName(_activeLayerIndex) == CurrentGestureLayerName)
            {
                return true;
            }

            _activeLayerIndex = ResolveLayer(CurrentGestureLayerName);
            return _activeLayerIndex >= 0;
        }

        private int ResolveLayer(string layerName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(layerName))
                return -1;

            return animator.GetLayerIndex(layerName);
        }

        private void WarnMissingLayer(string layerName)
        {
            if (_warnedMissingLayer)
                return;

            Debug.LogWarning(
                $"Animator layer '{layerName}' is missing. Open the project in Unity so the Editor First animation materializer can configure it.",
                this
            );
            _warnedMissingLayer = true;
        }

        private void EnsureReferences()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>() ?? GetComponentInParent<PlayerInputReader>();

            if (motor == null)
                motor = GetComponent<PlayerMotor>() ?? GetComponentInParent<PlayerMotor>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (equipment == null)
                equipment = GetComponent<WeaponEquipmentController>() ??
                            GetComponentInParent<WeaponEquipmentController>();

            if (health == null)
                health = GetComponent<Health>() ?? GetComponentInParent<Health>();

            if (parachute == null)
                parachute = GetComponent<ParachuteController>() ??
                            GetComponentInParent<ParachuteController>();

            if (consumable == null)
                consumable = GetComponent<ConsumableController>() ??
                             GetComponentInParent<ConsumableController>();
        }
    }
}
