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
        [SerializeField] private bool cancelWhileSprinting = true;
        [SerializeField] private bool cancelWhileProne = true;
        [SerializeField] private bool cancelWhileAirborne = true;
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

        private void Awake()
        {
            EnsureReferences();
            CacheHumanoidBones();
        }

        private void OnEnable()
        {
            State = PlayerLeanState.Center;
            CurrentLean = 0f;
        }

        private void Update()
        {
            EnsureReferences();

            if (input == null)
            {
                SetCenter();
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

            if (ShouldCancelLean())
            {
                SetCenter();
            }

            float target = ResolveObstructedTarget(TargetLean);
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

        private bool ShouldCancelLean()
        {
            if (health != null && !health.IsAlive)
            {
                return true;
            }

            if (parachute != null && parachute.IsAirbornePhase)
            {
                return true;
            }

            if (motor != null)
            {
                if (cancelWhileProne && motor.IsProne)
                {
                    return true;
                }

                if (cancelWhileAirborne && !motor.IsGrounded)
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

        private void EnsureReferences()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (equipment == null)
            {
                equipment = GetComponent<WeaponEquipmentController>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (parachute == null)
            {
                parachute = GetComponent<ParachuteController>();
            }

            if (animator == null || !animator.isHuman)
            {
                Animator[] animators = GetComponentsInChildren<Animator>(true);

                foreach (Animator candidate in animators)
                {
                    if (candidate != null && candidate.isHuman)
                    {
                        animator = candidate;
                        break;
                    }
                }
            }
        }

        private void CacheHumanoidBones()
        {
            EnsureReferences();

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
