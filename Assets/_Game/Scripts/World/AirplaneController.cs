using System;
using UnityEngine;

namespace ROS.Game.World
{
    public sealed class AirplaneController : MonoBehaviour
    {
        public static readonly Vector3 ModelEulerAngles =
            new Vector3(-90f, -90f, 0f);

        [Header("Passenger")]
        [SerializeField] private Transform passengerAnchor;
        [SerializeField] private Vector3 passengerOffset =
            new Vector3(0f, -1.6f, 0f);

        [Header("Flight")]
        [SerializeField] private float flightDuration = 45f;

        [Header("Route Exit")]
        [Tooltip("Distancia adicional que recorre el avión después de terminar la ruta principal.")]
        [Min(0f)]
        [SerializeField] private float exitDistance = 55f;

        [Tooltip("Tiempo del tramo de salida fuera del mapa antes de ocultar el avión.")]
        [Min(0.1f)]
        [SerializeField] private float exitDuration = 4f;

        [Tooltip("Desactiva el GameObject del avión cuando termina el tramo de salida.")]
        [SerializeField] private bool deactivateAfterExit = true;

        public float Progress { get; private set; }
        public bool IsFlying { get; private set; }
        public bool HasCompletedRoute { get; private set; }
        public bool IsExiting { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Transform PassengerAnchor => EnsurePassengerAnchor();

        public event Action FlightStarted;
        public event Action<float> FlightProgressed;
        public event Action RouteCompleted;
        public event Action FlightFinished;

        private Vector3 _startPosition;
        private Vector3 _endPosition;
        private Vector3 _exitEndPosition;
        private Vector3 _routeDirection;
        private Vector3 _lastPosition;
        private float _exitProgress;

        public void PrepareRoute(Vector3 start, Vector3 end)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            _startPosition = start;
            _endPosition = end;

            Vector3 route = _endPosition - _startPosition;
            _routeDirection = route.sqrMagnitude > 0.0001f
                ? route.normalized
                : transform.forward;

            _exitEndPosition =
                _endPosition + _routeDirection * Mathf.Max(0f, exitDistance);

            Progress = 0f;
            _exitProgress = 0f;
            Velocity = Vector3.zero;
            IsFlying = false;
            HasCompletedRoute = false;
            IsExiting = false;

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
                return;

            BeginFlight(start.position, end.position, duration);
        }

        private void Update()
        {
            if (!IsFlying)
                return;

            if (!HasCompletedRoute)
            {
                UpdateMainRoute();
                return;
            }

            UpdateExitRoute();
        }

        private void UpdateMainRoute()
        {
            Progress = Mathf.Clamp01(
                Progress + Time.deltaTime / flightDuration
            );

            transform.position = Vector3.Lerp(
                _startPosition,
                _endPosition,
                Progress
            );

            UpdateVelocity();
            FlightProgressed?.Invoke(Progress);

            if (Progress < 1f)
                return;

            HasCompletedRoute = true;
            IsExiting = true;
            _exitProgress = 0f;

            // Este evento ocurre mientras el avión conserva la velocidad de la
            // ruta principal. Los pasajeros pendientes pueden saltar con esa
            // inercia antes de iniciar el tramo de salida.
            RouteCompleted?.Invoke();

            if (exitDistance <= 0.001f)
                FinishFlight();
        }

        private void UpdateExitRoute()
        {
            if (!IsExiting)
                return;

            _exitProgress = Mathf.Clamp01(
                _exitProgress + Time.deltaTime / Mathf.Max(0.1f, exitDuration)
            );

            transform.position = Vector3.Lerp(
                _endPosition,
                _exitEndPosition,
                _exitProgress
            );

            UpdateVelocity();

            if (_exitProgress < 1f)
                return;

            FinishFlight();
        }

        private void UpdateVelocity()
        {
            Velocity = Time.deltaTime > 0f
                ? (transform.position - _lastPosition) / Time.deltaTime
                : Vector3.zero;

            _lastPosition = transform.position;
        }

        private void FinishFlight()
        {
            IsFlying = false;
            IsExiting = false;
            Velocity = Vector3.zero;

            FlightFinished?.Invoke();

            if (deactivateAfterExit)
                gameObject.SetActive(false);
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
                return passengerAnchor;

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
