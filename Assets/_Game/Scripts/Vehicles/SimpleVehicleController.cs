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
        private float _throttleInput;
        private float _steeringInput;

        public bool IsPlayerControlled => playerControlled;
        public float ThrottleInput => _throttleInput;
        public float SteeringInput => _steeringInput;
        public float NormalizedSpeed => _rb != null
            ? Mathf.Clamp01(_rb.linearVelocity.magnitude / Mathf.Max(0.01f, maxSpeed))
            : 0f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void SetControlled(bool value)
        {
            playerControlled = value;

            if (!value)
            {
                _throttleInput = 0f;
                _steeringInput = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (!playerControlled || Keyboard.current == null)
            {
                _throttleInput = 0f;
                _steeringInput = 0f;
                return;
            }

            float throttle = 0f;
            if (Keyboard.current.wKey.isPressed) throttle += 1f;
            if (Keyboard.current.sKey.isPressed) throttle -= 1f;

            float steer = 0f;
            if (Keyboard.current.dKey.isPressed) steer += 1f;
            if (Keyboard.current.aKey.isPressed) steer -= 1f;

            _throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            _steeringInput = Mathf.Clamp(steer, -1f, 1f);

            if (_rb.linearVelocity.magnitude < maxSpeed ||
                Vector3.Dot(_rb.linearVelocity, transform.forward) * _throttleInput < 0f)
            {
                _rb.AddForce(
                    transform.forward * (_throttleInput * acceleration),
                    ForceMode.Acceleration
                );
            }

            float steeringFactor = Mathf.Clamp01(_rb.linearVelocity.magnitude / 2f);
            _rb.MoveRotation(
                _rb.rotation * Quaternion.Euler(
                    0f,
                    _steeringInput * steering * steeringFactor * Time.fixedDeltaTime,
                    0f
                )
            );

            _rb.linearDamping = Mathf.Abs(_throttleInput) < 0.01f
                ? brakeDrag
                : 0.15f;
        }
    }
}
