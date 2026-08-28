using System.Collections.Generic;
using UnityEngine;

namespace ROS.Game.World
{
    public sealed class EchoValleyDoor : MonoBehaviour
    {
        private static readonly List<EchoValleyDoor> ActiveDoorsInternal =
            new List<EchoValleyDoor>();

        public static IReadOnlyList<EchoValleyDoor> ActiveDoors =>
            ActiveDoorsInternal;

        [SerializeField] private float openAngle = 95f;
        [SerializeField] private float rotationSpeed = 240f;
        [SerializeField] private float hingeDirection = 1f;

        private Quaternion _closedLocalRotation;
        private Quaternion _targetLocalRotation;
        private EchoValleyDoor _linkedDoor;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _closedLocalRotation = transform.localRotation;
            _targetLocalRotation = _closedLocalRotation;
        }

        private void OnEnable()
        {
            if (!ActiveDoorsInternal.Contains(this))
            {
                ActiveDoorsInternal.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveDoorsInternal.Remove(this);
        }

        private void Update()
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                _targetLocalRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        public void Configure(
            float angle,
            float speed,
            float direction
        )
        {
            openAngle = Mathf.Clamp(angle, 45f, 130f);
            rotationSpeed = Mathf.Max(30f, speed);
            hingeDirection = Mathf.Sign(direction);
            if (Mathf.Approximately(hingeDirection, 0f))
            {
                hingeDirection = 1f;
            }

            _closedLocalRotation = transform.localRotation;
            _targetLocalRotation = _closedLocalRotation;
        }

        public void Link(EchoValleyDoor other)
        {
            _linkedDoor = other;
        }

        public void Toggle(Vector3 actorPosition)
        {
            bool nextState = !_isOpen;
            SetOpen(nextState, actorPosition, true);
        }

        private void SetOpen(
            bool open,
            Vector3 actorPosition,
            bool propagate
        )
        {
            _isOpen = open;

            if (open)
            {
                Vector3 toActor = actorPosition - transform.position;
                float actorSide = Vector3.Dot(transform.forward, toActor);
                float swingSide = actorSide >= 0f ? -1f : 1f;
                float yaw = openAngle * hingeDirection * swingSide;

                _targetLocalRotation =
                    _closedLocalRotation * Quaternion.Euler(0f, yaw, 0f);
            }
            else
            {
                _targetLocalRotation = _closedLocalRotation;
            }

            if (propagate && _linkedDoor != null)
            {
                _linkedDoor.SetOpen(open, actorPosition, false);
            }
        }
    }
}
