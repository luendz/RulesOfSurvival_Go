using ROS.Game.Interaction;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Loot
{
    public sealed class LootPickup :
        MonoBehaviour,
        IInteractable
    {
        [SerializeField]
        private InventoryItemDefinition item;

        [SerializeField]
        private int amount = 1;

        private bool _consumed;

        public string InteractionLabel =>
            item == null
                ? "Recoger"
                : $"Recoger {item.displayName} x{amount}";

        public void Configure(
            InventoryItemDefinition definition,
            int quantity
        )
        {
            item = definition;

            amount =
                Mathf.Max(
                    1,
                    quantity
                );

            _consumed = false;
        }

        public bool CanInteract(
            GameObject interactor
        )
        {
            if (
                _consumed ||
                item == null ||
                interactor == null
            )
            {
                return false;
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (inventory == null)
            {
                return false;
            }

            return inventory.CanAdd(
                item,
                amount
            );
        }

        public bool IsBlockedByInventoryCapacity(
            GameObject interactor
        )
        {
            if (
                _consumed ||
                item == null ||
                interactor == null
            )
            {
                return false;
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (inventory == null)
            {
                return false;
            }

            return !inventory.CanAdd(
                item,
                amount
            );
        }

        public void Interact(
            GameObject interactor
        )
        {
            if (
                _consumed ||
                interactor == null ||
                item == null
            )
            {
                return;
            }

            InventoryComponent inventory =
                interactor.GetComponent<
                    InventoryComponent
                >();

            if (
                inventory == null ||
                !inventory.Add(
                    item,
                    amount
                )
            )
            {
                return;
            }

            _consumed = true;

            Collider[] colliders =
                GetComponentsInChildren<
                    Collider
                >(true);

            foreach (
                Collider col
                in colliders
            )
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }

            enabled = false;

            Destroy(gameObject);
        }
    }
}