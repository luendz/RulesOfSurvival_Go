using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Inventory
{
    [CreateAssetMenu(
        menuName = "ROS/Inventory/Item Definition",
        fileName = "Item_"
    )]
    public sealed class InventoryItemDefinition :
        ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "item_001";

        public string displayName = "Item";

        public ItemType itemType =
            ItemType.Misc;

        [Header("Inventory")]
        [Min(1)]
        public int maxStack = 1;

        [Min(0f)]
        public float weight = 1f;

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
    }
}