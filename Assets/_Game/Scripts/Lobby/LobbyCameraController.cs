using UnityEngine;

namespace ROS.Game.Lobby
{
    public sealed class LobbyCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float transitionSpeed = 7f;

        private Vector3 _desiredPosition;
        private Vector3 _desiredLookPoint;
        private bool _configured;

        public LobbyCameraPreset CurrentPreset { get; private set; } =
            LobbyCameraPreset.FullBody;

        public void Configure(Transform lookTarget)
        {
            target = lookTarget;
            _configured = target != null;
            SetPreset(LobbyCameraPreset.FullBody, true);
        }

        public void SetPreset(
            LobbyCameraPreset preset,
            bool immediate = false
        )
        {
            CurrentPreset = preset;

            Vector3 basePosition =
                target != null ? target.position : Vector3.zero;

            switch (preset)
            {
                case LobbyCameraPreset.UpperBody:
                    _desiredPosition = basePosition + new Vector3(0f, 1.55f, 3.25f);
                    _desiredLookPoint = basePosition + new Vector3(0f, 1.38f, 0f);
                    break;

                default:
                    _desiredPosition = basePosition + new Vector3(0f, 1.45f, 4.65f);
                    _desiredLookPoint = basePosition + new Vector3(0f, 1.05f, 0f);
                    break;
            }

            if (immediate)
            {
                transform.position = _desiredPosition;
                LookAtDesiredPoint();
            }
        }

        private void LateUpdate()
        {
            if (!_configured || target == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(
                transform.position,
                _desiredPosition,
                t
            );

            Quaternion desiredRotation = Quaternion.LookRotation(
                _desiredLookPoint - transform.position,
                Vector3.up
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                t
            );
        }

        private void LookAtDesiredPoint()
        {
            Vector3 direction = _desiredLookPoint - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
    }
}
