using ROS.Game.Animation;
using ROS.Game.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class GestureWheelUI : MonoBehaviour
    {
        private readonly struct GestureOption
        {
            public GestureOption(
                string displayName,
                string stateName
            )
            {
                DisplayName = displayName;
                StateName = stateName;
            }

            public string DisplayName { get; }
            public string StateName { get; }
        }

        private static readonly GestureOption[] Options =
        {
            new GestureOption("Dancing", "Gesture_Dancing"),
            new GestureOption("Fishing Cast", "Gesture_Fishing_Cast"),
            new GestureOption("Hip Hop Dancing", "Gesture_Hip_Hop_Dancing"),
            new GestureOption("Joyful Jump", "Gesture_Joyful_Jump"),
            new GestureOption("Opening", "Gesture_Opening"),
            new GestureOption("Rumba Dancing", "Gesture_Rumba_Dancing"),
            new GestureOption("Salute", "Gesture_Salute"),
            new GestureOption("Talking On Phone", "Gesture_Talking_On_Phone"),
            new GestureOption("Waving Gesture", "Gesture_Waving_Gesture")
        };

        [Header("Layout")]
        [SerializeField, Min(100f)]
        private float ringRadius = 250f;

        [SerializeField, Min(0f)]
        private float selectionDeadZone = 75f;

        private Canvas _canvas;
        private RectTransform _wheelOverlay;
        private RectTransform _wheelCenter;
        private Text _selectionLabel;
        private RectTransform _hintRoot;
        private Image[] _optionImages;
        private Text[] _optionTexts;

        private PlayerInputReader _input;
        private PlayerGestureController _gestureController;

        private bool _isOpen;
        private bool _blockedInputByWheel;
        private int _selectedIndex = -1;
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;
        private Font _font;

        private static readonly Color OverlayColor =
            new Color(0f, 0f, 0f, 0.55f);

        private static readonly Color WheelColor =
            new Color(0.055f, 0.065f, 0.075f, 0.94f);

        private static readonly Color ItemColor =
            new Color(0.12f, 0.14f, 0.16f, 0.96f);

        private static readonly Color ItemSelectedColor =
            new Color(0.88f, 0.55f, 0.12f, 1f);

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void EnsureRuntimeInstance()
        {
            if (FindFirstObjectByType<GestureWheelUI>() != null)
                return;

            GameObject instance =
                new GameObject("GestureWheelUI_Runtime");

            instance.AddComponent<GestureWheelUI>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

            BuildVisualTree();
            SetWheelVisible(false);
            ResolveDependencies();
        }

        private void OnDisable()
        {
            if (_isOpen)
            {
                CloseWheel();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _isOpen)
            {
                CloseWheel();
            }
        }

        private void Update()
        {
            ResolveDependencies();
            UpdateHintVisibility();

            if (TogglePressedThisFrame())
            {
                ToggleWheel();
                return;
            }

            if (!_isOpen)
                return;

            if (CancelPressedThisFrame())
            {
                CloseWheel();
                return;
            }

            int keyboardSelection =
                ReadKeyboardSelection();

            if (keyboardSelection >= 0)
            {
                SelectAndPlay(keyboardSelection);
                return;
            }

            UpdateSelection();

            if (ConfirmPressedThisFrame() &&
                _selectedIndex >= 0)
            {
                SelectAndPlay(_selectedIndex);
            }
        }

        public void ToggleWheel()
        {
            if (_isOpen)
            {
                CloseWheel();
            }
            else
            {
                OpenWheel();
            }
        }

        public void OpenWheel()
        {
            ResolveDependencies();

            if (_isOpen ||
                _gestureController == null ||
                !_gestureController.CanPlayGesture)
            {
                return;
            }

            if (_input != null && _input.UiBlocked)
                return;

            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            _blockedInputByWheel = false;

            if (_input != null)
            {
                _blockedInputByWheel = !_input.UiBlocked;

                if (_blockedInputByWheel)
                {
                    _input.SetUiBlocked(true);
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            _isOpen = true;
            _selectedIndex = -1;
            SetWheelVisible(true);
            RefreshSelectionVisuals();
        }

        public void CloseWheel()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            _selectedIndex = -1;
            SetWheelVisible(false);

            if (_input != null && _blockedInputByWheel)
            {
                _input.SetUiBlocked(false);
            }
            else if (_input == null)
            {
                Cursor.lockState = _previousCursorLock;
                Cursor.visible = _previousCursorVisible;
            }

            _blockedInputByWheel = false;
        }

        private void SelectAndPlay(int index)
        {
            if (index < 0 || index >= Options.Length)
                return;

            GestureOption option = Options[index];
            PlayerGestureController controller =
                _gestureController;

            CloseWheel();

            if (controller == null)
                return;

            controller.TryPlayGesture(
                option.StateName,
                option.DisplayName
            );
        }

        private void ResolveDependencies()
        {
            if (_input == null)
            {
                PlayerInputReader[] readers =
                    FindObjectsByType<PlayerInputReader>(
                        FindObjectsSortMode.None
                    );

                foreach (PlayerInputReader reader in readers)
                {
                    if (reader != null &&
                        !reader.UsesExternalControl)
                    {
                        _input = reader;
                        break;
                    }
                }
            }

            if (_gestureController == null &&
                _input != null)
            {
                _gestureController =
                    _input.GetComponent<PlayerGestureController>();

                if (_gestureController == null)
                {
                    _gestureController =
                        _input.gameObject.AddComponent<
                            PlayerGestureController
                        >();
                }
            }
        }

        private void UpdateHintVisibility()
        {
            if (_hintRoot == null)
                return;

            bool shouldShow =
                _input != null &&
                _gestureController != null &&
                !_isOpen;

            if (_hintRoot.gameObject.activeSelf != shouldShow)
            {
                _hintRoot.gameObject.SetActive(shouldShow);
            }
        }

        private void UpdateSelection()
        {
            Vector2 direction =
                ReadSelectionDirection();

            int newIndex = -1;

            if (direction.magnitude >= selectionDeadZone)
            {
                float angle =
                    Mathf.Atan2(direction.y, direction.x) *
                    Mathf.Rad2Deg;

                float clockwiseFromTop =
                    Mathf.Repeat(90f - angle, 360f);

                float sector =
                    360f / Options.Length;

                newIndex =
                    Mathf.FloorToInt(
                        (clockwiseFromTop + sector * 0.5f) /
                        sector
                    ) % Options.Length;
            }

            if (newIndex == _selectedIndex)
                return;

            _selectedIndex = newIndex;
            RefreshSelectionVisuals();
        }

        private Vector2 ReadSelectionDirection()
        {
            Gamepad gamepad = Gamepad.current;

            if (gamepad != null)
            {
                Vector2 stick =
                    gamepad.rightStick.ReadValue();

                if (stick.sqrMagnitude > 0.08f)
                {
                    return stick.normalized * ringRadius;
                }
            }

            Mouse mouse = Mouse.current;

            if (mouse == null)
                return Vector2.zero;

            Vector2 center = new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.5f
            );

            return mouse.position.ReadValue() - center;
        }

        private void RefreshSelectionVisuals()
        {
            if (_optionImages == null)
                return;

            for (int i = 0; i < _optionImages.Length; i++)
            {
                bool selected = i == _selectedIndex;

                _optionImages[i].color =
                    selected
                        ? ItemSelectedColor
                        : ItemColor;

                if (_optionTexts != null &&
                    i < _optionTexts.Length)
                {
                    _optionTexts[i].fontStyle =
                        selected
                            ? FontStyle.Bold
                            : FontStyle.Normal;
                }
            }

            if (_selectionLabel != null)
            {
                _selectionLabel.text =
                    _selectedIndex >= 0
                        ? Options[_selectedIndex].DisplayName
                        : "GESTOS";
            }
        }

        private static bool TogglePressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            return (keyboard != null &&
                    keyboard.gKey.wasPressedThisFrame) ||
                   (gamepad != null &&
                    gamepad.dpad.up.wasPressedThisFrame);
        }

        private static bool CancelPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            return (keyboard != null &&
                    keyboard.escapeKey.wasPressedThisFrame) ||
                   (gamepad != null &&
                    gamepad.buttonEast.wasPressedThisFrame);
        }

        private static bool ConfirmPressedThisFrame()
        {
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;

            return (mouse != null &&
                    mouse.leftButton.wasPressedThisFrame) ||
                   (gamepad != null &&
                    gamepad.buttonSouth.wasPressedThisFrame);
        }

        private static int ReadKeyboardSelection()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return -1;

            if (keyboard.digit1Key.wasPressedThisFrame) return 0;
            if (keyboard.digit2Key.wasPressedThisFrame) return 1;
            if (keyboard.digit3Key.wasPressedThisFrame) return 2;
            if (keyboard.digit4Key.wasPressedThisFrame) return 3;
            if (keyboard.digit5Key.wasPressedThisFrame) return 4;
            if (keyboard.digit6Key.wasPressedThisFrame) return 5;
            if (keyboard.digit7Key.wasPressedThisFrame) return 6;
            if (keyboard.digit8Key.wasPressedThisFrame) return 7;
            if (keyboard.digit9Key.wasPressedThisFrame) return 8;

            return -1;
        }

        private void BuildVisualTree()
        {
            GameObject canvasObject = new GameObject(
                "GestureCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            canvasObject.transform.SetParent(
                transform,
                false
            );

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _wheelOverlay = CreateRect(
                "WheelOverlay",
                canvasObject.transform
            );
            StretchFullScreen(_wheelOverlay);

            Image overlayImage =
                _wheelOverlay.gameObject.AddComponent<Image>();
            overlayImage.color = OverlayColor;
            overlayImage.raycastTarget = false;

            _wheelCenter = CreateRect(
                "WheelCenter",
                _wheelOverlay
            );
            _wheelCenter.anchorMin =
                new Vector2(0.5f, 0.5f);
            _wheelCenter.anchorMax =
                new Vector2(0.5f, 0.5f);
            _wheelCenter.pivot =
                new Vector2(0.5f, 0.5f);
            _wheelCenter.sizeDelta =
                new Vector2(720f, 720f);
            _wheelCenter.anchoredPosition =
                Vector2.zero;

            Image wheelBackground =
                _wheelCenter.gameObject.AddComponent<Image>();
            wheelBackground.color = WheelColor;
            wheelBackground.raycastTarget = false;

            RectTransform centerLabel = CreateRect(
                "SelectionLabel",
                _wheelCenter
            );
            centerLabel.anchorMin =
                new Vector2(0.5f, 0.5f);
            centerLabel.anchorMax =
                new Vector2(0.5f, 0.5f);
            centerLabel.sizeDelta =
                new Vector2(250f, 90f);
            centerLabel.anchoredPosition =
                Vector2.zero;

            _selectionLabel = CreateText(
                centerLabel,
                "GESTOS",
                28,
                TextAnchor.MiddleCenter
            );
            _selectionLabel.fontStyle = FontStyle.Bold;

            _optionImages =
                new Image[Options.Length];
            _optionTexts =
                new Text[Options.Length];

            float sector = 360f / Options.Length;

            for (int i = 0; i < Options.Length; i++)
            {
                float angle =
                    90f - sector * i;
                float radians =
                    angle * Mathf.Deg2Rad;

                RectTransform item = CreateRect(
                    $"Gesture_{i + 1}",
                    _wheelCenter
                );

                item.anchorMin =
                    new Vector2(0.5f, 0.5f);
                item.anchorMax =
                    new Vector2(0.5f, 0.5f);
                item.pivot =
                    new Vector2(0.5f, 0.5f);
                item.sizeDelta =
                    new Vector2(170f, 62f);
                item.anchoredPosition =
                    new Vector2(
                        Mathf.Cos(radians),
                        Mathf.Sin(radians)
                    ) * ringRadius;

                Image image =
                    item.gameObject.AddComponent<Image>();
                image.color = ItemColor;
                image.raycastTarget = false;
                _optionImages[i] = image;

                RectTransform label = CreateRect(
                    "Label",
                    item
                );
                StretchFullScreen(label);
                label.offsetMin = new Vector2(8f, 4f);
                label.offsetMax = new Vector2(-8f, -4f);

                Text text = CreateText(
                    label,
                    $"{i + 1}. {Options[i].DisplayName}",
                    17,
                    TextAnchor.MiddleCenter
                );
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 11;
                text.resizeTextMaxSize = 17;
                _optionTexts[i] = text;
            }

            RectTransform helpLabel = CreateRect(
                "Help",
                _wheelCenter
            );
            helpLabel.anchorMin =
                new Vector2(0.5f, 0f);
            helpLabel.anchorMax =
                new Vector2(0.5f, 0f);
            helpLabel.pivot =
                new Vector2(0.5f, 0f);
            helpLabel.sizeDelta =
                new Vector2(520f, 50f);
            helpLabel.anchoredPosition =
                new Vector2(0f, 18f);

            CreateText(
                helpLabel,
                "Mueve el mouse y haz clic · 1-9 acceso directo · G/Esc cerrar",
                16,
                TextAnchor.MiddleCenter
            );

            _hintRoot = CreateRect(
                "GestureHint",
                canvasObject.transform
            );
            _hintRoot.anchorMin =
                new Vector2(0.5f, 0f);
            _hintRoot.anchorMax =
                new Vector2(0.5f, 0f);
            _hintRoot.pivot =
                new Vector2(0f, 0f);
            _hintRoot.sizeDelta =
                new Vector2(180f, 48f);
            _hintRoot.anchoredPosition =
                new Vector2(245f, 34f);

            Image hintImage =
                _hintRoot.gameObject.AddComponent<Image>();
            hintImage.color = ItemColor;
            hintImage.raycastTarget = false;

            RectTransform hintTextRect = CreateRect(
                "HintText",
                _hintRoot
            );
            StretchFullScreen(hintTextRect);

            Text hintText = CreateText(
                hintTextRect,
                "G   GESTOS",
                18,
                TextAnchor.MiddleCenter
            );
            hintText.fontStyle = FontStyle.Bold;
        }

        private Text CreateText(
            RectTransform rect,
            string value,
            int fontSize,
            TextAnchor alignment
        )
        {
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Overflow;
            return text;
        }

        private static RectTransform CreateRect(
            string objectName,
            Transform parent
        )
        {
            GameObject target = new GameObject(
                objectName,
                typeof(RectTransform)
            );

            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }

        private static void StretchFullScreen(
            RectTransform rect
        )
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetWheelVisible(bool visible)
        {
            if (_wheelOverlay != null)
            {
                _wheelOverlay.gameObject.SetActive(visible);
            }
        }
    }
}
