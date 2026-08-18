using System;
using UnityEngine;

namespace ROS.Game.World
{
    public enum PingType { Location, Enemy, Loot, Vehicle, Danger }

    public sealed class PingMarker : MonoBehaviour
    {
        [SerializeField] private PingType type;
        [SerializeField] private float lifetime = 12f;
        public PingType Type => type;
        public event Action<PingMarker> Expired;
        private float _remaining;
        private void OnEnable() => _remaining = lifetime;
        private void Update()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) { Expired?.Invoke(this); Destroy(gameObject); }
        }
        public void Configure(PingType pingType, float seconds) { type = pingType; lifetime = seconds; _remaining = seconds; }
    }
}
