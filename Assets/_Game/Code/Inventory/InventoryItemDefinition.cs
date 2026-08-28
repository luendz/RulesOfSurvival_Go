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
            "Tipo de munición que representa este ítem (solo para ItemType.Ammo)."
        )]
        public AmmoType ammoType = AmmoType.None;

        [Tooltip(
            "Definición de consumible si este ítem se puede usar (vendaje, botiquín…)."
        )]
        public ConsumableDefinition consumableDefinition;

        [Tooltip(
            "Definición jugable si este objeto representa un arma."
        )]
        public WeaponDefinition weaponDefinition;

        [Tooltip(
            "Prefab visual o jugable que se instancia al equipar el arma."
        )]
        public GameObject weaponPrefab;

        [Tooltip(
            "0 = automático. 1-2 = principales, 3 = pistola, 4 = melee, 5 = arrojable. " +
            "La categoría real del arma siempre tiene prioridad sobre este valor."
        )]
        [Range(0, 5)]
        public int preferredWeaponSlot;

        [Header("UI")]
        public Sprite icon;

        [Header("Nearby Loot UI")]
        [Tooltip(
            "Segunda línea del cuadro NearbyLoot. Si está vacía, se deriva del tipo de objeto."
        )]
        public string nearbySecondaryText;

        [Tooltip(
            "Icono secundario. Para munición representa un arma compatible con ese tipo de bala."
        )]
        public Sprite nearbySecondaryIcon;

        [Header("World Visual")]
        public GameObject worldModel;

        public Vector3 worldOffset =
            Vector3.zero;

        public Vector3 worldEulerAngles =
            Vector3.zero;

        public Vector3 worldScale =
            Vector3.one;

        [Header("Hand Held Visual (raw model)")]
        [Tooltip(
            "Cuando está activo, el modelo FBX se posiciona con los valores de abajo " +
            "en lugar de los valores globales del PlayerAuxiliaryWeaponSlots."
        )]
        public bool overrideHandTransform;

        public Vector3 handLocalPosition;

        public Vector3 handLocalEulerAngles;

        public Vector3 handLocalScale = Vector3.one;

        public string StableId => itemId;

        public DataConfidence Confidence =>
            dataConfidence;

        public bool IsEquippable =>
            itemType == ItemType.Weapon ||
            itemType == ItemType.Throwable ||
            itemType == ItemType.Armor ||
            itemType == ItemType.Helmet ||
            itemType == ItemType.Backpack;
    }
}
