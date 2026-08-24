using System.IO;
using ROS.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa en el proyecto los elementos de presentacion que antes se
    /// construian por codigo. Los assets se crean solo si no existen, de modo
    /// que despues pueden editarse libremente desde Unity sin ser sobrescritos.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstPresentationBuilder
    {
        private const string RootFolder = "Assets/_Game/Resources/EditorFirst";
        private const string HudPath = RootFolder + "/ROS_HUD_Editable.prefab";
        private const string BotHealthBarPath = RootFolder + "/BotHealthBar.prefab";
        private const string StartMenuPath = RootFolder + "/BattleRoyaleStartMenuView.prefab";
        private const string GestureWheelPath = RootFolder + "/GestureWheelUI.prefab";
        private const string DamageNumberPath = RootFolder + "/DamageNumber.prefab";
        private const string CirclePath = RootFolder + "/ROS_UI_Circle.asset";
        private const string TrianglePath = RootFolder + "/ROS_UI_Triangle.asset";
        private const string MinimapRtPath = RootFolder + "/ROS_Minimap_RT.renderTexture";
        private const string SafeZoneLinePath = RootFolder + "/SafeZoneLine.mat";
        private const string SafeZoneWallPath = RootFolder + "/SafeZoneWall.mat";
        private const string SafeZoneWallMeshPath = RootFolder + "/SafeZoneWallMesh.asset";

        private static readonly Color Dark = new Color(0.025f, 0.035f, 0.045f, 0.84f);
        private static readonly Color DarkSoft = new Color(0.025f, 0.035f, 0.045f, 0.68f);
        private static readonly Color Yellow = new Color(0.96f, 0.86f, 0.02f, 0.96f);
        private static readonly Color HealthColor = new Color(0.86f, 0.88f, 0.88f, 1f);
        private static readonly Color ArmorColor = new Color(0.28f, 0.68f, 0.94f, 1f);

        static EditorFirstPresentationBuilder()
        {
            EditorApplication.delayCall += EnsureMaterialized;
        }

        [MenuItem("Rules Of Survival/Editor First/Materialize Missing Presentation Assets")]
        public static void EnsureMaterialized()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EnsureFolder(RootFolder);

            Sprite circle = EnsureShapeSprite(CirclePath, true, 128);
            Sprite triangle = EnsureShapeSprite(TrianglePath, false, 128);
            RenderTexture minimap = EnsureMinimapRenderTexture();
            EnsureMaterial(SafeZoneLinePath, new Color(0.15f, 0.65f, 1f, 0.95f));
            EnsureMaterial(SafeZoneWallPath, new Color(0.10f, 0.55f, 1f, 0.22f));
            EnsureSafeZoneWallMesh();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) == null)
                BuildHudPrefab(circle, triangle, minimap);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BotHealthBarPath) == null)
                BuildBotHealthBarPrefab();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(StartMenuPath) == null)
                BuildStartMenuPrefab();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GestureWheelPath) == null)
                BuildGestureWheelPrefab();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DamageNumberPath) == null)
                BuildDamageNumberPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Rules Of Survival/Editor First/Select Editable HUD Prefab")]
        private static void SelectHudPrefab()
        {
            EnsureMaterialized();
            Object hud = AssetDatabase.LoadAssetAtPath<Object>(HudPath);
            Selection.activeObject = hud;
            if (hud != null)
                EditorGUIUtility.PingObject(hud);
        }

        [MenuItem("Rules Of Survival/Editor First/Validate Runtime Presentation Creation")]
        private static void ValidateRuntimePresentationCreation()
        {
            string scriptsRoot = Path.GetFullPath("Assets/_Game/Scripts");
            if (!Directory.Exists(scriptsRoot))
                return;

            string[] patterns =
            {
                "new GameObject(",
                "new Material(",
                "new Mesh",
                "new RenderTexture(",
                "Sprite.Create("
            };

            int findings = 0;
            foreach (string file in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(file);
                for (int i = 0; i < patterns.Length; i++)
                {
                    if (!source.Contains(patterns[i]))
                        continue;

                    findings++;
                    Debug.LogWarning(
                        "[Editor First] Revisar creacion runtime '" + patterns[i] + "' en " +
                        file.Replace('\\', '/')
                    );
                }
            }

            if (findings == 0)
                Debug.Log("[Editor First] No se detectaron constructores de presentacion runtime conocidos.");
            else
                Debug.Log("[Editor First] Revision terminada. Hallazgos: " + findings + ".");
        }

        private static void BuildHudPrefab(Sprite circle, Sprite triangle, RenderTexture minimapTexture)
        {
            Font font = GetDefaultFont();
            GameObject root = new GameObject("ROS_HUD_Editable");
            root.AddComponent<RulesOfSurvivalHUD>();

            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform compass = CreatePanel(
                "CompassStrip", canvasObject.transform, new Vector2(520f, 30f),
                new Vector2(0f, -18f), new Vector2(0.5f, 1f), DarkSoft
            );
            Text compassText = CreateText(
                "CompassText", compass,
                "150     165     S 180     195     210", 13,
                TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font
            );
            Stretch(compassText.rectTransform, 6f, 2f, 6f, 2f);
            Text waypoint = CreateText(
                "Waypoint", canvasObject.transform, "◆ 1068m ▼", 13,
                TextAnchor.MiddleCenter, Yellow, FontStyle.Bold, font
            );
            SetRect(waypoint.rectTransform, new Vector2(110f, 24f), new Vector2(0f, -1f), new Vector2(0.5f, 1f));

            RectTransform stats = CreatePanel(
                "TopRightStats", canvasObject.transform, new Vector2(205f, 39f),
                new Vector2(-15f, -7f), Vector2.one, Dark
            );
            Text kill = CreateText("KillText", stats, "0 KILL", 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font);
            SetRect(kill.rectTransform, new Vector2(70f, 39f), new Vector2(-137f, 0f), new Vector2(1f, 0.5f));
            Text left = CreateText("LeftText", stats, "-- LEFT", 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font);
            SetRect(left.rectTransform, new Vector2(72f, 39f), new Vector2(-68f, 0f), new Vector2(1f, 0.5f));
            RectTransform distancePanel = CreatePanel(
                "DistancePanel", stats, new Vector2(72f, 39f), Vector2.zero,
                new Vector2(1f, 0.5f), new Color(0.92f, 0.92f, 0.92f, 0.94f)
            );
            Text distance = CreateText("DistanceText", distancePanel, "ZONE\n--m", 11, TextAnchor.MiddleCenter, Color.black, FontStyle.Bold, font);
            Stretch(distance.rectTransform, 2f, 2f, 2f, 2f);

            RectTransform minimapRoot = CreatePanel(
                "MinimapFrame", canvasObject.transform, new Vector2(205f, 205f),
                new Vector2(8f, 10f), Vector2.zero, new Color(0f, 0f, 0f, 0.72f)
            );
            RectTransform maskRect = CreateRect("CircleMask", minimapRoot);
            SetRect(maskRect, new Vector2(188f, 188f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Image maskImage = maskRect.gameObject.AddComponent<Image>();
            maskImage.sprite = circle;
            maskImage.color = Color.white;
            Mask mask = maskRect.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform rawRect = CreateRect("WorldMap", maskRect);
            Stretch(rawRect, 0f, 0f, 0f, 0f);
            RawImage raw = rawRect.gameObject.AddComponent<RawImage>();
            raw.texture = minimapTexture;

            Image playerArrow = CreateImage(
                "PlayerArrow", minimapRoot, new Vector2(24f, 24f), Vector2.zero,
                new Vector2(0.5f, 0.5f), Yellow
            );
            playerArrow.sprite = triangle;

            RectTransform badge = CreatePanel(
                "MapBadge", minimapRoot, new Vector2(28f, 28f), new Vector2(0f, 5f),
                new Vector2(0.5f, 0.5f), Yellow
            );
            Text badgeText = CreateText("Text", badge, "1", 16, TextAnchor.MiddleCenter, Color.black, FontStyle.Bold, font);
            Stretch(badgeText.rectTransform, 0f, 0f, 0f, 0f);
            Text latency = CreateText("Latency", canvasObject.transform, "64ms", 14, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold, font);
            SetRect(latency.rectTransform, new Vector2(75f, 24f), new Vector2(130f, 10f), Vector2.zero);

            GameObject minimapCameraObject = new GameObject("ROS_MinimapCamera", typeof(Camera));
            minimapCameraObject.transform.SetParent(root.transform, false);
            Camera minimapCamera = minimapCameraObject.GetComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 62f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.14f, 0.20f, 0.14f, 1f);
            minimapCamera.targetTexture = minimapTexture;
            minimapCamera.depth = -50f;
            minimapCamera.nearClipPlane = 1f;
            minimapCamera.farClipPlane = 500f;

            RectTransform vitals = CreatePanel(
                "Vitals", canvasObject.transform, new Vector2(385f, 68f),
                new Vector2(0f, 8f), new Vector2(0.5f, 0f),
                new Color(0.02f, 0.03f, 0.04f, 0.58f)
            );
            Text playerName = CreateText("PlayerName", vitals, "PLAYER", 14, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold, font);
            SetRect(playerName.rectTransform, new Vector2(155f, 22f), new Vector2(-84f, 16f), new Vector2(0.5f, 0.5f));
            RectTransform healthBack = CreatePanel("HealthBack", vitals, new Vector2(250f, 14f), new Vector2(0f, -8f), new Vector2(0.5f, 0.5f), new Color(0.05f, 0.05f, 0.05f, 0.9f));
            Image healthFill = CreateImage("HealthFill", healthBack, new Vector2(250f, 14f), new Vector2(-125f, 0f), new Vector2(0f, 0.5f), HealthColor);
            healthFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            RectTransform armorBack = CreatePanel("ArmorBack", vitals, new Vector2(250f, 5f), new Vector2(0f, -20f), new Vector2(0.5f, 0.5f), new Color(0.04f, 0.04f, 0.04f, 0.9f));
            Image armorFill = CreateImage("ArmorFill", armorBack, new Vector2(250f, 5f), new Vector2(-125f, 0f), new Vector2(0f, 0.5f), ArmorColor);
            armorFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            Text healthValue = CreateText("HealthValue", vitals, "100", 12, TextAnchor.MiddleRight, Color.white, FontStyle.Bold, font);
            SetRect(healthValue.rectTransform, new Vector2(55f, 20f), new Vector2(140f, -8f), new Vector2(0.5f, 0.5f));
            CreateCircularHint(vitals, "Med", "♡", new Vector2(-170f, 8f), circle, font);
            CreateCircularHint(vitals, "Boost", "⚡", new Vector2(170f, 8f), circle, font);
            Text interaction = CreateText("InteractionHint", canvasObject.transform, string.Empty, 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font);
            SetRect(interaction.rectTransform, new Vector2(420f, 32f), new Vector2(0f, 84f), new Vector2(0.5f, 0f));

            RectTransform loot = CreatePanel(
                "NearbyLoot", canvasObject.transform, new Vector2(185f, 310f),
                new Vector2(-235f, 18f), new Vector2(1f, 0.5f), Yellow
            );
            RectTransform title = CreatePanel("Title", loot, new Vector2(185f, 32f), new Vector2(0f, 139f), new Vector2(0.5f, 0.5f), Dark);
            Text titleText = CreateText("TitleText", title, "NEARBY LOOT", 17, TextAnchor.MiddleLeft, Color.white, FontStyle.Italic, font);
            Stretch(titleText.rectTransform, 10f, 2f, 4f, 2f);
            for (int i = 0; i < 7; i++)
            {
                Text row = CreateText("LootRow_" + i, loot, string.Empty, 14, TextAnchor.MiddleLeft, Color.black, FontStyle.Bold, font);
                SetRect(row.rectTransform, new Vector2(171f, 36f), new Vector2(5f, 102f - i * 38f), new Vector2(0.5f, 0.5f));
            }
            RectTransform toggleBg = CreatePanel("ToggleBg", loot, new Vector2(145f, 24f), new Vector2(-165f, 96f), new Vector2(0f, 0.5f), Dark);
            Text toggle = CreateText("ToggleHint", toggleBg, "↕ SCROLL TO SELECT", 11, TextAnchor.MiddleCenter, Yellow, FontStyle.Bold, font);
            Stretch(toggle.rectTransform, 2f, 2f, 2f, 2f);
            loot.gameObject.SetActive(false);

            RectTransform actions = CreatePanel(
                "Actions", canvasObject.transform, new Vector2(90f, 280f),
                new Vector2(-100f, -55f), new Vector2(1f, 0.5f), Color.clear
            );
            CreateCircularHint(actions, "Fire", "●", new Vector2(0f, 95f), circle, font);
            CreateCircularHint(actions, "Aim", "◎", new Vector2(0f, 42f), circle, font);
            CreateCircularHint(actions, "Interact", "F", new Vector2(0f, -11f), circle, font);
            CreateCircularHint(actions, "Crouch", "C", new Vector2(0f, -64f), circle, font);
            CreateCircularHint(actions, "Prone", "Z", new Vector2(0f, -117f), circle, font);

            RectTransform weapons = CreatePanel(
                "Weapons", canvasObject.transform, new Vector2(205f, 145f),
                new Vector2(-8f, 7f), new Vector2(1f, 0f), Color.clear
            );
            BuildWeaponSlot(weapons, 1, new Vector2(0f, 94f), 48f, font);
            BuildWeaponSlot(weapons, 2, new Vector2(0f, 46f), 44f, font);
            BuildWeaponSlot(weapons, 3, new Vector2(0f, 2f), 40f, font);

            Image crosshair = CreateImage("Crosshair", canvasObject.transform, new Vector2(4f, 4f), Vector2.zero, new Vector2(0.5f, 0.5f), new Color(1f, 1f, 1f, 0.92f));
            crosshair.sprite = circle;
            Text zone = CreateText("ZoneBanner", canvasObject.transform, string.Empty, 14, TextAnchor.MiddleCenter, Yellow, FontStyle.Bold, font);
            SetRect(zone.rectTransform, new Vector2(360f, 28f), new Vector2(0f, -52f), new Vector2(0.5f, 1f));

            RulesOfSurvivalHUD controller = root.GetComponent<RulesOfSurvivalHUD>();
            controller.BindViewFromHierarchy();
            SavePrefab(root, HudPath);
        }

        private static void BuildBotHealthBarPrefab()
        {
            GameObject root = new GameObject(
                "BotHealthBar",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(BotHealthBar)
            );
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1.1f, 0.12f);

            RectTransform background = CreateRect("BG", root.transform);
            Stretch(background, 0f, 0f, 0f, 0f);
            Image bgImage = background.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            RectTransform fill = CreateRect("Fill", root.transform);
            Stretch(fill, 0f, 0f, 0f, 0f);
            fill.pivot = new Vector2(0f, 0.5f);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = Color.green;

            SavePrefab(root, BotHealthBarPath);
        }

        private static void BuildDamageNumberPrefab()
        {
            GameObject root = new GameObject("DamageNumber");
            TextMesh text = root.AddComponent<TextMesh>();
            text.text = "25";
            text.fontSize = 36;
            text.characterSize = 0.08f;
            text.color = new Color(1f, 0.92f, 0.10f);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            SavePrefab(root, DamageNumberPath);
        }

        private static void BuildStartMenuPrefab()
        {
            Font font = GetDefaultFont();
            GameObject root = new GameObject(
                "BattleRoyaleStartMenuView",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(Image)
            );
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            root.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.045f, 0.94f);

            Text title = CreateText("Title", root.transform, "RULES OF SURVIVAL", 36, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font);
            SetRect(title.rectTransform, new Vector2(620f, 60f), new Vector2(0f, 150f), new Vector2(0.5f, 0.5f));
            Text subtitle = CreateText("Subtitle", root.transform, "Battle Royale · Sobrevive, consigue loot y sé el último en pie", 15, TextAnchor.MiddleCenter, new Color(0.64f, 0.76f, 0.9f), FontStyle.Normal, font);
            SetRect(subtitle.rectTransform, new Vector2(560f, 48f), new Vector2(0f, 90f), new Vector2(0.5f, 0.5f));

            CreateButton("StartMatchButton", root.transform, "INICIAR PARTIDA BR", new Vector2(280f, 60f), new Vector2(0f, 15f), font);
            Text sequence = CreateText("SequenceHint", root.transform, "Avión → paracaídas → loot → último en pie", 15, TextAnchor.MiddleCenter, new Color(0.64f, 0.76f, 0.9f), FontStyle.Normal, font);
            SetRect(sequence.rectTransform, new Vector2(560f, 22f), new Vector2(0f, -32f), new Vector2(0.5f, 0.5f));
            CreateButton("FreeroamButton", root.transform, "MODO LIBRE (sin BR)", new Vector2(280f, 52f), new Vector2(0f, -90f), font);
            Text freeHint = CreateText("FreeroamHint", root.transform, "Explora el mapa sin bots ni zona azul", 15, TextAnchor.MiddleCenter, new Color(0.64f, 0.76f, 0.9f), FontStyle.Normal, font);
            SetRect(freeHint.rectTransform, new Vector2(560f, 22f), new Vector2(0f, -132f), new Vector2(0.5f, 0.5f));

            SavePrefab(root, StartMenuPath);
        }

        private static void BuildGestureWheelPrefab()
        {
            Font font = GetDefaultFont();
            GameObject root = new GameObject("GestureWheelUI");
            root.AddComponent<GestureWheelUI>();

            GameObject canvasObject = new GameObject(
                "GestureCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform overlay = CreateRect("WheelOverlay", canvasObject.transform);
            Stretch(overlay, 0f, 0f, 0f, 0f);
            Image overlayImage = overlay.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
            overlayImage.raycastTarget = false;

            RectTransform center = CreateRect("WheelCenter", overlay);
            SetRect(center, new Vector2(720f, 720f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Image centerImage = center.gameObject.AddComponent<Image>();
            centerImage.color = new Color(0.055f, 0.065f, 0.075f, 0.94f);
            centerImage.raycastTarget = false;

            Text selection = CreateText("SelectionLabel", center, "GESTOS", 28, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font);
            SetRect(selection.rectTransform, new Vector2(250f, 90f), Vector2.zero, new Vector2(0.5f, 0.5f));

            string[] names =
            {
                "Dancing", "Fishing Cast", "Hip Hop Dancing", "Joyful Jump", "Opening",
                "Rumba Dancing", "Salute", "Talking On Phone", "Waving Gesture"
            };
            const float radius = 250f;
            float sector = 360f / names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                float radians = (90f - sector * i) * Mathf.Deg2Rad;
                RectTransform item = CreateRect("Gesture_" + (i + 1), center);
                SetRect(
                    item,
                    new Vector2(170f, 62f),
                    new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius,
                    new Vector2(0.5f, 0.5f)
                );
                Image itemImage = item.gameObject.AddComponent<Image>();
                itemImage.color = new Color(0.12f, 0.14f, 0.16f, 0.96f);
                itemImage.raycastTarget = false;
                Text label = CreateText("Label", item, (i + 1) + ". " + names[i], 17, TextAnchor.MiddleCenter, Color.white, FontStyle.Normal, font);
                Stretch(label.rectTransform, 8f, 4f, 8f, 4f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 17;
            }

            Text help = CreateText("Help", center, "Mueve el mouse y haz clic · 1-9 acceso directo · G/Esc cerrar", 16, TextAnchor.MiddleCenter, Color.white, FontStyle.Normal, font);
            SetRect(help.rectTransform, new Vector2(520f, 50f), new Vector2(0f, -320f), new Vector2(0.5f, 0.5f));

            RectTransform hint = CreatePanel(
                "GestureHint", canvasObject.transform, new Vector2(180f, 48f),
                new Vector2(245f, 34f), new Vector2(0.5f, 0f),
                new Color(0.12f, 0.14f, 0.16f, 0.96f)
            );
            Text hintText = CreateText("HintText", hint, "G   GESTOS", 18, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font);
            Stretch(hintText.rectTransform, 0f, 0f, 0f, 0f);

            overlay.gameObject.SetActive(false);
            GestureWheelUI controller = root.GetComponent<GestureWheelUI>();
            controller.BindViewFromHierarchy();
            SavePrefab(root, GestureWheelPath);
        }

        private static Sprite EnsureShapeSprite(string path, bool circle, int size)
        {
            Sprite existing = FindSpriteAtPath(path);
            if (existing != null)
                return existing;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = circle ? "ROS_UI_CircleTexture" : "ROS_UI_TriangleTexture",
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 clear = new Color32(255, 255, 255, 0);
            Color32 solid = new Color32(255, 255, 255, 255);
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside;
                    if (circle)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        inside = dx * dx + dy * dy <= center * center;
                    }
                    else
                    {
                        float normalizedY = y / (float)(size - 1);
                        float halfWidth = normalizedY * size * 0.45f;
                        inside = Mathf.Abs(x - center) <= halfWidth;
                    }
                    pixels[y * size + x] = inside ? solid : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            AssetDatabase.CreateAsset(texture, path);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
            sprite.name = circle ? "ROS_UI_CircleSprite" : "ROS_UI_TriangleSprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        private static Sprite FindSpriteAtPath(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }
            return null;
        }

        private static RenderTexture EnsureMinimapRenderTexture()
        {
            RenderTexture existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(MinimapRtPath);
            if (existing != null)
                return existing;

            RenderTexture texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
            {
                name = "ROS_Minimap_RT"
            };
            AssetDatabase.CreateAsset(texture, MinimapRtPath);
            return texture;
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("UI/Default");
            if (shader == null)
                return null;

            Material material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(path),
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Mesh EnsureSafeZoneWallMesh()
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(SafeZoneWallMeshPath);
            if (existing != null)
                return existing;

            Mesh mesh = BuildWallMeshAsset(100f, 50f, 128);
            mesh.name = "SafeZoneWallMesh";
            AssetDatabase.CreateAsset(mesh, SafeZoneWallMeshPath);
            return mesh;
        }

        private static Mesh BuildWallMeshAsset(float radius, float height, int segments)
        {
            int count = Mathf.Max(32, segments);
            Vector3[] vertices = new Vector3[(count + 1) * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i <= count; i++)
            {
                float normalized = (float)i / count;
                float angle = normalized * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                int bottom = i * 2;
                vertices[bottom] = new Vector3(x, 0f, z);
                vertices[bottom + 1] = new Vector3(x, height, z);
                uvs[bottom] = new Vector2(normalized, 0f);
                uvs[bottom + 1] = new Vector2(normalized, 1f);
            }

            int[] triangles = new int[count * 12];
            int t = 0;
            for (int i = 0; i < count; i++)
            {
                int bl = i * 2;
                int tl = bl + 1;
                int br = bl + 2;
                int tr = bl + 3;
                triangles[t++] = bl; triangles[t++] = tl; triangles[t++] = tr;
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = br;
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = tl;
                triangles[t++] = bl; triangles[t++] = br; triangles[t++] = tr;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void BuildWeaponSlot(Transform parent, int slotNumber, Vector2 position, float height, Font font)
        {
            RectTransform panel = CreatePanel(
                "WeaponSlot_" + slotNumber, parent, new Vector2(205f, height),
                position, new Vector2(0.5f, 0f), Dark
            );
            Text slot = CreateText("Slot", panel, slotNumber.ToString(), 12, TextAnchor.UpperLeft, Color.white, FontStyle.Bold, font);
            SetRect(slot.rectTransform, new Vector2(22f, height), new Vector2(-90f, 0f), new Vector2(0.5f, 0.5f));
            Text name = CreateText("WeaponName", panel, "EMPTY", 12, TextAnchor.MiddleLeft, new Color(0.86f, 0.88f, 0.9f), FontStyle.Bold, font);
            SetRect(name.rectTransform, new Vector2(96f, height), new Vector2(-33f, 0f), new Vector2(0.5f, 0.5f));
            Text ammo = CreateText("Ammo", panel, "--/--", 15, TextAnchor.MiddleRight, Color.white, FontStyle.Bold, font);
            SetRect(ammo.rectTransform, new Vector2(82f, height), new Vector2(56f, 0f), new Vector2(0.5f, 0.5f));
        }

        private static void CreateCircularHint(Transform parent, string name, string label, Vector2 position, Sprite sprite, Font font)
        {
            Image circle = CreateImage(name, parent, new Vector2(42f, 42f), position, new Vector2(0.5f, 0.5f), new Color(0.04f, 0.05f, 0.06f, 0.78f));
            circle.sprite = sprite;
            Text text = CreateText(name + "Text", circle.transform, label, 17, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font);
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 position, Font font)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, size, position, new Vector2(0.5f, 0.5f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.25f, 0.96f);
            Button button = rect.gameObject.AddComponent<Button>();
            Text text = CreateText("Text", rect, label, 19, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, font);
            Stretch(text.rectTransform, 4f, 4f, 4f, 4f);
            return button;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, size, position, anchor);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, size, position, anchor);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Color color, FontStyle style, Font font)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.78f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
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

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
