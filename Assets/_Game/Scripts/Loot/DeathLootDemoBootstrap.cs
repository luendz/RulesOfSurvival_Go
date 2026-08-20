using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Loot
{
    public static class DeathLootDemoBootstrap
    {
        private const string TargetSceneName =
            "07_BattleRoyaleTest";

        private const string DemoPlayerName =
            "JugadorMuerto_Demo";

        private const string LocalPlayerName =
            "Player_Prototype";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Initialize()
        {
            Scene scene =
                SceneManager.GetActiveScene();

            if (
                scene.name != TargetSceneName ||
                GameObject.Find(DemoPlayerName) != null
            )
            {
                return;
            }

            GameObject localPlayer =
                GameObject.Find(LocalPlayerName);

            if (localPlayer == null)
            {
                return;
            }

            Vector3 forward =
                Vector3.ProjectOnPlane(
                    localPlayer.transform.forward,
                    Vector3.up
                ).normalized;

            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            Vector3 right =
                Vector3.Cross(Vector3.up, forward);

            Vector3 corpsePosition =
                localPlayer.transform.position +
                forward * 2f +
                right * 0.9f;

            Vector3 cratePosition =
                localPlayer.transform.position +
                forward * 1.1f +
                right * 0.15f;

            GameObject corpse =
                new GameObject(DemoPlayerName);

            corpse.transform.position =
                corpsePosition;

            Health health =
                corpse.AddComponent<Health>();

            InventoryComponent inventory =
                corpse.AddComponent<
                    InventoryComponent
                >();

            CreateCorpseVisual(
                corpse.transform,
                forward
            );

            AddDemoItem(
                inventory,
                "demo_ammo_556",
                "Munición 5.56 mm",
                ItemType.Ammo,
                45,
                60,
                0.02f
            );

            AddDemoItem(
                inventory,
                "demo_bandage",
                "Vendaje",
                ItemType.Healing,
                6,
                10,
                0.1f
            );

            AddDemoItem(
                inventory,
                "demo_helmet_2",
                "Casco nivel 2",
                ItemType.Helmet,
                1,
                1,
                3f
            );

            AddDemoItem(
                inventory,
                "demo_scope_4x",
                "Mira 4x",
                ItemType.Attachment,
                1,
                1,
                0.8f
            );

            DeathLootContainer.Create(
                cratePosition,
                inventory
            );

            health.ApplyDamage(
                new DamageInfo(
                    999f,
                    corpsePosition,
                    -forward,
                    null
                )
            );
        }

        private static void AddDemoItem(
            InventoryComponent inventory,
            string stableId,
            string displayName,
            ItemType itemType,
            int amount,
            int maxStack,
            float weight
        )
        {
            InventoryItemDefinition item =
                ScriptableObject.CreateInstance<
                    InventoryItemDefinition
                >();

            item.name = displayName;
            item.itemId = stableId;
            item.displayName = displayName;
            item.itemType = itemType;
            item.dataConfidence =
                DataConfidence.Prototype;
            item.maxStack = maxStack;
            item.weight = weight;
            item.hideFlags = HideFlags.DontSave;

            inventory.Add(item, amount);
        }

        private static void CreateCorpseVisual(
            Transform parent,
            Vector3 forward
        )
        {
            GameObject visual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            visual.name = "Cuerpo_Demo";

            visual.transform.SetParent(
                parent,
                false
            );

            visual.transform.localPosition =
                new Vector3(0f, 0.42f, 0f);

            visual.transform.rotation =
                Quaternion.LookRotation(
                    forward,
                    Vector3.up
                ) *
                Quaternion.Euler(0f, 0f, 90f);

            visual.transform.localScale =
                new Vector3(0.55f, 0.85f, 0.55f);

            Collider collider =
                visual.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }

            Renderer renderer =
                visual.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color =
                    new Color(0.28f, 0.08f, 0.08f);
            }
        }
    }
}
