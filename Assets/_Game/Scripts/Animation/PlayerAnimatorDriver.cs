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
    /// Fuente unica de verdad para los parametros y pesos de capas del Animator
    /// del jugador.
    ///
    /// Arquitectura:
    /// - Locomotion: cuerpo completo (Idle/Walk/Run/Sprint/Crouch/airborne).
    /// - UpperBodyCombat: apuntado solamente de cintura hacia arriba.
    /// - Actions: curacion/pickup solamente de cintura hacia arriba.
    /// - WeaponUpperBody: recarga/cambio de arma solamente de cintura hacia arriba.
    /// - Gestures: cuerpo completo mientras existe un gesto activo.
    /// - Lean: se aplica despues del Animator mediante PlayerLeanRigApplier.
    ///
    /// Mantener esta clase como el unico escritor continuo de parametros/pesos
    /// de movimiento evita que torso y piernas se pisen entre sistemas.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        public const string LocomotionLayerName = "Locomotion";
        public const string UpperBodyCombatLayerName = "UpperBodyCombat";
        public const string ActionsLayerName = "Actions";
        public const string WeaponUpperBodyLayerName = "WeaponUpperBody";
        public const string GestureLayerName = "Gestures";
        public const string LegacyCrouchAimLayerName = "CrouchAimUpperBody";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private Health health;
        [SerializeField] private ParachuteController parachute;
        [SerializeField] private PlayerGestureController gestureController;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float dampTime = 0.12f;

        [Header("Layer Weights")]
        [SerializeField, Range(0f, 1f)]
        private float upperBodyCombatWeight = 1f;

        [SerializeField, Range(0f, 1f)]
        private float actionsUpperBodyWeight = 1f;

        [SerializeField, Range(0f, 1f)]
        private float weaponActionsUpperBodyWeight = 1f;

        [Header("Actions")]
        [SerializeField, Min(0.1f)]
        private float pickupUpperBodyDuration = 0.65f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugHasWeapon;
        [SerializeField] private bool debugAiming;
        [SerializeField] private bool debugHealing;
        [SerializeField] private bool debugReloading;
        [SerializeField] private bool debugWeaponSwitching;
        [SerializeField] private bool debugGesturePlaying;
        [SerializeField] private float debugCombatLayerWeight;
        [SerializeField] private float debugActionsLayerWeight;
        [SerializeField] private float debugWeaponActionsLayerWeight;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Crouch = Animator.StringToHash("Crouch");
        private static readonly int Prone = Animator.StringToHash("Prone");
        private static readonly int Aim = Animator.StringToHash("Aim");
        private static readonly int UpperBodyAim = Animator.StringToHash("UpperBodyAim");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int HasRifle = Animator.StringToHash("HasRifle");
        private static readonly int Reloading = Animator.StringToHash("Reloading");
        private static readonly int Dead = Animator.StringToHash("Dead");
        private static readonly int PickupItem = Animator.StringToHash("PickupItem");
        private static readonly int Healing = Animator.StringToHash("Healing");

        private PlayerInteractor _interactor;
        private ConsumableController _consumable;
        private float _pickupUpperBodyUntil;

        private int _locomotionLayer = -1;
        private int _upperBodyCombatLayer = -1;
        private int _actionsLayer = -1;
        private int _weaponUpperBodyLayer = -1;
        private int _gestureLayer = -1;
        private int _legacyCrouchAimLayer = -1;
        private RuntimeAnimatorController _resolvedController;

        private void Awake()
        {
            ResolveReferences();
            ResolveLayers(true);

            _interactor = GetComponentInParent<PlayerInteractor>();
            if (_interactor != null)
                _interactor.Interacted += OnInteracted;
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveLayers(true);
        }

        private void OnDestroy()
        {
            if (_interactor != null)
                _interactor.Interacted -= OnInteracted;
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            motor = GetComponentInParent<PlayerMotor>();
            input = GetComponentInParent<PlayerInputReader>();
            equipment = GetComponentInParent<WeaponEquipmentController>();
            health = GetComponentInParent<Health>();
            parachute = GetComponentInParent<ParachuteController>();
            gestureController = GetComponentInParent<PlayerGestureController>();
        }

        private void Update()
        {
            ResolveReferences();

            if (animator == null || motor == null || input == null)
                return;

            ResolveLayers(false);

            if (health != null && !health.IsAlive)
            {
                UpdateDeathState();
                return;
            }

            SetBoolIfPresent(Dead, false);
            UpdateMovement();
            UpdateStatesAndLayers();
        }

        private void OnInteracted(IInteractable interactable)
        {
            if (!(interactable is LootPickup) || animator == null)
                return;

            _pickupUpperBodyUntil = Time.time + pickupUpperBodyDuration;
            SetTriggerIfPresent(PickupItem);
        }

        private void UpdateDeathState()
        {
            animator.SetFloat(MoveX, 0f);
            animator.SetFloat(MoveY, 0f);
            animator.SetFloat(Speed, 0f);

            SetBoolIfPresent(Aim, false);
            SetBoolIfPresent(UpperBodyAim, false);
            SetBoolIfPresent(Reloading, false);
            SetBoolIfPresent(Healing, false);
            SetBoolIfPresent(Dead, true);

            SetLayerWeight(_upperBodyCombatLayer, 0f);
            SetLayerWeight(_actionsLayer, 0f);
            SetLayerWeight(_weaponUpperBodyLayer, 0f);
            SetLayerWeight(_gestureLayer, 0f);
            SetLayerWeight(_legacyCrouchAimLayer, 0f);

            UpdateDebug(false, false, false, false, false, false, 0f, 0f, 0f);
        }

        private void UpdateMovement()
        {
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);

            animator.SetFloat(MoveX, move.x, dampTime, Time.deltaTime);
            animator.SetFloat(MoveY, move.y, dampTime, Time.deltaTime);
            animator.SetFloat(
                Speed,
                ResolveAnimationSpeed(move),
                dampTime,
                Time.deltaTime
            );
        }

        private float ResolveAnimationSpeed(Vector2 move)
        {
            if (move.sqrMagnitude <= 0.01f)
                return 0f;

            if (motor.IsCrouching || motor.IsProne)
                return 0.33f;

            // Aim ya no fuerza Walk. Las piernas conservan su locomocion real
            // mientras el torso se resuelve en UpperBodyCombat.
            if (input.SprintHeld && move.y > 0.25f)
                return 1f;

            if (move.magnitude > 0.65f)
                return 0.66f;

            return 0.33f;
        }

        private void UpdateStatesAndLayers()
        {
            bool airborneDrop = parachute != null && parachute.IsAirbornePhase;
            bool hasWeapon = equipment != null && equipment.HasEquippedWeapon;
            bool aiming = hasWeapon && IsAimActive();
            bool reloading = hasWeapon && IsReloading();
            bool switchingWeapon = equipment != null && equipment.IsSwitchingWeapon;
            bool gesturePlaying = gestureController != null && gestureController.IsPlaying;

            if (_consumable == null)
                _consumable = GetComponentInParent<ConsumableController>();

            bool healing = _consumable != null && _consumable.IsUsing;
            bool pickupAction = Time.time < _pickupUpperBodyUntil;

            animator.SetBool(Grounded, !airborneDrop && motor.IsGrounded);
            animator.SetBool(Crouch, motor.IsCrouching);
            animator.SetBool(Prone, motor.IsProne);

            // Aim nunca vuelve a tomar la capa Locomotion de cuerpo completo.
            // El parametro legado se mantiene en false para que BT_AimLocomotion
            // no sustituya piernas/cadera. UpperBodyAim conduce la capa nueva.
            SetBoolIfPresent(Aim, false);

            bool upperBodyAimActive =
                aiming &&
                !healing &&
                !reloading &&
                !switchingWeapon &&
                !gesturePlaying &&
                !motor.IsProne;

            SetBoolIfPresent(UpperBodyAim, upperBodyAimActive);
            SetBoolIfPresent(HasRifle, hasWeapon);
            SetBoolIfPresent(Reloading, reloading);
            SetBoolIfPresent(Healing, healing);

            animator.SetFloat(
                VerticalVelocity,
                airborneDrop ? parachute.VerticalSpeed : motor.Velocity.y
            );

            float combatWeight = upperBodyAimActive
                ? upperBodyCombatWeight
                : 0f;

            float actionsWeight =
                !gesturePlaying && (healing || pickupAction)
                    ? actionsUpperBodyWeight
                    : 0f;

            float weaponActionsWeight =
                !gesturePlaying && !healing && (reloading || switchingWeapon)
                    ? weaponActionsUpperBodyWeight
                    : 0f;

            SetLayerWeight(_upperBodyCombatLayer, combatWeight);
            SetLayerWeight(_actionsLayer, actionsWeight);
            SetLayerWeight(_weaponUpperBodyLayer, weaponActionsWeight);

            // PlayerGestureController hace el CrossFade y pone Gestures a 1.
            // Aqui solo garantizamos que al terminar vuelva a cero y no quede un
            // GestureIdle Override pisando la locomocion de cuerpo completo.
            if (!gesturePlaying)
                SetLayerWeight(_gestureLayer, 0f);

            // Migracion: si una escena/controller aun conserva la capa vieja,
            // nunca debe competir con UpperBodyCombat.
            SetLayerWeight(_legacyCrouchAimLayer, 0f);

            UpdateDebug(
                hasWeapon,
                aiming,
                healing,
                reloading,
                switchingWeapon,
                gesturePlaying,
                combatWeight,
                actionsWeight,
                weaponActionsWeight
            );
        }

        private bool IsAimActive()
        {
            if (input == null)
                return false;

            if (equipment != null)
                return equipment.CombatState == PlayerCombatState.Aiming;

            return input.AimHeld;
        }

        private bool IsReloading()
        {
            return equipment != null &&
                   equipment.CombatState == PlayerCombatState.Reloading;
        }

        private void ResolveReferences()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (motor == null)
                motor = GetComponentInParent<PlayerMotor>();

            if (input == null)
                input = GetComponentInParent<PlayerInputReader>();

            if (equipment == null)
                equipment = GetComponentInParent<WeaponEquipmentController>();

            if (health == null)
                health = GetComponentInParent<Health>();

            if (parachute == null)
                parachute = GetComponentInParent<ParachuteController>();

            if (gestureController == null)
                gestureController = GetComponentInParent<PlayerGestureController>();

            if (_consumable == null)
                _consumable = GetComponentInParent<ConsumableController>();
        }

        private void ResolveLayers(bool force)
        {
            if (animator == null)
                return;

            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (!force && controller == _resolvedController)
                return;

            _resolvedController = controller;
            _locomotionLayer = animator.GetLayerIndex(LocomotionLayerName);
            _upperBodyCombatLayer = animator.GetLayerIndex(UpperBodyCombatLayerName);
            _actionsLayer = animator.GetLayerIndex(ActionsLayerName);
            _weaponUpperBodyLayer = animator.GetLayerIndex(WeaponUpperBodyLayerName);
            _gestureLayer = animator.GetLayerIndex(GestureLayerName);
            _legacyCrouchAimLayer = animator.GetLayerIndex(LegacyCrouchAimLayerName);

            if (_locomotionLayer >= 0)
                animator.SetLayerWeight(_locomotionLayer, 1f);

            // Un estado Empty con una capa Override a peso 1 es una fuente
            // comun de torso congelado. Todas las capas superiores arrancan a 0.
            SetLayerWeight(_upperBodyCombatLayer, 0f);
            SetLayerWeight(_actionsLayer, 0f);
            SetLayerWeight(_weaponUpperBodyLayer, 0f);
            SetLayerWeight(_gestureLayer, 0f);
            SetLayerWeight(_legacyCrouchAimLayer, 0f);
        }

        private void SetLayerWeight(int layerIndex, float weight)
        {
            if (animator == null ||
                layerIndex < 0 ||
                layerIndex >= animator.layerCount)
            {
                return;
            }

            float clamped = Mathf.Clamp01(weight);
            if (!Mathf.Approximately(animator.GetLayerWeight(layerIndex), clamped))
                animator.SetLayerWeight(layerIndex, clamped);
        }

        private void SetBoolIfPresent(int parameterHash, bool value)
        {
            if (animator == null)
                return;

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.nameHash != parameterHash ||
                    parameter.type != AnimatorControllerParameterType.Bool)
                {
                    continue;
                }

                animator.SetBool(parameterHash, value);
                return;
            }
        }

        private void SetTriggerIfPresent(int parameterHash)
        {
            if (animator == null)
                return;

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.nameHash != parameterHash ||
                    parameter.type != AnimatorControllerParameterType.Trigger)
                {
                    continue;
                }

                animator.SetTrigger(parameterHash);
                return;
            }
        }

        private void UpdateDebug(
            bool hasWeapon,
            bool aiming,
            bool healing,
            bool reloading,
            bool switchingWeapon,
            bool gesturePlaying,
            float combatWeight,
            float actionsWeight,
            float weaponActionsWeight
        )
        {
            debugHasWeapon = hasWeapon;
            debugAiming = aiming;
            debugHealing = healing;
            debugReloading = reloading;
            debugWeaponSwitching = switchingWeapon;
            debugGesturePlaying = gesturePlaying;
            debugCombatLayerWeight = combatWeight;
            debugActionsLayerWeight = actionsWeight;
            debugWeaponActionsLayerWeight = weaponActionsWeight;
        }
    }
}
