using ROS.Game.Core;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Weapons
{
    /// <summary>
    /// Conecta automáticamente las armas del portador a la munición de su
    /// InventoryComponent. Añadir al mismo GameObject que tiene el
    /// InventoryComponent y los WeaponController hijos.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponAmmoConnector : MonoBehaviour
    {
        private InventoryComponent _inventory;

        private void Start()
        {
            _inventory = GetComponent<InventoryComponent>();
            if (_inventory == null) return;

            ConnectAllWeapons();
            _inventory.Changed += ConnectAllWeapons;
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.Changed -= ConnectAllWeapons;
        }

        private void ConnectAllWeapons()
        {
            if (_inventory == null) return;

            WeaponController[] weapons =
                GetComponentsInChildren<WeaponController>(true);

            foreach (WeaponController weapon in weapons)
            {
                if (weapon == null || weapon.Definition == null) continue;

                AmmoType needed = weapon.Definition.ammoType;
                if (needed == AmmoType.None) continue;

                InventoryItemDefinition ammoDef = FindAmmoDef(needed);
                weapon.SetAmmoSource(_inventory, ammoDef);
            }
        }

        private InventoryItemDefinition FindAmmoDef(AmmoType needed)
        {
            foreach (InventoryStack stack in _inventory.Stacks)
            {
                if (stack.item != null &&
                    stack.item.itemType == ItemType.Ammo &&
                    stack.item.ammoType == needed)
                {
                    return stack.item;
                }
            }
            return null; // no hay ese tipo en inventario → reserva interna
        }
    }
}
