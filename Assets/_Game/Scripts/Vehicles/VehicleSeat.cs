using ROS.Game.Character;
using ROS.Game.Interaction;
using UnityEngine;

namespace ROS.Game.Vehicles
{
    public sealed class VehicleSeat : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform seatPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private SimpleVehicleController vehicle;
        [SerializeField] private bool driverSeat = true;

        private GameObject _occupant;
        public string InteractionLabel => _occupant == null ? "Entrar al vehículo" : "Salir del vehículo";

        public bool CanInteract(GameObject interactor) => _occupant == null || _occupant == interactor;

        public void Interact(GameObject interactor)
        {
            if (_occupant == null) Enter(interactor);
            else if (_occupant == interactor) Exit(interactor);
        }

        private void Enter(GameObject player)
        {
            _occupant = player;
            var controller = player.GetComponent<CharacterController>();
            var motor = player.GetComponent<PlayerMotor>();
            if (controller != null) controller.enabled = false;
            if (motor != null) motor.enabled = false;
            player.transform.SetParent(seatPoint != null ? seatPoint : transform, false);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;
            if (driverSeat && vehicle != null) vehicle.SetControlled(true);
        }

        private void Exit(GameObject player)
        {
            player.transform.SetParent(null);
            player.transform.position = exitPoint != null ? exitPoint.position : transform.position + transform.right * 2f;
            var controller = player.GetComponent<CharacterController>();
            var motor = player.GetComponent<PlayerMotor>();
            if (controller != null) controller.enabled = true;
            if (motor != null) motor.enabled = true;
            if (driverSeat && vehicle != null) vehicle.SetControlled(false);
            _occupant = null;
        }
    }
}
