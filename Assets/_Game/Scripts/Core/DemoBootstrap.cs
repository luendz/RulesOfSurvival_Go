using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using UnityEngine;
using ROS.Game.Input;
using ROS.Game.UI;

namespace ROS.Game.Core
{
    public sealed class DemoBootstrap : MonoBehaviour
    {
        [SerializeField] private BattleRoyaleManager matchManager;
        [SerializeField] private Health[] players;
        [SerializeField] private bool beginOnStart = true;

        public void SetBeginOnStart(bool enabled)
        {
            beginOnStart = enabled;
        }

        private void Start()
        {
            if (matchManager == null) matchManager = FindFirstObjectByType<BattleRoyaleManager>();
            if (players == null || players.Length == 0) players = FindObjectsByType<Health>(FindObjectsSortMode.None);
            if (matchManager == null) return;
            foreach (var player in players) if (player != null) matchManager.RegisterPlayer(player);
            EnsureMatchResultPresenter();
            if (beginOnStart) matchManager.BeginMatch();
        }

        private void EnsureMatchResultPresenter()
        {
            MatchResultPresenter presenter =
                FindFirstObjectByType<
                    MatchResultPresenter
                >();

            if (presenter == null)
            {
                presenter =
                    new GameObject(
                        "MatchResultPresenter"
                    ).AddComponent<
                        MatchResultPresenter
                    >();
            }

            PlayerInputReader localInput =
                BattleRoyaleBotController.FindLocalPlayerInput();

            Health localHealth =
                localInput != null
                    ? localInput.GetComponent<Health>()
                    : null;

            presenter.Bind(
                matchManager,
                localHealth
            );
        }
    }
}
