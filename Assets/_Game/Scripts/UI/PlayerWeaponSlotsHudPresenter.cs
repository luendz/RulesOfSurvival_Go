using ROS.Game.Core;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    [DefaultExecutionOrder(700)]
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponSlotsHudPresenter : MonoBehaviour
    {
        [SerializeField] private WeaponEquipmentController weapons;
        [SerializeField] private PlayerLootEquipment lootEquipment;
        [SerializeField] private PlayerAuxiliaryWeaponSlots auxiliarySlots;
        [SerializeField] private InventoryComponent inventory;

        private readonly SlotView[] _slots = new SlotView[5];

        private sealed class SlotView
        {
            public Image Background;
            public Text Slot;
            public Text LegacyName;
            public Text Ammo;
            public Image Icon;
            public RectTransform FireModeRoot;
            public Text FireModeKey;
            public Text FireModeText;
        }

        private static readonly Color Normal =
            new Color(0.025f, 0.035f, 0.045f, 0.84f);
        private static readonly Color Active =
            new Color(0.92f, 0.92f, 0.92f, 0.94f);
        private static readonly Color ActiveText =
            new Color(0.05f, 0.05f, 0.05f, 1f);

        private void Awake()
        {
            ResolveReferences();
            BindPhysicalSlots();
        }

        private void Update()
        {
            ResolveReferences();
            Refresh();
        }

        private void ResolveReferences()
        {
            if (weapons != null && lootEquipment != null &&
                auxiliarySlots != null && inventory != null)
                return;

            PlayerAuxiliaryWeaponSlots aux =
                FindFirstObjectByType<PlayerAuxiliaryWeaponSlots>();
            if (aux != null)
            {
                auxiliarySlots ??= aux;
                weapons ??= aux.GetComponent<WeaponEquipmentController>();
                lootEquipment ??= aux.GetComponent<PlayerLootEquipment>();
                inventory ??= aux.GetComponent<InventoryComponent>();
                return;
            }

            weapons ??= FindFirstObjectByType<WeaponEquipmentController>();
            if (weapons != null)
            {
                lootEquipment ??= weapons.GetComponent<PlayerLootEquipment>();
                auxiliarySlots ??= weapons.GetComponent<PlayerAuxiliaryWeaponSlots>();
                inventory ??= weapons.GetComponent<InventoryComponent>();
            }
        }

        private void BindPhysicalSlots()
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int slot = 1; slot <= 5; slot++)
            {
                Transform root = null;
                string wanted = "WeaponSlot_" + slot;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].name == wanted)
                    {
                        root = all[i];
                        break;
                    }
                }

                if (root == null)
                    continue;

                Transform fireMode = root.Find("FireModePanel");
                _slots[slot - 1] = new SlotView
                {
                    Background = root.GetComponent<Image>(),
                    Slot = FindNamed<Text>(root, "Slot"),
                    LegacyName = FindNamed<Text>(root, "WeaponName"),
                    Ammo = FindNamed<Text>(root, "Ammo"),
                    Icon = FindNamed<Image>(root, "Icon"),
                    FireModeRoot = fireMode as RectTransform,
                    FireModeKey = fireMode != null
                        ? FindNamed<Text>(fireMode, "Key")
                        : null,
                    FireModeText = fireMode != null
                        ? FindNamed<Text>(fireMode, "Mode")
                        : null
                };
            }
        }

        private void Refresh()
        {
            if (_slots[0] == null)
                BindPhysicalSlots();

            for (int slot = 1; slot <= 5; slot++)
                RefreshSlot(slot, _slots[slot - 1]);
        }

        private void RefreshSlot(int slot, SlotView view)
        {
            if (view == null)
                return;

            bool active = IsActive(slot);
            if (view.Background != null)
                view.Background.color = active ? Active : Normal;

            Color textColor = active ? ActiveText : Color.white;
            if (view.Slot != null)
            {
                view.Slot.text = slot.ToString();
                view.Slot.color = textColor;
            }

            if (view.LegacyName != null)
                view.LegacyName.gameObject.SetActive(false);

            if (slot <= 3)
            {
                RefreshFirearmSlot(slot, view, textColor, active);
                return;
            }

            RefreshAuxiliarySlot(slot, view, textColor);
            SetFireModeVisible(view, false, null);
        }

        private void RefreshFirearmSlot(
            int slot,
            SlotView view,
            Color color,
            bool active
        )
        {
            WeaponController weapon =
                weapons != null ? weapons.GetWeaponForSlot(slot) : null;
            InventoryItemDefinition item =
                lootEquipment != null ? lootEquipment.GetWeaponItem(slot) : null;

            SetIcon(view, item != null ? item.icon : null, color);

            if (view.Ammo != null)
            {
                view.Ammo.text = weapon != null
                    ? $"{weapon.AmmoInMagazine}/{weapon.ReserveAmmo}"
                    : string.Empty;
                view.Ammo.color = color;
            }

            bool showFireMode =
                slot <= 2 &&
                active &&
                weapon != null;

            SetFireModeVisible(
                view,
                showFireMode,
                weapon
            );
        }

        private void RefreshAuxiliarySlot(
            int slot,
            SlotView view,
            Color color
        )
        {
            InventoryItemDefinition item =
                lootEquipment != null ? lootEquipment.GetWeaponItem(slot) : null;

            SetIcon(view, item != null ? item.icon : null, color);

            if (view.Ammo == null)
                return;

            if (item == null)
            {
                view.Ammo.text = string.Empty;
                view.Ammo.color = color;
                return;
            }

            if (slot == (int)PlayerWeaponSlot.Melee)
            {
                view.Ammo.text = "∞";
                view.Ammo.color = color;
                return;
            }

            view.Ammo.text = GetInventoryCount(item).ToString();
            view.Ammo.color = color;
        }

        private static void SetIcon(
            SlotView view,
            Sprite sprite,
            Color textColor
        )
        {
            if (view.Icon == null)
                return;

            view.Icon.sprite = sprite;
            view.Icon.enabled = sprite != null;
            view.Icon.preserveAspect = true;
            view.Icon.color = sprite != null
                ? Color.white
                : textColor;
        }

        private static void SetFireModeVisible(
            SlotView view,
            bool visible,
            WeaponController weapon
        )
        {
            if (view.FireModeRoot == null)
                return;

            view.FireModeRoot.gameObject.SetActive(visible);
            if (!visible)
                return;

            if (view.FireModeKey != null)
                view.FireModeKey.text = "B";

            if (view.FireModeText != null)
            {
                view.FireModeText.text = weapon != null
                    ? ToRosFireModeText(weapon.CurrentFireMode)
                    : string.Empty;
            }
        }

        private static string ToRosFireModeText(WeaponFireMode mode)
        {
            return mode switch
            {
                WeaponFireMode.Auto => "AUTO",
                WeaponFireMode.Burst => "BURST",
                WeaponFireMode.Single => "SINGLE",
                _ => mode.ToString().ToUpperInvariant()
            };
        }

        private bool IsActive(int slot)
        {
            if (slot <= 3)
                return weapons != null && weapons.EquippedSlot == slot;

            return auxiliarySlots != null &&
                   (int)auxiliarySlots.SelectedAuxiliarySlot == slot;
        }

        private int GetInventoryCount(InventoryItemDefinition item)
        {
            if (inventory == null || item == null)
                return 0;

            int total = 0;
            foreach (InventoryStack stack in inventory.Stacks)
            {
                if (stack != null && stack.item == item)
                    total += Mathf.Max(0, stack.amount);
            }
            return total;
        }

        private static T FindNamed<T>(Transform root, string objectName)
            where T : Component
        {
            T[] all = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName)
                    return all[i];
            }
            return null;
        }
    }
}
