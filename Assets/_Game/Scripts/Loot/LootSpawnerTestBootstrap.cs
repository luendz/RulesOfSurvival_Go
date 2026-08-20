using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Loot
{
    public static class LootSpawnerTestBootstrap
    {
        private const string TargetSceneName = "07_BattleRoyaleTest";
        private const string SpawnerResourcePath = "LootSpawner_TestArea";
        private const string LootSystemName = "LootSystem";
        private const string PlayerName = "Player_Prototype";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Scene scene = SceneManager.GetActiveScene();

            if (scene.name != TargetSceneName)
            {
                return;
            }

            if (Object.FindFirstObjectByType<LootSpawner>() != null)
            {
                return;
            }

            GameObject spawnerPrefab =
                Resources.Load<GameObject>(SpawnerResourcePath);

            if (spawnerPrefab == null)
            {
                Debug.LogWarning(
                    "No se encontró LootSpawner_TestArea en Resources."
                );
                return;
            }

            GameObject lootSystem = GameObject.Find(LootSystemName);

            if (lootSystem == null)
            {
                lootSystem = new GameObject(LootSystemName);
            }

            Vector3 spawnCenter = ResolveSpawnCenter();

            GameObject spawnerInstance =
                Object.Instantiate(
                    spawnerPrefab,
                    spawnCenter,
                    Quaternion.identity,
                    lootSystem.transform
                );

            spawnerInstance.name = "LootSpawner_TestArea";
        }

        private static Vector3 ResolveSpawnCenter()
        {
            GameObject player = GameObject.Find(PlayerName);

            if (player == null)
            {
                return new Vector3(0f, 0f, 10f);
            }

            Vector3 forward = player.transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            return player.transform.position + forward * 10f;
        }
    }
}
