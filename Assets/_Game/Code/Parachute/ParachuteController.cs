using System;
using ROS.Game.Character;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Parachute
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ParachuteController : MonoBehaviour
    {
        public static readonly Vector3 ModelEulerAngles =
            new Vector3(-120f, 0f, 0f);

        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private CharacterController controller;
        [SerializeField] private GameObject parachuteVisual;
        [SerializeField] private GameObject parachuteBackpackVisual;

        [Header("Free Fall")]
        [Min(1f)]
        [SerializeField] private float freeFallSpeed = 24f;
        [Min(0f)]
        [SerializeField] private float freeFallAcceleration = 18f;
        [Min(0f)]
        [SerializeField] private float freeFallHorizontalSpeed = 9f;
        [Min(0f)]
        [SerializeField] private float airControl = 3.5f;

        [Header("Parachute")]
        [Min(1f)]
        [SerializeField] private float autoDeployGroundHeight = 32f;
        [Min(0f)]
        [SerializeField] private float manualDeployDelay = 0.35f;
        [Min(0.5f)]
        [SerializeField] private float glideFallSpeed = 5f;
        [Min(0f)]
        [SerializeField] private float glideSpeed = 10f;
        [Min(0f)]
        [SerializeField] private float steerSpeed = 85f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Runtime Debug")]
        [SerializeField] private int debugParachuteAnimationState;

        public AirDropState State { get; private set; } =
            AirDropState.Inactive;
        public bool IsParachuting => State == AirDropState.Parachuting;
        public bool IsAirbornePhase =>
            State == AirDropState.FreeFall || IsParachuting;
        public float GroundClearance { get; private set; } =
            float.PositiveInfinity;
        public float VerticalSpeed => _verticalSpeed;

        public event Action<AirDropState> StateChanged;
        public event Action ParachuteDeployed;
        public event Action Landed;

        private Vector3 _horizontalVelocity;
        private float _verticalSpeed;
        private float _airborneElapsed;
        private bool _airborneMovementStarted;
        private WeaponController[] _weapons;
        private bool[] _weaponEnabledStates;
        private bool _equipmentEnabledState;
        private bool _combatSuspended;

        private void Awake()
        {
            EnsureReferences();
            ApplyVisualState(State);
            enabled = false;
        }

        public void PrepareForPlane()
        {
            EnsureReferences();
            SetState(AirDropState.InPlane);
            _horizontalVelocity = Vector3.zero;
            _verticalSpeed = 0f;
            _airborneElapsed = 0f;
            _airborneMovementStarted = false;

            if (motor != null)
            {
                motor.enabled = false;
            }

            SuspendCombat();

            if (controller != null)
            {
                controller.enabled = false;
            }

            enabled = false;
        }

        public void BeginAirDrop()
        {
            BeginAirDrop(Vector3.zero);
        }

        public void BeginAirDrop(Vector3 initialVelocity)
        {
            EnsureReferences();

            if (controller != null)
            {
                controller.enabled = true;
            }

            if (motor != null)
            {
                motor.enabled = false;
            }

            SuspendCombat();

            _horizontalVelocity = Vector3.ProjectOnPlane(
                initialVelocity,
                Vector3.up
            );
            _verticalSpeed = Mathf.Min(initialVelocity.y, -2f);
            _airborneElapsed = 0f;
            _airborneMovementStarted = false;
            SetState(AirDropState.FreeFall);
            enabled = true;
        }

        public bool TryDeploy()
        {
            if (State != AirDropState.FreeFall)
            {
                return false;
            }

            _verticalSpeed = Mathf.Max(_verticalSpeed, -glideFallSpeed);
            SetState(AirDropState.Parachuting);
            ParachuteDeployed?.Invoke();
            return true;
        }

        private void Update()
        {
            if (!IsAirbornePhase || controller == null)
            {
                return;
            }

            UpdateGroundClearance();
            _airborneElapsed += Time.deltaTime;

            if (State == AirDropState.FreeFall &&
                ((input != null &&
                  input.JumpPressed &&
                  _airborneElapsed >= manualDeployDelay) ||
                 ParachuteFlightMath.ShouldAutoDeploy(
                     GroundClearance,
                     autoDeployGroundHeight
                 )))
            {
                TryDeploy();
            }

            Vector3 motion = State == AirDropState.Parachuting
                ? CalculateParachuteMotion()
                : CalculateFreeFallMotion();

            controller.Move(motion * Time.deltaTime);
            _airborneMovementStarted = true;

            if (_airborneMovementStarted &&
                controller.isGrounded &&
                _verticalSpeed <= 0f)
            {
                CompleteLanding();
            }
        }

        private Vector3 CalculateFreeFallMotion()
        {
            Vector2 move = input != null ? input.Move : Vector2.zero;
            Vector3 desired = transform.TransformDirection(
                new Vector3(move.x, 0f, move.y)
            );

            desired = Vector3.ClampMagnitude(desired, 1f) *
                      freeFallHorizontalSpeed;

            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                desired,
                airControl * Time.deltaTime
            );

            _verticalSpeed = Mathf.MoveTowards(
                _verticalSpeed,
                -freeFallSpeed,
                freeFallAcceleration * Time.deltaTime
            );

            return _horizontalVelocity + Vector3.up * _verticalSpeed;
        }

        private Vector3 CalculateParachuteMotion()
        {
            Vector2 move = input != null ? input.Move : Vector2.zero;
            float yaw = move.x * steerSpeed * Time.deltaTime;
            transform.Rotate(0f, yaw, 0f);

            float forwardInput = Mathf.Clamp01((move.y + 1f) * 0.5f);
            _horizontalVelocity = transform.forward *
                                  (forwardInput * glideSpeed);
            _verticalSpeed = -glideFallSpeed;

            return _horizontalVelocity + Vector3.up * _verticalSpeed;
        }

        private void UpdateGroundClearance()
        {
            float rayDistance = Mathf.Max(
                autoDeployGroundHeight * 4f,
                250f
            );

            if (Physics.Raycast(
                    transform.position,
                    Vector3.down,
                    out RaycastHit hit,
                    rayDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                ))
            {
                GroundClearance = hit.distance;
                return;
            }

            GroundClearance = Mathf.Max(0f, transform.position.y);
        }

        private void CompleteLanding()
        {
            _horizontalVelocity = Vector3.zero;
            _verticalSpeed = 0f;
            SetState(AirDropState.Landed);

            if (motor != null)
            {
                motor.enabled = true;
            }

            RestoreCombat();

            enabled = false;
            Landed?.Invoke();
        }

        private void EnsureReferences()
        {
            if (controller == null ||
                input == null ||
                motor == null ||
                equipment == null ||
                parachuteVisual == null ||
                parachuteBackpackVisual == null)
            {
                Debug.LogError(
                    "ParachuteController tiene referencias sin asignar. " +
                    "Completa el prefab antes de ejecutar.",
                    this
                );
            }

            if (_weapons == null)
            {
                _weapons = GetComponentsInChildren<WeaponController>(true);
            }
        }

        private void SuspendCombat()
        {
            if (_combatSuspended)
            {
                return;
            }

            _combatSuspended = true;
            _equipmentEnabledState = equipment != null && equipment.enabled;

            if (equipment != null)
            {
                equipment.enabled = false;
            }

            _weaponEnabledStates = new bool[_weapons.Length];
            for (int i = 0; i < _weapons.Length; i++)
            {
                WeaponController weapon = _weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                _weaponEnabledStates[i] = weapon.enabled;
                weapon.enabled = false;
            }
        }

        private void RestoreCombat()
        {
            if (!_combatSuspended)
            {
                return;
            }

            if (equipment != null)
            {
                equipment.enabled = _equipmentEnabledState;
            }

            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] != null)
                {
                    _weapons[i].enabled =
                        i < _weaponEnabledStates.Length &&
                        _weaponEnabledStates[i];
                }
            }

            _combatSuspended = false;
        }

        private void SetState(AirDropState state)
        {
            bool changed = State != state;

            State = state;
            debugParachuteAnimationState = ResolveAnimatorParachuteState(state);
            ApplyVisualState(state);

            if (changed)
            {
                StateChanged?.Invoke(state);
            }
        }

        private static int ResolveAnimatorParachuteState(AirDropState state)
        {
            switch (state)
            {
                case AirDropState.FreeFall:
                    return 1;
                case AirDropState.Parachuting:
                    return 2;
                case AirDropState.Landed:
                    return 3;
                default:
                    return 0;
            }
        }

        private void ApplyVisualState(AirDropState state)
        {
            if (parachuteVisual != null)
            {
                parachuteVisual.SetActive(
                    state == AirDropState.Parachuting
                );
            }

            if (parachuteBackpackVisual != null)
            {
                parachuteBackpackVisual.SetActive(
                    state == AirDropState.FreeFall
                );
            }
        }
    }
}
