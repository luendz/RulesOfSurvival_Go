using System;
using UnityEngine;

namespace ROS.Game.World
{
    public sealed class AirplaneController : MonoBehaviour
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private float flightDuration = 75f;
        public float Progress { get; private set; }
        public event Action FlightFinished;
        private bool _flying;

        public void BeginFlight(Transform start, Transform end, float duration)
        {
            startPoint = start; endPoint = end; flightDuration = Mathf.Max(1f, duration);
            Progress = 0f; _flying = true;
            if (startPoint != null) transform.position = startPoint.position;
        }

        private void Update()
        {
            if (!_flying || startPoint == null || endPoint == null) return;
            Progress = Mathf.Clamp01(Progress + Time.deltaTime / flightDuration);
            transform.position = Vector3.Lerp(startPoint.position, endPoint.position, Progress);
            Vector3 direction = endPoint.position - startPoint.position;
            if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction.normalized);
            if (Progress >= 1f) { _flying = false; FlightFinished?.Invoke(); }
        }
    }
}
