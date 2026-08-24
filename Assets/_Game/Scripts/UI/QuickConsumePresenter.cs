using ROS.Game.Core;
using ROS.Game.Gameplay;
using ROS.Game.Input;
using ROS.Game.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DisallowMultipleComponent]
    public sealed class QuickConsumePresenter : MonoBehaviour
    {
        private const int MaxSlots = 3;

        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private ConsumableController consumable;
        [SerializeField] private ConsumeSlot[] slots = new ConsumeSlot[MaxSlots];

        [System.Serializable]
        private sealed class ConsumeSlot
        {
            public GameObject root;
            public Image background;
            public Image icon;
            public Text nameLabel;
            public Text countLabel;
        }

        private static readonly Color BgNormal =
            new Color(0.06f, 0.06f, 0.06f, 0.82f);
        private static readonly Color BgActive =
            new Color(0.10f, 0.22f, 0.06f, 0.90f);

        private void Awake()
        {
            ResolvePhysicalView();
            ResolveGameplayReferences();
        }

        public void Bind(InventoryComponent playerInventory, ConsumableController controller)
        {
            inventory = playerInventory;
            consumable = controller;
            ResolvePhysicalView();
        }

        private void ResolveGameplayReferences()
        {
            PlayerInputReader input = FindFirstObjectByType<PlayerInputReader>();
            if (input == null) return;

            if (inventory == null)
                inventory = input.GetComponent<InventoryComponent>();
            if (consumable == null)
                consumable = input.GetComponent<ConsumableController>();
        }

        private void ResolvePhysicalView()
        {
            if (slots == null || slots.Length != MaxSlots)
                slots = new ConsumeSlot[MaxSlots];

            Transform quickConsumeRoot = ResolveQuickConsumeRoot();

            for (int i = 0; i < MaxSlots; i++)
            {
                Transform root = quickConsumeRoot != null
                    ? FindNamedUnderTransform(
                        quickConsumeRoot,
                        "QuickConsumeSlot_" + i
                    )
                    : null;

                if (root == null)
                    continue;

                ConsumeSlot slot = slots[i] ?? new ConsumeSlot();
                slot.root = root.gameObject;
                slot.background = root.GetComponent<Image>();
                slot.icon = FindNamedUnder<Image>(root, "Icon");
                slot.nameLabel = FindNamedUnder<Text>(root, "Name");
                slot.countLabel = FindNamedUnder<Text>(root, "Count");
                slots[i] = slot;
            }
        }

        private Transform ResolveQuickConsumeRoot()
        {
            Transform vitals = FindNamedTransform("Vitals");
            if (vitals != null)
            {
                Transform meds = vitals.Find("Meds");
                if (meds != null)
                {
                    Transform nested = meds.Find("QuickConsumeRoot");
                    if (nested != null)
                        return nested;
                }
            }

            // Compatibilidad con escenas antiguas. La escena 08 reparada ya no
            // debe usar este fallback porque el root válido vive en Vitals/Meds.
            return FindNamedTransform("QuickConsumeRoot");
        }

        private void Update()
        {
            ResolveGameplayReferences();
            if (inventory == null || slots == null)
                return;

            var entries = new (InventoryItemDefinition item, int count)[MaxSlots];
            int found = 0;

            foreach (InventoryStack stack in inventory.Stacks)
            {
                if (stack.item == null || stack.amount <= 0 ||
                    stack.item.itemType != ItemType.Healing)
                    continue;

                bool merged = false;
                for (int i = 0; i < found; i++)
                {
                    if (entries[i].item != stack.item) continue;
                    entries[i].count += stack.amount;
                    merged = true;
                    break;
                }

                if (!merged && found < MaxSlots)
                    entries[found++] = (stack.item, stack.amount);
            }

            bool healActive = consumable != null && consumable.IsUsing;

            for (int i = 0; i < MaxSlots; i++)
            {
                ConsumeSlot slot = slots[i];
                if (slot == null || slot.root == null) continue;

                bool hasItem = i < found;
                slot.root.SetActive(hasItem);
                if (!hasItem) continue;

                InventoryItemDefinition item = entries[i].item;
                bool first = i == 0;

                if (slot.nameLabel != null)
                    slot.nameLabel.text = ShortName(item.displayName);
                if (slot.countLabel != null)
                    slot.countLabel.text = entries[i].count.ToString();

                if (slot.icon != null)
                {
                    slot.icon.sprite = item.icon;
                    slot.icon.color = item.icon != null
                        ? Color.white
                        : LootIconHelper.GetIconColor(item.itemType);
                }

                if (slot.background != null)
                    slot.background.color = first && !healActive
                        ? BgActive
                        : BgNormal;
            }
        }

        private static string ShortName(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 8)
                return value;
            return value.Substring(0, 7) + ".";
        }

        private Transform FindNamedTransform(string name)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }

        private static Transform FindNamedUnderTransform(
            Transform root,
            string name
        )
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }

        private static T FindNamedUnder<T>(Transform root, string name)
            where T : Component
        {
            T[] all = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }
    }
}
