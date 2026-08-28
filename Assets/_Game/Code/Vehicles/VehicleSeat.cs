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

        private GameObject _occupant;
        private PlayerAnimationCoordinator _occupantAnimation;

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
            if (_occupant == null || _occupantAnimation == null)
                return;

            float normalizedSpeed = vehicle != null
                ? vehicle.NormalizedSpeed
                : 0f;
            float steeringInput = driverSeat && vehicle != null
                ? vehicle.SteeringInput
                : 0f;

            _occupantAnimation.SetVehicleAnimationState(
                true,
                driverSeat ? VehicleRoleDriver : VehicleRolePassenger,
                (int)animationStyle,
                normalizedSpeed,
                steeringInput
            );
        }

        private void Enter(GameObject player)
        {
            PlayerAnimationCoordinator animation =
                player.GetComponent<PlayerAnimationCoordinator>();
            if (animation == null)
            {
                Debug.LogError(
                    "El jugador no tiene PlayerAnimationCoordinator configurado.",
                    player
                );
                return;
            }

            _occupant = player;
            _occupantAnimation = animation;

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

            _occupantAnimation.SetVehicleAnimationState(
                true,
                driverSeat ? VehicleRoleDriver : VehicleRolePassenger,
                (int)animationStyle,
                0f,
                0f
            );
        }

        private void Exit(GameObject player)
        {
            _occupantAnimation?.SetVehicleAnimationState(
                false,
                0,
                0,
                0f,
                0f
            );

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

            _occupantAnimation = null;
            _occupant = null;
        }
    }
}
