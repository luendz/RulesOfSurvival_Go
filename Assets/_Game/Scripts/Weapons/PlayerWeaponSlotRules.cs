using System;
using ROS.Game.Core;
using ROS.Game.Inventory;

namespace ROS.Game.Weapons
{
    public enum PlayerWeaponSlot
    {
        None = 0,
        Primary1 = 1,
        Primary2 = 2,
        Pistol = 3,
        Melee = 4,
        Throwable = 5
    }

    public static class PlayerWeaponSlotRules
    {
        public const int SlotCount = 5;

        public static bool IsPrimarySlot(int slot) =>
            slot == (int)PlayerWeaponSlot.Primary1 ||
            slot == (int)PlayerWeaponSlot.Primary2;

        public static bool IsCompatible(
            InventoryItemDefinition item,
            int slot
        )
        {
            if (item == null || slot < 1 || slot > SlotCount)
                return false;

            PlayerWeaponSlot expected = ResolveFixedSlot(item);
            if (expected != PlayerWeaponSlot.None)
                return slot == (int)expected;

            return IsPrimarySlot(slot);
        }

        public static int ResolveSlot(
            InventoryItemDefinition item,
            int preferredSlot,
            Func<int, bool> isOccupied,
            int currentlyEquippedSlot
        )
        {
            if (item == null)
                return 0;

            PlayerWeaponSlot fixedSlot = ResolveFixedSlot(item);
            if (fixedSlot != PlayerWeaponSlot.None)
                return (int)fixedSlot;

            if (IsPrimarySlot(preferredSlot))
                return preferredSlot;

            if (isOccupied == null || !isOccupied(1))
                return 1;

            if (!isOccupied(2))
                return 2;

            return IsPrimarySlot(currentlyEquippedSlot)
                ? currentlyEquippedSlot
                : 1;
        }

        public static PlayerWeaponSlot ResolveFixedSlot(
            InventoryItemDefinition item
        )
        {
            if (item == null)
                return PlayerWeaponSlot.None;

            if (IsRpg(item))
                return PlayerWeaponSlot.None;

            if (item.itemType == ItemType.Throwable)
                return PlayerWeaponSlot.Throwable;

            WeaponDefinition definition = item.weaponDefinition;
            if (definition == null)
                return PlayerWeaponSlot.None;

            return definition.family switch
            {
                WeaponFamily.Pistol => PlayerWeaponSlot.Pistol,
                WeaponFamily.Melee => PlayerWeaponSlot.Melee,
                _ => PlayerWeaponSlot.None
            };
        }

        public static bool IsRpg(InventoryItemDefinition item)
        {
            if (item == null)
                return false;

            string weaponId = item.weaponDefinition != null
                ? item.weaponDefinition.weaponId
                : string.Empty;

            string itemId = item.itemId ?? string.Empty;
            string displayName = item.displayName ?? string.Empty;

            return ContainsRpg(weaponId) ||
                   ContainsRpg(itemId) ||
                   ContainsRpg(displayName);
        }

        private static bool ContainsRpg(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf("rpg", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
