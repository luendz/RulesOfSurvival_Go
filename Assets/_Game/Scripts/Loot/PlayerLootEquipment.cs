using System;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Inventory;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Loot
{
    [DefaultExecutionOrder(-30)]
    public sealed class PlayerLootEquipment : MonoBehaviour
    {
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private ProtectiveEquipment protection;
        [SerializeField] private WeaponEquipmentController weapons;

        [Header("Runtime Debug")]
        [SerializeField] private InventoryItemDefinition helmetItem;
        [SerializeField] private InventoryItemDefinition vestItem;
        [SerializeField] private InventoryItemDefinition backpackItem;
        [SerializeField] private InventoryItemDefinition[] weaponItems =
            new InventoryItemDefinition[PlayerWeaponSlotRules.SlotCount];

        private float _baseCapacity;

        public InventoryItemDefinition HelmetItem => helmetItem;
        public InventoryItemDefinition VestItem => vestItem;
        public InventoryItemDefinition BackpackItem => backpackItem;

        public InventoryItemDefinition GetWeaponItem(int slot) =>
            weaponItems != null && slot >= 1 && slot <= weaponItems.Length
                ? weaponItems[slot - 1]
                : null;

        public InventoryItemDefinition GetWeaponItem(PlayerWeaponSlot slot) =>
            GetWeaponItem((int)slot);

        public event Action EquipmentChanged;

        private void Awake()
        {
            EnsureReferences();
            _baseCapacity = inventory != null
                ? inventory.Capacity
                : 0f;
        }

        public bool CanEquip(InventoryItemDefinition item)
        {
            if (item == null || !item.IsEquippable)
                return false;

            EnsureReferences();

            return item.itemType switch
            {
                ItemType.Weapon =>
                    CanEquipWeaponLike(item),
                ItemType.Throwable =>
                    CanEquipWeaponLike(item),
                ItemType.Helmet =>
                    protection != null && IsEquipmentUpgrade(item),
                ItemType.Armor =>
                    protection != null && IsEquipmentUpgrade(item),
                ItemType.Backpack =>
                    inventory != null && IsEquipmentUpgrade(item),
                _ => false
            };
        }

        private bool CanEquipWeaponLike(InventoryItemDefinition item)
        {
            int slot = ResolveWeaponSlot(item);
            if (slot == 0)
                return false;

            // Primary 1/2 y pistola siguen utilizando WeaponController.
            if (slot <= (int)PlayerWeaponSlot.Pistol)
            {
                return weapons != null && item.weaponDefinition != null;
            }

            // Melee y throwable ya tienen slot lógico independiente aunque su
            // controlador de combate especializado se implemente por separado.
            return slot == (int)PlayerWeaponSlot.Melee ||
                   slot == (int)PlayerWeaponSlot.Throwable;
        }

        public bool IsEquipmentUpgrade(
            InventoryItemDefinition item
        )
        {
            if (item == null)
                return false;

            return item.itemType switch
            {
                ItemType.Helmet =>
                    IsProtectionUpgrade(item, helmetItem),
                ItemType.Armor =>
                    IsProtectionUpgrade(item, vestItem),
                ItemType.Backpack => IsBackpackUpgrade(item),
                _ => true
            };
        }

        private static bool IsProtectionUpgrade(
            InventoryItemDefinition candidate,
            InventoryItemDefinition equipped
        )
        {
            return equipped == null ||
                   (int)candidate.protectionLevel >
                   (int)equipped.protectionLevel;
        }

        private bool IsBackpackUpgrade(
            InventoryItemDefinition candidate
        )
        {
            return backpackItem == null ||
                   candidate.backpackCapacity >
                   backpackItem.backpackCapacity;
        }

        public bool TryEquip(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem
        )
        {
            replacedItem = null;

            if (!CanEquip(item))
                return false;

            bool equipped = item.itemType switch
            {
                ItemType.Weapon => TryEquipWeaponLike(item, out replacedItem),
                ItemType.Throwable => TryEquipWeaponLike(item, out replacedItem),
                ItemType.Helmet => EquipHelmet(item, out replacedItem),
                ItemType.Armor => EquipVest(item, out replacedItem),
                ItemType.Backpack => EquipBackpack(item, out replacedItem),
                _ => false
            };

            if (equipped)
                EquipmentChanged?.Invoke();

            return equipped;
        }

        private bool EquipHelmet(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem
        )
        {
            replacedItem = helmetItem;
            helmetItem = item;
            protection.EquipHelmet(item.protectionLevel);
            return true;
        }

        private bool EquipVest(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem
        )
        {
            replacedItem = vestItem;
            vestItem = item;
            protection.EquipVest(item.protectionLevel);
            return true;
        }

        private bool EquipBackpack(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem
        )
        {
            replacedItem = backpackItem;
            backpackItem = item;

            float targetCapacity = item.backpackCapacity > 0f
                ? Mathf.Max(_baseCapacity, item.backpackCapacity)
                : _baseCapacity;

            inventory.SetCapacity(targetCapacity);
            return true;
        }

        private bool TryEquipWeaponLike(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem
        )
        {
            replacedItem = null;

            int slot = ResolveWeaponSlot(item);
            if (slot == 0)
                return false;

            EnsureWeaponItemArray();
            replacedItem = weaponItems[slot - 1];

            // Slots 4 y 5 son slots lógicos independientes. No se fuerzan a
            // pasar por el controlador de armas de fuego.
            if (slot >= (int)PlayerWeaponSlot.Melee)
            {
                weaponItems[slot - 1] = item;
                return true;
            }

            WeaponController newWeapon = CreateWeapon(item);
            if (newWeapon == null)
                return false;

            WeaponController oldWeapon = weapons.GetWeaponForSlot(slot);
            weaponItems[slot - 1] = item;
            weapons.SetWeaponInSlot(slot, newWeapon, true);

            if (oldWeapon != null && oldWeapon != newWeapon)
                Destroy(oldWeapon.gameObject);

            return true;
        }

        private int ResolveWeaponSlot(InventoryItemDefinition item)
        {
            if (item == null)
                return 0;

            int equippedSlot = weapons != null
                ? weapons.EquippedSlot
                : 0;

            return PlayerWeaponSlotRules.ResolveSlot(
                item,
                item.preferredWeaponSlot,
                slot => IsSlotOccupied(slot),
                equippedSlot
            );
        }

        private bool IsSlotOccupied(int slot)
        {
            if (slot <= (int)PlayerWeaponSlot.Pistol)
                return weapons != null && weapons.HasWeaponInSlot(slot);

            return GetWeaponItem(slot) != null;
        }

        private WeaponController CreateWeapon(
            InventoryItemDefinition item
        )
        {
            if (item == null || item.weaponDefinition == null)
                return null;

            GameObject weaponObject;

            if (item.weaponPrefab != null)
            {
                weaponObject = Instantiate(item.weaponPrefab, transform);
            }
            else
            {
                weaponObject = new GameObject(item.displayName);
                weaponObject.transform.SetParent(transform, false);
            }

            weaponObject.name = $"Arma_{item.displayName}";

            WeaponController controller =
                weaponObject.GetComponent<WeaponController>() ??
                weaponObject.GetComponentInChildren<WeaponController>(true);

            if (controller == null)
                controller = weaponObject.AddComponent<WeaponController>();

            controller.ConfigureDefinition(item.weaponDefinition);
            DisableWorldPhysics(weaponObject);
            return controller;
        }

        private static void DisableWorldPhysics(GameObject weaponObject)
        {
            foreach (Collider collider in
                     weaponObject.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in
                     weaponObject.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
        }

        private void EnsureReferences()
        {
            if (inventory == null)
                inventory = GetComponent<InventoryComponent>();

            if (protection == null)
                protection = GetComponent<ProtectiveEquipment>();

            if (protection == null)
                protection = gameObject.AddComponent<ProtectiveEquipment>();

            if (weapons == null)
                weapons = GetComponent<WeaponEquipmentController>();

            EnsureWeaponItemArray();
        }

        private void EnsureWeaponItemArray()
        {
            if (weaponItems != null &&
                weaponItems.Length == PlayerWeaponSlotRules.SlotCount)
            {
                return;
            }

            InventoryItemDefinition[] oldItems = weaponItems;
            weaponItems = new InventoryItemDefinition[
                PlayerWeaponSlotRules.SlotCount
            ];

            if (oldItems == null)
                return;

            int copyCount = Mathf.Min(oldItems.Length, weaponItems.Length);
            for (int i = 0; i < copyCount; i++)
                weaponItems[i] = oldItems[i];
        }
    }
}
