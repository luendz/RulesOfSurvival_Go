using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbySceneBootstrap : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private GameObject characterPrefab;

        [Header("Flow")]
        [SerializeField] private string battleRoyaleSceneName = "07_BattleRoyaleTest";
        [SerializeField] private string mapName = "Ghillie Island";

        [Header("Profile mock")]
        [SerializeField] private string playerName = "Jugador";
        [SerializeField] private int playerLevel = 1;
        [SerializeField] private int gold = 1250;
        [SerializeField] private int diamonds = 300;

        private Font _font;
        private Canvas _canvas;
        private LobbyNavigationController _navigation;
        private LobbyCharacterRotator _rotator;
        private LobbyCameraController _cameraController;
        private GameObject _character;
        private Text _modeText;
        private LobbyMatchMode _selectedMode = LobbyMatchMode.Solo;
        private bool _built;

        private readonly Color _panelColor =
            new Color(0.018f, 0.028f, 0.045f, 0.88f);

        private readonly Color _buttonColor =
            new Color(0.075f, 0.11f, 0.16f, 0.96f);

        private readonly Color _primaryColor =
            new Color(0.92f, 0.56f, 0.08f, 1f);

        private readonly Color _accentColor =
            new Color(0.22f, 0.66f, 0.9f, 1f);

        private void Awake()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            LobbySession.CancelLaunchRequest();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildEnvironment();
            SpawnCharacter();
            BuildCamera();
            BuildUi();
        }

        private void OnDestroy()
        {
            if (_navigation != null)
            {
                _navigation.MenuChanged -= HandleMenuChanged;
            }
        }

        private void BuildEnvironment()
        {
            Camera existingCamera = Camera.main;
            if (existingCamera != null)
            {
                existingCamera.gameObject.SetActive(false);
            }

            GameObject cameraObject = new GameObject("Lobby Camera");
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.075f, 1f);
            cameraObject.AddComponent<AudioListener>();

            _cameraController = cameraObject.AddComponent<LobbyCameraController>();

            GameObject keyLightObject = new GameObject("Lobby Key Light");
            Light keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.35f;
            keyLight.color = new Color(1f, 0.93f, 0.84f, 1f);
            keyLight.shadows = LightShadows.Soft;
            keyLightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

            CreateFillLight(
                "Lobby Fill Light",
                new Vector3(-2.5f, 2.4f, 2.2f),
                new Color(0.38f, 0.66f, 1f, 1f),
                3.4f,
                7f
            );

            CreateFillLight(
                "Lobby Rim Light",
                new Vector3(2.2f, 2.8f, -1.7f),
                new Color(1f, 0.52f, 0.18f, 1f),
                2.7f,
                6f
            );

            GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stage.name = "Lobby Character Stage";
            stage.transform.position = new Vector3(0f, -0.06f, 0f);
            stage.transform.localScale = new Vector3(1.55f, 0.06f, 1.55f);

            Collider stageCollider = stage.GetComponent<Collider>();
            if (stageCollider != null)
            {
                stageCollider.enabled = false;
            }

            Renderer renderer = stage.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = CreateRuntimeMaterial(
                    new Color(0.075f, 0.095f, 0.12f, 1f),
                    0.25f
                );
                if (material != null)
                {
                    renderer.material = material;
                }
            }
        }

        private void CreateFillLight(
            string objectName,
            Vector3 position,
            Color color,
            float intensity,
            float range
        )
        {
            GameObject lightObject = new GameObject(objectName);
            lightObject.transform.position = position;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private Material CreateRuntimeMaterial(
            Color color,
            float smoothness
        )
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader)
            {
                color = color
            };

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            return material;
        }

        private void SpawnCharacter()
        {
            if (characterPrefab != null)
            {
                _character = Instantiate(characterPrefab);
                _character.name = "Lobby Character";
            }
            else
            {
                Debug.LogWarning(
                    "LobbySceneBootstrap no tiene characterPrefab asignado. " +
                    "Se usará una cápsula de respaldo."
                );

                _character = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _character.name = "Lobby Character Placeholder";
            }

            _character.transform.position = Vector3.zero;
            _character.transform.rotation = Quaternion.identity;

            DisableGameplayComponents(_character);
            NormalizeCharacter(_character.transform, 1.88f);
        }

        private static void DisableGameplayComponents(GameObject character)
        {
            MonoBehaviour[] behaviours =
                character.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }

            Rigidbody[] rigidbodies =
                character.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody body in rigidbodies)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
            }

            CharacterController[] controllers =
                character.GetComponentsInChildren<CharacterController>(true);
            foreach (CharacterController controller in controllers)
            {
                controller.enabled = false;
            }

            Collider[] colliders =
                character.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            AudioSource[] audioSources =
                character.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource audioSource in audioSources)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            Animator[] animators =
                character.GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                animator.applyRootMotion = false;
            }
        }

        private static void NormalizeCharacter(
            Transform root,
            float desiredHeight
        )
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            if (bounds.size.y > 0.001f)
            {
                float factor = desiredHeight / bounds.size.y;
                root.localScale *= factor;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            root.position += Vector3.up * -bounds.min.y;
        }

        private void BuildCamera()
        {
            if (_cameraController != null && _character != null)
            {
                _cameraController.Configure(_character.transform);
            }
        }

        private void BuildUi()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("Lobby Canvas");
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            _navigation = GetComponent<LobbyNavigationController>();
            if (_navigation == null)
            {
                _navigation = gameObject.AddComponent<LobbyNavigationController>();
            }

            CreateCharacterDragArea(canvasObject.transform);
            CreateTopCenterTitle(canvasObject.transform);
            CreateTopLeft(canvasObject.transform);
            CreateTopRight(canvasObject.transform);
            CreateBottomLeft(canvasObject.transform);
            CreateBottomRight(canvasObject.transform);
            CreateMenuPanels(canvasObject.transform);

            _navigation.MenuChanged += HandleMenuChanged;
            HandleMenuChanged(LobbyMenuId.None);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module =
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private void CreateCharacterDragArea(Transform canvasTransform)
        {
            GameObject dragObject = new GameObject(
                "Character Drag Area",
                typeof(RectTransform),
                typeof(Image)
            );
            dragObject.transform.SetParent(canvasTransform, false);

            RectTransform rect = dragObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.18f, 0.08f);
            rect.anchorMax = new Vector2(0.82f, 0.94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = dragObject.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            _rotator = dragObject.AddComponent<LobbyCharacterRotator>();
            _rotator.Configure(_character != null ? _character.transform : null);
        }

        private void CreateTopCenterTitle(Transform parent)
        {
            CreateLabel(
                "Game Title",
                parent,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -30f),
                new Vector2(520f, 60f),
                "RULES OF SURVIVAL",
                30,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold
            );

            CreateLabel(
                "Drag Hint",
                parent,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 22f),
                new Vector2(520f, 34f),
                "Arrastra horizontalmente sobre el personaje para girarlo 360°",
                14,
                TextAnchor.MiddleCenter,
                new Color(0.72f, 0.8f, 0.9f, 0.9f),
                FontStyle.Normal
            );
        }

        private void CreateTopLeft(Transform parent)
        {
            RectTransform panel = CreatePanel(
                "Top Left",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(28f, -28f),
                new Vector2(390f, 160f),
                _panelColor,
                false
            );

            CreateButton(
                "Profile",
                panel,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(14f, -14f),
                new Vector2(362f, 72f),
                $"{playerName.ToUpperInvariant()}   |   NIVEL {Mathf.Max(1, playerLevel)}",
                () => _navigation.Open(LobbyMenuId.Character),
                false
            );

            CreateButton(
                "Events",
                panel,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(14f, 14f),
                new Vector2(174f, 52f),
                "EVENTOS",
                () => _navigation.Open(LobbyMenuId.Events),
                false
            );

            CreateButton(
                "Missions",
                panel,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-14f, 14f),
                new Vector2(174f, 52f),
                "MISIONES",
                () => _navigation.Open(LobbyMenuId.Missions),
                false
            );
        }

        private void CreateTopRight(Transform parent)
        {
            RectTransform panel = CreatePanel(
                "Top Right",
                parent,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-28f, -28f),
                new Vector2(470f, 112f),
                _panelColor,
                false
            );

            CreateLabel(
                "Currency",
                panel,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(16f, -14f),
                new Vector2(300f, 38f),
                $"ORO {Mathf.Max(0, gold)}   |   DIAMANTES {Mathf.Max(0, diamonds)}",
                15,
                TextAnchor.MiddleLeft,
                new Color(0.95f, 0.83f, 0.45f, 1f),
                FontStyle.Bold
            );

            CreateButton(
                "Friends",
                panel,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(16f, 14f),
                new Vector2(210f, 44f),
                "AMIGOS / EQUIPO",
                () => _navigation.Open(LobbyMenuId.Friends),
                false
            );

            CreateButton(
                "Settings",
                panel,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f),
                new Vector2(150f, 72f),
                "AJUSTES",
                () => _navigation.Open(LobbyMenuId.Settings),
                false
            );
        }

        private void CreateBottomLeft(Transform parent)
        {
            RectTransform panel = CreatePanel(
                "Bottom Left",
                parent,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(28f, 28f),
                new Vector2(590f, 142f),
                _panelColor,
                false
            );

            string[] names =
            {
                "PERSONAJE",
                "INVENTARIO",
                "ARMAS",
                "TIENDA"
            };

            LobbyMenuId[] menus =
            {
                LobbyMenuId.Character,
                LobbyMenuId.Inventory,
                LobbyMenuId.Weapons,
                LobbyMenuId.Store
            };

            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                CreateButton(
                    names[i],
                    panel,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(14f + i * 142f, 18f),
                    new Vector2(132f, 104f),
                    names[i],
                    () => _navigation.Open(menus[index]),
                    false
                );
            }
        }

        private void CreateBottomRight(Transform parent)
        {
            RectTransform panel = CreatePanel(
                "Bottom Right",
                parent,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-28f, 28f),
                new Vector2(500f, 166f),
                _panelColor,
                false
            );

            CreateLabel(
                "Map",
                panel,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-14f, -10f),
                new Vector2(470f, 34f),
                $"MAPA: {mapName.ToUpperInvariant()}",
                14,
                TextAnchor.MiddleRight,
                new Color(0.7f, 0.82f, 0.95f, 1f),
                FontStyle.Bold
            );

            Button modeButton = CreateButton(
                "Mode",
                panel,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(14f, 18f),
                new Vector2(205f, 88f),
                "SOLO",
                () => _navigation.Open(LobbyMenuId.PlayMode),
                false
            );
            _modeText = modeButton.GetComponentInChildren<Text>();

            CreateButton(
                "Play",
                panel,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-14f, 18f),
                new Vector2(250f, 88f),
                "JUGAR",
                StartBattleRoyale,
                true
            );
        }

        private void CreateMenuPanels(Transform parent)
        {
            CreateMenuPanel(
                LobbyMenuId.Character,
                "PERSONAJE",
                "Vista 3D del personaje. La estructura queda preparada para conectar ropa, skins, casco, mochila y equipamiento sin mezclar esa lógica con la navegación.",
                out _
            );

            CreateMenuPanel(
                LobbyMenuId.Inventory,
                "INVENTARIO",
                "Panel preparado para mostrar objetos, consumibles, equipamiento y la carga persistente del jugador.",
                out _
            );

            CreateMenuPanel(
                LobbyMenuId.Weapons,
                "ARMAS",
                "Panel preparado para selección de arma, accesorios, skin y vista de detalle. La cámara se acerca automáticamente al abrirlo.",
                out _
            );

            CreateMenuPanel(
                LobbyMenuId.Store,
                "TIENDA",
                "Cascarón de tienda listo para conectar catálogo, moneda, compras y cosméticos.",
                out _
            );

            CreateMenuPanel(
                LobbyMenuId.Events,
                "EVENTOS",
                "Cascarón de eventos temporales, recompensas y novedades del lobby.",
                out _
            );

            CreateMenuPanel(
                LobbyMenuId.Missions,
                "MISIONES",
                "Cascarón para misiones diarias, semanales y progreso de objetivos.",
                out _
            );

            CreateMenuPanel(
                LobbyMenuId.Friends,
                "AMIGOS / EQUIPO",
                "Preparado para lista de amigos, invitaciones, estado del grupo y composición Solo / Duo / Squad.",
                out _
            );

            CreateMenuPanel(
                LobbyMenuId.Settings,
                "AJUSTES",
                "Preparado para controles, sensibilidad, audio, gráficos y opciones de interfaz.",
                out _
            );

            GameObject modePanel = CreateMenuPanel(
                LobbyMenuId.PlayMode,
                "MODO DE JUEGO",
                "Selecciona el tamaño del equipo. Por ahora todos los modos reutilizan la escena Battle Royale local existente.",
                out RectTransform modeCard
            );

            CreateButton(
                "Solo",
                modeCard,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(34f, -72f),
                new Vector2(136f, 64f),
                "SOLO",
                () => SelectMode(LobbyMatchMode.Solo),
                false
            );

            CreateButton(
                "Duo",
                modeCard,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -72f),
                new Vector2(136f, 64f),
                "DUO",
                () => SelectMode(LobbyMatchMode.Duo),
                false
            );

            CreateButton(
                "Squad",
                modeCard,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-34f, -72f),
                new Vector2(136f, 64f),
                "SQUAD",
                () => SelectMode(LobbyMatchMode.Squad),
                false
            );

            modePanel.SetActive(false);
        }

        private GameObject CreateMenuPanel(
            LobbyMenuId menu,
            string title,
            string body,
            out RectTransform card
        )
        {
            GameObject overlay = new GameObject(
                $"Menu {menu}",
                typeof(RectTransform),
                typeof(Image)
            );
            overlay.transform.SetParent(_canvas.transform, false);

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            Stretch(overlayRect);

            Image overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.28f);
            overlayImage.raycastTarget = true;

            card = CreatePanel(
                "Drawer",
                overlay.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-34f, 0f),
                new Vector2(520f, 760f),
                new Color(0.02f, 0.035f, 0.06f, 0.98f),
                true
            );

            CreateLabel(
                "Title",
                card,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(28f, -28f),
                new Vector2(360f, 54f),
                title,
                26,
                TextAnchor.MiddleLeft,
                Color.white,
                FontStyle.Bold
            );

            CreateLabel(
                "Body",
                card,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f),
                new Vector2(450f, 300f),
                body,
                17,
                TextAnchor.UpperLeft,
                new Color(0.78f, 0.84f, 0.92f, 1f),
                FontStyle.Normal
            );

            CreateButton(
                "Back",
                card,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(92f, 48f),
                "VOLVER",
                _navigation.Back,
                false
            );

            _navigation.RegisterPanel(menu, overlay);
            return overlay;
        }

        private void SelectMode(LobbyMatchMode mode)
        {
            _selectedMode = mode;

            if (_modeText != null)
            {
                _modeText.text = mode.ToString().ToUpperInvariant();
            }

            _navigation.CloseAll();
        }

        private void StartBattleRoyale()
        {
            if (string.IsNullOrWhiteSpace(battleRoyaleSceneName))
            {
                Debug.LogError("No se configuró la escena Battle Royale del lobby.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(battleRoyaleSceneName))
            {
                Debug.LogError(
                    $"La escena '{battleRoyaleSceneName}' no está incluida en Build Settings."
                );
                return;
            }

            LobbySession.RequestMatch(_selectedMode, mapName);
            SceneManager.LoadScene(battleRoyaleSceneName);
        }

        private void HandleMenuChanged(LobbyMenuId menu)
        {
            if (_cameraController == null)
            {
                return;
            }

            LobbyCameraPreset preset = menu == LobbyMenuId.Weapons
                ? LobbyCameraPreset.UpperBody
                : LobbyCameraPreset.FullBody;

            _cameraController.SetPreset(preset);
        }

        private RectTransform CreatePanel(
            string objectName,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color,
            bool raycastTarget
        )
        {
            GameObject panelObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image)
            );
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            SetRect(rect, anchor, pivot, anchoredPosition, size);

            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;

            return rect;
        }

        private Button CreateButton(
            string objectName,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            string label,
            UnityAction action,
            bool primary
        )
        {
            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, anchor, pivot, anchoredPosition, size);

            Image image = buttonObject.GetComponent<Image>();
            image.color = primary ? _primaryColor : _buttonColor;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;

            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            Text text = CreateStretchLabel(
                "Text",
                buttonObject.transform,
                label,
                primary ? 25 : 16,
                TextAnchor.MiddleCenter,
                primary ? Color.white : new Color(0.9f, 0.94f, 1f, 1f),
                FontStyle.Bold
            );
            text.raycastTarget = false;

            return button;
        }

        private Text CreateLabel(
            string objectName,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle fontStyle
        )
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text)
            );
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetRect(rect, anchor, pivot, anchoredPosition, size);

            Text text = textObject.GetComponent<Text>();
            ConfigureText(text, value, fontSize, alignment, color, fontStyle);
            return text;
        }

        private Text CreateStretchLabel(
            string objectName,
            Transform parent,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle fontStyle
        )
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text)
            );
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            Stretch(rect);
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);

            Text text = textObject.GetComponent<Text>();
            ConfigureText(text, value, fontSize, alignment, color, fontStyle);
            return text;
        }

        private void ConfigureText(
            Text text,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle fontStyle
        )
        {
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size
        )
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
