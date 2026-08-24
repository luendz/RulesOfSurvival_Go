using System.Collections.Generic;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Controla exclusivamente las vistas fisicas NearbyObjectIndicator y
    /// DeathLootPanelROS ya presentes dentro de HUD_ROS_EDITABLE.
    /// Nunca crea GameObjects, Canvas, filas, textos ni imagenes en runtime.
    /// </summary>
    [DefaultExecutionOrder(2800)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDNearbyLootPresenter : MonoBehaviour
    {
        private const float MaximumOpenDistance = 4.5f;
        private const int VisibleRows = 7;

        private static readonly Color Yellow =
            new Color(1f, 0.86f, 0.03f, 0.98f);
        private static readonly Color YellowSelected =
            new Color(1f, 0.93f, 0.28f, 1f);

        [Header("Physical HUD References")]
        [SerializeField] private RectTransform nearbyRoot;
        [SerializeField] private Image nearbyIcon;
        [SerializeField] private Text nearbyText;
        [SerializeField] private RectTransform deathLootRoot;
        [SerializeField] private Text deathLootTitle;
        [SerializeField] private Text deathLootFooter;
        [SerializeField] private LootRowView[] rowViews =
            new LootRowView[VisibleRows];

        [System.Serializable]
        private sealed class LootRowView
        {
            public RectTransform root;
            public Image background;
            public Image icon;
            public Text name;
            public Text amount;
            public Image selection;
        }

        private PlayerInputReader _localInput;
        private PlayerInteractor _interactor;
        private InventoryComponent _inventory;
        private DeathLootContainer _openedContainer;
        private int _selectedIndex;
        private int _openedFrame = -1;
        private float _nextResolveTime;

        public bool IsOpen =>
            _openedContainer != null && _inventory != null;

        public DeathLootContainer OpenedContainer => _openedContainer;

        private void Awake()
        {
            BindPhysicalView();
            ResolveLocalPlayer();
            SetDeathLootVisible(false);
            SetNearbyVisible(false);
        }

        public static RulesOfSurvivalHUDNearbyLootPresenter OpenOrCreate(
            DeathLootContainer container,
            GameObject interactor
        )
        {
            RulesOfSurvivalHUDNearbyLootPresenter presenter =
                FindFirstObjectByType<RulesOfSurvivalHUDNearbyLootPresenter>();

            if (presenter == null)
            {
                Debug.LogError(
                    "[Editor First] Falta RulesOfSurvivalHUDNearbyLootPresenter " +
                    "fisico en el HUD. No se creara en runtime."
                );
                return null;
            }

            presenter.Open(container, interactor);
            return presenter;
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

            BindPhysicalView();
            if (deathLootRoot == null)
            {
                Debug.LogError(
                    "[Editor First] Falta DeathLootPanelROS fisico en HUD_ROS_EDITABLE."
                );
                return false;
            }

            _localInput = interactor.GetComponent<PlayerInputReader>();
            _interactor = interactor.GetComponent<PlayerInteractor>();
            _inventory = inventory;

            if (_openedContainer != container)
            {
                _openedContainer = container;
                _selectedIndex = 0;
                _openedFrame = Time.frameCount;
            }

            DrawOpenedPanel();
            return true;
        }

        public void Close()
        {
            _openedContainer = null;
            _selectedIndex = 0;
            _openedFrame = -1;
            SetDeathLootVisible(false);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.15f;
                ResolveLocalPlayer();
                BindPhysicalView();
            }

            HideLegacyNearbyLootPanel();
            UpdateNearbyIndicator();
            UpdateOpenedLoot();
        }

        private void ResolveLocalPlayer()
        {
            if (_localInput != null && _localInput.gameObject.activeInHierarchy)
            {
                _interactor ??= _localInput.GetComponent<PlayerInteractor>();
                _inventory ??= _localInput.GetComponent<InventoryComponent>();
                return;
            }

            PlayerInputReader[] inputs =
                FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None);

            _localInput = null;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null || !inputs[i].gameObject.activeInHierarchy)
                    continue;

                if (inputs[i].GetComponent<PlayerInteractor>() == null)
                    continue;

                _localInput = inputs[i];
                break;
            }

            _interactor = _localInput != null
                ? _localInput.GetComponent<PlayerInteractor>()
                : null;
            _inventory = _localInput != null
                ? _localInput.GetComponent<InventoryComponent>()
                : null;
        }

        private void BindPhysicalView()
        {
            Transform canvas = transform.Find("Canvas");
            if (canvas == null)
            {
                Canvas childCanvas = GetComponentInChildren<Canvas>(true);
                canvas = childCanvas != null ? childCanvas.transform : null;
            }

            if (canvas == null)
                return;

            if (nearbyRoot == null)
            {
                Transform t = canvas.Find("NearbyObjectIndicator");
                nearbyRoot = t as RectTransform;
            }

            if (nearbyRoot != null)
            {
                nearbyIcon ??= nearbyRoot.Find("Icon")?.GetComponent<Image>();
                nearbyText ??= nearbyRoot.Find("Text")?.GetComponent<Text>();
            }

            if (deathLootRoot == null)
            {
                Transform t = canvas.Find("DeathLootPanelROS");
                deathLootRoot = t as RectTransform;
            }

            if (deathLootRoot == null)
                return;

            deathLootTitle ??=
                deathLootRoot.Find("Title/Text")?.GetComponent<Text>();
            deathLootFooter ??=
                deathLootRoot.Find("Footer/Text")?.GetComponent<Text>();

            if (rowViews == null || rowViews.Length != VisibleRows)
                rowViews = new LootRowView[VisibleRows];

            for (int i = 0; i < VisibleRows; i++)
            {
                Transform row = deathLootRoot.Find("Row_" + i);
                if (row == null)
                    continue;

                LootRowView view = rowViews[i] ?? new LootRowView();
                view.root = row as RectTransform;
                view.background = row.GetComponent<Image>();
                view.icon = row.Find("Icon")?.GetComponent<Image>();
                view.name = row.Find("Name")?.GetComponent<Text>();
                view.amount = row.Find("Amount")?.GetComponent<Text>();
                view.selection = row.Find("Selection")?.GetComponent<Image>();
                rowViews[i] = view;
            }
        }

        private void UpdateOpenedLoot()
        {
            if (!IsOpen)
            {
                SetDeathLootVisible(false);
                return;
            }

            if (_localInput == null || _inventory == null ||
                _openedContainer == null || _openedContainer.IsEmpty)
            {
                Close();
                return;
            }

            float distance = Vector3.Distance(
                _localInput.transform.position,
                _openedContainer.transform.position
            );

            if (distance > MaximumOpenDistance)
            {
                Close();
                return;
            }

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            List<InventoryStack> stacks = Snapshot(_openedContainer);
            if (stacks.Count == 0)
            {
                Close();
                return;
            }

            HandleSelection(stacks.Count);
            DrawOpenedPanel(stacks);

            if (Time.frameCount != _openedFrame)
                HandlePickup(stacks);
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

        private void HandlePickup(List<InventoryStack> stacks)
        {
            if (Keyboard.current == null ||
                !Keyboard.current.fKey.wasPressedThisFrame ||
                _selectedIndex < 0 || _selectedIndex >= stacks.Count)
                return;

            InventoryStack selected = stacks[_selectedIndex];
            if (selected == null || selected.item == null)
                return;

            _openedContainer.TryLoot(
                selected.item,
                selected.amount,
                _inventory
            );

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

        private void DrawOpenedPanel()
        {
            if (_openedContainer != null)
                DrawOpenedPanel(Snapshot(_openedContainer));
        }

        private void DrawOpenedPanel(List<InventoryStack> stacks)
        {
            BindPhysicalView();
            if (deathLootRoot == null)
                return;

            SetDeathLootVisible(true);
            deathLootRoot.SetAsLastSibling();

            if (deathLootTitle != null)
            {
                deathLootTitle.text = _openedContainer != null
                    ? _openedContainer.DisplayName.ToUpperInvariant()
                    : "LOOT";
            }

            int firstVisible = Mathf.Clamp(
                _selectedIndex - VisibleRows + 1,
                0,
                Mathf.Max(0, stacks.Count - VisibleRows)
            );

            for (int rowIndex = 0; rowIndex < VisibleRows; rowIndex++)
            {
                LootRowView view = rowViews != null && rowIndex < rowViews.Length
                    ? rowViews[rowIndex]
                    : null;
                int stackIndex = firstVisible + rowIndex;

                if (view?.root == null)
                    continue;

                if (stackIndex >= stacks.Count)
                {
                    view.root.gameObject.SetActive(false);
                    continue;
                }

                view.root.gameObject.SetActive(true);
                InventoryStack stack = stacks[stackIndex];
                bool selected = stackIndex == _selectedIndex;

                if (view.name != null)
                    view.name.text = stack.item.displayName;

                if (view.amount != null)
                    view.amount.text = stack.amount > 1
                        ? $"x{stack.amount}"
                        : string.Empty;

                if (view.icon != null)
                {
                    view.icon.sprite = stack.item.icon;
                    view.icon.enabled = stack.item.icon != null;
                    view.icon.color = Color.white;
                }

                if (view.background != null)
                    view.background.color = selected ? YellowSelected : Yellow;

                if (view.selection != null)
                {
                    view.selection.color = selected
                        ? new Color(0.35f, 0.12f, 0.42f, 0.18f)
                        : Color.clear;
                }
            }

            if (deathLootFooter != null)
            {
                deathLootFooter.text =
                    $"{_selectedIndex + 1}/{stacks.Count}  •  RUEDA  •  F RECOGER  •  ESC";
            }
        }

        private void UpdateNearbyIndicator()
        {
            if (nearbyRoot == null || _localInput == null)
                return;

            IInteractable interactable = ResolveNearestInteractable();
            if (interactable == null)
            {
                SetNearbyVisible(false);
                return;
            }

            string label = interactable.InteractionLabel;
            Sprite icon = ResolveIcon(interactable);

            if (IsOpen && interactable is DeathLootContainer opened &&
                opened == _openedContainer)
            {
                label = $"Caja abierta: {_openedContainer.ItemCount} objetos";
            }

            if (nearbyText != null)
            {
                nearbyText.text = string.IsNullOrWhiteSpace(label)
                    ? "OBJETO CERCANO"
                    : label;
            }

            if (nearbyIcon != null)
            {
                nearbyIcon.sprite = icon;
                nearbyIcon.enabled = icon != null;
            }

            SetNearbyVisible(true);
            nearbyRoot.SetAsLastSibling();
        }

        private IInteractable ResolveNearestInteractable()
        {
            if (_interactor != null)
            {
                IInteractable current = _interactor.Current;
                if (current != null)
                    return current;

                IReadOnlyList<IInteractable> nearby = _interactor.Nearby;
                if (nearby != null && nearby.Count > 0)
                    return nearby[0];
            }

            return _openedContainer;
        }

        private static Sprite ResolveIcon(IInteractable interactable)
        {
            if (interactable is not DeathLootContainer deathContainer ||
                deathContainer.StoredInventory == null)
                return null;

            IReadOnlyList<InventoryStack> stacks =
                deathContainer.StoredInventory.Stacks;

            for (int i = 0; i < stacks.Count; i++)
            {
                InventoryItemDefinition item = stacks[i]?.item;
                if (item != null && item.icon != null)
                    return item.icon;
            }

            return null;
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

        private static void HideLegacyNearbyLootPanel()
        {
            RulesOfSurvivalHUD hud = FindFirstObjectByType<RulesOfSurvivalHUD>();
            if (hud == null)
                return;

            Transform legacy = hud.transform.Find("Canvas/NearbyLoot");
            if (legacy != null && legacy.gameObject.activeSelf)
                legacy.gameObject.SetActive(false);
        }

        private void SetNearbyVisible(bool visible)
        {
            if (nearbyRoot != null && nearbyRoot.gameObject.activeSelf != visible)
                nearbyRoot.gameObject.SetActive(visible);
        }

        private void SetDeathLootVisible(bool visible)
        {
            if (deathLootRoot != null && deathLootRoot.gameObject.activeSelf != visible)
                deathLootRoot.gameObject.SetActive(visible);
        }
    }
}
