using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class EquipmentStatusPresenter : MonoBehaviour
    {
        [SerializeField] private ProtectiveEquipment protection;
        [SerializeField] private PlayerLootEquipment loot;
        [SerializeField] private Text helmetLabel;
        [SerializeField] private Text vestLabel;
        [SerializeField] private Text backpackLabel;

        private void Awake()
        {
            ResolvePhysicalView();
            ResolveGameplayReferences();
        }

        private void OnEnable()
        {
            ResolveGameplayReferences();
            Subscribe();
            Refresh();
        }

        public void Bind(PlayerLootEquipment lootEquipment, ProtectiveEquipment protectiveEquipment)
        {
            Unsubscribe();
            loot = lootEquipment;
            protection = protectiveEquipment;
            ResolvePhysicalView();
            Subscribe();
            Refresh();
        }

        private void ResolveGameplayReferences()
        {
            PlayerInputReader input = FindFirstObjectByType<PlayerInputReader>();
            if (input == null) return;

            if (protection == null)
                protection = input.GetComponent<ProtectiveEquipment>();
            if (loot == null)
                loot = input.GetComponent<PlayerLootEquipment>();
        }

        private void ResolvePhysicalView()
        {
            helmetLabel ??= FindNamed<Text>("HelmetStatus");
            vestLabel ??= FindNamed<Text>("VestStatus");
            backpackLabel ??= FindNamed<Text>("BackpackStatus");
        }

        private void Subscribe()
        {
            if (protection != null)
            {
                protection.Changed -= Refresh;
                protection.Changed += Refresh;
            }

            if (loot != null)
            {
                loot.EquipmentChanged -= Refresh;
                loot.EquipmentChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (protection != null)
                protection.Changed -= Refresh;
            if (loot != null)
                loot.EquipmentChanged -= Refresh;
        }

        private void Refresh()
        {
            if (protection != null)
            {
                if (helmetLabel != null)
                    helmetLabel.text = LevelText("CASCO", protection.HelmetLevel);
                if (vestLabel != null)
                    vestLabel.text = LevelText("CHALECO", protection.VestLevel);
            }

            if (loot != null && backpackLabel != null)
            {
                InventoryItemDefinition bp = loot.BackpackItem;
                backpackLabel.text = bp != null && bp.backpackCapacity > 0f
                    ? $"MOCHILA L{BackpackLevel(bp.backpackCapacity)}"
                    : "MOCHILA —";
            }
        }

        private static string LevelText(string name, ProtectionLevel level)
        {
            if (level == ProtectionLevel.None) return $"{name} —";
            int lv = level == ProtectionLevel.Level1 ? 1
                : level == ProtectionLevel.Level2 ? 2 : 3;
            return $"{name} L{lv}";
        }

        private static int BackpackLevel(float capacity)
        {
            if (capacity >= 150f) return 3;
            if (capacity >= 100f) return 2;
            return 1;
        }

        private T FindNamed<T>(string objectName) where T : Component
        {
            T[] all = GetComponentsInChildren<T>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == objectName) return all[i];
            return null;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }
    }
}
