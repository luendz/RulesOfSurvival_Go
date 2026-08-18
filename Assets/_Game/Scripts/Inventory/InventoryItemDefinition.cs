using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Inventory
{
    [CreateAssetMenu(menuName = "ROS/Inventory/Item Definition", fileName = "Item_")]
    public sealed class InventoryItemDefinition : ScriptableObject
    {
        public string itemId = "item_001";
        public string displayName = "Item";
        public ItemType itemType = ItemType.Misc;
        [Min(1)] public int maxStack = 1;
        [Min(0f)] public float weight = 1f;
        public Sprite icon;
    }
}
