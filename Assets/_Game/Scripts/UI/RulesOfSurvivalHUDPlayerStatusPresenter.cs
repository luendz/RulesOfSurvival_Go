using System.Collections.Generic;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Completa el HUD reconstruido con el bloque inferior del jugador:
    /// barra de vida/armadura y cuatro slots visuales de armas como en ROS.
    /// Los slots 1-3 reflejan WeaponEquipmentController. El slot 4 queda
    /// reservado a melee y solo muestra contenido si existe un melee poseído.
    /// </summary>
    [DefaultExecutionOrder(1400)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDPlayerStatusPresenter : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const float HealthWidth = 276f;

        private static readonly Color Dark =
            new Color(0.025f, 0.03f, 0.035f, 0.88f);

        private static readonly Color DarkSelected =
            new Color(0.10f, 0.095f, 0.035f, 0.96f);

        private static readonly Color Yellow =
            new Color(0.98f, 0.86f, 0.05f, 1f);

        private static readonly Color HealthColor =
            new Color(0.82f, 0.83f, 0.81f, 1f);

        private static readonly Color ArmorColor =
            new Color(0.22f, 0.66f, 0.92f, 1f);

        private PlayerInputReader _input;
        private Health _health;
        private InventoryComponent _inventory;
        private PlayerLootEquipment _lootEquipment;
        private WeaponEquipmentController _weaponEquipment;

        private Transform _hudCanvas;
        private RectTransform _root;
        private Text _playerName;
        private Text _healthValue;
        private Image _healthFill;
        private Image _armorFill;

        private readonly SlotView[] _slots = new SlotView[4];
        private float _nextResolveTime;

        private sealed class SlotView
        {
            public RectTransform Root;
            public Image Background;
            public Image Selection;
            public Image Icon;
            public Text Number;
            public Text Name;
            public Text Ammo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDPlayerStatusPresenter>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_PlayerStatus")
                .AddComponent<RulesOfSurvivalHUDPlayerStatusPresenter>();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.25f;
                ResolveGameplayReferences();
                ResolveHud();
            }

            if (_root == null)
            {
                return;
            }

            // Los paneles iniciales de Vitals/Weapons quedan sustituidos por este
            // bloque. Functionality puede reactivarlos antes en el frame, por eso
            // se ocultan aquí, con un execution order posterior.
            SetLegacyBlockActive("Vitals", false);
            SetLegacyBlockActive("Weapons", false);

            UpdateHealth();
            UpdateSlots();
        }

        private void ResolveGameplayReferences()
        {
            if (!IsValidLocalInput(_input))
            {
                _input = FindLocalPlayerInput();
                _health = null;
                _inventory = null;
                _lootEquipment = null;
                _weaponEquipment = null;
            }

            if (_input == null)
            {
                return;
            }

            GameObject player = _input.gameObject;
            _health ??= player.GetComponent<Health>();
            _inventory ??= player.GetComponent<InventoryComponent>();
            _lootEquipment ??= player.GetComponent<PlayerLootEquipment>();
            _weaponEquipment ??= player.GetComponent<WeaponEquipmentController>();
        }

        private void ResolveHud()
        {
            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                _hudCanvas = null;
                _root = null;
                return;
            }

            Transform canvas = hud.transform.Find("Canvas");
            if (canvas == null)
            {
                return;
            }

            if (_hudCanvas != canvas)
            {
                _hudCanvas = canvas;
                _root = null;
            }

            if (_root == null)
            {
                Transform existing = canvas.Find("PlayerStatusFidelity");
                if (existing != null)
                {
                    _root = existing as RectTransform;
                    CacheExistingUi();
                }
                else
                {
                    BuildUi();
                }
            }

            if (_root != null)
            {
                _root.SetAsLastSibling();
            }
        }

        private void BuildUi()
        {
            if (_hudCanvas == null)
            {
                return;
            }

            GameObject rootObject = new GameObject("PlayerStatusFidelity");
            rootObject.transform.SetParent(_hudCanvas, false);
            _root = rootObject.AddComponent<RectTransform>();
            Stretch(_root);

            BuildVitals();
            BuildWeaponSlots();
        }

        private void BuildVitals()
        {
            GameObject vitalsObject = new GameObject("PlayerVitals");
            vitalsObject.transform.SetParent(_root, false);
            RectTransform vitals = vitalsObject.AddComponent<RectTransform>();
            vitals.anchorMin = new Vector2(0.5f, 0f);
            vitals.anchorMax = new Vector2(0.5f, 0f);
            vitals.pivot = new Vector2(0.5f, 0f);
            vitals.anchoredPosition = new Vector2(0f, 24f);
            vitals.sizeDelta = new Vector2(410f, 72f);

            Image softBackground = vitalsObject.AddComponent<Image>();
            softBackground.color = new Color(0f, 0f, 0f, 0.18f);

            _playerName = CreateText(
                "PlayerName",
                vitals,
                "PLAYER",
                13,
                TextAnchor.MiddleLeft,
                Color.white,
                FontStyle.Bold
            );
            SetRect(_playerName.rectTransform, new Vector2(170f, 20f), new Vector2(-48f, 46f));

            GameObject healthBackObject = new GameObject("HealthBack");
            healthBackObject.transform.SetParent(vitals, false);
            RectTransform healthBack = healthBackObject.AddComponent<RectTransform>();
            healthBack.anchorMin = new Vector2(0.5f, 0f);
            healthBack.anchorMax = new Vector2(0.5f, 0f);
            healthBack.pivot = new Vector2(0.5f, 0.5f);
            healthBack.anchoredPosition = new Vector2(12f, 30f);
            healthBack.sizeDelta = new Vector2(HealthWidth, 13f);
            Image healthBackImage = healthBackObject.AddComponent<Image>();
            healthBackImage.color = new Color(0.05f, 0.055f, 0.06f, 0.92f);

            GameObject healthFillObject = new GameObject("HealthFill");
            healthFillObject.transform.SetParent(healthBack, false);
            RectTransform healthFillRect = healthFillObject.AddComponent<RectTransform>();
            healthFillRect.anchorMin = new Vector2(0f, 0.5f);
            healthFillRect.anchorMax = new Vector2(0f, 0.5f);
            healthFillRect.pivot = new Vector2(0f, 0.5f);
            healthFillRect.anchoredPosition = Vector2.zero;
            healthFillRect.sizeDelta = new Vector2(HealthWidth, 13f);
            _healthFill = healthFillObject.AddComponent<Image>();
            _healthFill.color = HealthColor;

            GameObject armorBackObject = new GameObject("ArmorBack");
            armorBackObject.transform.SetParent(vitals, false);
            RectTransform armorBack = armorBackObject.AddComponent<RectTransform>();
            armorBack.anchorMin = new Vector2(0.5f, 0f);
            armorBack.anchorMax = new Vector2(0.5f, 0f);
            armorBack.pivot = new Vector2(0.5f, 0.5f);
            armorBack.anchoredPosition = new Vector2(12f, 20f);
            armorBack.sizeDelta = new Vector2(HealthWidth, 4f);
            Image armorBackImage = armorBackObject.AddComponent<Image>();
            armorBackImage.color = new Color(0.04f, 0.05f, 0.06f, 0.88f);

            GameObject armorFillObject = new GameObject("ArmorFill");
            armorFillObject.transform.SetParent(armorBack, false);
            RectTransform armorFillRect = armorFillObject.AddComponent<RectTransform>();
            armorFillRect.anchorMin = new Vector2(0f, 0.5f);
            armorFillRect.anchorMax = new Vector2(0f, 0.5f);
            armorFillRect.pivot = new Vector2(0f, 0.5f);
            armorFillRect.anchoredPosition = Vector2.zero;
            armorFillRect.sizeDelta = new Vector2(HealthWidth, 4f);
            _armorFill = armorFillObject.AddComponent<Image>();
            _armorFill.color = ArmorColor;

            _healthValue = CreateText(
                "HealthValue",
                vitals,
                "100",
                12,
                TextAnchor.MiddleRight,
                Color.white,
                FontStyle.Bold
            );
            SetRect(_healthValue.rectTransform, new Vector2(52f, 18f), new Vector2(176f, 30f));

            Text healthIcon = CreateText(
                "HealthIcon",
                vitals,
                "♥",
                22,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold
            );
            SetRect(healthIcon.rectTransform, new Vector2(34f, 34f), new Vector2(-172f, 29f));
        }

        private void BuildWeaponSlots()
        {
            GameObject panelObject = new GameObject("WeaponSlots");
            panelObject.transform.SetParent(_root, false);
            RectTransform panel = panelObject.AddComponent<RectTransform>();
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-18f, 18f);
            panel.sizeDelta = new Vector2(222f, 118f);

            _slots[0] = CreateWeaponSlot(panel, 1, new Vector2(0f, 59f));
            _slots[3] = CreateWeaponSlot(panel, 4, new Vector2(111f, 59f));
            _slots[1] = CreateWeaponSlot(panel, 2, new Vector2(0f, 0f));
            _slots[2] = CreateWeaponSlot(panel, 3, new Vector2(111f, 0f));
        }

        private SlotView CreateWeaponSlot(
            RectTransform parent,
            int slotNumber,
            Vector2 position
        )
        {
            GameObject slotObject = new GameObject($"Slot_{slotNumber}");
            slotObject.transform.SetParent(parent, false);
            RectTransform rect = slotObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(108f, 56f);

            Image background = slotObject.AddComponent<Image>();
            background.color = Dark;

            GameObject selectionObject = new GameObject("Selection");
            selectionObject.transform.SetParent(rect, false);
            RectTransform selectionRect = selectionObject.AddComponent<RectTransform>();
            selectionRect.anchorMin = new Vector2(0f, 0f);
            selectionRect.anchorMax = new Vector2(0f, 1f);
            selectionRect.pivot = new Vector2(0f, 0.5f);
            selectionRect.anchoredPosition = Vector2.zero;
            selectionRect.sizeDelta = new Vector2(3f, 0f);
            Image selection = selectionObject.AddComponent<Image>();
            selection.color = Color.clear;

            Text number = CreateText(
                "Number",
                rect,
                slotNumber.ToString(),
                10,
                TextAnchor.UpperLeft,
                new Color(1f, 1f, 1f, 0.88f),
                FontStyle.Bold
            );
            SetRect(number.rectTransform, new Vector2(18f, 18f), new Vector2(11f, 45f));

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(rect, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-10f, 4f);
            iconRect.sizeDelta = new Vector2(58f, 30f);
            Image icon = iconObject.AddComponent<Image>();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.enabled = false;

            Text name = CreateText(
                "Name",
                rect,
                "—",
                10,
                TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.72f),
                FontStyle.Bold
            );
            SetRect(name.rectTransform, new Vector2(72f, 20f), new Vector2(49f, 31f));

            Text ammo = CreateText(
                "Ammo",
                rect,
                string.Empty,
                12,
                TextAnchor.LowerRight,
                Color.white,
                FontStyle.Bold
            );
            SetRect(ammo.rectTransform, new Vector2(70f, 20f), new Vector2(70f, 10f));

            return new SlotView
            {
                Root = rect,
                Background = background,
                Selection = selection,
                Icon = icon,
                Number = number,
                Name = name,
                Ammo = ammo
            };
        }

        private void UpdateHealth()
        {
            if (_health == null)
            {
                return;
            }

            float health01 = _health.MaxHealth > 0f
                ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth)
                : 0f;

            float armor01 = _health.MaxArmor > 0f
                ? Mathf.Clamp01(_health.CurrentArmor / _health.MaxArmor)
                : 0f;

            SetWidth(_healthFill, HealthWidth * health01);
            SetWidth(_armorFill, HealthWidth * armor01);

            if (_healthValue != null)
            {
                _healthValue.text = Mathf.CeilToInt(_health.CurrentHealth).ToString();
            }

            if (_playerName != null)
            {
                _playerName.text = _health.gameObject.name
                    .Replace("_Prototype", string.Empty)
                    .Replace("Player_", string.Empty);
            }
        }

        private void UpdateSlots()
        {
            if (_weaponEquipment == null)
            {
                return;
            }

            UpdateFirearmSlot(1, _weaponEquipment.PrimarySlot1);
            UpdateFirearmSlot(2, _weaponEquipment.PrimarySlot2);
            UpdateFirearmSlot(3, _weaponEquipment.SidearmSlot);
            UpdateMeleeSlot();
        }

        private void UpdateFirearmSlot(int slot, WeaponController weapon)
        {
            SlotView view = _slots[slot - 1];
            if (view == null)
            {
                return;
            }

            InventoryItemDefinition item = _lootEquipment?.GetWeaponItem(slot);
            bool selected = weapon != null &&
                            _weaponEquipment.EquippedSlot == slot &&
                            _weaponEquipment.EquippedWeapon == weapon;

            ApplySlotSelection(view, selected);

            if (weapon == null)
            {
                SetEmptySlot(view);
                return;
            }

            string name = item != null
                ? item.displayName
                : weapon.Definition != null
                    ? weapon.Definition.displayName
                    : weapon.name;

            Sprite icon = item != null ? item.icon : null;
            SetSlotContent(
                view,
                name,
                icon,
                $"{weapon.AmmoInMagazine}/{weapon.ReserveAmmo}"
            );
        }

        private void UpdateMeleeSlot()
        {
            SlotView view = _slots[3];
            if (view == null)
            {
                return;
            }

            InventoryItemDefinition melee = FindOwnedMeleeItem();
            ApplySlotSelection(view, false);

            if (melee == null)
            {
                SetEmptySlot(view);
                return;
            }

            SetSlotContent(view, melee.displayName, melee.icon, "∞");
        }

        private InventoryItemDefinition FindOwnedMeleeItem()
        {
            if (_inventory == null)
            {
                return null;
            }

            IReadOnlyList<InventoryStack> stacks = _inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                InventoryStack stack = stacks[i];
                if (stack == null || stack.item == null || stack.amount <= 0)
                {
                    continue;
                }

                InventoryItemDefinition item = stack.item;
                if (item.itemType == ItemType.Weapon &&
                    item.weaponDefinition != null &&
                    item.weaponDefinition.family == WeaponFamily.Melee)
                {
                    return item;
                }
            }

            return null;
        }

        private void SetSlotContent(
            SlotView view,
            string displayName,
            Sprite icon,
            string ammo
        )
        {
            if (view.Icon != null)
            {
                view.Icon.sprite = icon;
                view.Icon.enabled = icon != null;
            }

            if (view.Name != null)
            {
                view.Name.text = string.IsNullOrWhiteSpace(displayName)
                    ? "ARMA"
                    : displayName.ToUpperInvariant();
                view.Name.gameObject.SetActive(icon == null);
            }

            if (view.Ammo != null)
            {
                view.Ammo.text = ammo;
            }
        }

        private static void SetEmptySlot(SlotView view)
        {
            if (view.Icon != null)
            {
                view.Icon.sprite = null;
                view.Icon.enabled = false;
            }

            if (view.Name != null)
            {
                view.Name.gameObject.SetActive(true);
                view.Name.text = "—";
            }

            if (view.Ammo != null)
            {
                view.Ammo.text = string.Empty;
            }
        }

        private static void ApplySlotSelection(SlotView view, bool selected)
        {
            if (view.Background != null)
            {
                view.Background.color = selected ? DarkSelected : Dark;
            }

            if (view.Selection != null)
            {
                view.Selection.color = selected ? Yellow : Color.clear;
            }

            if (view.Number != null)
            {
                view.Number.color = selected
                    ? Yellow
                    : new Color(1f, 1f, 1f, 0.88f);
            }
        }

        private void CacheExistingUi()
        {
            if (_root == null)
            {
                return;
            }

            _playerName = FindText(_root, "PlayerVitals/PlayerName");
            _healthValue = FindText(_root, "PlayerVitals/HealthValue");
            _healthFill = FindImage(_root, "PlayerVitals/HealthBack/HealthFill");
            _armorFill = FindImage(_root, "PlayerVitals/ArmorBack/ArmorFill");

            for (int i = 1; i <= 4; i++)
            {
                Transform slotRoot = _root.Find($"WeaponSlots/Slot_{i}");
                if (slotRoot == null)
                {
                    continue;
                }

                _slots[i - 1] = new SlotView
                {
                    Root = slotRoot as RectTransform,
                    Background = slotRoot.GetComponent<Image>(),
                    Selection = FindImage(slotRoot, "Selection"),
                    Icon = FindImage(slotRoot, "Icon"),
                    Number = FindText(slotRoot, "Number"),
                    Name = FindText(slotRoot, "Name"),
                    Ammo = FindText(slotRoot, "Ammo")
                };
            }
        }

        private void SetLegacyBlockActive(string childName, bool active)
        {
            if (_hudCanvas == null)
            {
                return;
            }

            Transform child = _hudCanvas.Find(childName);
            if (child != null && child.gameObject.activeSelf != active)
            {
                child.gameObject.SetActive(active);
            }
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

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle fontStyle
        )
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.78f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetWidth(Image image, float width)
        {
            if (image == null)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            Vector2 size = rect.sizeDelta;
            size.x = Mathf.Max(0f, width);
            rect.sizeDelta = size;
        }

        private static Text FindText(Transform root, string path)
        {
            Transform found = root.Find(path);
            return found != null ? found.GetComponent<Text>() : null;
        }

        private static Image FindImage(Transform root, string path)
        {
            Transform found = root.Find(path);
            return found != null ? found.GetComponent<Image>() : null;
        }
    }
}
