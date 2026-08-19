using ROS.Game.BattleRoyale;
using ROS.Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class BattleRoyalePanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleRoyaleManager battleRoyale;

        [Header("UI - Legacy Text")]
        [SerializeField] private Text aliveText;
        [SerializeField] private Text matchStateText;

        private BattleRoyaleManager _subscribedManager;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            Subscribe();
            RefreshUI();
        }

        private void Start()
        {
            EnsureReferences();
            Subscribe();
            RefreshUI();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void EnsureReferences()
        {
            if (battleRoyale == null)
            {
                battleRoyale =
                    FindFirstObjectByType<BattleRoyaleManager>();
            }
        }

        private void Subscribe()
        {
            if (battleRoyale == null)
                return;

            if (_subscribedManager == battleRoyale)
                return;

            Unsubscribe();

            battleRoyale.AliveCountChanged += OnAliveCountChanged;
            battleRoyale.StateChanged += OnStateChanged;

            _subscribedManager = battleRoyale;
        }

        private void Unsubscribe()
        {
            if (_subscribedManager == null)
                return;

            _subscribedManager.AliveCountChanged -= OnAliveCountChanged;
            _subscribedManager.StateChanged -= OnStateChanged;

            _subscribedManager = null;
        }

        private void OnAliveCountChanged(int aliveCount)
        {
            UpdateAliveText(aliveCount);
        }

        private void OnStateChanged(MatchState state)
        {
            UpdateStateText(state);
        }

        private void RefreshUI()
        {
            if (battleRoyale == null)
            {
                if (aliveText != null)
                    aliveText.text = "VIVOS --";

                if (matchStateText != null)
                    matchStateText.text = string.Empty;

                return;
            }

            UpdateAliveText(
                battleRoyale.AliveCount
            );

            UpdateStateText(
                battleRoyale.State
            );
        }

        private void UpdateAliveText(int aliveCount)
        {
            if (aliveText == null)
                return;

            aliveText.text =
                $"VIVOS {aliveCount}";
        }

        private void UpdateStateText(MatchState state)
        {
            if (matchStateText == null)
                return;

            matchStateText.text =
                GetStateText(state);
        }

        private static string GetStateText(MatchState state)
        {
            switch (state)
            {
                case MatchState.WaitingPlayers:
                    return "ESPERANDO JUGADORES";

                case MatchState.Warmup:
                    return "CALENTAMIENTO";

                case MatchState.Plane:
                    return "EN AVIÓN";

                case MatchState.Playing:
                    return "EN PARTIDA";

                case MatchState.FinalCircle:
                    return "CÍRCULO FINAL";

                case MatchState.Finished:
                    return "PARTIDA FINALIZADA";

                default:
                    return state.ToString();
            }
        }
    }
}