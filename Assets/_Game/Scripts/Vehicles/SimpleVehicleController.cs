using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SimpleVehicleController : MonoBehaviour
    {
        [SerializeField] private float acceleration = 16f;
        [SerializeField] private float maxSpeed = 28f;
        [SerializeField] private float steering = 75f;
        [SerializeField] private float brakeDrag = 2.5f;
        [SerializeField] private bool playerControlled;

        private Rigidbody _rb;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void SetControlled(bool value) => playerControlled = value;

        private void FixedUpdate()
        {
            if (!playerControlled || Keyboard.current == null) return;
            float throttle = 0f;
            if (Keyboard.current.wKey.isPressed) throttle += 1f;
            if (Keyboard.current.sKey.isPressed) throttle -= 1f;
            float steer = 0f;
            if (Keyboard.current.dKey.isPressed) steer += 1f;
            if (Keyboard.current.aKey.isPressed) steer -= 1f;

            if (_rb.linearVelocity.magnitude < maxSpeed || Vector3.Dot(_rb.linearVelocity, transform.forward) * throttle < 0f)
                _rb.AddForce(transform.forward * (throttle * acceleration), ForceMode.Acceleration);

            float steeringFactor = Mathf.Clamp01(_rb.linearVelocity.magnitude / 2f);
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, steer * steering * steeringFactor * Time.fixedDeltaTime, 0f));
            _rb.linearDamping = Mathf.Abs(throttle) < 0.01f ? brakeDrag : 0.15f;
        }
    }
}
