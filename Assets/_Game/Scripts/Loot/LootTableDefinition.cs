using System;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Loot
{
    [Serializable]
    public struct LootEntry
    {
        public InventoryItemDefinition item;

        [Min(0.01f)]
        public float weight;

        [Min(1)]
        public int minAmount;

        [Min(1)]
        public int maxAmount;

        public bool IsValid =>
            item != null &&
            weight > 0f;

        public int GetRandomAmount()
        {
            int minimum = Mathf.Max(1, minAmount);
            int maximum = Mathf.Max(minimum, maxAmount);

            return UnityEngine.Random.Range(
                minimum,
                maximum + 1
            );
        }
    }

    [CreateAssetMenu(
        menuName = "ROS/Loot/Loot Table",
        fileName = "LootTable_"
    )]
    public sealed class LootTableDefinition : ScriptableObject
    {
        [SerializeField]
        private LootEntry[] entries;

        public bool HasValidEntries()
        {
            if (entries == null || entries.Length == 0)
            {
                return false;
            }

            foreach (LootEntry entry in entries)
            {
                if (entry.IsValid)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryPick(out LootEntry selected)
        {
            selected = default;

            if (entries == null || entries.Length == 0)
            {
                return false;
            }

            float totalWeight = 0f;

            foreach (LootEntry entry in entries)
            {
                if (!entry.IsValid)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0f, entry.weight);
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            float roll =
                UnityEngine.Random.value * totalWeight;

            LootEntry fallback = default;
            bool hasFallback = false;

            foreach (LootEntry entry in entries)
            {
                if (!entry.IsValid)
                {
                    continue;
                }

                float entryWeight =
                    Mathf.Max(0f, entry.weight);

                fallback = entry;
                hasFallback = true;

                roll -= entryWeight;

                if (roll <= 0f)
                {
                    selected = entry;
                    return true;
                }
            }

            if (hasFallback)
            {
                selected = fallback;
                return true;
            }

            return false;
        }
    }
}
