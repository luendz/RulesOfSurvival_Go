using ROS.Game.Animation;
using ROS.Game.Character;
using ROS.Game.Interaction;
using UnityEngine;

namespace ROS.Game.Vehicles
{
    public enum VehicleAnimationStyle
    {
        Generic = 0,
        Car = 1,
        Motorcycle = 2,
        ATV = 3,
        Boat = 4,
        Truck = 5
    }

    [DefaultExecutionOrder(120)]
    public sealed class VehicleSeat : MonoBehaviour, IInteractable
    {
        private const int VehicleStateNone = 0;
        private const int VehicleStateSeated = 1;
        private const int VehicleRoleNone = 0;
        private const int VehicleRoleDriver = 1;
        private const int VehicleRolePassenger = 2;

        [Header("Seat")]
        [SerializeField] private Transform seatPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private SimpleVehicleController vehicle;
        [SerializeField] private bool driverSeat = true;

        [Header("Animation")]
        [Tooltip("Estilo de pose para este asiento. Permite reutilizar el mismo sub-state machine Vehicle con autos, motos, ATV, botes, etc.")]
        [SerializeField] private VehicleAnimationStyle animationStyle = VehicleAnimationStyle.Generic;

        private static readonly int VehicleState = Animator.StringToHash("VehicleState");
        private static readonly int VehicleRole = Animator.StringToHash("VehicleRole");
        private static readonly int VehicleStyle = Animator.StringToHash("VehicleStyle");
        private static readonly int VehicleSpeed = Animator.StringToHash("VehicleSpeed");
        private static readonly int VehicleSteer = Animator.StringToHash("VehicleSteer");
        private static readonly int ShouldFall = Animator.StringToHash("ShouldFall");
        private static readonly int FullBodyAction =
            Animator.StringToHash("FullBodyAction");

        private GameObject _occupant;
        private Animator _occupantAnimator;

        public bool IsOccupied => _occupant != null;
        public bool IsDriverSeat => driverSeat;
        public VehicleAnimationStyle AnimationStyle => animationStyle;
        public string InteractionLabel =>
            _occupant == null ? "Entrar al vehículo" : "Salir del vehículo";

        public bool CanInteract(GameObject interactor)
        {
            return _occupant == null || _occupant == interactor;
        }

        public void Interact(GameObject interactor)
        {
            if (_occupant == null)
                Enter(interactor);
            else if (_occupant == interactor)
                Exit(interactor);
        }

        private void Update()
        {
            if (_occupant == null || _occupantAnimator == null)
                return;

            // PlayerAnimationCoordinator se ejecuta antes (80). El asiento se
            // ejecuta en 120 para que la pose completa del vehiculo tenga la
            // ultima palabra mientras el personaje permanezca sentado.
            ApplyVehicleAnimatorState(
                VehicleStateSeated,
                driverSeat ? VehicleRoleDriver : VehicleRolePassenger
            );

            float normalizedSpeed = vehicle != null
                ? vehicle.NormalizedSpeed
                : 0f;
            float steeringInput = driverSeat && vehicle != null
                ? vehicle.SteeringInput
                : 0f;

            SetFloatIfPresent(_occupantAnimator, VehicleSpeed, normalizedSpeed);
            SetFloatIfPresent(_occupantAnimator, VehicleSteer, steeringInput);
            SetBoolIfPresent(_occupantAnimator, ShouldFall, false);

            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.LocomotionLayerName,
                1f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.WeaponUpperBodyLayerName,
                0f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.UpperBodyActionsLayerName,
                0f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.AimRecoilLayerName,
                0f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.FullBodyOverrideLayerName,
                0f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.ClassicWeaponUpperBodyLayerName,
                0f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.ClassicUpperBodyActionsLayerName,
                0f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.ClassicAimOffsetLayerName,
                0f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.ClassicLeanLayerName,
                0f
            );
            SetLayerWeightIfPresent(
                _occupantAnimator,
                PlayerAnimationCoordinator.ClassicFullBodyActionsLayerName,
                1f
            );
        }

