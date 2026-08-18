using ROS.Game.Interaction;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Loot
{
    public sealed class LootPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private InventoryItemDefinition item;
        [SerializeField] private int amount = 1;

        private bool _consumed;

        public string InteractionLabel => item == null ? "Recoger" : $"Recoger {item.displayName} x{amount}";

        public void Configure(InventoryItemDefinition definition, int quantity)
        {
            item = definition;
            amount = Mathf.Max(1, quantity);
            _consumed = false;
        }

        public bool CanInteract(GameObject interactor)
        {
            return !_consumed &&
                   item != null &&
                   interactor != null &&
                   interactor.GetComponent<InventoryComponent>() != null;
        }

        public void Interact(GameObject interactor)
        {
            if (_consumed || interactor == null || item == null)
                return;

            var inventory = interactor.GetComponent<InventoryComponent>();
            if (inventory == null || !inventory.Add(item, amount))
                return;

            _consumed = true;

            // Stop any other raycast/interactor from seeing this pickup while
            // Destroy() waits until the end of the current frame.
            foreach (var col in GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            enabled = false;
            Destroy(gameObject);
        }
    }
}
