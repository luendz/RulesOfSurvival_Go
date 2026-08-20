using ROS.Game.AI;
using ROS.Game.Character;
using ROS.Game.Input;
using ROS.Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Combat
{
    public static class PlayerDamageRuntimeSetup
    {
        private const string WatcherName = "PlayerDamageRuntimeSetup";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (GameObject.Find(WatcherName) != null)
            {
                return;
            }

            GameObject watcher = new GameObject(WatcherName);
            watcher.hideFlags = HideFlags.DontSave;
            watcher.AddComponent<PlayerDamageRuntimeWatcher>();
        }

        public static void ConfigureExistingPlayers()
        {
            Health[] healthComponents =
                Object.FindObjectsByType<Health>(
                    FindObjectsSortMode.None
                );

            foreach (Health health in healthComponents)
            {
                if (health == null ||
                    health.GetComponent<PlayerMotor>() == null)
                {
                    continue;
                }

                bool isBot = BattleRoyaleBotController.IsBot(health);

                if (health.GetComponent<ProtectiveEquipment>() == null)
                {
                    health.gameObject.AddComponent<ProtectiveEquipment>();
                }

                PlayerDamageHitboxRig rig =
                    health.GetComponent<PlayerDamageHitboxRig>();

                if (rig == null)
                {
                    rig = health.gameObject
                        .AddComponent<PlayerDamageHitboxRig>();
                }

                rig.EnsureHitboxes();

                if (health.GetComponent<FallDamageReceiver>() == null)
                {
                    health.gameObject.AddComponent<FallDamageReceiver>();
                }

                if (!isBot &&
                    health.GetComponent<PlayerInputReader>() != null &&
                    health.GetComponent<CombatFeedbackPresenter>() == null)
                {
                    health.gameObject.AddComponent<CombatFeedbackPresenter>();
                }

                if (SceneManager.GetActiveScene().name ==
                        "07_BattleRoyaleTest" &&
                    !isBot &&
                    health.GetComponent<PlayerInputReader>() != null &&
                    health.GetComponent<DamageDebugControls>() == null)
                {
                    health.gameObject.AddComponent<DamageDebugControls>();
                }
            }
        }

        private sealed class PlayerDamageRuntimeWatcher : MonoBehaviour
        {
            private float _nextRefresh;

            private void Start()
            {
                ConfigureExistingPlayers();
            }

            private void Update()
            {
                if (Time.unscaledTime < _nextRefresh)
                {
                    return;
                }

                _nextRefresh = Time.unscaledTime + 1f;
                ConfigureExistingPlayers();
            }
        }
    }
}