        private void Enter(GameObject player)
        {
            _occupant = player;
            _occupantAnimator = player.GetComponentInChildren<Animator>(true);

            CharacterController controller = player.GetComponent<CharacterController>();
            PlayerMotor motor = player.GetComponent<PlayerMotor>();

            if (controller != null)
                controller.enabled = false;
            if (motor != null)
                motor.enabled = false;

            player.transform.SetParent(seatPoint != null ? seatPoint : transform, false);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;

            if (driverSeat && vehicle != null)
                vehicle.SetControlled(true);

            ApplyVehicleAnimatorState(
                VehicleStateSeated,
                driverSeat ? VehicleRoleDriver : VehicleRolePassenger
            );
            SetFloatIfPresent(_occupantAnimator, VehicleSpeed, 0f);
            SetFloatIfPresent(_occupantAnimator, VehicleSteer, 0f);
            SetBoolIfPresent(_occupantAnimator, ShouldFall, false);
        }

        private void Exit(GameObject player)
        {
            if (_occupantAnimator == null)
                _occupantAnimator = player.GetComponentInChildren<Animator>(true);

            ApplyVehicleAnimatorState(VehicleStateNone, VehicleRoleNone);
            SetIntegerIfPresent(_occupantAnimator, VehicleStyle, 0);
            SetFloatIfPresent(_occupantAnimator, VehicleSpeed, 0f);
            SetFloatIfPresent(_occupantAnimator, VehicleSteer, 0f);

            player.transform.SetParent(null);
            player.transform.position = exitPoint != null
                ? exitPoint.position
                : transform.position + transform.right * 2f;

            CharacterController controller = player.GetComponent<CharacterController>();
            PlayerMotor motor = player.GetComponent<PlayerMotor>();

            if (controller != null)
                controller.enabled = true;
            if (motor != null)
                motor.enabled = true;

            if (driverSeat && vehicle != null)
                vehicle.SetControlled(false);

            _occupantAnimator = null;
            _occupant = null;
        }

        private void ApplyVehicleAnimatorState(int state, int role)
        {
            if (_occupantAnimator == null)
                return;

            SetIntegerIfPresent(_occupantAnimator, VehicleState, state);
            SetIntegerIfPresent(_occupantAnimator, VehicleRole, role);
            SetIntegerIfPresent(
                _occupantAnimator,
                VehicleStyle,
                state == VehicleStateNone ? 0 : (int)animationStyle
            );
            SetIntegerIfPresent(
                _occupantAnimator,
                FullBodyAction,
                state == VehicleStateNone
                    ? PlayerAnimationCoordinator.FullBodyActionNone
                    : PlayerAnimationCoordinator.FullBodyActionVehicle
            );
        }

        private static void SetLayerWeightIfPresent(
            Animator animator,
            string layerName,
            float weight)
        {
            if (animator == null)
                return;

            int layerIndex = animator.GetLayerIndex(layerName);
            if (layerIndex < 0 || layerIndex >= animator.layerCount)
                return;

            float value = Mathf.Clamp01(weight);
            if (!Mathf.Approximately(animator.GetLayerWeight(layerIndex), value))
                animator.SetLayerWeight(layerIndex, value);
        }

        private static void SetIntegerIfPresent(
            Animator animator,
            int parameterHash,
            int value)
        {
            if (!HasParameter(animator, parameterHash, AnimatorControllerParameterType.Int))
                return;

            animator.SetInteger(parameterHash, value);
        }

        private static void SetFloatIfPresent(
            Animator animator,
            int parameterHash,
            float value)
        {
            if (!HasParameter(animator, parameterHash, AnimatorControllerParameterType.Float))
                return;

            animator.SetFloat(parameterHash, value);
        }

        private static void SetBoolIfPresent(
            Animator animator,
            int parameterHash,
            bool value)
        {
            if (!HasParameter(animator, parameterHash, AnimatorControllerParameterType.Bool))
                return;

            animator.SetBool(parameterHash, value);
        }

        private static bool HasParameter(
            Animator animator,
            int parameterHash,
            AnimatorControllerParameterType type)
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
    }
}
