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
    /// Único propietario del panel amarillo de loot de jugador muerto del HUD ROS.
    ///
    /// Flujo definitivo:
    /// 1) PlayerInteractor detecta DeathLootContainer y muestra "Abrir...".
    /// 2) F ejecuta DeathLootContainer.Interact().
    /// 3) DeathLootContainer llama OpenOrCreate() de este presenter.
    /// 4) El panel amarillo queda abierto hasta alejarse, vaciar la caja o ESC.
    /// 5) Con el panel abierto, rueda selecciona y F recoge.
    ///
    /// También mantiene el indicador discreto de objeto cercano debajo de KILL/LEFT.
    /// </summary>
    [DefaultExecutionOrder(2600)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDNearbyLootPresenter : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const float MaximumOpenDistance = 4.5f;

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

        /// <summary>
        /// Punto único de entrada desde DeathLootContainer.Interact().
        /// </summary>
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

            // Si F se vuelve a pulsar mientras ya está abierta la misma caja,
            // NO reiniciar el frame de apertura. Así ese mismo F puede recoger
            // el objeto seleccionado en LateUpdate.
            if (_openedContainer == container && IsOpen)
            {
                return true;
            }

            _localInput = interactor.GetComponent<PlayerInputReader>();
            _interactor = interactor.GetComponent<PlayerInteractor>();
            _inventory = inventory;
            _openedContainer = container;
            _selectedIndex = 0;
            _openedFrame = Time.frameCount;

            RepairEmptyContainerIfNeeded(_openedContainer);
            DrawOpenedPanel();
            return true;
        }

        public void Close()
        {
            _openedContainer = null;
            _selectedIndex = 0;
            _openedFrame = -1;
            HideLootPanel();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.15f;
                ResolveLocalPlayer();
                EnsureNearbyIndicator();
            }

            UpdateNearbyIndicator();
            UpdateOpenedLoot();
        }

        private void ResolveLocalPlayer()
        {
            if (IsValidLocalInput(_localInput))
            {
                if (_interactor == null)
                {
                    _interactor = _localInput.GetComponent<PlayerInteractor>();
                }

                if (_inventory == null)
                {
                    _inventory = _localInput.GetComponent<InventoryComponent>();
                }

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
                // Este presenter es el dueño final del panel. Si no hay caja
                // explícitamente abierta, cualquier activación de presenters
                // anteriores se anula al final del frame.
                HideLootPanel();
                return;
            }

            if (_localInput == null || _inventory == null)
            {
                Close();
                return;
            }

            if (_openedContainer == null)
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

            // No recoger en el mismo frame en el que F abrió la caja.
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
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform panel = hud.transform.Find("Canvas/NearbyLoot");
            if (panel == null)
            {
                return;
            }

            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();

            Text title = panel.Find("Title/TitleText")?.GetComponent<Text>();
            if (title != null)
            {
                title.text = _openedContainer != null
                    ? _openedContainer.DisplayName.ToUpperInvariant()
                    : "LOOT";
            }

            const int visibleRows = 7;
            int firstVisible = Mathf.Clamp(
                _selectedIndex - visibleRows + 1,
                0,
                Mathf.Max(0, stacks.Count - visibleRows)
            );

            for (int rowIndex = 0; rowIndex < visibleRows; rowIndex++)
            {
                Text row = panel.Find($"LootRow_{rowIndex}")?.GetComponent<Text>();
                if (row == null)
                {
                    continue;
                }

                int stackIndex = firstVisible + rowIndex;
                if (stackIndex >= stacks.Count)
                {
                    row.text = string.Empty;
                    continue;
                }

                InventoryStack stack = stacks[stackIndex];
                bool selected = stackIndex == _selectedIndex;

                row.text =
                    $"{(selected ? "▶ " : "  ")}{stack.item.displayName}  x{stack.amount}";
                row.color = selected
                    ? new Color(0.15f, 0.08f, 0.20f, 1f)
                    : Color.black;
            }

            Text toggle = panel.Find("ToggleBg/ToggleHint")?.GetComponent<Text>();
            if (toggle != null)
            {
                toggle.text = "RUEDA • F RECOGER • ESC CERRAR";
            }

            Text interaction =
                hud.transform.Find("Canvas/InteractionHint")?.GetComponent<Text>();
            if (interaction != null)
            {
                interaction.text = "RUEDA: seleccionar   |   [F] recoger   |   [ESC] cerrar";
            }
        }

        private static void HideLootPanel()
        {
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            Transform panel = hud.transform.Find("Canvas/NearbyLoot");
            if (panel != null && panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(false);
            }
        }

        // -----------------------------------------------------------------
        // Indicador de objeto cercano debajo de KILL / LEFT
        // -----------------------------------------------------------------

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
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(45f, 3f);
            textRect.offsetMax = new Vector2(-5f, -3f);

            _nearbyText = textObject.AddComponent<Text>();
            _nearbyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _nearbyText.fontSize = 11;
            _nearbyText.fontStyle = FontStyle.Bold;
            _nearbyText.alignment = TextAnchor.MiddleLeft;
            _nearbyText.color = Color.white;
            _nearbyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _nearbyText.verticalOverflow = VerticalWrapMode.Truncate;
            _nearbyText.raycastTarget = false;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

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

            _nearbyText.text = string.IsNullOrWhiteSpace(label)
                ? "OBJETO CERCANO"
                : label;

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

            const BindingFlags flags =
                BindingFlags.Instance |
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
                    // Ignorar propiedades Unity no disponibles durante teardown.
                }
            }

            return null;
        }

        // -----------------------------------------------------------------
        // Respaldo para cajas antiguas que hayan quedado vacías.
        // -----------------------------------------------------------------

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

            HashSet<WeaponDefinition> represented =
                new HashSet<WeaponDefinition>();

            WeaponController[] weapons =
                source.GetComponentsInChildren<WeaponController>(true);

            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponController weapon = weapons[i];
                if (weapon == null || weapon.Definition == null)
                {
                    continue;
                }

                WeaponDefinition definition = weapon.Definition;

                if (represented.Add(definition))
                {
                    InventoryItemDefinition weaponItem =
                        ScriptableObject.CreateInstance<InventoryItemDefinition>();

                    weaponItem.name = $"DeathLoot_{definition.displayName}";
                    weaponItem.itemId =
                        $"death_weapon_{definition.weaponId}_{source.GetInstanceID()}_{i}";
                    weaponItem.displayName =
                        string.IsNullOrWhiteSpace(definition.displayName)
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

                if (definition.ammoType == AmmoType.None || ammoAmount <= 0)
                {
                    continue;
                }

                InventoryItemDefinition ammo =
                    ScriptableObject.CreateInstance<InventoryItemDefinition>();

                ammo.name = $"DeathLoot_Ammo_{definition.ammoType}";
                ammo.itemId =
                    $"death_ammo_{definition.ammoType}_{source.GetInstanceID()}_{i}";
                ammo.displayName = $"Munición {definition.ammoType}";
                ammo.itemType = ItemType.Ammo;
                ammo.maxStack = Mathf.Max(1, ammoAmount);
                ammo.weight = 0f;
                ammo.ammoType = definition.ammoType;
                ammo.hideFlags = HideFlags.DontSave;

                container.StoredInventory.Add(ammo, ammoAmount);
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

            IReadOnlyList<InventoryStack> stacks =
                container.StoredInventory.Stacks;

            for (int i = 0; i < stacks.Count; i++)
            {
                InventoryStack stack = stacks[i];
                if (stack != null &&
                    stack.item != null &&
                    stack.amount > 0)
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

            Scene scene = SceneManager.GetActiveScene();
            PlayerInputReader fallback = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInputReader candidate = inputs[i];
                if (!IsValidLocalInput(candidate) ||
                    candidate.gameObject.scene != scene)
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
    }
}
