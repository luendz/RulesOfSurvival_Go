using System;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Gameplay;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Loot;
using ROS.Game.Parachute;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Animation
{
    /// <summary>
    /// Fuente única de verdad para los parámetros del Animator del jugador.
    ///
    /// Arquitectura:
    /// - Locomotion (base): cuerpo completo sin arma. Conserva el movimiento
    ///   natural de piernas, cadera, columna y brazos.
    /// - UpperBodyCombat: solo torso/brazos. Superpone postura de arma y Aim
    ///   sin sustituir la locomoción de la parte baja.
    /// - UpperBodyActions: solo torso/brazos. Healing, Reload y WeaponSwitch.
    /// - Lean continúa aplicándose al final por PlayerLeanRigApplier como
    ///   modificación aditiva de la pose evaluada.
    /// - Gestures es full-body y temporalmente silencia las capas superiores.
    ///
    /// Los parámetros legacy HasRifle/Aim se fuerzan a false para impedir que
    /// la capa base vuelva a entrar en locomociones armadas full-body.
    /// </summary>
    [DefaultExecutionOrder(80)]
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationCoordinator : MonoBehaviour
    {
        public const string CombatLayerName = "UpperBodyCombat";
        public const string ActionsLayerName = "UpperBodyActions";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private Health health;
        [SerializeField] private ParachuteController parachute;
        [SerializeField] private ConsumableController consumable;
        [SerializeField] private PlayerGestureController gestureController;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float movementDampTime = 0.12f;

        [Header("Aim")]
        [SerializeField, Range(20f, 89f)] private float aimPitchRange = 70f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugUpperBodyArmed;
        [SerializeField] private bool debugUpperBodyAim;
        [SerializeField] private bool debugReloading;
        [SerializeField] private bool debugHealing;
        [SerializeField] private float debugReloadSpeed = 1f;
        [SerializeField] private float debugAimPitch;

        private PlayerInteractor _interactor;
        private int _combatLayer = -1;
        private int _actionsLayer = -1;
        private float _standingReloadClipLength = -1f;
        private float _crouchReloadClipLength = -1f;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Crouch = Animator.StringToHash("Crouch");
        private static readonly int Prone = Animator.StringToHash("Prone");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int Dead = Animator.StringToHash("Dead");
        private static readonly int PickupItem = Animator.StringToHash("PickupItem");

        // Parámetros legacy de la capa base. Se mantienen siempre apagados.
        private static readonly int LegacyHasRifle = Animator.StringToHash("HasRifle");
        private static readonly int LegacyAim = Animator.StringToHash("Aim");

        // Parámetros de la arquitectura consolidada.
        private static readonly int UpperBodyArmed = Animator.StringToHash("UpperBodyArmed");
        private static readonly int UpperBodyAim = Animator.StringToHash("UpperBodyAim");
        private static readonly int Reloading = Animator.StringToHash("Reloading");
        private static readonly int ReloadSpeed = Animator.StringToHash("ReloadSpeed");
        private static readonly int Healing = Animator.StringToHash("Healing");
        private static readonly int AimPitch = Animator.StringToHash("AimPitch");

        private void Awake()
        {
            ResolveReferences();
            BindInteractor();
            ResolveLayerIndexes();
            ResolveReloadClipLengths();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindInteractor();
        }

        private void OnDisable()
        {
            UnbindInteractor();
        }

        private void OnDestroy()
        {
            UnbindInteractor();
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>(true);
            motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputReader>();
            equipment = GetComponent<WeaponEquipmentController>();
            aimController = GetComponent<PlayerAimController>();
            health = GetComponent<Health>();
            parachute = GetComponent<ParachuteController>();
            consumable = GetComponent<ConsumableController>();
            gestureController = GetComponent<PlayerGestureController>();
        }

        private void Update()
        {
            ResolveReferences();

            if (animator == null || motor == null || input == null)
                return;

            ResolveLayerIndexes();
            UpdateMovementParameters();
            UpdateBaseStateParameters();
            UpdateUpperBodyParameters();
        }

        private void UpdateMovementParameters()
        {
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);

            animator.SetFloat(MoveX, move.x, movementDampTime, Time.deltaTime);
            animator.SetFloat(MoveY, move.y, movementDampTime, Time.deltaTime);
            animator.SetFloat(
                Speed,
                ResolveAnimationSpeed(move),
                movementDampTime,
                Time.deltaTime
            );
        }

        private float ResolveAnimationSpeed(Vector2 move)
        {
            if (move.sqrMagnitude <= 0.01f)
                return 0f;

            if (motor.IsCrouching || motor.IsProne)
                return 0.33f;

            // Aim ya no obliga a reproducir Walk. La parte inferior mantiene
            // Walk/Run según el movimiento real mientras el torso apunta.
            if (input.SprintHeld && move.y > 0.25f)
                return 1f;

            if (move.magnitude > 0.65f)
                return 0.66f;

            return 0.33f;
        }

        private void UpdateBaseStateParameters()
        {
            bool airborneDrop = parachute != null && parachute.IsAirbornePhase;
            bool dead = health != null && !health.IsAlive;

            SetBoolIfPresent(Grounded, !airborneDrop && motor.IsGrounded);
            SetBoolIfPresent(Crouch, motor.IsCrouching);
            SetBoolIfPresent(Prone, motor.IsProne);
            SetBoolIfPresent(Dead, dead);

            SetFloatIfPresent(
                VerticalVelocity,
                airborneDrop && parachute != null
                    ? parachute.VerticalSpeed
                    : motor.Velocity.y
            );

            // Fundamental para la consolidación: la capa Base solo resuelve
            // locomoción. Armas y Aim se componen en UpperBodyCombat.
            SetBoolIfPresent(LegacyHasRifle, false);
            SetBoolIfPresent(LegacyAim, false);
        }

        private void UpdateUpperBodyParameters()
        {
            bool dead = health != null && !health.IsAlive;
            bool gesturing = gestureController != null && gestureController.IsPlaying;
            bool healing = consumable != null && consumable.IsUsing;

            WeaponController weapon = equipment != null
                ? equipment.EquippedWeapon
                : null;

            bool hasWeapon = weapon != null;
            bool reloading = hasWeapon && weapon.IsReloading && !healing && !gesturing;
            bool aiming = hasWeapon &&
                          !reloading &&
                          !healing &&
                          !gesturing &&
                          !motor.IsProne &&
                          equipment != null &&
                          equipment.CombatState == PlayerCombatState.Aiming;

            // Prone conserva por ahora su locomoción full-body existente. No se
            // mezcla con la nueva capa hasta tener clips prone de torso dedicados.
            bool armedUpperBody = hasWeapon && !gesturing && !dead && !motor.IsProne;

            float reloadSpeed = reloading
                ? ResolveReloadSpeed(weapon, motor.IsCrouching)
                : 1f;

            float aimPitch = aiming ? ResolveAimPitch() : 0f;

            SetBoolIfPresent(UpperBodyArmed, armedUpperBody);
            SetBoolIfPresent(UpperBodyAim, aiming);
            SetBoolIfPresent(Reloading, reloading);
            SetBoolIfPresent(Healing, healing && !gesturing && !dead);
            SetFloatIfPresent(ReloadSpeed, reloadSpeed);
            SetFloatIfPresent(AimPitch, aimPitch);

            // Los gestos son full-body. Mientras uno se reproduce, ninguna capa
            // de torso puede sobrescribir brazos, pecho o cabeza.
            float upperBodyWeight = dead || gesturing ? 0f : 1f;
            SetLayerWeightSafe(_combatLayer, upperBodyWeight);
            SetLayerWeightSafe(_actionsLayer, upperBodyWeight);

            debugUpperBodyArmed = armedUpperBody;
            debugUpperBodyAim = aiming;
            debugReloading = reloading;
            debugHealing = healing;
            debugReloadSpeed = reloadSpeed;
            debugAimPitch = aimPitch;
        }

        private float ResolveAimPitch()
        {
            if (aimController == null)
                return 0f;

            Vector3 direction = aimController.AimDirection;
            if (direction.sqrMagnitude <= 0.001f)
                return 0f;

            float pitchDegrees = Mathf.Asin(
                Mathf.Clamp(direction.normalized.y, -1f, 1f)
            ) * Mathf.Rad2Deg;

            return Mathf.Clamp(
                pitchDegrees / Mathf.Max(1f, aimPitchRange),
                -1f,
                1f
            );
        }

        private float ResolveReloadSpeed(
            WeaponController weapon,
            bool crouching)
        {
            if (weapon == null || weapon.ActiveReloadDuration <= 0.01f)
                return 1f;

            ResolveReloadClipLengths();

            float clipLength = crouching
                ? _crouchReloadClipLength
                : _standingReloadClipLength;

            if (clipLength <= 0.01f)
                return 1f;

            // Animator speed = duración del clip / duración real del arma.
            // Así el movimiento visual termina junto con la recarga real.
            return Mathf.Clamp(
                clipLength / weapon.ActiveReloadDuration,
                0.2f,
                3f
            );
        }

        private void ResolveReloadClipLengths()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            if (_standingReloadClipLength > 0f && _crouchReloadClipLength > 0f)
                return;

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null)
                    continue;

                string clipName = clip.name ?? string.Empty;
                if (_standingReloadClipLength <= 0f &&
                    clipName.IndexOf("ReloadStanding", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _standingReloadClipLength = clip.length;
                }

                if (_crouchReloadClipLength <= 0f &&
                    clipName.IndexOf("ReloadCrouch", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _crouchReloadClipLength = clip.length;
                }
            }
        }

        private void ResolveLayerIndexes()
        {
            if (animator == null)
                return;

            if (_combatLayer < 0)
                _combatLayer = animator.GetLayerIndex(CombatLayerName);

            if (_actionsLayer < 0)
                _actionsLayer = animator.GetLayerIndex(ActionsLayerName);
        }

        private void SetLayerWeightSafe(int index, float value)
        {
            if (index >= 0 && index < animator.layerCount)
                animator.SetLayerWeight(index, value);
        }

        private void SetBoolIfPresent(int parameterHash, bool value)
        {
            if (!HasParameter(parameterHash, AnimatorControllerParameterType.Bool))
                return;

            animator.SetBool(parameterHash, value);
        }

        private void SetFloatIfPresent(int parameterHash, float value)
        {
            if (!HasParameter(parameterHash, AnimatorControllerParameterType.Float))
                return;

            animator.SetFloat(parameterHash, value);
        }

        private bool HasParameter(
            int parameterHash,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == parameterHash &&
                    parameters[i].type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private void BindInteractor()
        {
            PlayerInteractor found = GetComponent<PlayerInteractor>();
            if (found == null)
                found = GetComponentInChildren<PlayerInteractor>(true);

            if (found == _interactor)
                return;

            UnbindInteractor();
            _interactor = found;

            if (_interactor != null)
                _interactor.Interacted += OnInteracted;
        }

        private void UnbindInteractor()
        {
            if (_interactor != null)
                _interactor.Interacted -= OnInteracted;

            _interactor = null;
        }

        private void OnInteracted(IInteractable interactable)
        {
            if (animator != null && interactable is LootPickup)
                animator.SetTrigger(PickupItem);
        }

        private void ResolveReferences()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (motor == null)
                motor = GetComponent<PlayerMotor>() ?? GetComponentInParent<PlayerMotor>();

            if (input == null)
                input = GetComponent<PlayerInputReader>() ?? GetComponentInParent<PlayerInputReader>();

            if (equipment == null)
                equipment = GetComponent<WeaponEquipmentController>() ??
                            GetComponentInParent<WeaponEquipmentController>();

            if (aimController == null)
                aimController = GetComponent<PlayerAimController>() ??
                                GetComponentInParent<PlayerAimController>();

            if (health == null)
                health = GetComponent<Health>() ?? GetComponentInParent<Health>();

            if (parachute == null)
                parachute = GetComponent<ParachuteController>() ??
                            GetComponentInParent<ParachuteController>();

            if (consumable == null)
                consumable = GetComponent<ConsumableController>() ??
                             GetComponentInParent<ConsumableController>();

            if (gestureController == null)
                gestureController = GetComponent<PlayerGestureController>() ??
                                    GetComponentInParent<PlayerGestureController>();
        }
    }
}
