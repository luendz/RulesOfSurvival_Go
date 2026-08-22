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
    /// Presentador final del loot cercano del HUD ROS.
    /// Se ejecuta después del resto de presentadores para mantener visible
    /// el loot de una caja de muerte cuando el jugador está cerca.
    /// También muestra debajo de KILL/LEFT el objeto cercano y su icono.
    /// </summary>
    [DefaultExecutionOrder(2600)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDNearbyLootPresenter : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const float DeathLootDistance = 5.5f;

        private PlayerInputReader _localInput;
        private PlayerInteractor _interactor;
        private InventoryComponent _inventory;
        private DeathLootContainer _activeDeathContainer;
        private int _selectedIndex;
        private float _nextResolveTime;

        private RectTransform _nearbyRoot;
        private Image _nearbyIcon;
        private Text _nearbyText;

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

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.15f;
                ResolveLocalPlayer();
                EnsureNearbyIndicator();
            }

            UpdateNearbyIndicator();
            UpdateDeathLootPanel();
        }

        private void ResolveLocalPlayer()
        {
            if (IsValidLocalInput(_localInput))
            {
                return;
            }

            _localInput = FindLocalPlayerInput();
            _interactor = null;
            _inventory = null;
            _activeDeathContainer = null;
            _selectedIndex = 0;

            if (_localInput == null)
            {
                return;
            }

            _interactor = _localInput.GetComponent<PlayerInteractor>();
            _inventory = _localInput.GetComponent<InventoryComponent>();
        }

        private void UpdateDeathLootPanel()
        {
            if (_localInput == null || _inventory == null)
            {
                return;
            }

            DeathLootContainer nearest = FindNearestDeathContainer();
            if (nearest != _activeDeathContainer)
            {
                _activeDeathContainer = nearest;
                _selectedIndex = 0;
            }

            if (_activeDeathContainer == null)
            {
                return;
            }

            if (_activeDeathContainer.IsEmpty)
            {
                RepairEmptyDeathContainer(_activeDeathContainer);
            }

            List<InventoryStack> stacks = Snapshot(_activeDeathContainer);
            DrawDeathLoot(stacks);

            if (stacks.Count == 0)
            {
                return;
            }

            HandleSelection(stacks.Count);
            HandlePickup(stacks);
        }

        private DeathLootContainer FindNearestDeathContainer()
        {
            if (_localInput == null)
            {
                return null;
            }

            Scene scene = SceneManager.GetActiveScene();
            DeathLootContainer[] containers =
                Resources.FindObjectsOfTypeAll<DeathLootContainer>();

            Vector3 playerPosition = _localInput.transform.position;
            float nearestSqr = DeathLootDistance * DeathLootDistance;
            DeathLootContainer nearest = null;

            for (int i = 0; i < containers.Length; i++)
            {
                DeathLootContainer candidate = containers[i];
                if (candidate == null ||
                    candidate.gameObject.scene != scene ||
                    !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 delta = candidate.transform.position - playerPosition;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr > nearestSqr)
                {
                    continue;
                }

                nearestSqr = sqr;
                nearest = candidate;
            }

            return nearest;
        }

        private static void RepairEmptyDeathContainer(DeathLootContainer container)
        {
            if (container == null || !container.IsEmpty || container.StoredInventory == null)
            {
                return;
            }

            string sourceName = container.SourcePlayerName;
            GameObject source = string.IsNullOrWhiteSpace(sourceName)
                ? null
                : GameObject.Find(sourceName);

            if (source != null)
            {
                HashSet<WeaponDefinition> added = new HashSet<WeaponDefinition>();
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
                    if (definition != null && added.Add(definition))
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

                    int ammoAmount = Mathf.Max(0, weapon.AmmoInMagazine) +
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

            if (!container.IsEmpty)
            {
                return;
            }

            InventoryItemDefinition fallback =
                ScriptableObject.CreateInstance<InventoryItemDefinition>();
            fallback.name = "DeathLoot_Suministros";
            fallback.itemId = $"death_runtime_supplies_{container.GetInstanceID()}";
            fallback.displayName = "Suministros del jugador";
            fallback.itemType = ItemType.Misc;
            fallback.maxStack = 1;
            fallback.weight = 0f;
            fallback.hideFlags = HideFlags.DontSave;
            container.StoredInventory.Add(fallback, 1);
        }

        private static List<InventoryStack> Snapshot(DeathLootContainer container)
        {
            List<InventoryStack> result = new List<InventoryStack>();
            if (container == null || container.StoredInventory == null)
            {
                return result;
            }

            IReadOnlyList<InventoryStack> stacks = container.StoredInventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                InventoryStack stack = stacks[i];
                if (stack != null && stack.item != null && stack.amount > 0)
                {
                    result.Add(stack);
                }
            }

            return result;
        }

        private void DrawDeathLoot(List<InventoryStack> stacks)
        {
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                return;
            }

            RectTransform panel =
                hud.transform.Find("Canvas/NearbyLoot") as RectTransform;
            if (panel == null)
            {
                return;
            }

            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();

            Text title = panel.Find("Title/TitleText")?.GetComponent<Text>();
            if (title != null)
            {
                title.text = _activeDeathContainer.DisplayName.ToUpperInvariant();
            }

            const int visibleRows = 7;
            _selectedIndex = Mathf.Clamp(
                _selectedIndex,
                0,
                Mathf.Max(0, stacks.Count - 1)
            );

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
                    row.text = rowIndex == 0 && stacks.Count == 0
                        ? "SIN OBJETOS"
                        : string.Empty;
                    row.color = Color.black;
                    continue;
                }

                InventoryStack stack = stacks[stackIndex];
                bool selected = stackIndex == _selectedIndex;
                row.text = $"{(selected ? "▶ " : "  ")}{stack.item.displayName}  x{stack.amount}";
                row.color = selected
                    ? new Color(0.15f, 0.08f, 0.20f, 1f)
                    : Color.black;
            }

            Text toggle = panel.Find("ToggleBg/ToggleHint")?.GetComponent<Text>();
            if (toggle != null)
            {
                toggle.text = stacks.Count > 0
                    ? "SCROLL • F RECOGER"
                    : "SIN OBJETOS";
            }
        }

        private void HandleSelection(int count)
        {
            if (count <= 0 || Mouse.current == null)
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
            if (selected?.item == null)
            {
                return;
            }

            _activeDeathContainer.TryLoot(
                selected.item,
                selected.amount,
                _inventory
            );
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

            GameObject root = new GameObject("NearbyObjectIndicator");
            root.transform.SetParent(canvas, false);

            _nearbyRoot = root.AddComponent<RectTransform>();
            _nearbyRoot.anchorMin = Vector2.one;
            _nearbyRoot.anchorMax = Vector2.one;
            _nearbyRoot.pivot = Vector2.one;
            _nearbyRoot.anchoredPosition = new Vector2(-24f, -60f);
            _nearbyRoot.sizeDelta = new Vector2(214f, 42f);

            Image background = root.AddComponent<Image>();
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

            _nearbyText.text = string.IsNullOrWhiteSpace(interactable.InteractionLabel)
                ? "OBJETO CERCANO"
                : interactable.InteractionLabel;

            Sprite icon = ResolveIcon(interactable);
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
                if (_interactor.Current != null)
                {
                    return _interactor.Current;
                }

                IReadOnlyList<IInteractable> nearby = _interactor.Nearby;
                if (nearby != null && nearby.Count > 0 && nearby[0] != null)
                {
                    return nearby[0];
                }
            }

            return FindNearestDeathContainer();
        }

        private static Sprite ResolveIcon(IInteractable interactable)
        {
            if (interactable is DeathLootContainer death)
            {
                if (death.StoredInventory != null)
                {
                    IReadOnlyList<InventoryStack> stacks = death.StoredInventory.Stacks;
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

        private static InventoryItemDefinition ExtractItemDefinition(MonoBehaviour component)
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

        private static PlayerInputReader FindLocalPlayerInput()
        {
            PlayerInputReader[] inputs =
                Resources.FindObjectsOfTypeAll<PlayerInputReader>();
            Scene scene = SceneManager.GetActiveScene();
            PlayerInputReader fallback = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInputReader candidate = inputs[i];
                if (!IsValidLocalInput(candidate) || candidate.gameObject.scene != scene)
                {
                    continue;
                }

                if (candidate.gameObject.name == "Player_Prototype" ||
                    candidate.gameObject.name.StartsWith("Player_"))
                {
                    return candidate;
                }

                if (fallback == null && !candidate.gameObject.name.StartsWith("Bot_"))
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
