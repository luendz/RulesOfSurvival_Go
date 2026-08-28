using System.Collections.Generic;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// NearbyLoot es la única lista visual de loot del HUD.
    /// Muestra pickups del suelo y contenido de cajas de jugadores eliminados.
    /// Nunca crea UI en runtime: solo enlaza y actualiza filas físicas existentes.
    /// </summary>
    [DefaultExecutionOrder(2800)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDNearbyLootPresenter : MonoBehaviour
    {
        private const float MaximumOpenDistance = 4.5f;
        private const int VisibleRows = 7;
        private const float WeaponIconTilt = -12f;

        private static readonly Color Yellow =
            new Color(1f, 0.86f, 0.03f, 0.98f);
        private static readonly Color YellowSelected =
            new Color(1f, 0.93f, 0.28f, 1f);
        private static readonly Color UnavailableBackground =
            new Color(1f, 0.86f, 0.03f, 0.16f);
        private static readonly Color TextAvailable =
            new Color(0.05f, 0.05f, 0.05f, 1f);
        private static readonly Color TextUnavailable =
            new Color(1f, 1f, 1f, 0.42f);

        [Header("Physical NearbyLoot References")]
        [SerializeField] private RectTransform nearbyRoot;
        [SerializeField] private Text nearbyTitle;
        [SerializeField] private Text nearbyHint;
        [SerializeField] private LootRowView[] rowViews =
            new LootRowView[VisibleRows];

        [System.Serializable]
        private sealed class LootRowView
        {
            public RectTransform root;
            public Image background;
            public Image mainIcon;
            public Text name;
            public Text secondaryText;
            public Image secondaryIcon;
            public Image selection;
        }

        private sealed class NearbyEntry
        {
            public InventoryItemDefinition item;
            public LootPickup pickup;
            public DeathLootContainer container;
            public bool canCollect;
            public bool isCurrent;
        }

        [Header("Player References")]
        [SerializeField] private PlayerInputReader _localInput;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private InventoryComponent _inventory;
        [SerializeField] private PlayerLootEquipment _lootEquipment;
        private DeathLootContainer _openedContainer;
        private int _selectedIndex;
        private int _openedFrame = -1;

        public static RulesOfSurvivalHUDNearbyLootPresenter Instance { get; private set; }

        public bool IsOpen =>
            _openedContainer != null && _inventory != null;

        public DeathLootContainer OpenedContainer => _openedContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[{nameof(RulesOfSurvivalHUDNearbyLootPresenter)}] Hay más de una instancia activa.", this);
                enabled = false;
                return;
            }

            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    $"[{nameof(RulesOfSurvivalHUDNearbyLootPresenter)}] Referencias incompletas en '{name}'.",
                    this);
                enabled = false;
                return;
            }

            Instance = this;
            SetNearbyVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static RulesOfSurvivalHUDNearbyLootPresenter OpenConfigured(
            DeathLootContainer container,
            GameObject interactor
        )
        {
            if (Instance == null)
            {
                Debug.LogError(
                    "[Editor First] Falta RulesOfSurvivalHUDNearbyLootPresenter " +
                    "configurado y activo en la escena."
                );
                return null;
            }

            Instance.Open(container, interactor);
            return Instance;
        }

        public bool Open(
            DeathLootContainer container,
            GameObject interactor
        )
        {
            if (container == null || interactor == null)
                return false;

            InventoryComponent inventory =
                interactor.GetComponent<InventoryComponent>();
            if (inventory == null)
                return false;

            if (_localInput == null || interactor != _localInput.gameObject)
            {
                Debug.LogError(
                    "[Editor First] El HUD de loot no pertenece al jugador que interactuó."
                );
                return false;
            }

            if (_openedContainer != container)
            {
                _openedContainer = container;
                _selectedIndex = 0;
                _openedFrame = Time.frameCount;
            }

            DrawOpenedContainer();
            return true;
        }

        public void Close()
        {
            _openedContainer = null;
            _selectedIndex = 0;
            _openedFrame = -1;
        }

        private void LateUpdate()
        {
            if (IsOpen)
                UpdateOpenedContainer();
            else
                UpdateNearbyLoot();
        }

        private bool HasRequiredReferences()
        {
            if (nearbyRoot == null || nearbyTitle == null || nearbyHint == null ||
                _localInput == null || _interactor == null || _inventory == null ||
                _lootEquipment == null || rowViews == null || rowViews.Length != VisibleRows)
                return false;

            for (int i = 0; i < rowViews.Length; i++)
            {
                if (rowViews[i] == null || rowViews[i].root == null ||
                    rowViews[i].background == null || rowViews[i].name == null)
                    return false;
            }

            return true;
        }

        private void UpdateNearbyLoot()
        {
            if (nearbyRoot == null || _localInput == null || _interactor == null)
            {
                SetNearbyVisible(false);
                return;
            }

            List<NearbyEntry> entries = BuildNearbyEntries();
            if (entries.Count == 0)
            {
                SetNearbyVisible(false);
                return;
            }

            SetNearbyVisible(true);
            nearbyRoot.SetAsLastSibling();

            bool heritage = false;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].container != null)
                {
                    heritage = true;
                    break;
                }
            }

            if (nearbyTitle != null)
                nearbyTitle.text = heritage ? "HERITAGE" : "NEARBY";

            if (nearbyHint != null)
                nearbyHint.text = "F RECOGER";

            DrawEntries(entries, 0, -1);
        }

        private List<NearbyEntry> BuildNearbyEntries()
        {
            List<NearbyEntry> result = new List<NearbyEntry>();
            IReadOnlyList<IInteractable> nearby = _interactor.Nearby;
            IInteractable current = _interactor.Current;

            if (nearby == null)
                return result;

            for (int i = 0; i < nearby.Count; i++)
            {
                IInteractable interactable = nearby[i];
                if (interactable == null)
                    continue;

                if (interactable is LootPickup pickup)
                {
                    if (pickup.Item == null || pickup.IsConsumed)
                        continue;

                    result.Add(new NearbyEntry
                    {
                        item = pickup.Item,
                        pickup = pickup,
                        canCollect = pickup.CanInteract(_localInput.gameObject),
                        isCurrent = ReferenceEquals(interactable, current)
                    });
                    continue;
                }

                if (interactable is DeathLootContainer container)
                {
                    List<InventoryStack> stacks = Snapshot(container);
                    for (int s = 0; s < stacks.Count; s++)
                    {
                        InventoryStack stack = stacks[s];
                        result.Add(new NearbyEntry
                        {
                            item = stack.item,
                            container = container,
                            canCollect = CanReceiveItem(stack.item),
                            isCurrent = ReferenceEquals(interactable, current) && s == 0
                        });
                    }
                }
            }

            return result;
        }

        private void UpdateOpenedContainer()
        {
            if (_localInput == null || _inventory == null ||
                _openedContainer == null || _openedContainer.IsEmpty)
            {
                Close();
                UpdateNearbyLoot();
                return;
            }

            float distance = Vector3.Distance(
                _localInput.transform.position,
                _openedContainer.transform.position
            );

            if (distance > MaximumOpenDistance)
            {
                Close();
                UpdateNearbyLoot();
                return;
            }

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                UpdateNearbyLoot();
                return;
            }

            List<InventoryStack> stacks = Snapshot(_openedContainer);
            if (stacks.Count == 0)
            {
                Close();
                UpdateNearbyLoot();
                return;
            }

            HandleSelection(stacks.Count);
            DrawOpenedContainer(stacks);

            if (Time.frameCount != _openedFrame)
                HandleOpenedPickup(stacks);
        }

        private void HandleSelection(int count)
        {
            _selectedIndex = Mathf.Clamp(
                _selectedIndex,
                0,
                Mathf.Max(0, count - 1)
            );

            if (Mouse.current == null)
                return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0.01f)
                _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
            else if (scroll < -0.01f)
                _selectedIndex = Mathf.Min(count - 1, _selectedIndex + 1);
        }

        private void HandleOpenedPickup(List<InventoryStack> stacks)
        {
            if (Keyboard.current == null ||
                !Keyboard.current.fKey.wasPressedThisFrame ||
                _selectedIndex < 0 || _selectedIndex >= stacks.Count)
                return;

            InventoryStack selected = stacks[_selectedIndex];
            if (selected == null || selected.item == null ||
                !CanReceiveItem(selected.item))
                return;

            bool collected = TryCollectFromOpenedContainer(selected);
            if (!collected)
                return;

            if (_openedContainer == null || _openedContainer.IsEmpty)
            {
                Close();
                return;
            }

            List<InventoryStack> remaining = Snapshot(_openedContainer);
            _selectedIndex = Mathf.Clamp(
                _selectedIndex,
                0,
                Mathf.Max(0, remaining.Count - 1)
            );
        }

        private bool TryCollectFromOpenedContainer(InventoryStack stack)
        {
            if (_openedContainer == null || stack?.item == null)
                return false;

            InventoryItemDefinition item = stack.item;

            if (item.IsEquippable)
            {
                if (_lootEquipment == null ||
                    !_lootEquipment.TryEquip(item, out InventoryItemDefinition replaced))
                    return false;

                if (!_openedContainer.StoredInventory.Remove(item, 1))
                    return false;

                if (replaced != null && replaced != item)
                    _openedContainer.StoredInventory.Add(replaced, 1);

                _openedContainer.DestroyIfEmptyAfterExternalLoot();
                return true;
            }

            return _openedContainer.TryLoot(
                item,
                stack.amount,
                _inventory
            ) > 0;
        }

        private void DrawOpenedContainer()
        {
            if (_openedContainer != null)
                DrawOpenedContainer(Snapshot(_openedContainer));
        }

        private void DrawOpenedContainer(List<InventoryStack> stacks)
        {
            if (nearbyRoot == null)
                return;

            SetNearbyVisible(true);
            nearbyRoot.SetAsLastSibling();

            if (nearbyTitle != null)
                nearbyTitle.text = "HERITAGE";

            if (nearbyHint != null)
                nearbyHint.text = "RUEDA  •  F RECOGER  •  ESC";

            List<NearbyEntry> entries = new List<NearbyEntry>(stacks.Count);
            for (int i = 0; i < stacks.Count; i++)
            {
                InventoryStack stack = stacks[i];
                entries.Add(new NearbyEntry
                {
                    item = stack.item,
                    container = _openedContainer,
                    canCollect = CanReceiveItem(stack.item),
                    isCurrent = i == _selectedIndex
                });
            }

            int firstVisible = Mathf.Clamp(
                _selectedIndex - VisibleRows + 1,
                0,
                Mathf.Max(0, entries.Count - VisibleRows)
            );

            DrawEntries(entries, firstVisible, _selectedIndex);
        }

        private void DrawEntries(
            List<NearbyEntry> entries,
            int firstVisible,
            int selectedAbsoluteIndex
        )
        {
            for (int rowIndex = 0; rowIndex < VisibleRows; rowIndex++)
            {
                LootRowView view = rowViews != null && rowIndex < rowViews.Length
                    ? rowViews[rowIndex]
                    : null;
                int entryIndex = firstVisible + rowIndex;

                if (view?.root == null)
                    continue;

                if (entryIndex >= entries.Count)
                {
                    view.root.gameObject.SetActive(false);
                    continue;
                }

                NearbyEntry entry = entries[entryIndex];
                InventoryItemDefinition item = entry.item;
                if (item == null)
                {
                    view.root.gameObject.SetActive(false);
                    continue;
                }

                view.root.gameObject.SetActive(true);
                bool selected = selectedAbsoluteIndex >= 0
                    ? entryIndex == selectedAbsoluteIndex
                    : entry.isCurrent;

                Color textColor = entry.canCollect
                    ? TextAvailable
                    : TextUnavailable;

                if (view.background != null)
                {
                    view.background.color = entry.canCollect
                        ? (selected ? YellowSelected : Yellow)
                        : UnavailableBackground;
                }

                if (view.name != null)
                {
                    view.name.text = item.displayName;
                    view.name.color = textColor;
                }

                ConfigureMainIcon(view.mainIcon, item, entry.canCollect);
                ConfigureSecondary(view, item, textColor);

                if (view.selection != null)
                {
                    view.selection.color = selected && entry.canCollect
                        ? new Color(0.35f, 0.12f, 0.42f, 0.16f)
                        : Color.clear;
                }
            }
        }

        private static void ConfigureMainIcon(
            Image image,
            InventoryItemDefinition item,
            bool available
        )
        {
            if (image == null)
                return;

            image.sprite = item != null ? item.icon : null;
            image.enabled = image.sprite != null;
            image.preserveAspect = true;
            image.color = available
                ? Color.white
                : new Color(1f, 1f, 1f, 0.42f);

            image.rectTransform.localEulerAngles = new Vector3(
                0f,
                0f,
                item != null && item.itemType == ItemType.Weapon
                    ? WeaponIconTilt
                    : 0f
            );
        }

        private static void ConfigureSecondary(
            LootRowView view,
            InventoryItemDefinition item,
            Color textColor
        )
        {
            bool ammo = item.itemType == ItemType.Ammo;

            if (view.secondaryText != null)
            {
                view.secondaryText.gameObject.SetActive(!ammo);
                view.secondaryText.text = ammo
                    ? string.Empty
                    : ResolveSecondaryText(item);
                view.secondaryText.color = textColor;
            }

            if (view.secondaryIcon != null)
            {
                Sprite icon = ammo ? item.nearbySecondaryIcon : null;
                view.secondaryIcon.sprite = icon;
                view.secondaryIcon.enabled = icon != null;
                view.secondaryIcon.gameObject.SetActive(ammo);
                view.secondaryIcon.preserveAspect = true;
                view.secondaryIcon.color = textColor.a < 0.9f
                    ? new Color(1f, 1f, 1f, 0.42f)
                    : Color.white;
            }
        }

        private static string ResolveSecondaryText(InventoryItemDefinition item)
        {
            if (item == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(item.nearbySecondaryText))
                return item.nearbySecondaryText;

            switch (item.itemType)
            {
                case ItemType.Weapon:
                    return item.weaponDefinition != null
                        ? WeaponFamilyLabel(item.weaponDefinition.family)
                        : "Weapon";
                case ItemType.Healing:
                    if (item.consumableDefinition != null)
                    {
                        bool hp = item.consumableDefinition.healAmount > 0f;
                        bool energy = item.consumableDefinition.energyAmount > 0f;
                        if (hp && energy) return "Speed and HP up";
                        if (hp) return "+HP";
                        if (energy) return "Energy up";
                    }
                    return "Healing item";
                case ItemType.Armor:
                    return "Reduces damage";
                case ItemType.Helmet:
                    return "Reduces head damage";
                case ItemType.Backpack:
                    return "+capacity";
                case ItemType.Throwable:
                    return "Throwable";
                case ItemType.Attachment:
                    return "Attachment";
                default:
                    return string.Empty;
            }
        }

        private static string WeaponFamilyLabel(WeaponFamily family)
        {
            return family switch
            {
                WeaponFamily.AssaultRifle => "Assault Rifle",
                WeaponFamily.SubmachineGun => "SMG",
                WeaponFamily.SniperRifle => "Sniper Rifle",
                WeaponFamily.Shotgun => "Shotgun",
                WeaponFamily.Pistol => "Pistol",
                WeaponFamily.LightMachineGun => "LMG",
                WeaponFamily.Melee => "Melee",
                _ => "Weapon"
            };
        }

        private bool CanReceiveItem(InventoryItemDefinition item)
        {
            if (item == null || _localInput == null)
                return false;

            if (item.IsEquippable)
                return _lootEquipment != null && _lootEquipment.CanEquip(item);

            return _inventory != null &&
                   _inventory.GetMaxAddableAmount(item) > 0;
        }

        private static List<InventoryStack> Snapshot(
            DeathLootContainer container
        )
        {
            List<InventoryStack> result = new List<InventoryStack>();
            if (container == null || container.StoredInventory == null)
                return result;

            IReadOnlyList<InventoryStack> stacks =
                container.StoredInventory.Stacks;

            for (int i = 0; i < stacks.Count; i++)
            {
                InventoryStack stack = stacks[i];
                if (stack != null && stack.item != null && stack.amount > 0)
                    result.Add(stack);
            }

            return result;
        }

        private void SetNearbyVisible(bool visible)
        {
            if (nearbyRoot != null && nearbyRoot.gameObject.activeSelf != visible)
                nearbyRoot.gameObject.SetActive(visible);
        }
    }
}
