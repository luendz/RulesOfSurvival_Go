using System;
using UnityEngine;

namespace ROS.Game.World
{
    public sealed class AirplaneController : MonoBehaviour
    {
        [SerializeField] private Transform passengerAnchor;
        [SerializeField] private float flightDuration = 45f;
        [SerializeField] private Vector3 passengerOffset =
            new Vector3(0f, -1.6f, 0f);

        public float Progress { get; private set; }
        public bool IsFlying { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Transform PassengerAnchor => EnsurePassengerAnchor();

        public event Action FlightStarted;
        public event Action<float> FlightProgressed;
        public event Action FlightFinished;

        private Vector3 _startPosition;
        private Vector3 _endPosition;
        private Vector3 _lastPosition;

        public void PrepareRoute(Vector3 start, Vector3 end)
        {
            _startPosition = start;
            _endPosition = end;
            Progress = 0f;
            Velocity = Vector3.zero;
            IsFlying = false;
            transform.position = start;
            FaceRoute();
            _lastPosition = transform.position;
        }

        public void BeginFlight(Vector3 start, Vector3 end, float duration)
        {
            PrepareRoute(start, end);
            flightDuration = Mathf.Max(1f, duration);
            IsFlying = true;
            FlightStarted?.Invoke();
        }

        public void BeginFlight(Transform start, Transform end, float duration)
        {
            if (start == null || end == null)
            {
                return;
            }

            BeginFlight(start.position, end.position, duration);
        }

        private void Update()
        {
            if (!IsFlying)
            {
                return;
            }

            Progress = Mathf.Clamp01(
                Progress + Time.deltaTime / flightDuration
            );

            transform.position = Vector3.Lerp(
                _startPosition,
                _endPosition,
                Progress
            );

            Velocity = Time.deltaTime > 0f
                ? (transform.position - _lastPosition) / Time.deltaTime
                : Vector3.zero;

            _lastPosition = transform.position;
            FlightProgressed?.Invoke(Progress);

            if (Progress < 1f)
            {
                return;
            }

            IsFlying = false;
            Velocity = Vector3.zero;
            FlightFinished?.Invoke();
        }

        private void FaceRoute()
        {
            Vector3 direction = _endPosition - _startPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(
                    direction.normalized
                );
            }
        }

        private Transform EnsurePassengerAnchor()
        {
            if (passengerAnchor != null)
            {
                return passengerAnchor;
            }

            Transform existing = transform.Find("PassengerAnchor");
            if (existing != null)
            {
                passengerAnchor = existing;
                return passengerAnchor;
            }

            GameObject anchor = new GameObject("PassengerAnchor");
            passengerAnchor = anchor.transform;
            passengerAnchor.SetParent(transform, false);
            passengerAnchor.localPosition = passengerOffset;
            passengerAnchor.localRotation = Quaternion.identity;
            return passengerAnchor;
        }
    }
}
