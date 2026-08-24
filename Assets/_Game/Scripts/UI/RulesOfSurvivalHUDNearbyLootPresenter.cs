using System.Collections.Generic;
using System.Reflection;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Propietario único del HUD de loot de jugadores muertos.
    ///
    /// Flujo:
    /// - PlayerInteractor detecta DeathLootContainer y muestra "Abrir...".
    /// - F llama DeathLootContainer.Interact().
    /// - Interact() llama OpenOrCreate() aquí.
    /// - Este presenter crea y controla un panel ROS dedicado que ningún otro
    ///   presenter conoce, evitando conflictos con Canvas/NearbyLoot heredado.
    /// - Rueda selecciona, F recoge, ESC o alejarse cierra.
    ///
    /// También mantiene el indicador discreto de objeto cercano debajo de
    /// KILL/LEFT y usa el icono real del InventoryItemDefinition cuando existe.
    /// </summary>
    [DefaultExecutionOrder(2800)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDNearbyLootPresenter : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const float MaximumOpenDistance = 4.5f;
        private const int VisibleRows = 7;

        private static readonly Color Yellow =
            new Color(1f, 0.86f, 0.03f, 0.98f);

        private static readonly Color YellowSelected =
            new Color(1f, 0.93f, 0.28f, 1f);

        private static readonly Color Dark =
            new Color(0.025f, 0.035f, 0.045f, 0.94f);

        private static readonly Color RowText =
            new Color(0.05f, 0.05f, 0.05f, 1f);

        private PlayerInputReader _localInput;
        private PlayerInteractor _interactor;
        private InventoryComponent _inventory;

        private DeathLootContainer _openedContainer;
        private int _selectedIndex;
        private int _openedFrame = -1;
        private float _nextResolveTime;

        private RectTransform _nearbyRoot;
        private Image _nearbyIcon;
        private Text _nearbyText;

        private RectTransform _deathLootRoot;
        private Text _deathLootTitle;
        private Text _deathLootFooter;
        private readonly List<LootRowView> _rowViews =
            new List<LootRowView>(VisibleRows);

        private sealed class LootRowView
        {
            public RectTransform Root;
            public Image Background;
            public Image Icon;
            public Text Name;
            public Text Amount;
            public Image Selection;
        }

        public bool IsOpen =>
            _openedContainer != null &&
            _inventory != null;

        public DeathLootContainer OpenedContainer => _openedContainer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDNearbyLootPresenter>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_NearbyLootPresenter")
                .AddComponent<RulesOfSurvivalHUDNearbyLootPresenter>();
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
                presenter = new GameObject("ROS_HUD_NearbyLootPresenter")
                    .AddComponent<RulesOfSurvivalHUDNearbyLootPresenter>();
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
            {
                return false;
            }

            InventoryComponent inventory =
                interactor.GetComponent<InventoryComponent>();

            if (inventory == null)
            {
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

            RepairEmptyContainerIfNeeded(_openedContainer);
            EnsureDeathLootPanel();
            DrawOpenedPanel();
            return true;
        }

        public void Close()
        {
            _openedContainer = null;
            _selectedIndex = 0;
            _openedFrame = -1;

            if (_deathLootRoot != null)
            {
                _deathLootRoot.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.15f;
                ResolveLocalPlayer();
                EnsureNearbyIndicator();
                EnsureDeathLootPanel();
            }

            HideLegacyNearbyLootPanel();
            UpdateNearbyIndicator();
            UpdateOpenedLoot();
        }

        private void ResolveLocalPlayer()
        {
            if (IsValidLocalInput(_localInput))
            {
                _interactor ??= _localInput.GetComponent<PlayerInteractor>();
                _inventory ??= _localInput.GetComponent<InventoryComponent>();
                return;
            }

            _localInput = FindLocalPlayerInput();
            _interactor = null;
            _inventory = null;

            if (_localInput == null)
            {
                return;
            }

            _interactor = _localInput.GetComponent<PlayerInteractor>();
            _inventory = _localInput.GetComponent<InventoryComponent>();
        }

        private void UpdateOpenedLoot()
        {
            if (!IsOpen)
            {
                if (_deathLootRoot != null)
                {
                    _deathLootRoot.gameObject.SetActive(false);
                }
                return;
            }

            if (_localInput == null || _inventory == null || _openedContainer == null)
            {
                Close();
                return;
            }

            RepairEmptyContainerIfNeeded(_openedContainer);

            if (_openedContainer.IsEmpty)
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
            {
                HandlePickup(stacks);
            }
        }

        private void HandleSelection(int count)
        {
            _selectedIndex = Mathf.Clamp(
                _selectedIndex,
                0,
                Mathf.Max(0, count - 1)
            );

            if (Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0.01f)
            {
                _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
            }
            else if (scroll < -0.01f)
            {
                _selectedIndex = Mathf.Min(count - 1, _selectedIndex + 1);
            }
        }

        private void HandlePickup(List<InventoryStack> stacks)
        {
            if (Keyboard.current == null ||
                !Keyboard.current.fKey.wasPressedThisFrame ||
                _selectedIndex < 0 ||
                _selectedIndex >= stacks.Count)
            {
                return;
            }

            InventoryStack selected = stacks[_selectedIndex];
            if (selected == null || selected.item == null)
            {
                return;
            }

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

        private void EnsureDeathLootPanel()
        {
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform canvas = hud.transform.Find("Canvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.Find("DeathLootPanelROS");
            if (existing != null)
            {
                _deathLootRoot = existing as RectTransform;
                CacheDeathLootPanelReferences(existing);
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject rootObject = new GameObject("DeathLootPanelROS");
            rootObject.transform.SetParent(canvas, false);
            _deathLootRoot = rootObject.AddComponent<RectTransform>();
            _deathLootRoot.anchorMin = new Vector2(1f, 0.5f);
            _deathLootRoot.anchorMax = new Vector2(1f, 0.5f);
            _deathLootRoot.pivot = new Vector2(1f, 0.5f);
            _deathLootRoot.anchoredPosition = new Vector2(-22f, 0f);
            _deathLootRoot.sizeDelta = new Vector2(270f, 430f);

            Image rootBackground = rootObject.AddComponent<Image>();
            rootBackground.color = Yellow;
            rootBackground.raycastTarget = false;

            Outline rootOutline = rootObject.AddComponent<Outline>();
            rootOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            rootOutline.effectDistance = new Vector2(2f, -2f);

            GameObject titleObject = new GameObject("Title");
            titleObject.transform.SetParent(_deathLootRoot, false);
            RectTransform titleRect = titleObject.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 38f);
            Image titleBg = titleObject.AddComponent<Image>();
            titleBg.color = Dark;
            titleBg.raycastTarget = false;

            GameObject titleTextObject = new GameObject("Text");
            titleTextObject.transform.SetParent(titleRect, false);
            RectTransform titleTextRect = titleTextObject.AddComponent<RectTransform>();
            Stretch(titleTextRect, 10f, 3f, 8f, 2f);
            _deathLootTitle = titleTextObject.AddComponent<Text>();
            _deathLootTitle.font = font;
            _deathLootTitle.fontSize = 17;
            _deathLootTitle.fontStyle = FontStyle.BoldAndItalic;
            _deathLootTitle.alignment = TextAnchor.MiddleLeft;
            _deathLootTitle.color = Color.white;
            _deathLootTitle.raycastTarget = false;
            AddOutline(titleTextObject, new Color(0f, 0f, 0f, 0.9f));

            _rowViews.Clear();
            for (int i = 0; i < VisibleRows; i++)
            {
                _rowViews.Add(CreateLootRow(_deathLootRoot, font, i));
            }

            GameObject footerObject = new GameObject("Footer");
            footerObject.transform.SetParent(_deathLootRoot, false);
            RectTransform footerRect = footerObject.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.anchoredPosition = Vector2.zero;
            footerRect.sizeDelta = new Vector2(0f, 30f);
            Image footerBg = footerObject.AddComponent<Image>();
            footerBg.color = Dark;
            footerBg.raycastTarget = false;

            GameObject footerTextObject = new GameObject("Text");
            footerTextObject.transform.SetParent(footerRect, false);
            RectTransform footerTextRect = footerTextObject.AddComponent<RectTransform>();
            Stretch(footerTextRect, 5f, 2f, 5f, 2f);
            _deathLootFooter = footerTextObject.AddComponent<Text>();
            _deathLootFooter.font = font;
            _deathLootFooter.fontSize = 10;
            _deathLootFooter.fontStyle = FontStyle.Bold;
            _deathLootFooter.alignment = TextAnchor.MiddleCenter;
            _deathLootFooter.color = new Color(1f, 0.90f, 0.12f, 1f);
            _deathLootFooter.raycastTarget = false;

            _deathLootRoot.gameObject.SetActive(false);
        }

        private LootRowView CreateLootRow(
            RectTransform parent,
            Font font,
            int rowIndex
        )
        {
            GameObject rowObject = new GameObject($"Row_{rowIndex}");
            rowObject.transform.SetParent(parent, false);
            RectTransform rowRect = rowObject.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -38f - rowIndex * 51f);
            rowRect.sizeDelta = new Vector2(0f, 51f);

            Image rowBackground = rowObject.AddComponent<Image>();
            rowBackground.color = Yellow;
            rowBackground.raycastTarget = false;

            GameObject selectionObject = new GameObject("Selection");
            selectionObject.transform.SetParent(rowRect, false);
            RectTransform selectionRect = selectionObject.AddComponent<RectTransform>();
            selectionRect.anchorMin = Vector2.zero;
            selectionRect.anchorMax = Vector2.one;
            selectionRect.offsetMin = Vector2.zero;
            selectionRect.offsetMax = Vector2.zero;
            Image selection = selectionObject.AddComponent<Image>();
            selection.color = Color.clear;
            selection.raycastTarget = false;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(rowRect, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(7f, 0f);
            iconRect.sizeDelta = new Vector2(43f, 43f);
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            GameObject nameObject = new GameObject("Name");
            nameObject.transform.SetParent(rowRect, false);
            RectTransform nameRect = nameObject.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(56f, 3f);
            nameRect.offsetMax = new Vector2(-42f, -3f);
            Text name = nameObject.AddComponent<Text>();
            name.font = font;
            name.fontSize = 13;
            name.fontStyle = FontStyle.Bold;
            name.alignment = TextAnchor.MiddleLeft;
            name.color = RowText;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;

            GameObject amountObject = new GameObject("Amount");
            amountObject.transform.SetParent(rowRect, false);
            RectTransform amountRect = amountObject.AddComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(1f, 0f);
            amountRect.anchorMax = new Vector2(1f, 1f);
            amountRect.pivot = new Vector2(1f, 0.5f);
            amountRect.anchoredPosition = new Vector2(-5f, 0f);
            amountRect.sizeDelta = new Vector2(36f, 0f);
            Text amount = amountObject.AddComponent<Text>();
            amount.font = font;
            amount.fontSize = 12;
            amount.fontStyle = FontStyle.Bold;
            amount.alignment = TextAnchor.MiddleRight;
            amount.color = RowText;
            amount.raycastTarget = false;

            GameObject dividerObject = new GameObject("Divider");
            dividerObject.transform.SetParent(rowRect, false);
            RectTransform dividerRect = dividerObject.AddComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(0f, 0f);
            dividerRect.anchorMax = new Vector2(1f, 0f);
            dividerRect.pivot = new Vector2(0.5f, 0f);
            dividerRect.anchoredPosition = Vector2.zero;
            dividerRect.sizeDelta = new Vector2(0f, 1f);
            Image divider = dividerObject.AddComponent<Image>();
            divider.color = new Color(0f, 0f, 0f, 0.22f);
            divider.raycastTarget = false;

            return new LootRowView
            {
                Root = rowRect,
                Background = rowBackground,
                Icon = icon,
                Name = name,
                Amount = amount,
                Selection = selection
            };
        }

        private void CacheDeathLootPanelReferences(Transform root)
        {
            _deathLootTitle ??= root.Find("Title/Text")?.GetComponent<Text>();
            _deathLootFooter ??= root.Find("Footer/Text")?.GetComponent<Text>();

            if (_rowViews.Count == VisibleRows)
            {
                return;
            }

            _rowViews.Clear();
            for (int i = 0; i < VisibleRows; i++)
            {
                Transform row = root.Find($"Row_{i}");
                if (row == null)
                {
                    continue;
                }

                _rowViews.Add(new LootRowView
                {
                    Root = row as RectTransform,
                    Background = row.GetComponent<Image>(),
                    Icon = row.Find("Icon")?.GetComponent<Image>(),
                    Name = row.Find("Name")?.GetComponent<Text>(),
                    Amount = row.Find("Amount")?.GetComponent<Text>(),
                    Selection = row.Find("Selection")?.GetComponent<Image>()
                });
            }
        }

        private void DrawOpenedPanel()
        {
            if (_openedContainer == null)
            {
                return;
            }

            DrawOpenedPanel(Snapshot(_openedContainer));
        }

        private void DrawOpenedPanel(List<InventoryStack> stacks)
        {
            EnsureDeathLootPanel();
            if (_deathLootRoot == null)
            {
                return;
            }

            _deathLootRoot.gameObject.SetActive(true);
            _deathLootRoot.SetAsLastSibling();

            if (_deathLootTitle != null)
            {
                _deathLootTitle.text = _openedContainer != null
                    ? _openedContainer.DisplayName.ToUpperInvariant()
                    : "LOOT";
            }

            int firstVisible = Mathf.Clamp(
                _selectedIndex - VisibleRows + 1,
                0,
                Mathf.Max(0, stacks.Count - VisibleRows)
            );

            for (int rowIndex = 0; rowIndex < _rowViews.Count; rowIndex++)
            {
                LootRowView view = _rowViews[rowIndex];
                int stackIndex = firstVisible + rowIndex;

                if (view?.Root == null)
                {
                    continue;
                }

                if (stackIndex >= stacks.Count)
                {
                    view.Root.gameObject.SetActive(false);
                    continue;
                }

                view.Root.gameObject.SetActive(true);

                InventoryStack stack = stacks[stackIndex];
                bool selected = stackIndex == _selectedIndex;

                if (view.Name != null)
                {
                    view.Name.text = stack.item.displayName;
                }

                if (view.Amount != null)
                {
                    view.Amount.text = stack.amount > 1
                        ? $"x{stack.amount}"
                        : string.Empty;
                }

                if (view.Icon != null)
                {
                    view.Icon.sprite = stack.item.icon;
                    view.Icon.enabled = stack.item.icon != null;
                    view.Icon.color = Color.white;
                }

                if (view.Background != null)
                {
                    view.Background.color = selected
                        ? YellowSelected
                        : Yellow;
                }

                if (view.Selection != null)
                {
                    view.Selection.color = selected
                        ? new Color(0.35f, 0.12f, 0.42f, 0.18f)
                        : Color.clear;
                }
            }

            if (_deathLootFooter != null)
            {
                _deathLootFooter.text =
                    $"{_selectedIndex + 1}/{stacks.Count}  •  RUEDA  •  F RECOGER  •  ESC";
            }
        }

        private static void HideLegacyNearbyLootPanel()
        {
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform legacyPanel = hud.transform.Find("Canvas/NearbyLoot");
            if (legacyPanel != null && legacyPanel.gameObject.activeSelf)
            {
                legacyPanel.gameObject.SetActive(false);
            }
        }

        private void EnsureNearbyIndicator()
        {
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform canvas = hud.transform.Find("Canvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.Find("NearbyObjectIndicator");
            if (existing != null)
            {
                _nearbyRoot = existing as RectTransform;
                _nearbyIcon = existing.Find("Icon")?.GetComponent<Image>();
                _nearbyText = existing.Find("Text")?.GetComponent<Text>();
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject rootObject = new GameObject("NearbyObjectIndicator");
            rootObject.transform.SetParent(canvas, false);
            _nearbyRoot = rootObject.AddComponent<RectTransform>();
            _nearbyRoot.anchorMin = Vector2.one;
            _nearbyRoot.anchorMax = Vector2.one;
            _nearbyRoot.pivot = Vector2.one;
            _nearbyRoot.anchoredPosition = new Vector2(-24f, -58f);
            _nearbyRoot.sizeDelta = new Vector2(214f, 42f);

            Image background = rootObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.03f, 0.04f, 0.72f);
            background.raycastTarget = false;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(_nearbyRoot, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(6f, 0f);
            iconRect.sizeDelta = new Vector2(34f, 34f);
            _nearbyIcon = iconObject.AddComponent<Image>();
            _nearbyIcon.preserveAspect = true;
            _nearbyIcon.raycastTarget = false;

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(_nearbyRoot, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            Stretch(textRect, 45f, 3f, 5f, 3f);
            _nearbyText = textObject.AddComponent<Text>();
            _nearbyText.font = font;
            _nearbyText.fontSize = 11;
            _nearbyText.fontStyle = FontStyle.Bold;
            _nearbyText.alignment = TextAnchor.MiddleLeft;
            _nearbyText.color = Color.white;
            _nearbyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _nearbyText.verticalOverflow = VerticalWrapMode.Truncate;
            _nearbyText.raycastTarget = false;
            AddOutline(textObject, new Color(0f, 0f, 0f, 0.8f));

            _nearbyRoot.gameObject.SetActive(false);
        }

        private void UpdateNearbyIndicator()
        {
            if (_nearbyRoot == null || _localInput == null)
            {
                return;
            }

            IInteractable interactable = ResolveNearestInteractable();
            if (interactable == null)
            {
                _nearbyRoot.gameObject.SetActive(false);
                return;
            }

            string label = interactable.InteractionLabel;
            Sprite icon = ResolveIcon(interactable);

            if (IsOpen && interactable is DeathLootContainer opened &&
                opened == _openedContainer)
            {
                label = $"Caja abierta: {_openedContainer.ItemCount} objetos";
            }

            if (_nearbyText != null)
            {
                _nearbyText.text = string.IsNullOrWhiteSpace(label)
                    ? "OBJETO CERCANO"
                    : label;
            }

            if (_nearbyIcon != null)
            {
                _nearbyIcon.sprite = icon;
                _nearbyIcon.enabled = icon != null;
            }

            _nearbyRoot.gameObject.SetActive(true);
            _nearbyRoot.SetAsLastSibling();
        }

        private IInteractable ResolveNearestInteractable()
        {
            if (_interactor != null)
            {
                IInteractable current = _interactor.Current;
                if (current != null)
                {
                    return current;
                }

                IReadOnlyList<IInteractable> nearby = _interactor.Nearby;
                if (nearby != null && nearby.Count > 0 && nearby[0] != null)
                {
                    return nearby[0];
                }
            }

            return _openedContainer;
        }

        private static Sprite ResolveIcon(IInteractable interactable)
        {
            if (interactable is DeathLootContainer deathContainer)
            {
                IReadOnlyList<InventoryStack> stacks =
                    deathContainer.StoredInventory != null
                        ? deathContainer.StoredInventory.Stacks
                        : null;

                if (stacks != null)
                {
                    for (int i = 0; i < stacks.Count; i++)
                    {
                        InventoryItemDefinition item = stacks[i]?.item;
                        if (item != null && item.icon != null)
                        {
                            return item.icon;
                        }
                    }
                }

                return null;
            }

            if (interactable is not MonoBehaviour behaviour)
            {
                return null;
            }

            MonoBehaviour[] components =
                behaviour.GetComponentsInParent<MonoBehaviour>(true);

            for (int i = 0; i < components.Length; i++)
            {
                InventoryItemDefinition item = ExtractItemDefinition(components[i]);
                if (item != null && item.icon != null)
                {
                    return item.icon;
                }
            }

            return null;
        }

        private static InventoryItemDefinition ExtractItemDefinition(
            MonoBehaviour component
        )
        {
            if (component == null)
            {
                return null;
            }

            const BindingFlags flags = BindingFlags.Instance |
                                       BindingFlags.Public |
                                       BindingFlags.NonPublic;

            System.Type type = component.GetType();

            FieldInfo[] fields = type.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                if (typeof(InventoryItemDefinition).IsAssignableFrom(fields[i].FieldType))
                {
                    return fields[i].GetValue(component) as InventoryItemDefinition;
                }
            }

            PropertyInfo[] properties = type.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (!property.CanRead ||
                    property.GetIndexParameters().Length > 0 ||
                    !typeof(InventoryItemDefinition).IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }

                try
                {
                    return property.GetValue(component) as InventoryItemDefinition;
                }
                catch
                {
                    // Algunas propiedades Unity pueden lanzar durante teardown.
                }
            }

            return null;
        }

        private static void RepairEmptyContainerIfNeeded(
            DeathLootContainer container
        )
        {
            if (container == null ||
                !container.IsEmpty ||
                container.StoredInventory == null)
            {
                return;
            }

            string sourceName = container.SourcePlayerName;
            if (string.IsNullOrWhiteSpace(sourceName))
            {
                return;
            }

            GameObject source = GameObject.Find(sourceName);
            if (source == null)
            {
                return;
            }

            HashSet<WeaponDefinition> addedDefinitions =
                new HashSet<WeaponDefinition>();

            WeaponController[] weapons =
                source.GetComponentsInChildren<WeaponController>(true);

            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponController weapon = weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                WeaponDefinition definition = weapon.Definition;
                if (definition != null && addedDefinitions.Add(definition))
                {
                    InventoryItemDefinition weaponItem =
                        ScriptableObject.CreateInstance<InventoryItemDefinition>();
                    weaponItem.name = $"DeathLoot_{definition.displayName}";
                    weaponItem.itemId =
                        $"death_runtime_weapon_{definition.weaponId}_{source.GetInstanceID()}_{i}";
                    weaponItem.displayName = string.IsNullOrWhiteSpace(definition.displayName)
                        ? weapon.gameObject.name
                        : definition.displayName;
                    weaponItem.itemType = ItemType.Weapon;
                    weaponItem.maxStack = 1;
                    weaponItem.weight = 0f;
                    weaponItem.weaponDefinition = definition;
                    weaponItem.preferredWeaponSlot = Mathf.Clamp(i + 1, 1, 3);
                    weaponItem.hideFlags = HideFlags.DontSave;
                    container.StoredInventory.Add(weaponItem, 1);
                }

                int ammoAmount =
                    Mathf.Max(0, weapon.AmmoInMagazine) +
                    Mathf.Max(0, weapon.ReserveAmmo);

                if (definition != null &&
                    definition.ammoType != AmmoType.None &&
                    ammoAmount > 0)
                {
                    InventoryItemDefinition ammo =
                        ScriptableObject.CreateInstance<InventoryItemDefinition>();
                    ammo.name = $"DeathLoot_Ammo_{definition.ammoType}";
                    ammo.itemId =
                        $"death_runtime_ammo_{definition.ammoType}_{source.GetInstanceID()}_{i}";
                    ammo.displayName = $"Munición {definition.ammoType}";
                    ammo.itemType = ItemType.Ammo;
                    ammo.maxStack = Mathf.Max(1, ammoAmount);
                    ammo.weight = 0f;
                    ammo.ammoType = definition.ammoType;
                    ammo.hideFlags = HideFlags.DontSave;
                    container.StoredInventory.Add(ammo, ammoAmount);
                }
            }
        }

        private static List<InventoryStack> Snapshot(
            DeathLootContainer container
        )
        {
            List<InventoryStack> result = new List<InventoryStack>();

            if (container == null || container.StoredInventory == null)
            {
                return result;
            }

            IReadOnlyList<InventoryStack> source = container.StoredInventory.Stacks;
            for (int i = 0; i < source.Count; i++)
            {
                InventoryStack stack = source[i];
                if (stack != null && stack.item != null && stack.amount > 0)
                {
                    result.Add(stack);
                }
            }

            return result;
        }

        private static PlayerInputReader FindLocalPlayerInput()
        {
            PlayerInputReader[] inputs =
                Resources.FindObjectsOfTypeAll<PlayerInputReader>();

            Scene activeScene = SceneManager.GetActiveScene();
            PlayerInputReader fallback = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInputReader candidate = inputs[i];
                if (!IsValidLocalInput(candidate) ||
                    candidate.gameObject.scene != activeScene)
                {
                    continue;
                }

                if (candidate.gameObject.name == "Player_Prototype" ||
                    candidate.gameObject.name.StartsWith("Player_"))
                {
                    return candidate;
                }

                if (fallback == null &&
                    !candidate.gameObject.name.StartsWith("Bot_"))
                {
                    fallback = candidate;
                }
            }

            return fallback;
        }

        private static bool IsValidLocalInput(PlayerInputReader input)
        {
            return input != null &&
                   input.gameObject.scene.IsValid() &&
                   !input.UsesExternalControl;
        }

        private static void Stretch(
            RectTransform rect,
            float left,
            float top,
            float right,
            float bottom
        )
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void AddOutline(GameObject target, Color color)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }
}
