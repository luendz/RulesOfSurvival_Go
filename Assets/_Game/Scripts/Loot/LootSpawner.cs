using System;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Loot
{
    [Serializable]
    public struct LootEntry
    {
        public InventoryItemDefinition item;
        [Min(0.01f)] public float weight;
        [Min(1)] public int minAmount;
        [Min(1)] public int maxAmount;
    }

    public sealed class LootSpawner : MonoBehaviour
    {
        [SerializeField] private LootPickup pickupPrefab;
        [SerializeField] private LootEntry[] entries;
        [SerializeField] private int spawnCount = 3;
        [SerializeField] private float radius = 2f;

        public void Spawn()
        {
            if (pickupPrefab == null || entries == null || entries.Length == 0) return;
            for (int i = 0; i < spawnCount; i++)
            {
                var entry = PickWeighted();
                if (entry.item == null) continue;
                Vector2 r = UnityEngine.Random.insideUnitCircle * radius;
                var pickup = Instantiate(pickupPrefab, transform.position + new Vector3(r.x, 0.2f, r.y), Quaternion.identity);
                pickup.Configure(entry.item, UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1));
            }
        }

        private LootEntry PickWeighted()
        {
            float total = 0f;
            foreach (var entry in entries) total += Mathf.Max(0f, entry.weight);
            float roll = UnityEngine.Random.value * total;
            foreach (var entry in entries)
            {
                roll -= Mathf.Max(0f, entry.weight);
                if (roll <= 0f) return entry;
            }
            return entries[entries.Length - 1];
        }
    }
}
