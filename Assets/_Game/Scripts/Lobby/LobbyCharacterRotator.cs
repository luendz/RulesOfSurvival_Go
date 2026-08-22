using UnityEngine;
using UnityEngine.EventSystems;

namespace ROS.Game.Lobby
{
    public sealed class LobbyCharacterRotator : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        [SerializeField] private Transform target;
        [SerializeField] private float degreesPerPixel = 0.28f;
        [SerializeField] private float smoothTime = 0.06f;
        [SerializeField] private float inertia = 0.88f;
        [SerializeField] private float minimumVelocity = 0.02f;

        private float _currentYaw;
        private float _targetYaw;
        private float _smoothVelocity;
        private float _dragVelocity;
        private bool _dragging;

        public void Configure(Transform rotationTarget)
        {
            target = rotationTarget;

            if (target == null)
            {
                return;
            }

            _currentYaw = target.eulerAngles.y;
            _targetYaw = _currentYaw;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragging = true;
            _dragVelocity = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            float delta = -eventData.delta.x * degreesPerPixel;
            _targetYaw += delta;
            _dragVelocity = delta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragging = false;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (!_dragging && Mathf.Abs(_dragVelocity) > minimumVelocity)
            {
                _targetYaw += _dragVelocity;
                _dragVelocity *= Mathf.Pow(inertia, Time.unscaledDeltaTime * 60f);
            }
            else if (!_dragging)
            {
                _dragVelocity = 0f;
            }

            _currentYaw = Mathf.SmoothDampAngle(
                _currentYaw,
                _targetYaw,
                ref _smoothVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

            target.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        }

        public void ResetRotation(float yaw = 0f)
        {
            _dragVelocity = 0f;
            _smoothVelocity = 0f;
            _targetYaw = yaw;
            _currentYaw = yaw;

            if (target != null)
            {
                target.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }
    }
}
