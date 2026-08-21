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
            new InventoryItemDefinition[3];

        private float _baseCapacity;

        public InventoryItemDefinition HelmetItem => helmetItem;
        public InventoryItemDefinition VestItem => vestItem;
        public InventoryItemDefinition BackpackItem => backpackItem;
        public InventoryItemDefinition GetWeaponItem(int slot) =>
            weaponItems != null && slot >= 1 && slot <= weaponItems.Length
                ? weaponItems[slot - 1]
                : null;

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
            {
                return false;
            }

            EnsureReferences();

            return item.itemType switch
            {
                ItemType.Weapon =>
                    weapons != null &&
                    item.weaponDefinition != null,
                ItemType.Helmet =>
                    protection != null &&
                    IsEquipmentUpgrade(item),
                ItemType.Armor =>
                    protection != null &&
                    IsEquipmentUpgrade(item),
                ItemType.Backpack =>
                    inventory != null &&
                    IsEquipmentUpgrade(item),
                _ => false
            };
        }

        public bool IsEquipmentUpgrade(
            InventoryItemDefinition item
        )
        {
            if (item == null)
            {
                return false;
            }

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
            return
                equipped == null ||
                (int)candidate.protectionLevel >
                (int)equipped.protectionLevel;
        }

        private bool IsBackpackUpgrade(
            InventoryItemDefinition candidate
        )
        {
            return
                backpackItem == null ||
                candidate.backpackCapacity >
                backpackItem.backpackCapacity;
        }

        public bool TryEquip(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem)
        {
            replacedItem = null;

            if (!CanEquip(item))
            {
                return false;
            }

            bool equipped = item.itemType switch
            {
                ItemType.Weapon => TryEquipWeapon(item, out replacedItem),
                ItemType.Helmet => EquipHelmet(item, out replacedItem),
                ItemType.Armor => EquipVest(item, out replacedItem),
                ItemType.Backpack => EquipBackpack(item, out replacedItem),
                _ => false
            };

            if (equipped)
            {
                EquipmentChanged?.Invoke();
            }

            return equipped;
        }

        private bool EquipHelmet(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem)
        {
            replacedItem = helmetItem;
            helmetItem = item;
            protection.EquipHelmet(item.protectionLevel);
            return true;
        }

        private bool EquipVest(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem)
        {
            replacedItem = vestItem;
            vestItem = item;
            protection.EquipVest(item.protectionLevel);
            return true;
        }

        private bool EquipBackpack(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem)
        {
            replacedItem = backpackItem;
            backpackItem = item;

            float targetCapacity = item.backpackCapacity > 0f
                ? Mathf.Max(_baseCapacity, item.backpackCapacity)
                : _baseCapacity;

            inventory.SetCapacity(targetCapacity);
            return true;
        }

        private bool TryEquipWeapon(
            InventoryItemDefinition item,
            out InventoryItemDefinition replacedItem)
        {
            replacedItem = null;

            int slot = ResolveWeaponSlot(item.preferredWeaponSlot);
            if (slot == 0)
            {
                return false;
            }

            WeaponController newWeapon = CreateWeapon(item);
            if (newWeapon == null)
            {
                return false;
            }

            WeaponController oldWeapon = weapons.GetWeaponForSlot(slot);
            replacedItem = weaponItems[slot - 1];
            weaponItems[slot - 1] = item;

            weapons.SetWeaponInSlot(slot, newWeapon, true);

            if (oldWeapon != null && oldWeapon != newWeapon)
            {
                Destroy(oldWeapon.gameObject);
            }

            return true;
        }

        private int ResolveWeaponSlot(int preferredSlot)
        {
            if (preferredSlot >= 1 && preferredSlot <= 3)
            {
                return preferredSlot;
            }

            if (!weapons.HasWeaponInSlot(1))
            {
                return 1;
            }

            if (!weapons.HasWeaponInSlot(2))
            {
                return 2;
            }

            int equippedSlot = weapons.EquippedSlot;
            return equippedSlot == 1 || equippedSlot == 2
                ? equippedSlot
                : 1;
        }

        private WeaponController CreateWeapon(
            InventoryItemDefinition item)
        {
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
            {
                controller = weaponObject.AddComponent<WeaponController>();
            }

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
            {
                inventory = GetComponent<InventoryComponent>();
            }

            if (protection == null)
            {
                protection = GetComponent<ProtectiveEquipment>();
            }

            if (protection == null)
            {
                protection = gameObject.AddComponent<ProtectiveEquipment>();
            }

            if (weapons == null)
            {
                weapons = GetComponent<WeaponEquipmentController>();
            }

            if (weaponItems == null || weaponItems.Length != 3)
            {
                weaponItems = new InventoryItemDefinition[3];
            }
        }
    }
}
