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
    /// Fuente unica de verdad para parametros y pesos del Animator del jugador.
    ///
    /// Arquitectura legacy compatible:
    /// 0 Locomotion       -> locomocion base y estados aereos.
    /// 1 WeaponUpperBody  -> posturas de arma de fuego/melee, Aim/Reload/Switch.
    /// 2 UpperBodyActions -> Heal/Pickup/gestos de torso.
    /// 3 AimRecoil        -> AimPitch/Recoil/Lean/offsets aditivos.
    /// 4 FullBodyOverride -> acciones que toman todo el cuerpo (gestos/ataques melee fuertes).
    ///
    /// ROS Classic usa los equivalentes Base_Locomotion, UpperBody_Weapon,
    /// UpperBody_Actions, Aim_Offset y FullBody_Actions. En ROS Classic Reload,
    /// Switch, consumibles e interacciones se escriben en UpperBody_Actions,
    /// dejando UpperBody_Weapon para la pose/aim/fire permanente del arma.
    /// </summary>
    [DefaultExecutionOrder(80)]
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationCoordinator : MonoBehaviour
    {
        public const string LocomotionLayerName = "Locomotion";
        public const string WeaponUpperBodyLayerName = "WeaponUpperBody";
        public const string UpperBodyActionsLayerName = "UpperBodyActions";
        public const string AimRecoilLayerName = "AimRecoil";
        public const string FullBodyOverrideLayerName = "FullBodyOverride";

        private const string ClassicLocomotionLayerName = "Base_Locomotion";
        private const string ClassicWeaponUpperBodyLayerName = "UpperBody_Weapon";
        private const string ClassicUpperBodyActionsLayerName = "UpperBody_Actions";
        private const string ClassicAimOffsetLayerName = "Aim_Offset";
        private const string ClassicFullBodyActionsLayerName = "FullBody_Actions";

        public const int WeaponCategoryNone = 0;
        public const int WeaponCategoryFirearm = 1;
        public const int WeaponCategoryMelee = 2;
        public const int WeaponCategoryThrowable = 3;

        private const int ClassicWeaponUnarmed = 0;
        private const int ClassicWeaponRifle = 1;
        private const int ClassicWeaponPistol = 2;
        private const int ClassicWeaponShotgun = 3;
        private const int ClassicWeaponSniper = 4;
        private const int ClassicWeaponMelee = 5;
        private const int ClassicWeaponThrowable = 6;

        private const int ClassicStanceStanding = 0;
        private const int ClassicStanceCrouch = 1;
        private const int ClassicStanceProne = 2;

        public const string CombatLayerName = WeaponUpperBodyLayerName;
        public const string ActionsLayerName = UpperBodyActionsLayerName;

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

        [Header("Airborne / Fall")]
        [Tooltip("Distancia real que debe descender desde el punto mas alto antes de entrar a Fall. Un salto corto puede ir directamente de Jump a Landing.")]
        [SerializeField, Min(0.05f)] private float fallAnimationDistance = 1.50f;
        [Tooltip("Velocidad vertical minima de descenso requerida para activar Fall.")]
        [SerializeField] private float fallAnimationMinDownVelocity = -0.2f;

        [Header("Aim")]
        [SerializeField, Range(20f, 89f)] private float aimPitchRange = 70f;

        [Header("Actions")]
        [SerializeField, Min(0.05f)] private float pickupUpperBodyDuration = 0.65f;

        [Header("ROS Classic Runtime")]
        [Tooltip("Duracion visual del pulso IsFiring producido por cada disparo real. Es un valor provisional y ajustable; no representa un timing medido del Rules of Survival original.")]
        [SerializeField, Min(0.01f)] private float classicFirePulseDuration = 0.08f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugUpperBodyArmed;
        [SerializeField] private bool debugUpperBodyAim;
        [SerializeField] private bool debugReloading;
        [SerializeField] private bool debugHealing;
        [SerializeField] private bool debugWeaponLayerActive;
        [SerializeField] private bool debugActionsLayerActive;
        [SerializeField] private bool debugFullBodyOverride;
        [SerializeField] private float debugReloadSpeed = 1f;
        [SerializeField] private float debugAimPitch;
        [SerializeField] private float debugAirbornePeakY;
        [SerializeField] private float debugFallDistance;
        [SerializeField] private bool debugShouldFall;
        [SerializeField] private int debugWeaponCategory;
        [SerializeField] private int debugWeaponStyle;
        [SerializeField] private int debugClassicWeaponType;
        [SerializeField] private bool debugClassicFiring;
        [SerializeField] private int debugClassicStance;
        [SerializeField] private bool debugClassicSprinting;
        [SerializeField] private bool debugClassicAutoRunning;
        [SerializeField] private bool debugClassicSwitchingWeapon;
        [SerializeField] private bool debugClassicUsingConsumable;
        [SerializeField] private bool debugClassicPickingUp;

        private PlayerInteractor _interactor;
        private WeaponController _classicObservedWeapon;
        private int _locomotionLayer = -1;
        private int _weaponUpperBodyLayer = -1;
        private int _upperBodyActionsLayer = -1;
        private int _aimRecoilLayer = -1;
        private int _fullBodyOverrideLayer = -1;
        private RuntimeAnimatorController _resolvedController;
        private float _standingReloadClipLength = -1f;
        private float _crouchReloadClipLength = -1f;
        private float _pickupUpperBodyUntil;
        private float _classicFiringUntil;
        private bool _manualFullBodyOverride;
        private bool _manualRootMotion;
        private bool _initialApplyRootMotion;
        private bool _capturedRootMotion;
        private bool _wasGrounded = true;
        private float _airbornePeakY;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Crouch = Animator.StringToHash("Crouch");
        private static readonly int Prone = Animator.StringToHash("Prone");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int ShouldFall = Animator.StringToHash("ShouldFall");
        private static readonly int Dead = Animator.StringToHash("Dead");
        private static readonly int PickupItem = Animator.StringToHash("PickupItem");

        private static readonly int LegacyHasRifle = Animator.StringToHash("HasRifle");
        private static readonly int LegacyAim = Animator.StringToHash("Aim");

        private static readonly int UpperBodyArmed = Animator.StringToHash("UpperBodyArmed");
        private static readonly int UpperBodyAim = Animator.StringToHash("UpperBodyAim");
        private static readonly int Reloading = Animator.StringToHash("Reloading");
        private static readonly int ReloadSpeed = Animator.StringToHash("ReloadSpeed");
        private static readonly int Healing = Animator.StringToHash("Healing");
        private static readonly int AimPitch = Animator.StringToHash("AimPitch");
        private static readonly int WeaponCategory = Animator.StringToHash("WeaponCategory");
        private static readonly int WeaponStyle = Animator.StringToHash("WeaponStyle");

        private static readonly int ClassicIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int ClassicIsSprinting = Animator.StringToHash("IsSprinting");
        private static readonly int ClassicIsAutoRunning = Animator.StringToHash("IsAutoRunning");
        private static readonly int ClassicStance = Animator.StringToHash("Stance");
        private static readonly int ClassicWeaponType = Animator.StringToHash("WeaponType");
        private static readonly int ClassicIsAiming = Animator.StringToHash("IsAiming");
        private static readonly int ClassicIsFiring = Animator.StringToHash("IsFiring");
        private static readonly int ClassicIsReloading = Animator.StringToHash("IsReloading");
        private static readonly int ClassicIsSwitchingWeapon = Animator.StringToHash("IsSwitchingWeapon");
        private static readonly int ClassicIsUsingConsumable = Animator.StringToHash("IsUsingConsumable");
        private static readonly int ClassicIsPickingUp = Animator.StringToHash("IsPickingUp");

        public bool IsFullBodyOverrideActive
        {
            get
            {
                bool gestureFullBody = gestureController != null &&
                                       gestureController.IsPlaying &&
                                       gestureController.IsFullBodyGesture;
                return _manualFullBodyOverride || gestureFullBody;
            }
        }

        private void Awake()
        {
            ResolveReferences();
            BindClassicWeaponFire(equipment != null ? equipment.EquippedWeapon : null);
            CaptureRootMotionDefault();
            BindInteractor();
            ResolveLayerIndexes(true);
            ResetUpperLayerWeights();
            ResolveReloadClipLengths();
            ResetAirborneTracking();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindClassicWeaponFire(equipment != null ? equipment.EquippedWeapon : null);
            CaptureRootMotionDefault();
            BindInteractor();
            ResolveLayerIndexes(true);
            ResetUpperLayerWeights();
            ResetAirborneTracking();
        }

        private void OnDisable()
        {
            UnbindInteractor();
            UnbindClassicWeaponFire();
            SetBoolIfPresent(ShouldFall, false);
            SetBoolIfPresent(ClassicIsFiring, false);
            SetBoolIfPresent(ClassicIsSprinting, false);
            SetBoolIfPresent(ClassicIsAutoRunning, false);
            SetBoolIfPresent(ClassicIsSwitchingWeapon, false);
            SetBoolIfPresent(ClassicIsUsingConsumable, false);
            SetBoolIfPresent(ClassicIsPickingUp, false);
            ResetUpperLayerWeights();
            RestoreRootMotionDefault();
        }

        private void OnDestroy()
        {
            UnbindInteractor();
            UnbindClassicWeaponFire();
            RestoreRootMotionDefault();
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

            ResolveLayerIndexes(false);
            UpdateMovementParameters();
            UpdateBaseStateParameters();
            UpdateUpperBodyParametersAndLayers();
        }

        public void SetFullBodyOverride(bool active, bool useRootMotion = false)
        {
            _manualFullBodyOverride = active;
            _manualRootMotion = active && useRootMotion;
            ApplyRootMotionPolicy();
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

            bool sprinting = motor.MovementState == PlayerMovementState.Sprinting;
            bool autoRunning = input.AutoRunActive;

            SetBoolIfPresent(ClassicIsSprinting, sprinting);
            SetBoolIfPresent(ClassicIsAutoRunning, autoRunning);

            debugClassicSprinting = sprinting;
            debugClassicAutoRunning = autoRunning;
        }

        private float ResolveAnimationSpeed(Vector2 move)
        {
            if (move.sqrMagnitude <= 0.01f)
                return 0f;

            if (motor.IsCrouching || motor.IsProne)
                return 0.33f;

            if (input.SprintHeld && move.y > 0.25f)
                return 1f;

            if (move.magnitude > 0.65f)
                return 0.66f;

            return 0.33f;
        }

        private void UpdateBaseStateParameters()
        {
            bool airborneDrop = parachute != null && parachute.IsAirbornePhase;
            bool grounded = !airborneDrop && motor.IsGrounded;
            bool dead = health != null && !health.IsAlive;
            float verticalVelocity = airborneDrop && parachute != null
                ? parachute.VerticalSpeed
                : motor.Velocity.y;

            bool shouldFall = UpdateFallAnimationGate(
                grounded,
                airborneDrop,
                verticalVelocity
            );

            int classicStance = motor.IsProne
                ? ClassicStanceProne
                : motor.IsCrouching
                    ? ClassicStanceCrouch
                    : ClassicStanceStanding;

            SetBoolIfPresent(Grounded, grounded);
            SetBoolIfPresent(Crouch, motor.IsCrouching);
            SetBoolIfPresent(Prone, motor.IsProne);
            SetBoolIfPresent(Dead, dead);
            SetBoolIfPresent(ShouldFall, shouldFall);
            SetFloatIfPresent(VerticalVelocity, verticalVelocity);

            SetBoolIfPresent(ClassicIsGrounded, grounded);
            SetIntegerIfPresent(ClassicStance, classicStance);

            debugClassicStance = classicStance;

            SetBoolIfPresent(LegacyHasRifle, false);
            SetBoolIfPresent(LegacyAim, false);
        }

        private bool UpdateFallAnimationGate(
            bool grounded,
            bool parachuting,
            float verticalVelocity)
        {
            float currentY = transform.position.y;

            if (grounded)
            {
                _airbornePeakY = currentY;
                _wasGrounded = true;
                debugAirbornePeakY = _airbornePeakY;
                debugFallDistance = 0f;
                debugShouldFall = false;
                return false;
            }

            if (_wasGrounded)
            {
                _airbornePeakY = currentY;
                _wasGrounded = false;
            }

            if (currentY > _airbornePeakY)
                _airbornePeakY = currentY;

            float fallDistance = Mathf.Max(0f, _airbornePeakY - currentY);
            bool shouldFall = !parachuting &&
                              verticalVelocity <= fallAnimationMinDownVelocity &&
                              fallDistance >= fallAnimationDistance;

            debugAirbornePeakY = _airbornePeakY;
            debugFallDistance = fallDistance;
            debugShouldFall = shouldFall;
            return shouldFall;
        }

        private void ResetAirborneTracking()
        {
            _airbornePeakY = transform.position.y;
            _wasGrounded = motor == null || motor.IsGrounded;
            debugAirbornePeakY = _airbornePeakY;
            debugFallDistance = 0f;
            debugShouldFall = false;
            SetBoolIfPresent(ShouldFall, false);
        }

        private void UpdateUpperBodyParametersAndLayers()
        {
            bool dead = health != null && !health.IsAlive;
            bool gesturing = gestureController != null && gestureController.IsPlaying;
            bool fullBodyGesture = gesturing && gestureController.IsFullBodyGesture;
            bool upperBodyGesture = gesturing && !fullBodyGesture;
            bool fullBodyOverride = !dead && (_manualFullBodyOverride || fullBodyGesture);
            bool healing = consumable != null && consumable.IsUsing;
            bool pickupAction = Time.time < _pickupUpperBodyUntil;

            WeaponController weapon = equipment != null
                ? equipment.EquippedWeapon
                : null;

            BindClassicWeaponFire(weapon);

            bool hasWeapon = weapon != null;
            int weaponCategory = ResolveWeaponCategory(weapon);
            int weaponStyle = ResolveWeaponStyle(weapon);
            int classicWeaponType = ResolveClassicWeaponType(weapon);
            bool firearm = weaponCategory == WeaponCategoryFirearm;
            bool switchingWeapon = equipment != null && equipment.IsSwitchingWeapon;
            bool equipmentReloading = equipment != null &&
                                      equipment.CombatState == PlayerCombatState.Reloading;

            bool reloading = firearm &&
                             (weapon.IsReloading || equipmentReloading) &&
                             !healing &&
                             !gesturing &&
                             !fullBodyOverride;

            bool aiming = firearm &&
                          !reloading &&
                          !healing &&
                          !gesturing &&
                          !fullBodyOverride &&
                          !motor.IsProne &&
                          equipment != null &&
                          equipment.CombatState == PlayerCombatState.Aiming;

            bool classicFiring = firearm &&
                                 Time.time < _classicFiringUntil &&
                                 !dead &&
                                 !reloading &&
                                 !healing &&
                                 !gesturing &&
                                 !fullBodyOverride;

            bool classicUsingConsumable = healing &&
                                          !dead &&
                                          !switchingWeapon &&
                                          !reloading &&
                                          !gesturing &&
                                          !fullBodyOverride;

            bool classicPickingUp = pickupAction &&
                                    !dead &&
                                    !switchingWeapon &&
                                    !reloading &&
                                    !healing &&
                                    !gesturing &&
                                    !fullBodyOverride;

            bool armedUpperBody = hasWeapon &&
                                  !fullBodyOverride &&
                                  !dead &&
                                  !motor.IsProne;

            bool weaponLayerActive = !dead &&
                                     !fullBodyOverride &&
                                     !motor.IsProne &&
                                     (hasWeapon || switchingWeapon);

            bool actionsLayerActive = !dead &&
                                      !fullBodyOverride &&
                                      (reloading ||
                                       switchingWeapon ||
                                       classicUsingConsumable ||
                                       upperBodyGesture ||
                                       classicPickingUp);

            float reloadSpeed = reloading
                ? ResolveReloadSpeed(weapon, motor.IsCrouching)
                : 1f;

            float aimPitch = aiming ? ResolveAimPitch() : 0f;

            SetIntegerIfPresent(WeaponCategory, weaponCategory);
            SetIntegerIfPresent(WeaponStyle, weaponStyle);
            SetBoolIfPresent(UpperBodyArmed, armedUpperBody);
            SetBoolIfPresent(UpperBodyAim, aiming);
            SetBoolIfPresent(Reloading, reloading);
            SetBoolIfPresent(Healing, healing && !gesturing && !fullBodyOverride && !dead);
            SetFloatIfPresent(ReloadSpeed, reloadSpeed);
            SetFloatIfPresent(AimPitch, aimPitch);

            SetIntegerIfPresent(ClassicWeaponType, classicWeaponType);
            SetBoolIfPresent(ClassicIsAiming, aiming);
            SetBoolIfPresent(ClassicIsFiring, classicFiring);
            SetBoolIfPresent(ClassicIsReloading, reloading);
            SetBoolIfPresent(ClassicIsSwitchingWeapon, switchingWeapon && !dead && !fullBodyOverride);
            SetBoolIfPresent(ClassicIsUsingConsumable, classicUsingConsumable);
            SetBoolIfPresent(ClassicIsPickingUp, classicPickingUp);

            SetLayerWeightSafe(_locomotionLayer, 1f);
            SetLayerWeightSafe(_weaponUpperBodyLayer, weaponLayerActive ? 1f : 0f);
            SetLayerWeightSafe(_upperBodyActionsLayer, actionsLayerActive ? 1f : 0f);
            SetLayerWeightSafe(
                _aimRecoilLayer,
                aiming && !dead && !fullBodyOverride ? 1f : 0f
            );
            SetLayerWeightSafe(_fullBodyOverrideLayer, fullBodyOverride ? 1f : 0f);

            ApplyRootMotionPolicy();

            debugUpperBodyArmed = armedUpperBody;
            debugUpperBodyAim = aiming;
            debugReloading = reloading;
            debugHealing = healing;
            debugWeaponLayerActive = weaponLayerActive;
            debugActionsLayerActive = actionsLayerActive;
            debugFullBodyOverride = fullBodyOverride;
            debugReloadSpeed = reloadSpeed;
            debugAimPitch = aimPitch;
            debugWeaponCategory = weaponCategory;
            debugWeaponStyle = weaponStyle;
            debugClassicWeaponType = classicWeaponType;
            debugClassicFiring = classicFiring;
            debugClassicSwitchingWeapon = switchingWeapon;
            debugClassicUsingConsumable = classicUsingConsumable;
            debugClassicPickingUp = classicPickingUp;
        }

        private static int ResolveWeaponCategory(WeaponController weapon)
        {
            if (weapon == null || weapon.Definition == null)
                return WeaponCategoryNone;

            return weapon.Definition.family == WeaponFamily.Melee
                ? WeaponCategoryMelee
                : WeaponCategoryFirearm;
        }

        private static int ResolveWeaponStyle(WeaponController weapon)
        {
            if (weapon == null || weapon.Definition == null)
                return (int)WeaponAnimationStyle.Default;

            WeaponDefinition definition = weapon.Definition;
            if (definition.animationStyle != WeaponAnimationStyle.Default)
                return (int)definition.animationStyle;

            if (definition.family == WeaponFamily.Pistol)
                return (int)WeaponAnimationStyle.Pistol;

            if (definition.family == WeaponFamily.Melee)
                return (int)WeaponAnimationStyle.Default;

            return (int)WeaponAnimationStyle.Rifle;
        }

        private static int ResolveClassicWeaponType(WeaponController weapon)
        {
            if (weapon == null || weapon.Definition == null)
                return ClassicWeaponUnarmed;

            switch (weapon.Definition.family)
            {
                case WeaponFamily.Pistol:
                    return ClassicWeaponPistol;

                case WeaponFamily.Shotgun:
                    return ClassicWeaponShotgun;

                case WeaponFamily.SniperRifle:
                    return ClassicWeaponSniper;

                case WeaponFamily.Melee:
                    return ClassicWeaponMelee;

                case WeaponFamily.AssaultRifle:
                case WeaponFamily.SubmachineGun:
                case WeaponFamily.LightMachineGun:
                default:
                    return ClassicWeaponRifle;
            }
        }

        private void BindClassicWeaponFire(WeaponController weapon)
        {
            if (_classicObservedWeapon == weapon)
                return;

            if (_classicObservedWeapon != null)
                _classicObservedWeapon.Fired -= OnClassicWeaponFired;

            _classicObservedWeapon = weapon;
            _classicFiringUntil = 0f;

            if (_classicObservedWeapon != null)
                _classicObservedWeapon.Fired += OnClassicWeaponFired;
        }

        private void UnbindClassicWeaponFire()
        {
            if (_classicObservedWeapon != null)
                _classicObservedWeapon.Fired -= OnClassicWeaponFired;

            _classicObservedWeapon = null;
            _classicFiringUntil = 0f;
        }

        private void OnClassicWeaponFired()
        {
            _classicFiringUntil = Mathf.Max(
                _classicFiringUntil,
                Time.time + Mathf.Max(0.01f, classicFirePulseDuration)
            );
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

        private float ResolveReloadSpeed(WeaponController weapon, bool crouching)
        {
            if (weapon == null || weapon.ActiveReloadDuration <= 0.01f)
                return 1f;

            ResolveReloadClipLengths();

            float clipLength = crouching
                ? _crouchReloadClipLength
                : _standingReloadClipLength;

            if (clipLength <= 0.01f)
                return 1f;

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

        private void ResolveLayerIndexes(bool force)
        {
            if (animator == null)
                return;

            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (!force && controller == _resolvedController)
                return;

            _resolvedController = controller;
            _locomotionLayer = ResolveLayerIndex(
                LocomotionLayerName,
                ClassicLocomotionLayerName
            );
            _weaponUpperBodyLayer = ResolveLayerIndex(
                WeaponUpperBodyLayerName,
                ClassicWeaponUpperBodyLayerName
            );
            _upperBodyActionsLayer = ResolveLayerIndex(
                UpperBodyActionsLayerName,
                ClassicUpperBodyActionsLayerName
            );
            _aimRecoilLayer = ResolveLayerIndex(
                AimRecoilLayerName,
                ClassicAimOffsetLayerName
            );
            _fullBodyOverrideLayer = ResolveLayerIndex(
                FullBodyOverrideLayerName,
                ClassicFullBodyActionsLayerName
            );
        }

        private int ResolveLayerIndex(string primaryName, string fallbackName)
        {
            if (animator == null)
                return -1;

            int index = animator.GetLayerIndex(primaryName);
            return index >= 0
                ? index
                : animator.GetLayerIndex(fallbackName);
        }

        private void ResetUpperLayerWeights()
        {
            if (animator == null)
                return;

            SetLayerWeightSafe(_locomotionLayer, 1f);
            SetLayerWeightSafe(_weaponUpperBodyLayer, 0f);
            SetLayerWeightSafe(_upperBodyActionsLayer, 0f);
            SetLayerWeightSafe(_aimRecoilLayer, 0f);
            SetLayerWeightSafe(_fullBodyOverrideLayer, 0f);
        }

        private void CaptureRootMotionDefault()
        {
            if (_capturedRootMotion || animator == null)
                return;

            _initialApplyRootMotion = animator.applyRootMotion;
            _capturedRootMotion = true;
        }

        private void ApplyRootMotionPolicy()
        {
            if (animator == null)
                return;

            CaptureRootMotionDefault();
            animator.applyRootMotion = _manualFullBodyOverride && _manualRootMotion
                ? true
                : _initialApplyRootMotion;
        }

        private void RestoreRootMotionDefault()
        {
            if (animator != null && _capturedRootMotion)
                animator.applyRootMotion = _initialApplyRootMotion;
        }

        private void SetLayerWeightSafe(int index, float value)
        {
            if (animator == null || index < 0 || index >= animator.layerCount)
                return;

            float clamped = Mathf.Clamp01(value);
            if (!Mathf.Approximately(animator.GetLayerWeight(index), clamped))
                animator.SetLayerWeight(index, clamped);
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

        private void SetIntegerIfPresent(int parameterHash, int value)
        {
            if (!HasParameter(parameterHash, AnimatorControllerParameterType.Int))
                return;

            animator.SetInteger(parameterHash, value);
        }

        private void SetTriggerIfPresent(int parameterHash)
        {
            if (!HasParameter(parameterHash, AnimatorControllerParameterType.Trigger))
                return;

            animator.SetTrigger(parameterHash);
        }

        private bool HasParameter(int parameterHash, AnimatorControllerParameterType type)
        {
            if (animator == null)
                return false;

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == parameterHash && parameters[i].type == type)
                    return true;
            }

            return false;
        }

        private void BindInteractor()
        {
            PlayerInteractor found = GetComponent<PlayerInteractor>();
            if (found == null)
                found = GetComponentInChildren<PlayerInteractor>(true);
            if (found == null)
                found = GetComponentInParent<PlayerInteractor>();

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
            if (animator == null || !(interactable is LootPickup))
                return;

            _pickupUpperBodyUntil = Time.time + pickupUpperBodyDuration;
            SetTriggerIfPresent(PickupItem);
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
