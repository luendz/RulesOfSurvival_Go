using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using UnityEngine;

namespace ROS.Game.Core
{
    public sealed class DemoBootstrap : MonoBehaviour
    {
        [SerializeField] private BattleRoyaleManager matchManager;
        [SerializeField] private Health[] players;
        [SerializeField] private bool beginOnStart = true;

        private void Start()
        {
            if (matchManager == null) matchManager = FindFirstObjectByType<BattleRoyaleManager>();
            if (players == null || players.Length == 0) players = FindObjectsByType<Health>(FindObjectsSortMode.None);
            if (matchManager == null) return;
            foreach (var player in players) if (player != null) matchManager.RegisterPlayer(player);
            if (beginOnStart) matchManager.BeginMatch();
        }
    }
}
