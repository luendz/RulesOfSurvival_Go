using System;
using System.Collections.Generic;
using UnityEngine;

namespace ROS.Game.Inventory
{
    [Serializable]
    public sealed class InventoryStack
    {
        public InventoryItemDefinition item;
        public int amount;
    }

    public sealed class InventoryComponent : MonoBehaviour
    {
        [SerializeField] private float capacity = 100f;
        [SerializeField] private List<InventoryStack> stacks = new List<InventoryStack>();

        public IReadOnlyList<InventoryStack> Stacks => stacks;
        public float Capacity => capacity;
        public float UsedCapacity
        {
            get
            {
                float total = 0f;
                foreach (var stack in stacks)
                    if (stack.item != null) total += stack.item.weight * stack.amount;
                return total;
            }
        }

        public event Action Changed;

        public bool CanAdd(InventoryItemDefinition item, int amount)
        {
            return item != null && amount > 0 && UsedCapacity + item.weight * amount <= capacity + 0.001f;
        }

        public bool Add(InventoryItemDefinition item, int amount)
        {
            if (!CanAdd(item, amount)) return false;
            var stack = stacks.Find(x => x.item == item && x.amount < item.maxStack);
            while (amount > 0)
            {
                if (stack == null || stack.amount >= item.maxStack)
                {
                    stack = new InventoryStack { item = item, amount = 0 };
                    stacks.Add(stack);
                }
                int room = item.maxStack - stack.amount;
                int add = Mathf.Min(room, amount);
                stack.amount += add;
                amount -= add;
                stack = null;
            }
            Changed?.Invoke();
            return true;
        }

        public bool Remove(InventoryItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return false;
            int available = 0;
            foreach (var stack in stacks) if (stack.item == item) available += stack.amount;
            if (available < amount) return false;

            for (int i = stacks.Count - 1; i >= 0 && amount > 0; i--)
            {
                var stack = stacks[i];
                if (stack.item != item) continue;
                int take = Mathf.Min(stack.amount, amount);
                stack.amount -= take;
                amount -= take;
                if (stack.amount <= 0) stacks.RemoveAt(i);
            }
            Changed?.Invoke();
            return true;
        }

        public void SetCapacity(float newCapacity)
        {
            capacity = Mathf.Max(0f, newCapacity);
            Changed?.Invoke();
        }
    }
}
