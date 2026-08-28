using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Parachute;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Character
{
    public enum PlayerLeanState
    {
        Center,
        Left,
        Right
    }

    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerLeanController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private Health health;
        [SerializeField] private ParachuteController parachute;
        [SerializeField] private Animator animator;

        [Header("Lean")]
        [Min(0.1f)]
        [SerializeField] private float leanSpeed = 20f;

        [Header("Restrictions")]
        [Tooltip("Si se activa, el sprint centra visualmente el torso de forma temporal, pero no borra el lado de lean elegido.")]
        [SerializeField] private bool cancelWhileSprinting = false;

        [Tooltip("Prone cancela por completo el lean seleccionado.")]
        [SerializeField] private bool cancelWhileProne = true;

        [Tooltip("En el aire el lean puede centrarse temporalmente. Al volver al suelo recupera el lado elegido.")]
        [SerializeField] private bool cancelWhileAirborne = true;

        [Tooltip("Tiempo que debe permanecer realmente en el aire antes de ocultar el lean. Evita perderlo por falsos frames de Grounded al caminar/correr por desniveles.")]
        [Min(0f)]
        [SerializeField] private float airborneSuppressDelay = 0.12f;

        [Tooltip("Durante recarga el lean puede centrarse temporalmente. Al terminar recupera el lado elegido.")]
        [SerializeField] private bool cancelWhileReloading = true;

        [Header("Body Collision")]
        [SerializeField] private LayerMask obstructionMask = ~0;
        [Min(0.05f)]
        [SerializeField] private float clearanceRadius = 0.14f;
        [Min(0.05f)]
        [SerializeField] private float clearanceDistance = 0.42f;

        public PlayerLeanState State { get; private set; } =
            PlayerLeanState.Center;

        public float CurrentLean { get; private set; }

        public bool IsTemporarilySuppressed { get; private set; }

        public float TargetLean
        {
            get
            {
                switch (State)
                {
                    case PlayerLeanState.Left:
                        return -1f;
                    case PlayerLeanState.Right:
                        return 1f;
                    default:
                        return 0f;
                }
            }
        }

        private Transform _hips;
        private Transform _spine;
        private Transform _chest;
        private Transform _upperChest;
        private float _ungroundedTime;
        private void Awake()
        {
            if (!HasValidReferences())
            {
                Debug.LogError(
                    "PlayerLeanController tiene referencias sin asignar. " +
                    "Completa el prefab antes de ejecutar.",
                    this
                );
                enabled = false;
                return;
            }

            CacheHumanoidBones();
        }

        private void OnEnable()
        {
            State = PlayerLeanState.Center;
            CurrentLean = 0f;
            IsTemporarilySuppressed = false;
            _ungroundedTime = 0f;
        }

        private void OnDisable()
        {
            CurrentLean = 0f;
        }

        private void Update()
        {
            if (input == null)
            {
                SetCenter();
                IsTemporarilySuppressed = false;
                _ungroundedTime = 0f;
                UpdateLeanValue(0f);
                return;
            }

            if (input.LeanLeftPressed)
            {
                ToggleLeft();
            }
            else if (input.LeanRightPressed)
            {
                ToggleRight();
            }

            UpdateGroundedGraceTimer();

            // Solo estados que realmente invalidan el lean borran la selección.
            // Caminar, correr, apuntar, un frame sin Grounded o una recarga NO
            // deben hacer perder el lado que el jugador eligió.
            if (ShouldClearLeanState())
            {
                SetCenter();
            }

            IsTemporarilySuppressed = ShouldTemporarilySuppressLean();

            float target = IsTemporarilySuppressed
                ? 0f
                : ResolveObstructedTarget(TargetLean);

            UpdateLeanValue(target);
        }

        public void ToggleLeft()
        {
            State = State == PlayerLeanState.Left
                ? PlayerLeanState.Center
                : PlayerLeanState.Left;
        }

        public void ToggleRight()
        {
            State = State == PlayerLeanState.Right
                ? PlayerLeanState.Center
                : PlayerLeanState.Right;
        }

        public void SetCenter()
        {
            State = PlayerLeanState.Center;
        }

        private void UpdateLeanValue(float target)
        {
            CurrentLean = Mathf.MoveTowards(
                CurrentLean,
                target,
                leanSpeed * Time.deltaTime
            );
        }

        private void UpdateGroundedGraceTimer()
        {
            if (motor == null || motor.IsGrounded)
            {
                _ungroundedTime = 0f;
                return;
            }

            _ungroundedTime += Time.deltaTime;
        }

        private bool ShouldClearLeanState()
        {
            if (health != null && !health.IsAlive)
            {
                return true;
            }

            // Durante avión/paracaídas no conservamos una selección de lean de
            // la fase terrestre anterior.
            if (parachute != null && parachute.IsAirbornePhase)
            {
                return true;
            }

            if (motor != null && cancelWhileProne && motor.IsProne)
            {
                return true;
            }

            return false;
        }

        private bool ShouldTemporarilySuppressLean()
        {
            if (State == PlayerLeanState.Center)
            {
                return false;
            }

            if (motor != null)
            {
                // CharacterController.isGrounded puede fluctuar uno o varios
                // frames al bajar pendientes o superar escalones. Esperamos un
                // pequeño margen antes de considerar que realmente está en aire.
                if (
                    cancelWhileAirborne &&
                    !motor.IsGrounded &&
                    _ungroundedTime >= airborneSuppressDelay
                )
                {
                    return true;
                }

                if (
                    cancelWhileSprinting &&
                    motor.MovementState == PlayerMovementState.Sprinting
                )
                {
                    return true;
                }
            }

            if (
                cancelWhileReloading &&
                equipment != null &&
                equipment.CombatState == PlayerCombatState.Reloading
            )
            {
                return true;
            }

            return false;
        }

        private float ResolveObstructedTarget(float requestedLean)
        {
            if (
                Mathf.Abs(requestedLean) <= 0.001f ||
                clearanceDistance <= 0f
            )
            {
                return requestedLean;
            }

            if (_hips == null &&
                _spine == null &&
                _chest == null &&
                _upperChest == null)
            {
                CacheHumanoidBones();
            }

            Transform originBone =
                _upperChest != null
                    ? _upperChest
                    : _chest != null
                        ? _chest
                        : _spine != null
                            ? _spine
                            : _hips;

            Vector3 origin = originBone != null
                ? originBone.position
                : transform.position + Vector3.up * 1.25f;

            Vector3 direction =
                transform.right * Mathf.Sign(requestedLean);

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                clearanceRadius,
                direction,
                clearanceDistance,
                obstructionMask,
                QueryTriggerInteraction.Ignore
            );

            float closestDistance = clearanceDistance;
            bool foundObstruction = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                Transform hitTransform = hit.collider.transform;

                if (
                    hitTransform == transform ||
                    hitTransform.IsChildOf(transform)
                )
                {
                    continue;
                }

                closestDistance = Mathf.Min(
                    closestDistance,
                    hit.distance
                );
                foundObstruction = true;
            }

            if (!foundObstruction)
            {
                return requestedLean;
            }

            float usableDistance = Mathf.Max(
                0f,
                closestDistance - clearanceRadius * 0.5f
            );

            float allowedLean = Mathf.Clamp01(
                usableDistance / clearanceDistance
            );

            return Mathf.Sign(requestedLean) * allowedLean;
        }

        private bool HasValidReferences()
        {
            return input != null &&
                   motor != null &&
                   equipment != null &&
                   health != null &&
                   parachute != null &&
                   animator != null &&
                   animator.isHuman;
        }

        private void CacheHumanoidBones()
        {
            if (animator == null || !animator.isHuman)
            {
                _hips = null;
                _spine = null;
                _chest = null;
                _upperChest = null;
                return;
            }

            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            _spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            _chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            _upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        }
    }
}
