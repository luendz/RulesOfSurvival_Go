using System;
using System.Collections.Generic;
using ROS.Game.Combat;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.BattleRoyale
{
    public sealed class BattleRoyaleManager : MonoBehaviour
    {
        [Header("Safe Zone")]
        [SerializeField] private SafeZoneController safeZone;
        [SerializeField] private float initialZoneRadius = 350f;

        [Header("Match")]
        [Min(0)]
        [SerializeField] private int finalCirclePhaseIndex = 3;

        public MatchState State
        {
            get;
            private set;
        } = MatchState.WaitingPlayers;

        public int AliveCount
        {
            get;
            private set;
        }

        public Health Winner { get; private set; }
        public EliminationInfo LastElimination { get; private set; }

        public SafeZoneController SafeZone => safeZone;

        public event Action<MatchState> StateChanged;
        public event Action<int> AliveCountChanged;
        public event Action<EliminationInfo> PlayerEliminated;
        public event Action<Health> MatchFinished;

        private readonly List<Health> _players =
            new List<Health>();

        private readonly Dictionary<
            Health,
            Action<DamageInfo>
        > _deathHandlers =
            new Dictionary<Health, Action<DamageInfo>>();

        private readonly Dictionary<Health, int> _killCounts =
            new Dictionary<Health, int>();

        private readonly Dictionary<Health, int> _placements =
            new Dictionary<Health, int>();

        public void RegisterPlayer(Health health)
        {
            if (health == null)
                return;

            if (_players.Contains(health))
                return;

            _players.Add(health);

            PlayerEliminationController elimination =
                health.GetComponent<PlayerEliminationController>();

            if (elimination == null)
            {
                elimination =
                    health.gameObject.AddComponent<
                        PlayerEliminationController
                    >();
            }

            elimination.Bind(this);

            Action<DamageInfo> handler =
                damage => OnPlayerDied(health, damage);

            _deathHandlers.Add(health, handler);
            health.Died += handler;

            RefreshAliveCount();
        }

        private void OnDestroy()
        {
            foreach (
                KeyValuePair<Health, Action<DamageInfo>> pair
                in _deathHandlers
            )
            {
                if (pair.Key != null)
                {
                    pair.Key.Died -= pair.Value;
                }
            }

            _deathHandlers.Clear();
        }

        public void BeginWarmup()
        {
            ResetMatchProgress();
            SetState(MatchState.Warmup);
            RefreshAliveCount();
        }

        public void BeginPlanePhase()
        {
            SetState(MatchState.Plane);
            RefreshAliveCount();
        }

        public void BeginGameplay()
        {
            SetState(MatchState.Playing);

            if (safeZone != null)
            {
                safeZone.Begin(Vector3.zero, initialZoneRadius);
            }

            RefreshAliveCount();
        }

        public void BeginMatch()
        {
            ResetMatchProgress();
            BeginGameplay();
        }

        private void ResetMatchProgress()
        {
            Winner = null;
            LastElimination = default;
            _killCounts.Clear();
            _placements.Clear();
        }

        public int GetKillCount(Health player)
        {
            if (
                player != null &&
                _killCounts.TryGetValue(player, out int count)
            )
            {
                return count;
            }

            return 0;
        }

        public bool TryGetPlacement(
            Health player,
            out int placement
        )
        {
            placement = 0;

            if (player == null)
            {
                return false;
            }

            return _placements.TryGetValue(
                player,
                out placement
            );
        }

        private void Update()
        {
            if (
                State != MatchState.Playing &&
                State != MatchState.FinalCircle
            )
            {
                return;
            }

            ApplyZoneDamage();

            UpdateMatchState();

            CheckMatchFinished();
        }

        private void ApplyZoneDamage()
        {
            if (safeZone == null)
                return;

            for (
                int i = 0;
                i < _players.Count;
                i++
            )
            {
                Health player =
                    _players[i];

                if (player == null)
                    continue;

                safeZone.ApplyZoneDamage(
                    player
                );
            }
        }

        private void UpdateMatchState()
        {
            if (safeZone == null)
                return;

            if (
                State == MatchState.FinalCircle
            )
            {
                return;
            }

            if (
                safeZone.CurrentPhase >=
                finalCirclePhaseIndex
            )
            {
                SetState(
                    MatchState.FinalCircle
                );
            }
        }

        private void CheckMatchFinished()
        {
            if (_players.Count <= 1)
                return;

            if (AliveCount <= 1)
            {
                Winner = FindLivingPlayer();

                SetState(
                    MatchState.Finished
                );

                MatchFinished?.Invoke(Winner);
            }
        }

        private void OnPlayerDied(
            Health victim,
            DamageInfo damage
        )
        {
            if (State == MatchState.Finished)
            {
                return;
            }

            RefreshAliveCount();

            int placement =
                Mathf.Max(1, AliveCount + 1);

            _placements[victim] = placement;

            Health killer =
                ResolveInstigatorHealth(
                    damage.Instigator
                );

            if (killer == victim)
            {
                killer = null;
            }

            if (killer != null)
            {
                _killCounts.TryGetValue(
                    killer,
                    out int kills
                );

                _killCounts[killer] = kills + 1;
            }

            LastElimination =
                new EliminationInfo(
                    victim,
                    killer,
                    damage,
                    placement
                );

            PlayerEliminated?.Invoke(
                LastElimination
            );

            CheckMatchFinished();
        }

        private Health FindLivingPlayer()
        {
            foreach (Health player in _players)
            {
                if (
                    player != null &&
                    player.IsAlive
                )
                {
                    return player;
                }
            }

            return null;
        }

        private static Health ResolveInstigatorHealth(
            GameObject instigator
        )
        {
            if (instigator == null)
            {
                return null;
            }

            Health health =
                instigator.GetComponent<Health>();

            if (health == null)
            {
                health =
                    instigator.GetComponentInParent<Health>();
            }

            if (health == null)
            {
                health =
                    instigator.GetComponentInChildren<Health>();
            }

            return health;
        }

        private void RefreshAliveCount()
        {
            int alive = 0;

            foreach (
                Health player in _players
            )
            {
                if (
                    player != null &&
                    player.IsAlive
                )
                {
                    alive++;
                }
            }

            AliveCount = alive;

            AliveCountChanged?.Invoke(
                alive
            );
        }

        private void SetState(
            MatchState state
        )
        {
            if (State == state)
                return;

            State = state;

            StateChanged?.Invoke(
                state
            );
        }
    }
}
