using ROS.Game.Input;
using ROS.Game.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.Loot
{
    public sealed class LootDropController : MonoBehaviour
    {
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private float dropDistance = 1.25f;
        [SerializeField] private float dropHeight = 0.25f;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<InventoryComponent>();
            }

            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }
        }

        private void Update()
        {
            if (inventory == null ||
                (input != null && input.UiBlocked) ||
                Keyboard.current == null ||
                !Keyboard.current.gKey.wasPressedThisFrame)
            {
                return;
            }

            bool dropWholeStack =
                Keyboard.current.leftShiftKey.isPressed ||
                Keyboard.current.rightShiftKey.isPressed;

            DropLastStack(dropWholeStack);
        }

        public bool DropLastStack(bool wholeStack)
        {
            if (inventory == null || inventory.Stacks.Count == 0)
            {
                return false;
            }

            InventoryStack stack =
                inventory.Stacks[inventory.Stacks.Count - 1];

            if (stack == null || stack.item == null || stack.amount <= 0)
            {
                return false;
            }

            return Drop(
                stack.item,
                wholeStack ? stack.amount : 1
            );
        }

        public bool Drop(
            InventoryItemDefinition item,
            int requestedAmount)
        {
            if (inventory == null || item == null || requestedAmount <= 0)
            {
                return false;
            }

            int amount = Mathf.Min(
                requestedAmount,
                inventory.GetAmount(item)
            );

            if (amount <= 0 || !inventory.Remove(item, amount))
            {
                return false;
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            Vector3 position =
                transform.position +
                forward.normalized * dropDistance +
                Vector3.up * dropHeight;

            LootPickup pickup = LootPickup.SpawnRuntime(
                item,
                amount,
                position,
                gameObject
            );

            if (pickup != null)
            {
                return true;
            }

            inventory.Add(item, amount);
            return false;
        }
    }
}
