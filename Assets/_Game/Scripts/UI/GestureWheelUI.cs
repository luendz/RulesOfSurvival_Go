using ROS.Game.Animation;
using ROS.Game.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class GestureWheelUI : MonoBehaviour
    {
        private const string ResourcePath = "EditorFirst/GestureWheelUI";

        private readonly struct GestureOption
        {
            public GestureOption(string displayName, string stateName)
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
        [SerializeField, Min(100f)] private float ringRadius = 250f;
        [SerializeField, Min(0f)] private float selectionDeadZone = 75f;

        [Header("Editable View")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform wheelOverlay;
        [SerializeField] private RectTransform wheelCenter;
        [SerializeField] private Text selectionLabel;
        [SerializeField] private RectTransform hintRoot;
        [SerializeField] private Image[] optionImages;
        [SerializeField] private Text[] optionTexts;

        private PlayerInputReader _input;
        private PlayerGestureController _gestureController;
        private RectTransform _internalHintRoot;
        private bool _isOpen;
        private bool _blockedInputByWheel;
        private int _selectedIndex = -1;
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;
        private float _nextHintRebindTime;

        private static readonly Color ItemColor =
            new Color(0.12f, 0.14f, 0.16f, 0.96f);
        private static readonly Color ItemSelectedColor =
            new Color(0.88f, 0.55f, 0.12f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (FindFirstObjectByType<GestureWheelUI>() != null)
                return;

            GameObject prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    "No existe el prefab editable EditorFirst/GestureWheelUI. " +
                    "Abre el proyecto en Unity para materializar los assets editor-first."
                );
                return;
            }

            Instantiate(prefab);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            BindViewFromHierarchy();
            SetWheelVisible(false);
            ResolveDependencies();
        }

        [ContextMenu("Rebind Gesture Wheel")]
        public void BindViewFromHierarchy()
        {
            canvas = FindNamed<Canvas>("GestureCanvas") ?? GetComponentInChildren<Canvas>(true);
            wheelOverlay = FindNamed<RectTransform>("WheelOverlay");
            wheelCenter = FindNamed<RectTransform>("WheelCenter");
            selectionLabel = FindNamed<Text>("SelectionLabel");
            _internalHintRoot = FindNamed<RectTransform>("GestureHint");
            BindHintTarget();

            optionImages = new Image[Options.Length];
            optionTexts = new Text[Options.Length];
            for (int i = 0; i < Options.Length; i++)
            {
                Transform option = FindTransform("Gesture_" + (i + 1));
                if (option == null)
                    continue;

                optionImages[i] = option.GetComponent<Image>();
                optionTexts[i] = FindNamedUnder<Text>(option, "Label");
            }
        }

        private void OnDisable()
        {
            if (_isOpen)
                CloseWheel();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _isOpen)
                CloseWheel();
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

            int keyboardSelection = ReadKeyboardSelection();
            if (keyboardSelection >= 0)
            {
                SelectAndPlay(keyboardSelection);
                return;
            }

            UpdateSelection();

            if (ConfirmPressedThisFrame() && _selectedIndex >= 0)
                SelectAndPlay(_selectedIndex);
        }

        public void ToggleWheel()
        {
            if (_isOpen)
                CloseWheel();
            else
                OpenWheel();
        }

        public void OpenWheel()
        {
            ResolveDependencies();

            if (_isOpen || _gestureController == null || !_gestureController.CanPlayGesture)
                return;

            if (_input != null && _input.UiBlocked)
                return;

            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _blockedInputByWheel = false;

            if (_input != null)
            {
                _blockedInputByWheel = !_input.UiBlocked;
                if (_blockedInputByWheel)
                    _input.SetUiBlocked(true);
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
            UpdateHintVisibility();
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
            UpdateHintVisibility();
        }

        private void SelectAndPlay(int index)
        {
            if (index < 0 || index >= Options.Length)
                return;

            GestureOption option = Options[index];
            PlayerGestureController controller = _gestureController;
            CloseWheel();

            if (controller != null)
                controller.TryPlayGesture(option.StateName, option.DisplayName);
        }

        private void ResolveDependencies()
        {
            if (_input == null)
            {
                PlayerInputReader[] readers = FindObjectsByType<PlayerInputReader>(
                    FindObjectsSortMode.None
                );

                foreach (PlayerInputReader reader in readers)
                {
                    if (reader != null && !reader.UsesExternalControl)
                    {
                        _input = reader;
                        break;
                    }
                }
            }

            if (_gestureController == null && _input != null)
            {
                _gestureController = _input.GetComponent<PlayerGestureController>();
                if (_gestureController == null)
                {
                    _gestureController =
                        _input.gameObject.AddComponent<PlayerGestureController>();
                }
            }
        }

        private void UpdateHintVisibility()
        {
            if (Time.unscaledTime >= _nextHintRebindTime)
            {
                _nextHintRebindTime = Time.unscaledTime + 0.5f;
                BindHintTarget();
            }

            if (hintRoot == null)
                return;

            bool shouldShow = _input != null &&
                              _gestureController != null &&
                              _gestureController.CanPlayGesture &&
                              !_input.UiBlocked &&
                              !_isOpen;
            if (hintRoot.gameObject.activeSelf != shouldShow)
                hintRoot.gameObject.SetActive(shouldShow);
        }

        private void BindHintTarget()
        {
            RectTransform externalHint = FindSceneHudHint();
            if (externalHint != null)
            {
                if (_internalHintRoot != null &&
                    _internalHintRoot != externalHint &&
                    _internalHintRoot.gameObject.activeSelf)
                {
                    _internalHintRoot.gameObject.SetActive(false);
                }

                hintRoot = externalHint;
                return;
            }

            if (_internalHintRoot == null)
                _internalHintRoot = FindNamed<RectTransform>("GestureHint");

            hintRoot = _internalHintRoot;
        }

        private static RectTransform FindSceneHudHint()
        {
            RectTransform[] all = Resources.FindObjectsOfTypeAll<RectTransform>();
            for (int i = 0; i < all.Length; i++)
            {
                RectTransform current = all[i];
                if (current == null || current.name != "GestureHintHUD")
                    continue;

                if (!current.gameObject.scene.IsValid() ||
                    !current.gameObject.scene.isLoaded)
                {
                    continue;
                }

                return current;
            }

            return null;
        }

        private void UpdateSelection()
        {
            Vector2 direction = ReadSelectionDirection();
            int newIndex = -1;

            if (direction.magnitude >= selectionDeadZone)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float clockwiseFromTop = Mathf.Repeat(90f - angle, 360f);
                float sector = 360f / Options.Length;
                newIndex = Mathf.FloorToInt(
                    (clockwiseFromTop + sector * 0.5f) / sector
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
                Vector2 stick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.08f)
                    return stick.normalized * ringRadius;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return Vector2.zero;

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return mouse.position.ReadValue() - center;
        }

        private void RefreshSelectionVisuals()
        {
            if (optionImages == null)
                return;

            for (int i = 0; i < optionImages.Length; i++)
            {
                bool selected = i == _selectedIndex;
                if (optionImages[i] != null)
                {
                    optionImages[i].color = selected
                        ? ItemSelectedColor
                        : ItemColor;
                }

                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                {
                    optionTexts[i].fontStyle = selected
                        ? FontStyle.Bold
                        : FontStyle.Normal;
                }
            }

            if (selectionLabel != null)
            {
                selectionLabel.text = _selectedIndex >= 0
                    ? Options[_selectedIndex].DisplayName
                    : "GESTOS";
            }
        }

        private T FindNamed<T>(string objectName) where T : Component
        {
            Transform target = FindTransform(objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private Transform FindTransform(string objectName)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName)
                    return all[i];
            }
            return null;
        }

        private static T FindNamedUnder<T>(Transform root, string objectName)
            where T : Component
        {
            T[] all = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName)
                    return all[i];
            }
            return null;
        }

        private static bool TogglePressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.gKey.wasPressedThisFrame) ||
                   (gamepad != null && gamepad.dpad.up.wasPressedThisFrame);
        }

        private static bool CancelPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                   (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
        }

        private static bool ConfirmPressedThisFrame()
        {
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;
            return (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                   (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
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

        private void SetWheelVisible(bool visible)
        {
            if (wheelOverlay != null)
                wheelOverlay.gameObject.SetActive(visible);
        }
    }
}
