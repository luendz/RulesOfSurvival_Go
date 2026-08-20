using ROS.Game.Core;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Inventory
{
    [CreateAssetMenu(
        menuName = "ROS/Inventory/Item Definition",
        fileName = "Item_"
    )]
    public sealed class InventoryItemDefinition :
        ScriptableObject,
        IGameDataDefinition
    {
        [Header("Identity")]
        public string itemId = "item_001";

        public string displayName = "Item";

        public ItemType itemType =
            ItemType.Misc;

        [Header("Data Provenance")]
        public DataConfidence dataConfidence =
            DataConfidence.Prototype;

        [Header("Inventory")]
        [Min(1)]
        public int maxStack = 1;

        [Min(0f)]
        public float weight = 1f;

        [Header("World Loot")]
        public LootRarity rarity =
            LootRarity.Common;

        public LootPickupMode pickupMode =
            LootPickupMode.Manual;

        [Tooltip(
            "Nivel usado por cascos y chalecos."
        )]
        public ProtectionLevel protectionLevel =
            ProtectionLevel.None;

        [Tooltip(
            "Capacidad total al equipar esta mochila. Cero conserva la capacidad base."
        )]
        [Min(0f)]
        public float backpackCapacity;

        [Tooltip(
            "Definición jugable si este objeto representa un arma."
        )]
        public WeaponDefinition weaponDefinition;

        [Tooltip(
            "Prefab visual o jugable que se instancia al equipar el arma."
        )]
        public GameObject weaponPrefab;

        [Range(0, 3)]
        public int preferredWeaponSlot;

        [Header("UI")]
        public Sprite icon;

        [Header("World Visual")]
        public GameObject worldModel;

        public Vector3 worldOffset =
            Vector3.zero;

        public Vector3 worldEulerAngles =
            Vector3.zero;

        public Vector3 worldScale =
            Vector3.one;

        public string StableId => itemId;

        public DataConfidence Confidence =>
            dataConfidence;

        public bool IsEquippable =>
            itemType == ItemType.Weapon ||
            itemType == ItemType.Armor ||
            itemType == ItemType.Helmet ||
            itemType == ItemType.Backpack;

        public bool IsAutomaticPickup =>
            pickupMode == LootPickupMode.Automatic;
    }
}
