using System;
using System.Collections.Generic;
using ROS.Game.Combat;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.BattleRoyale
{
    public sealed class BattleRoyaleManager : MonoBehaviour
    {
        [SerializeField] private SafeZoneController safeZone;
        [SerializeField] private float initialZoneRadius = 350f;

        public MatchState State { get; private set; } = MatchState.WaitingPlayers;
        public int AliveCount { get; private set; }
        public event Action<MatchState> StateChanged;
        public event Action<int> AliveCountChanged;

        private readonly List<Health> _players = new List<Health>();

        public void RegisterPlayer(Health health)
        {
            if (health == null || _players.Contains(health)) return;
            _players.Add(health);
            health.Died += _ => RefreshAliveCount();
            RefreshAliveCount();
        }

        public void BeginMatch()
        {
            SetState(MatchState.Playing);
            if (safeZone != null) safeZone.Begin(Vector3.zero, initialZoneRadius);
            RefreshAliveCount();
        }

        private void Update()
        {
            if (State != MatchState.Playing && State != MatchState.FinalCircle) return;
            for (int i = 0; i < _players.Count; i++)
                if (_players[i] != null && safeZone != null) safeZone.ApplyZoneDamage(_players[i]);

            if (AliveCount <= 1 && _players.Count > 1) SetState(MatchState.Finished);
        }

        private void RefreshAliveCount()
        {
            int alive = 0;
            foreach (var player in _players) if (player != null && player.IsAlive) alive++;
            AliveCount = alive;
            AliveCountChanged?.Invoke(alive);
        }

        private void SetState(MatchState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
