using System.Collections.Generic;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Interaction;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// HUD runtime para la escena BattleRoyaleTest, reconstruido a partir de la
    /// referencia visual de Rules of Survival. Mantiene una resolución lógica
    /// de 1600x900 y enlaza los paneles a los sistemas reales del prototipo.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class RulesOfSurvivalHUD : MonoBehaviour
    {
        private static readonly Color Dark = new Color(0.025f, 0.035f, 0.045f, 0.84f);
        private static readonly Color DarkSoft = new Color(0.025f, 0.035f, 0.045f, 0.68f);
        private static readonly Color Yellow = new Color(0.96f, 0.86f, 0.02f, 0.96f);
        private static readonly Color Cyan = new Color(0.02f, 0.86f, 0.98f, 1f);
        private static readonly Color HealthColor = new Color(0.86f, 0.88f, 0.88f, 1f);
        private static readonly Color ArmorColor = new Color(0.28f, 0.68f, 0.94f, 1f);

        private Canvas _canvas;
        private Font _font;
        private Camera _worldCamera;
        private Camera _minimapCamera;
        private RenderTexture _minimapTexture;

        private Health _health;
        private WeaponEquipmentController _equipment;
        private BattleRoyaleManager _battleRoyale;
        private PlayerInteractor _interactor;

        private Text _compassText;
        private Text _killText;
        private Text _leftText;
        private Text _distanceText;
        private Text _playerNameText;
        private Image _healthFill;
        private Image _armorFill;
        private Text _healthValueText;
        private Text _zoneText;
        private Text _interactionText;

        private RectTransform _lootPanel;
        private readonly List<Text> _lootRows = new List<Text>();

        private readonly WeaponSlotView[] _weaponSlots = new WeaponSlotView[3];

        private RectTransform _minimapRoot;
        private Image _minimapPlayerArrow;

        private sealed class WeaponSlotView
        {
            public Image Background;
            public Text Slot;
            public Text Name;
            public Text Ammo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "07_BattleRoyaleTest")
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUD>() != null)
            {
                return;
            }

            GameObject hudObject = new GameObject("ROS_HUD_Runtime");
            hudObject.AddComponent<RulesOfSurvivalHUD>();
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ResolveReferences();
            DisableLegacyCombatCanvas();
            BuildCanvas();
            BuildCompass();
            BuildTopRightStats();
            BuildMinimap();
            BuildVitals();
            BuildLootPanel();
            BuildRightActions();
            BuildWeaponPanel();
            BuildCrosshair();
            BuildZoneBanner();
        }

        private void OnDestroy()
        {
            if (_minimapTexture != null)
            {
                _minimapTexture.Release();
                Destroy(_minimapTexture);
            }
        }

        private void Update()
        {
            ResolveReferences();
            UpdateCompass();
            UpdateBattleRoyaleStats();
            UpdateVitals();
            UpdateWeapons();
            UpdateLootPanel();
            UpdateMinimap();
            UpdateZoneBanner();
        }

        private void ResolveReferences()
        {
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            if (_equipment == null)
            {
                _equipment = FindFirstObjectByType<WeaponEquipmentController>();
            }

            if (_interactor == null)
            {
                _interactor = FindFirstObjectByType<PlayerInteractor>();
            }

            if (_battleRoyale == null)
            {
                _battleRoyale = FindFirstObjectByType<BattleRoyaleManager>();
            }

            if (_health == null)
            {
                if (_equipment != null)
                {
                    _health = _equipment.GetComponent<Health>();
                }

                if (_health == null && _interactor != null)
                {
                    _health = _interactor.GetComponent<Health>();
                }

                if (_health == null)
                {
                    _health = FindFirstObjectByType<Health>();
                }
            }
        }

        private void DisableLegacyCombatCanvas()
        {
            GameObject legacy = GameObject.Find("CombatCanvas");
            if (legacy != null)
            {
                legacy.SetActive(false);
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
        }

        private void BuildCompass()
        {
            RectTransform strip = CreatePanel(
                "CompassStrip",
                _canvas.transform,
                new Vector2(520f, 30f),
                new Vector2(0f, -18f),
                new Vector2(0.5f, 1f),
                DarkSoft
            );

            _compassText = CreateText(
                "CompassText",
                strip,
                "LEFT REAR   150  165   S   195  210  SW  240  255   W   285  300  NW  330   RIGHT REAR",
                13,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold
            );
            Stretch(_compassText.rectTransform, 6f, 2f, 6f, 2f);

            Text marker = CreateText(
                "Waypoint",
                _canvas.transform,
                "◆ 1068m ▼",
                13,
                TextAnchor.MiddleCenter,
                Yellow,
                FontStyle.Bold
            );
            SetRect(marker.rectTransform, new Vector2(110f, 24f), new Vector2(0f, -1f), new Vector2(0.5f, 1f));
        }

        private void BuildTopRightStats()
        {
            RectTransform stats = CreatePanel(
                "TopRightStats",
                _canvas.transform,
                new Vector2(205f, 39f),
                new Vector2(-15f, -7f),
                new Vector2(1f, 1f),
                Dark
            );

            _killText = CreateText("KillText", stats, "0 KILL", 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            SetRect(_killText.rectTransform, new Vector2(70f, 39f), new Vector2(-137f, 0f), new Vector2(1f, 0.5f));

            _leftText = CreateText("LeftText", stats, "-- LEFT", 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            SetRect(_leftText.rectTransform, new Vector2(72f, 39f), new Vector2(-68f, 0f), new Vector2(1f, 0.5f));

            RectTransform distancePanel = CreatePanel(
                "DistancePanel",
                stats,
                new Vector2(72f, 39f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0.5f),
                new Color(0.92f, 0.92f, 0.92f, 0.94f)
            );
            _distanceText = CreateText("DistanceText", distancePanel, "ZONE\n--m", 11, TextAnchor.MiddleCenter, Color.black, FontStyle.Bold);
            Stretch(_distanceText.rectTransform, 2f, 2f, 2f, 2f);
        }

        private void BuildMinimap()
        {
            _minimapRoot = CreatePanel(
                "MinimapFrame",
                _canvas.transform,
                new Vector2(205f, 205f),
                new Vector2(8f, 10f),
                new Vector2(0f, 0f),
                new Color(0f, 0f, 0f, 0.72f)
            );

            GameObject maskObject = new GameObject("CircleMask");
            maskObject.transform.SetParent(_minimapRoot, false);
            RectTransform maskRect = maskObject.AddComponent<RectTransform>();
            SetRect(maskRect, new Vector2(188f, 188f), Vector2.zero, new Vector2(0.5f, 0.5f));

            Image maskImage = maskObject.AddComponent<Image>();
            maskImage.sprite = CreateCircleSprite(256);
            maskImage.color = Color.white;
            Mask mask = maskObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject rawObject = new GameObject("WorldMap");
            rawObject.transform.SetParent(maskObject.transform, false);
            RectTransform rawRect = rawObject.AddComponent<RectTransform>();
            Stretch(rawRect, 0f, 0f, 0f, 0f);
            RawImage raw = rawObject.AddComponent<RawImage>();

            _minimapTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
            {
                name = "ROS_Minimap_RT"
            };
            _minimapTexture.Create();
            raw.texture = _minimapTexture;

            GameObject cameraObject = new GameObject("ROS_MinimapCamera");
            cameraObject.transform.SetParent(transform, false);
            _minimapCamera = cameraObject.AddComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = 62f;
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = new Color(0.14f, 0.20f, 0.14f, 1f);
            _minimapCamera.targetTexture = _minimapTexture;
            _minimapCamera.depth = -50f;
            _minimapCamera.nearClipPlane = 1f;
            _minimapCamera.farClipPlane = 500f;

            _minimapPlayerArrow = CreateImage(
                "PlayerArrow",
                _minimapRoot,
                new Vector2(24f, 24f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                Yellow
            );
            _minimapPlayerArrow.sprite = CreateTriangleSprite(64);

            Text mapBadge = CreateText("MapBadge", _minimapRoot, "1", 16, TextAnchor.MiddleCenter, Color.black, FontStyle.Bold);
            Image badgeBg = mapBadge.gameObject.AddComponent<Image>();
            badgeBg.color = Yellow;
            badgeBg.transform.SetAsFirstSibling();
            SetRect(mapBadge.rectTransform, new Vector2(28f, 28f), new Vector2(0f, 5f), new Vector2(0.5f, 0.5f));

            Text latency = CreateText("Latency", _canvas.transform, "64ms", 14, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
            SetRect(latency.rectTransform, new Vector2(75f, 24f), new Vector2(130f, 10f), new Vector2(0f, 0f));
        }

        private void BuildVitals()
        {
            RectTransform root = CreatePanel(
                "Vitals",
                _canvas.transform,
                new Vector2(385f, 68f),
                new Vector2(0f, 8f),
                new Vector2(0.5f, 0f),
                new Color(0.02f, 0.03f, 0.04f, 0.58f)
            );

            _playerNameText = CreateText("PlayerName", root, "PLAYER", 14, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
            SetRect(_playerNameText.rectTransform, new Vector2(155f, 22f), new Vector2(-84f, 16f), new Vector2(0.5f, 0.5f));

            RectTransform healthBack = CreatePanel("HealthBack", root, new Vector2(250f, 14f), new Vector2(0f, -8f), new Vector2(0.5f, 0.5f), new Color(0.05f, 0.05f, 0.05f, 0.9f));
            _healthFill = CreateImage("HealthFill", healthBack, new Vector2(250f, 14f), new Vector2(-125f, 0f), new Vector2(0f, 0.5f), HealthColor);
            _healthFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            RectTransform armorBack = CreatePanel("ArmorBack", root, new Vector2(250f, 5f), new Vector2(0f, -20f), new Vector2(0.5f, 0.5f), new Color(0.04f, 0.04f, 0.04f, 0.9f));
            _armorFill = CreateImage("ArmorFill", armorBack, new Vector2(250f, 5f), new Vector2(-125f, 0f), new Vector2(0f, 0.5f), ArmorColor);
            _armorFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            _healthValueText = CreateText("HealthValue", root, "100", 12, TextAnchor.MiddleRight, Color.white, FontStyle.Bold);
            SetRect(_healthValueText.rectTransform, new Vector2(55f, 20f), new Vector2(140f, -8f), new Vector2(0.5f, 0.5f));

            CreateCircularHint(root, "Med", "♡", new Vector2(-170f, 8f));
            CreateCircularHint(root, "Boost", "⚡", new Vector2(170f, 8f));

            _interactionText = CreateText("InteractionHint", _canvas.transform, string.Empty, 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            SetRect(_interactionText.rectTransform, new Vector2(420f, 32f), new Vector2(0f, 84f), new Vector2(0.5f, 0f));
        }

        private void BuildLootPanel()
        {
            _lootPanel = CreatePanel(
                "NearbyLoot",
                _canvas.transform,
                new Vector2(185f, 310f),
                new Vector2(-235f, 18f),
                new Vector2(1f, 0.5f),
                Yellow
            );

            RectTransform title = CreatePanel("Title", _lootPanel, new Vector2(185f, 32f), new Vector2(0f, 139f), new Vector2(0.5f, 0.5f), Dark);
            Text titleText = CreateText("TitleText", title, "NEARBY LOOT", 17, TextAnchor.MiddleLeft, Color.white, FontStyle.Italic);
            Stretch(titleText.rectTransform, 10f, 2f, 4f, 2f);

            for (int i = 0; i < 7; i++)
            {
                Text row = CreateText(
                    "LootRow_" + i,
                    _lootPanel,
                    string.Empty,
                    14,
                    TextAnchor.MiddleLeft,
                    Color.black,
                    FontStyle.Bold
                );
                SetRect(row.rectTransform, new Vector2(171f, 36f), new Vector2(5f, 102f - i * 38f), new Vector2(0.5f, 0.5f));
                _lootRows.Add(row);
            }

            Text toggle = CreateText("ToggleHint", _lootPanel, "↕ SCROLL TO SELECT", 11, TextAnchor.MiddleCenter, Yellow, FontStyle.Bold);
            RectTransform toggleBg = CreatePanel("ToggleBg", _lootPanel, new Vector2(145f, 24f), new Vector2(-165f, 96f), new Vector2(0f, 0.5f), Dark);
            toggle.transform.SetParent(toggleBg, false);
            Stretch(toggle.rectTransform, 2f, 2f, 2f, 2f);

            _lootPanel.gameObject.SetActive(false);
        }

        private void BuildRightActions()
        {
            RectTransform actionRoot = CreatePanel(
                "Actions",
                _canvas.transform,
                new Vector2(90f, 280f),
                new Vector2(-100f, -55f),
                new Vector2(1f, 0.5f),
                Color.clear
            );

            CreateCircularHint(actionRoot, "Fire", "●", new Vector2(0f, 95f));
            CreateCircularHint(actionRoot, "Aim", "◎", new Vector2(0f, 42f));
            CreateCircularHint(actionRoot, "Interact", "F", new Vector2(0f, -11f));
            CreateCircularHint(actionRoot, "Crouch", "C", new Vector2(0f, -64f));
            CreateCircularHint(actionRoot, "Prone", "Z", new Vector2(0f, -117f));
        }

        private void BuildWeaponPanel()
        {
            RectTransform root = CreatePanel(
                "Weapons",
                _canvas.transform,
                new Vector2(205f, 145f),
                new Vector2(-8f, 7f),
                new Vector2(1f, 0f),
                Color.clear
            );

            _weaponSlots[0] = BuildWeaponSlot(root, 1, new Vector2(0f, 94f), 48f);
            _weaponSlots[1] = BuildWeaponSlot(root, 2, new Vector2(0f, 46f), 44f);
            _weaponSlots[2] = BuildWeaponSlot(root, 3, new Vector2(0f, 2f), 40f);
        }

        private WeaponSlotView BuildWeaponSlot(RectTransform parent, int slotNumber, Vector2 pos, float height)
        {
            RectTransform panel = CreatePanel(
                "WeaponSlot_" + slotNumber,
                parent,
                new Vector2(205f, height),
                pos,
                new Vector2(0.5f, 0f),
                Dark
            );

            Text slot = CreateText("Slot", panel, slotNumber.ToString(), 12, TextAnchor.UpperLeft, Color.white, FontStyle.Bold);
            SetRect(slot.rectTransform, new Vector2(22f, height), new Vector2(-90f, 0f), new Vector2(0.5f, 0.5f));

            Text name = CreateText("WeaponName", panel, "EMPTY", 12, TextAnchor.MiddleLeft, new Color(0.86f, 0.88f, 0.9f), FontStyle.Bold);
            SetRect(name.rectTransform, new Vector2(96f, height), new Vector2(-33f, 0f), new Vector2(0.5f, 0.5f));

            Text ammo = CreateText("Ammo", panel, "--/--", 15, TextAnchor.MiddleRight, Color.white, FontStyle.Bold);
            SetRect(ammo.rectTransform, new Vector2(82f, height), new Vector2(56f, 0f), new Vector2(0.5f, 0.5f));

            return new WeaponSlotView
            {
                Background = panel.GetComponent<Image>(),
                Slot = slot,
                Name = name,
                Ammo = ammo
            };
        }

        private void BuildCrosshair()
        {
            Image dot = CreateImage("Crosshair", _canvas.transform, new Vector2(4f, 4f), Vector2.zero, new Vector2(0.5f, 0.5f), new Color(1f, 1f, 1f, 0.92f));
            dot.sprite = CreateCircleSprite(32);
        }

        private void BuildZoneBanner()
        {
            _zoneText = CreateText("ZoneBanner", _canvas.transform, string.Empty, 14, TextAnchor.MiddleCenter, Yellow, FontStyle.Bold);
            SetRect(_zoneText.rectTransform, new Vector2(360f, 28f), new Vector2(0f, -52f), new Vector2(0.5f, 1f));
        }

        private void UpdateCompass()
        {
            if (_compassText == null || _worldCamera == null)
            {
                return;
            }

            float heading = Mathf.Repeat(_worldCamera.transform.eulerAngles.y, 360f);
            string cardinal = GetCardinal(heading);
            int left30 = Mathf.RoundToInt(Mathf.Repeat(heading - 30f, 360f) / 5f) * 5;
            int left15 = Mathf.RoundToInt(Mathf.Repeat(heading - 15f, 360f) / 5f) * 5;
            int center = Mathf.RoundToInt(heading / 5f) * 5;
            int right15 = Mathf.RoundToInt(Mathf.Repeat(heading + 15f, 360f) / 5f) * 5;
            int right30 = Mathf.RoundToInt(Mathf.Repeat(heading + 30f, 360f) / 5f) * 5;

            _compassText.text = $"{left30:000}     {left15:000}     {cardinal} {center:000}     {right15:000}     {right30:000}";
        }

        private void UpdateBattleRoyaleStats()
        {
            if (_battleRoyale == null)
            {
                return;
            }

            int kills = _health != null ? _battleRoyale.GetKillCount(_health) : 0;
            _killText.text = $"{kills} KILL";
            _leftText.text = $"{_battleRoyale.AliveCount} LEFT";

            SafeZoneController zone = _battleRoyale.SafeZone;
            if (zone != null && _health != null)
            {
                Vector3 playerFlat = _health.transform.position;
                playerFlat.y = 0f;
                Vector3 centerFlat = zone.Center;
                centerFlat.y = 0f;
                float fromCenter = Vector3.Distance(playerFlat, centerFlat);
                float toBorder = Mathf.Max(0f, fromCenter - zone.Radius);
                _distanceText.text = toBorder > 0f ? $"ZONE\n{toBorder:0}m" : $"SAFE\n{zone.Radius:0}m";
            }
        }

        private void UpdateVitals()
        {
            if (_health == null)
            {
                return;
            }

            float healthNormalized = _health.MaxHealth > 0f ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth) : 0f;
            float armorNormalized = _health.MaxArmor > 0f ? Mathf.Clamp01(_health.CurrentArmor / _health.MaxArmor) : 0f;

            SetWidth(_healthFill.rectTransform, 250f * healthNormalized);
            SetWidth(_armorFill.rectTransform, 250f * armorNormalized);
            _healthValueText.text = Mathf.CeilToInt(_health.CurrentHealth).ToString();
            _playerNameText.text = _health.gameObject.name.Replace("_Prototype", string.Empty);
        }

        private void UpdateWeapons()
        {
            if (_equipment == null)
            {
                return;
            }

            RefreshWeaponSlot(_weaponSlots[0], 1, _equipment.PrimarySlot1);
            RefreshWeaponSlot(_weaponSlots[1], 2, _equipment.PrimarySlot2);
            RefreshWeaponSlot(_weaponSlots[2], 3, _equipment.SidearmSlot);
        }

        private void RefreshWeaponSlot(WeaponSlotView view, int slotNumber, WeaponController weapon)
        {
            if (view == null)
            {
                return;
            }

            bool active = _equipment != null && _equipment.EquippedSlot == slotNumber && _equipment.EquippedWeapon == weapon && weapon != null;
            view.Background.color = active ? new Color(0.08f, 0.10f, 0.12f, 0.96f) : Dark;
            view.Slot.color = active ? Yellow : Color.white;

            if (weapon == null)
            {
                view.Name.text = "EMPTY";
                view.Ammo.text = "--/--";
                view.Name.color = new Color(1f, 1f, 1f, 0.45f);
                return;
            }

            view.Name.color = Color.white;
            view.Name.text = weapon.Definition != null ? weapon.Definition.displayName.ToUpperInvariant() : weapon.name.ToUpperInvariant();
            view.Ammo.text = $"{weapon.AmmoInMagazine}/{weapon.ReserveAmmo}";
        }

        private void UpdateLootPanel()
        {
            if (_interactor == null || _lootPanel == null)
            {
                return;
            }

            IReadOnlyList<IInteractable> nearby = _interactor.Nearby;
            bool visible = nearby != null && nearby.Count > 0;
            _lootPanel.gameObject.SetActive(visible);

            if (!visible)
            {
                _interactionText.text = string.Empty;
                return;
            }

            for (int i = 0; i < _lootRows.Count; i++)
            {
                if (i < nearby.Count && nearby[i] != null)
                {
                    IInteractable item = nearby[i];
                    _lootRows[i].text = "▣  " + item.InteractionLabel;
                    _lootRows[i].color = i == 0 ? Color.black : new Color(0.08f, 0.08f, 0.08f, 0.88f);
                }
                else
                {
                    _lootRows[i].text = string.Empty;
                }
            }

            IInteractable current = _interactor.Current;
            _interactionText.text = current != null ? $"[F] {current.InteractionLabel}" : string.Empty;
        }

        private void UpdateMinimap()
        {
            if (_minimapCamera == null || _health == null)
            {
                return;
            }

            Vector3 player = _health.transform.position;
            _minimapCamera.transform.position = player + Vector3.up * 180f;
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (_minimapPlayerArrow != null)
            {
                _minimapPlayerArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -_health.transform.eulerAngles.y);
            }
        }

        private void UpdateZoneBanner()
        {
            if (_zoneText == null || _battleRoyale == null || _battleRoyale.SafeZone == null)
            {
                return;
            }

            SafeZoneController zone = _battleRoyale.SafeZone;
            if (zone.CurrentPhase < 0)
            {
                _zoneText.text = string.Empty;
                return;
            }

            int seconds = Mathf.CeilToInt(zone.PhaseTimeRemaining);
            _zoneText.text = zone.IsShrinking
                ? $"SAFE ZONE CLOSING  {seconds}s"
                : $"SAFE ZONE SHRINKS IN  {seconds}s";
        }

        private static string GetCardinal(float heading)
        {
            string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int index = Mathf.RoundToInt(heading / 45f) % names.Length;
            return names[index];
        }

        private void CreateCircularHint(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            Image circle = CreateImage(name, parent, new Vector2(42f, 42f), anchoredPosition, new Vector2(0.5f, 0.5f), new Color(0.04f, 0.05f, 0.06f, 0.78f));
            circle.sprite = CreateCircleSprite(64);
            Text text = CreateText(name + "Text", circle.transform, label, 17, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            SetRect(rect, size, position, anchor);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            SetRect(rect, size, position, anchor);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Color color, FontStyle style)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            Text text = go.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.78f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetWidth(RectTransform rect, float width)
        {
            Vector2 size = rect.sizeDelta;
            size.x = Mathf.Max(0f, width);
            rect.sizeDelta = size;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ROS_UI_Circle";
            texture.wrapMode = TextureWrapMode.Clamp;
            float radius = (size - 1) * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            Color32 clear = new Color32(255, 255, 255, 0);
            Color32 solid = new Color32(255, 255, 255, 255);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), center) <= radius ? solid : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateTriangleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ROS_UI_Triangle";
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32 clear = new Color32(255, 255, 255, 0);
            Color32 solid = new Color32(255, 255, 255, 255);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float normalizedY = y / (float)(size - 1);
                float halfWidth = normalizedY * size * 0.45f;
                float centerX = (size - 1) * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = Mathf.Abs(x - centerX) <= halfWidth ? solid : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
