using System.Collections.Generic;
using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Combat;
using ROS.Game.Input;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Reconstrucción funcional del HUD de referencia de Rules of Survival.
    /// Se construye completamente en runtime sobre un Canvas 1920x1080 y
    /// reutiliza los sistemas actuales del proyecto: vida, kills, vivos,
    /// armas/munición, brújula, loot cercano y minimapa.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RosReferenceHud : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float NearbyLootRadius = 4.5f;
        private const int MaxLootRows = 8;

        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Health health;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private BattleRoyaleManager battleRoyale;
        [SerializeField] private ThirdPersonCamera thirdPersonCamera;

        [Header("HUD")]
        [SerializeField] private bool hideLegacyRuntimeHud = true;
        [SerializeField] private string fallbackPlayerName = "PLAYER";
        [SerializeField] private int fallbackPingMs = 64;
        [SerializeField] private int supplyDistanceMeters = 3098;
        [SerializeField] private int markerDistanceMeters = 1068;

        [Header("Minimap")]
        [SerializeField] private float minimapWorldSize = 58f;
        [SerializeField] private float minimapCameraHeight = 70f;

        private Canvas _canvas;
        private Font _font;
        private Text _killsText;
        private Text _aliveText;
        private Text _supplyText;
        private Text _markerDistanceText;
        private Text _playerNameText;
        private Text _pingText;
        private Image _healthFill;
        private Image _armorFill;
        private Text _weaponSlot1Ammo;
        private Text _weaponSlot2Ammo;
        private Text _weaponSlot3Ammo;
        private Text[] _lootRows;
        private RectTransform _compassTicksRoot;
        private Text _headingText;
        private Camera _minimapCamera;
        private RenderTexture _minimapTexture;
        private RawImage _minimapImage;
        private RectTransform _minimapPlayerArrow;
        private float _nextLootRefresh;

        private static readonly Color Dark = new Color(0.035f, 0.045f, 0.055f, 0.92f);
        private static readonly Color DarkSoft = new Color(0.06f, 0.075f, 0.085f, 0.82f);
        private static readonly Color SlotGrey = new Color(0.24f, 0.27f, 0.29f, 0.90f);
        private static readonly Color White = new Color(1f, 1f, 1f, 0.97f);
        private static readonly Color MutedWhite = new Color(0.88f, 0.90f, 0.91f, 0.90f);
        private static readonly Color Yellow = new Color(1f, 0.90f, 0f, 0.98f);
        private static readonly Color Cyan = new Color(0.08f, 0.86f, 1f, 1f);
        private static readonly Color HealthGreen = new Color(0.83f, 0.91f, 0.82f, 0.98f);
        private static readonly Color ArmorBlue = new Color(0.12f, 0.64f, 0.86f, 0.95f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttach()
        {
            if (FindFirstObjectByType<RosReferenceHud>() != null)
                return;

            PlayerInputReader playerInput = BattleRoyaleBotController.FindLocalPlayerInput();
            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInputReader>();

            BattleRoyaleManager manager = FindFirstObjectByType<BattleRoyaleManager>();
            if (playerInput == null || manager == null)
                return;

            RosReferenceHud hud = playerInput.GetComponent<RosReferenceHud>();
            if (hud == null)
                hud = playerInput.gameObject.AddComponent<RosReferenceHud>();
        }

        private void Awake()
        {
            ResolveReferences();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildHud();
            BuildMinimapCamera();
            if (hideLegacyRuntimeHud)
                DisableLegacyHudObjects();
        }

        private void OnDestroy()
        {
            if (_minimapCamera != null)
                Destroy(_minimapCamera.gameObject);

            if (_minimapTexture != null)
            {
                _minimapTexture.Release();
                Destroy(_minimapTexture);
            }

            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }

        private void Update()
        {
            ResolveReferences();
            RefreshVitalsAndMatch();
            RefreshWeapons();
            RefreshCompass();
            RefreshMinimap();

            if (Time.unscaledTime >= _nextLootRefresh)
            {
                _nextLootRefresh = Time.unscaledTime + 0.2f;
                RefreshNearbyLoot();
            }
        }

        private void ResolveReferences()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>();
            if (health == null)
                health = GetComponent<Health>();
            if (equipment == null)
                equipment = GetComponent<WeaponEquipmentController>();
            if (battleRoyale == null)
                battleRoyale = FindFirstObjectByType<BattleRoyaleManager>();
            if (thirdPersonCamera == null)
                thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        }

        private void BuildHud()
        {
            GameObject root = new GameObject("ROS_Reference_HUD");
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            BuildCompass(root.transform);
            BuildTopRightStatus(root.transform);
            BuildLootPanel(root.transform);
            BuildBottomLeft(root.transform);
            BuildBottomCenter(root.transform);
            BuildBottomRight(root.transform);
            BuildCenterDot(root.transform);
        }

        private void BuildCompass(Transform parent)
        {
            RectTransform bar = Panel("CompassBar", parent, DarkSoft,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(760f, 34f), new Vector2(0f, -15f));

            _compassTicksRoot = Rect("Ticks", bar, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(620f, 34f), Vector2.zero);

            for (int i = -5; i <= 5; i++)
            {
                Text t = Label("Tick", _compassTicksRoot, "", 15, White, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(58f, 32f), new Vector2(i * 56f, 0f));
                t.fontStyle = i == 0 ? FontStyle.Bold : FontStyle.Normal;
            }

            Label("LeftRear", bar, "LEFT REAR", 12, White, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(82f, 26f), new Vector2(-48f, 0f), Dark);
            Label("RightRear", bar, "RIGHT REAR", 12, White, TextAnchor.MiddleCenter,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(92f, 26f), new Vector2(52f, 0f), Dark);

            _headingText = Label("Heading", bar, "W 285", 16, White, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110f, 30f), Vector2.zero);
            _headingText.fontStyle = FontStyle.Bold;

            RectTransform marker = Rect("Marker", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(100f, 28f), new Vector2(-62f, -2f));
            Label("Pin", marker, "◆", 21, Cyan, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 28f), new Vector2(14f, 0f));
            _markerDistanceText = Label("Distance", marker, markerDistanceMeters + "m", 15, Yellow,
                TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(68f, 28f), new Vector2(58f, 0f));
            _markerDistanceText.fontStyle = FontStyle.Bold;
        }

        private void BuildTopRightStatus(Transform parent)
        {
            RectTransform panel = Panel("MatchStatus", parent, Dark,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(250f, 48f), new Vector2(-10f, -8f));

            _killsText = Label("Kills", panel, "0 KILL", 15, White, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(78f, 48f), new Vector2(39f, -24f));
            _aliveText = Label("Alive", panel, "-- LEFT", 15, White, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(80f, 48f), new Vector2(118f, -24f));
            _supplyText = Label("Supply", panel, "37 Sup.\n3098m Dist.", 12, Color.black, TextAnchor.MiddleCenter,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(92f, 48f), new Vector2(-46f, -24f),
                new Color(0.90f, 0.90f, 0.88f, 0.98f));

            _killsText.fontStyle = FontStyle.Bold;
            _aliveText.fontStyle = FontStyle.Bold;
            _supplyText.fontStyle = FontStyle.Bold;
        }

        private void BuildLootPanel(Transform parent)
        {
            RectTransform panel = Panel("LootPanel", parent, Yellow,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(205f, 500f), new Vector2(-230f, 25f));

            RectTransform header = Panel("Header", panel, Dark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 34f), new Vector2(0f, -17f));
            Text title = Label("Title", header, "Pirate Treasure", 18, White, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            title.rectTransform.offsetMin = new Vector2(10f, 0f);
            title.fontStyle = FontStyle.Italic;

            _lootRows = new Text[MaxLootRows];
            for (int i = 0; i < MaxLootRows; i++)
            {
                float y = -51f - i * 56f;
                RectTransform row = Panel("LootRow_" + i, panel,
                    new Color(1f, 0.91f, 0f, i % 2 == 0 ? 0.94f : 0.82f),
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 54f), new Vector2(0f, y));
                Label("Icon", row, "◆", 20, new Color(0.10f, 0.10f, 0.10f, 1f), TextAnchor.MiddleCenter,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 54f), new Vector2(20f, 0f));
                _lootRows[i] = Label("Text", row, "", 14, Color.black, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                _lootRows[i].rectTransform.offsetMin = new Vector2(44f, 2f);
                _lootRows[i].rectTransform.offsetMax = new Vector2(-5f, -2f);
                _lootRows[i].fontStyle = FontStyle.Bold;
            }

            RectTransform scroll = Panel("ScrollToSelect", parent, Dark,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(30f, 158f), new Vector2(-435f, 102f));
            Text scrollText = Label("ScrollText", scroll, "SCROLL TO SELECT", 11, Yellow, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrollText.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);

            RectTransform fold = Panel("Fold", parent, DarkSoft,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(56f, 130f), new Vector2(-160f, 20f));
            Text foldText = Label("FoldText", fold, "FOLD", 14, White, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            foldText.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
            CircleButton(parent, new Vector2(1f, 0.5f), new Vector2(-184f, 80f), "●", 34f);
            CircleButton(parent, new Vector2(1f, 0.5f), new Vector2(-184f, 25f), "◆", 42f);
        }

        private void BuildBottomLeft(Transform parent)
        {
            RectTransform minimapFrame = Rect("MinimapFrame", parent,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(214f, 214f), new Vector2(118f, 118f));

            GameObject maskGo = new GameObject("CircularMask");
            maskGo.transform.SetParent(minimapFrame, false);
            RectTransform maskRect = maskGo.AddComponent<RectTransform>();
            Stretch(maskRect);
            Image maskImage = maskGo.AddComponent<Image>();
            maskImage.sprite = CreateCircleSprite(256);
            maskImage.color = Color.white;
            Mask mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject rawGo = new GameObject("MinimapImage");
            rawGo.transform.SetParent(maskGo.transform, false);
            _minimapImage = rawGo.AddComponent<RawImage>();
            RectTransform rawRect = rawGo.GetComponent<RectTransform>();
            Stretch(rawRect);

            Image border = minimapFrame.gameObject.AddComponent<Image>();
            border.sprite = CreateRingSprite(256, 7);
            border.color = new Color(0.86f, 0.88f, 0.86f, 0.95f);
            border.raycastTarget = false;

            _minimapPlayerArrow = Rect("PlayerArrow", minimapFrame,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 26f), Vector2.zero);
            Image arrowImage = _minimapPlayerArrow.gameObject.AddComponent<Image>();
            arrowImage.sprite = CreateTriangleSprite(64);
            arrowImage.color = Yellow;

            Label("MapM", parent, "M", 15, White, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(16f, 64f), DarkSoft);
            Label("Settings", parent, "⚙", 18, White, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 32f), new Vector2(18f, 28f), DarkSoft);

            _pingText = Label("Ping", parent, fallbackPingMs + "ms", 14, White, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(72f, 28f), new Vector2(185f, 18f), DarkSoft);

            CircleButton(parent, new Vector2(0f, 0f), new Vector2(250f, 48f), "TAB", 46f);

            RectTransform badge = Panel("PlayerBadge", parent, new Color(0.02f, 0.12f, 0.18f, 0.90f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(178f, 68f), new Vector2(360f, 40f));
            Label("Avatar", badge, "◉", 40, Cyan, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(62f, 62f), new Vector2(32f, 0f));
            Label("Tag", badge, "GAP", 13, White, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(0f, 10f), Cyan);
        }

        private void BuildBottomCenter(Transform parent)
        {
            RectTransform center = Rect("BottomCenter", parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(520f, 112f), new Vector2(0f, 56f));

            _playerNameText = Label("PlayerName", center, fallbackPlayerName, 14, White, TextAnchor.MiddleLeft,
                new Vector2(0.18f, 0.44f), new Vector2(0.58f, 0.72f), Vector2.zero, Vector2.zero);
            _playerNameText.fontStyle = FontStyle.Bold;

            RectTransform healthBg = Panel("HealthBg", center, new Color(0.08f, 0.09f, 0.09f, 0.92f),
                new Vector2(0.18f, 0.28f), new Vector2(0.72f, 0.44f), Vector2.zero, Vector2.zero);
            _healthFill = FillImage("HealthFill", healthBg, HealthGreen);

            RectTransform armorBg = Panel("ArmorBg", center, new Color(0.05f, 0.07f, 0.08f, 0.85f),
                new Vector2(0.18f, 0.20f), new Vector2(0.72f, 0.26f), Vector2.zero, Vector2.zero);
            _armorFill = FillImage("ArmorFill", armorBg, ArmorBlue);

            string[] utilityIcons = { "♥", "⚔", "◈", "✹" };
            for (int i = 0; i < utilityIcons.Length; i++)
                CircleButton(center, new Vector2(0.55f + i * 0.085f, 0.62f), Vector2.zero, utilityIcons[i], 34f);

            CircleButton(center, new Vector2(0.08f, 0.35f), Vector2.zero, "G\n6", 48f);
            CircleButton(center, new Vector2(0.84f, 0.35f), Vector2.zero, "G\n3", 48f);
            Label("ArrowLeft", center, "⌃", 20, White, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.82f), new Vector2(0.08f, 0.82f), new Vector2(42f, 28f), Vector2.zero, DarkSoft);
            Label("ArrowRight", center, "⌃", 20, White, TextAnchor.MiddleCenter,
                new Vector2(0.84f, 0.82f), new Vector2(0.84f, 0.82f), new Vector2(42f, 28f), Vector2.zero, DarkSoft);
        }

        private void BuildBottomRight(Transform parent)
        {
            RectTransform controls = Rect("Controls", parent,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(260f, 235f), new Vector2(-136f, 122f));

            Label("Shift", controls, "SHIFT+F1", 12, White, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(92f, 25f), new Vector2(0f, -8f), DarkSoft);
            string[] keys = { "ENTER", "CTRL+T", "CAPS" };
            for (int i = 0; i < keys.Length; i++)
                CircleButton(controls, new Vector2(0.28f + i * 0.27f, 0.78f), Vector2.zero, keys[i], 42f);

            RectTransform slots = Panel("WeaponSlots", controls, new Color(0.12f, 0.14f, 0.15f, 0.82f),
                new Vector2(0f, 0f), new Vector2(1f, 0.70f), Vector2.zero, Vector2.zero);

            _weaponSlot1Ammo = BuildWeaponRow(slots, 0, "1", equipment != null ? equipment.PrimarySlot1 : null);
            _weaponSlot2Ammo = BuildWeaponRow(slots, 1, "2", equipment != null ? equipment.PrimarySlot2 : null);
            _weaponSlot3Ammo = BuildWeaponRow(slots, 2, "3", equipment != null ? equipment.SidearmSlot : null);
        }

        private Text BuildWeaponRow(RectTransform parent, int index, string slotNumber, WeaponController weapon)
        {
            float rowHeight = 0.3333f;
            RectTransform row = Panel("WeaponRow_" + slotNumber, parent, index % 2 == 0 ? SlotGrey : new Color(0.18f, 0.21f, 0.23f, 0.92f),
                new Vector2(0f, 1f - rowHeight * (index + 1)), new Vector2(1f, 1f - rowHeight * index), Vector2.zero, Vector2.zero);

            Label("Slot", row, slotNumber, 13, White, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).rectTransform.offsetMin = new Vector2(5f, 0f);

            Text silhouette = Label("Silhouette", row, WeaponSilhouette(weapon), 22, White, TextAnchor.MiddleCenter,
                new Vector2(0.20f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero);
            silhouette.fontStyle = FontStyle.Bold;

            Text ammo = Label("Ammo", row, AmmoText(weapon), 15, White, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0f), new Vector2(0.32f, 0.52f), Vector2.zero, Vector2.zero);
            ammo.fontStyle = FontStyle.Bold;
            return ammo;
        }

        private void BuildCenterDot(Transform parent)
        {
            RectTransform dot = Rect("AimDot", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(3f, 3f), Vector2.zero);
            Image image = dot.gameObject.AddComponent<Image>();
            image.color = White;
        }

        private void RefreshVitalsAndMatch()
        {
            if (_healthFill != null)
            {
                float ratio = health != null && health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;
                _healthFill.fillAmount = Mathf.Clamp01(ratio);
            }

            if (_armorFill != null)
            {
                float ratio = health != null && health.MaxArmor > 0f ? health.CurrentArmor / health.MaxArmor : 0f;
                _armorFill.fillAmount = Mathf.Clamp01(ratio);
            }

            if (_playerNameText != null)
                _playerNameText.text = health != null ? health.gameObject.name : fallbackPlayerName;

            if (_killsText != null)
            {
                int kills = battleRoyale != null && health != null ? battleRoyale.GetKillCount(health) : 0;
                _killsText.text = kills + " KILL";
            }

            if (_aliveText != null)
                _aliveText.text = (battleRoyale != null ? battleRoyale.AliveCount.ToString() : "--") + " LEFT";

            if (_supplyText != null)
                _supplyText.text = "37 Sup.\n" + supplyDistanceMeters + "m Dist.";

            if (_markerDistanceText != null)
                _markerDistanceText.text = markerDistanceMeters + "m";

            if (_pingText != null)
                _pingText.text = fallbackPingMs + "ms";
        }

        private void RefreshWeapons()
        {
            if (equipment == null)
                return;

            if (_weaponSlot1Ammo != null)
                _weaponSlot1Ammo.text = AmmoText(equipment.PrimarySlot1);
            if (_weaponSlot2Ammo != null)
                _weaponSlot2Ammo.text = AmmoText(equipment.PrimarySlot2);
            if (_weaponSlot3Ammo != null)
                _weaponSlot3Ammo.text = AmmoText(equipment.SidearmSlot);
        }

        private void RefreshCompass()
        {
            if (_compassTicksRoot == null || input == null)
                return;

            float yaw = thirdPersonCamera != null ? thirdPersonCamera.CameraYaw : input.transform.eulerAngles.y;
            yaw = Mathf.Repeat(yaw, 360f);

            string[] cardinal = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            for (int i = -5; i <= 5; i++)
            {
                int childIndex = i + 5;
                Text t = _compassTicksRoot.GetChild(childIndex).GetComponent<Text>();
                float angle = Mathf.Repeat(yaw + i * 15f, 360f);
                int rounded = Mathf.RoundToInt(angle / 15f) * 15;
                rounded = ((rounded % 360) + 360) % 360;
                string label = rounded.ToString();
                if (rounded % 45 == 0)
                    label = cardinal[(rounded / 45) % 8] + "\n" + rounded;
                t.text = label;
            }

            if (_headingText != null)
            {
                int heading = Mathf.RoundToInt(yaw);
                string dir = cardinal[Mathf.RoundToInt(yaw / 45f) % 8];
                _headingText.text = dir + " " + heading;
            }
        }

        private void RefreshNearbyLoot()
        {
            if (_lootRows == null)
                return;

            LootPickup[] pickups = FindObjectsByType<LootPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<LootPickup> nearby = new List<LootPickup>();

            for (int i = 0; i < pickups.Length; i++)
            {
                LootPickup pickup = pickups[i];
                if (pickup == null || pickup.IsConsumed || pickup.Item == null)
                    continue;
                if (Vector3.Distance(transform.position, pickup.transform.position) <= NearbyLootRadius)
                    nearby.Add(pickup);
            }

            nearby.Sort((a, b) => Vector3.SqrMagnitude(a.transform.position - transform.position)
                .CompareTo(Vector3.SqrMagnitude(b.transform.position - transform.position)));

            string[] screenshotFallback =
            {
                "Drone",
                "Drone Battery\nDrone Power Up",
                "Attack Module\nLv. 2 Module",
                "Drone Battery\nDrone Power Up",
                "Molotov Cocktail\nContinuous Fire Damage",
                "Lv. 3 Armor\nReduces damage",
                "Molotov Cocktail\nContinuous Fire Damage",
                "Smoke Grenade\nblock your sight"
            };

            for (int i = 0; i < _lootRows.Length; i++)
            {
                if (i < nearby.Count)
                {
                    LootPickup pickup = nearby[i];
                    _lootRows[i].text = pickup.Item.displayName + (pickup.Amount > 1 ? "  x" + pickup.Amount : "");
                }
                else
                {
                    _lootRows[i].text = screenshotFallback[i];
                }
            }
        }

        private void BuildMinimapCamera()
        {
            if (_minimapImage == null)
                return;

            GameObject cameraGo = new GameObject("ROS_MinimapCamera");
            _minimapCamera = cameraGo.AddComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = minimapWorldSize * 0.5f;
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = new Color(0.08f, 0.15f, 0.10f, 1f);
            _minimapCamera.depth = -50f;
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _minimapTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            _minimapTexture.name = "ROS_Minimap_RT";
            _minimapCamera.targetTexture = _minimapTexture;
            _minimapImage.texture = _minimapTexture;
        }

        private void RefreshMinimap()
        {
            if (_minimapCamera == null)
                return;

            Vector3 p = transform.position;
            _minimapCamera.transform.position = new Vector3(p.x, p.y + minimapCameraHeight, p.z);

            float yaw = thirdPersonCamera != null ? thirdPersonCamera.CameraYaw : transform.eulerAngles.y;
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, yaw, 0f);

            if (_minimapPlayerArrow != null)
                _minimapPlayerArrow.localEulerAngles = Vector3.zero;
        }

        private void DisableLegacyHudObjects()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas c = canvases[i];
                if (c == null || c == _canvas)
                    continue;

                string n = c.gameObject.name;
                if (n == "CompassCanvas" || n == "CrosshairCanvas" ||
                    n.Contains("VitalsPanel") || n.Contains("BattleRoyalePanel") ||
                    n.Contains("WeaponPanel"))
                {
                    c.enabled = false;
                }
            }
        }

        private static string AmmoText(WeaponController weapon)
        {
            return weapon == null ? "--/--" : weapon.AmmoInMagazine + "/" + weapon.ReserveAmmo;
        }

        private static string WeaponSilhouette(WeaponController weapon)
        {
            if (weapon == null)
                return "—";

            string name = weapon.Definition != null ? weapon.Definition.displayName : weapon.name;
            string lower = name.ToLowerInvariant();
            if (lower.Contains("pistol") || lower.Contains("desert"))
                return "▰╾";
            if (lower.Contains("awm") || lower.Contains("sniper"))
                return "━━━╾";
            if (lower.Contains("shotgun") || lower.Contains("1887"))
                return "━━━━";
            return "━━▰━━";
        }

        private RectTransform Panel(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos)
        {
            RectTransform rt = Rect(name, parent, anchorMin, anchorMax, size, pos);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return rt;
        }

        private Text Label(string name, Transform parent, string value, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos,
            Color? background = null)
        {
            RectTransform rt = Rect(name, parent, anchorMin, anchorMax, size, pos);
            if (background.HasValue)
            {
                Image bg = rt.gameObject.AddComponent<Image>();
                bg.color = background.Value;
            }

            Text text = rt.gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 size, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        private Image FillImage(string name, RectTransform parent, Color color)
        {
            RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(rt);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
            return image;
        }

        private Text CircleButton(Transform parent, Vector2 anchor, Vector2 pos, string text, float size)
        {
            RectTransform rt = Rect("CircleButton_" + text, parent, anchor, anchor, new Vector2(size, size), pos);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = CreateCircleSprite(96);
            image.color = DarkSoft;
            Text t = Label("Label", rt, text, Mathf.Max(9, Mathf.RoundToInt(size * 0.25f)), White,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(t.rectTransform);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float r = size * 0.5f - 1f;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), c) <= r ? Color.white : Color.clear;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateRingSprite(int size, int thickness)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float outer = size * 0.5f - 1f;
            float inner = outer - thickness;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                pixels[y * size + x] = d <= outer && d >= inner ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateTriangleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float normalizedY = y / (float)(size - 1);
                float halfWidth = (1f - normalizedY) * size * 0.46f;
                float centerX = size * 0.5f;
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Mathf.Abs(x - centerX) <= halfWidth ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
