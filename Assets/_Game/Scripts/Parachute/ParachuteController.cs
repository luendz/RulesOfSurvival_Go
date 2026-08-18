using ROS.Game.Character;
using ROS.Game.Input;
using UnityEngine;

namespace ROS.Game.Parachute
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ParachuteController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private GameObject parachuteVisual;
        [SerializeField] private float deployHeight = 70f;
        [SerializeField] private float fallSpeed = 18f;
        [SerializeField] private float glideFallSpeed = 5f;
        [SerializeField] private float glideSpeed = 10f;
        [SerializeField] private float steerSpeed = 85f;

        public bool IsParachuting { get; private set; }
        public bool IsAirbornePhase { get; private set; }

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (parachuteVisual != null) parachuteVisual.SetActive(false);
            enabled = false;
        }

        public void BeginAirDrop()
        {
            IsAirbornePhase = true;
            IsParachuting = false;
            if (motor != null) motor.enabled = false;
            enabled = true;
        }

        private void Update()
        {
            if (!IsAirbornePhase) return;
            if (!IsParachuting && transform.position.y <= deployHeight) Deploy();

            if (IsParachuting)
            {
                float yaw = input != null ? input.Move.x * steerSpeed * Time.deltaTime : 0f;
                transform.Rotate(0f, yaw, 0f);
                float forward = input != null ? Mathf.Max(0f, input.Move.y) : 0f;
                Vector3 motion = transform.forward * (forward * glideSpeed) + Vector3.down * glideFallSpeed;
                _controller.Move(motion * Time.deltaTime);
            }
            else
            {
                Vector3 localPlanar = input != null ? new Vector3(input.Move.x, 0f, input.Move.y) : Vector3.zero;
                Vector3 planar = transform.TransformDirection(localPlanar) * 5f;
                _controller.Move((planar + Vector3.down * fallSpeed) * Time.deltaTime);
            }

            if (_controller.isGrounded && transform.position.y < deployHeight)
            {
                IsAirbornePhase = false;
                IsParachuting = false;
                if (parachuteVisual != null) parachuteVisual.SetActive(false);
                if (motor != null) motor.enabled = true;
                enabled = false;
            }
        }

        private void Deploy()
        {
            IsParachuting = true;
            if (parachuteVisual != null) parachuteVisual.SetActive(true);
        }
    }
}
