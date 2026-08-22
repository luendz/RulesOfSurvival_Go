using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ROS.Game.Lobby
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class LobbySettingsController : MonoBehaviour
    {
        private const string GeneralTab = "general";
        private const string LightingTab = "lighting";

        private const string AmbientKey = "ROS.Lobby.Lighting.Ambient";
        private const string KeyLightKey = "ROS.Lobby.Lighting.Key";
        private const string FaceFillKey = "ROS.Lobby.Lighting.FaceFill";
        private const string CoolFillKey = "ROS.Lobby.Lighting.CoolFill";
        private const string RimKey = "ROS.Lobby.Lighting.Rim";
        private const string BackgroundKey = "ROS.Lobby.Lighting.Background";
        private const string ExposureKey = "ROS.Lobby.Lighting.Exposure";
        private const string ContrastKey = "ROS.Lobby.Lighting.Contrast";
        private const string SaturationKey = "ROS.Lobby.Lighting.Saturation";

        private const float DefaultAmbient = 1.15f;
        private const float DefaultKey = 1.55f;
        private const float DefaultFaceFill = 3.60f;
        private const float DefaultCoolFill = 2.50f;
        private const float DefaultRim = 3.40f;
        private const float DefaultBackground = 0.90f;
        private const float DefaultExposure = 0.04f;
        private const float DefaultContrast = 1.04f;
        private const float DefaultSaturation = 1.04f;

        private readonly Dictionary<string, GameObject> _tabContents = new();
        private readonly Dictionary<string, Image> _tabImages = new();
        private readonly Dictionary<string, Slider> _sliders = new();
        private readonly Dictionary<string, Text> _valueTexts = new();

        private Font _font;
        private LobbyLightingController _lighting;
        private LobbyBackgroundController _background;
        private LobbyColorGradeEffect _colorGrade;
        private RectTransform _drawer;

        private readonly Color _panelColor = new Color(0.026f, 0.045f, 0.072f, 0.98f);
        private readonly Color _buttonColor = new Color(0.075f, 0.11f, 0.16f, 1f);
        private readonly Color _primaryColor = new Color(0.92f, 0.56f, 0.08f, 1f);
        private readonly Color _mutedTextColor = new Color(0.72f, 0.80f, 0.90f, 1f);

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ResolveControllers();
            ApplySavedSettings();

            _drawer = FindSettingsDrawer();
            if (_drawer == null)
            {
                Debug.LogWarning("LobbySettingsController no encontró el panel de AJUSTES del lobby.");
                return;
            }

            BuildTabbedSettings();
            ShowTab(GeneralTab);
        }

        private void OnDisable()
        {
            PlayerPrefs.Save();
        }

        private void ResolveControllers()
        {
            _lighting = FindFirstObjectByType<LobbyLightingController>();
            _background = FindFirstObjectByType<LobbyBackgroundController>();

            Camera camera = Camera.main;
            if (camera != null)
            {
                _colorGrade = camera.GetComponent<LobbyColorGradeEffect>();
                if (_colorGrade == null)
                {
                    _colorGrade = camera.gameObject.AddComponent<LobbyColorGradeEffect>();
                }
            }
        }

        private void ApplySavedSettings()
        {
            if (_lighting != null)
            {
                _lighting.SetAmbientIntensity(
                    PlayerPrefs.GetFloat(AmbientKey, _lighting.AmbientIntensity)
                );
                _lighting.SetKeyIntensity(
                    PlayerPrefs.GetFloat(KeyLightKey, _lighting.KeyIntensity)
                );
                _lighting.SetFaceFillIntensity(
                    PlayerPrefs.GetFloat(FaceFillKey, _lighting.FaceFillIntensity)
                );
                _lighting.SetCoolFillIntensity(
                    PlayerPrefs.GetFloat(CoolFillKey, _lighting.CoolFillIntensity)
                );
                _lighting.SetRimIntensity(
                    PlayerPrefs.GetFloat(RimKey, _lighting.RimIntensity)
                );
            }

            if (_background != null)
            {
                _background.SetBackgroundBrightness(
                    PlayerPrefs.GetFloat(BackgroundKey, _background.BackgroundBrightness)
                );
            }

            if (_colorGrade != null)
            {
                _colorGrade.SetExposure(
                    PlayerPrefs.GetFloat(ExposureKey, _colorGrade.Exposure)
                );
                _colorGrade.SetContrast(
                    PlayerPrefs.GetFloat(ContrastKey, _colorGrade.Contrast)
                );
                _colorGrade.SetSaturation(
                    PlayerPrefs.GetFloat(SaturationKey, _colorGrade.Saturation)
                );
            }
        }

        private void BuildTabbedSettings()
        {
            Transform oldRoot = _drawer.Find("Settings Tabs Root");
            if (oldRoot != null)
            {
                Destroy(oldRoot.gameObject);
            }

            Transform body = _drawer.Find("Body");
            if (body != null)
            {
                body.gameObject.SetActive(false);
            }

            RectTransform root = CreateRect(
                "Settings Tabs Root",
                _drawer,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0f),
                new Vector2(520f, 760f)
            );

            RegisterTab(
                root,
                GeneralTab,
                "GENERAL",
                0,
                BuildGeneralTab
            );

            RegisterTab(
                root,
                LightingTab,
                "ILUMINACIÓN",
                1,
                BuildLightingTab
            );
        }

        private void RegisterTab(
            RectTransform root,
            string id,
            string label,
            int index,
            Action<RectTransform> builder
        )
        {
            float width = id == LightingTab ? 178f : 142f;
            float x = index == 0 ? 28f : 180f;

            Button button = CreateButton(
                $"Tab {label}",
                root,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(x, -104f),
                new Vector2(width, 44f),
                label,
                () => ShowTab(id)
            );

            _tabImages[id] = button.GetComponent<Image>();

            RectTransform content = CreateRect(
                $"Tab Content {label}",
                root,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(28f, -164f),
                new Vector2(464f, 500f)
            );

            Image background = content.gameObject.AddComponent<Image>();
            background.color = new Color(_panelColor.r, _panelColor.g, _panelColor.b, 0.52f);
            background.raycastTarget = false;

            builder(content);
            _tabContents[id] = content.gameObject;
        }

        private void BuildGeneralTab(RectTransform content)
        {
            CreateText(
                "General Title",
                content,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -22f),
                new Vector2(420f, 34f),
                "AJUSTES GENERALES",
                18,
                TextAnchor.MiddleLeft,
                Color.white,
                FontStyle.Bold
            );

            CreateText(
                "General Description",
                content,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -72f),
                new Vector2(420f, 180f),
                "Este panel queda preparado para crecer sin cambiar la navegación del lobby.\n\nPróximas categorías sugeridas:\n• Audio\n• Gráficos\n• Controles\n• Interfaz\n• Gameplay",
                15,
                TextAnchor.UpperLeft,
                _mutedTextColor,
                FontStyle.Normal
            );

            CreateText(
                "General Hint",
                content,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(22f, 22f),
                new Vector2(420f, 80f),
                "Selecciona ILUMINACIÓN para modificar la presentación del personaje y del fondo en tiempo real.",
                14,
                TextAnchor.LowerLeft,
                new Color(0.86f, 0.72f, 0.42f, 1f),
                FontStyle.Normal
            );
        }

        private void BuildLightingTab(RectTransform content)
        {
            int row = 0;

            CreateLightingSlider(
                content,
                AmbientKey,
                "Ambiente",
                row++,
                0f,
                2f,
                _lighting != null ? _lighting.AmbientIntensity : DefaultAmbient,
                value =>
                {
                    _lighting?.SetAmbientIntensity(value);
                    PlayerPrefs.SetFloat(AmbientKey, value);
                }
            );

            CreateLightingSlider(
                content,
                KeyLightKey,
                "Luz principal",
                row++,
                0f,
                4f,
                _lighting != null ? _lighting.KeyIntensity : DefaultKey,
                value =>
                {
                    _lighting?.SetKeyIntensity(value);
                    PlayerPrefs.SetFloat(KeyLightKey, value);
                }
            );

            CreateLightingSlider(
                content,
                FaceFillKey,
                "Luz de rostro",
                row++,
                0f,
                8f,
                _lighting != null ? _lighting.FaceFillIntensity : DefaultFaceFill,
                value =>
                {
                    _lighting?.SetFaceFillIntensity(value);
                    PlayerPrefs.SetFloat(FaceFillKey, value);
                }
            );

            CreateLightingSlider(
                content,
                CoolFillKey,
                "Relleno frío",
                row++,
                0f,
                8f,
                _lighting != null ? _lighting.CoolFillIntensity : DefaultCoolFill,
                value =>
                {
                    _lighting?.SetCoolFillIntensity(value);
                    PlayerPrefs.SetFloat(CoolFillKey, value);
                }
            );

            CreateLightingSlider(
                content,
                RimKey,
                "Contraluz",
                row++,
                0f,
                8f,
                _lighting != null ? _lighting.RimIntensity : DefaultRim,
                value =>
                {
                    _lighting?.SetRimIntensity(value);
                    PlayerPrefs.SetFloat(RimKey, value);
                }
            );

            CreateLightingSlider(
                content,
                BackgroundKey,
                "Brillo fondo",
                row++,
                0.5f,
                1f,
                _background != null ? _background.BackgroundBrightness : DefaultBackground,
                value =>
                {
                    _background?.SetBackgroundBrightness(value);
                    PlayerPrefs.SetFloat(BackgroundKey, value);
                }
            );

            CreateLightingSlider(
                content,
                ExposureKey,
                "Exposición",
                row++,
                -0.5f,
                0.5f,
                _colorGrade != null ? _colorGrade.Exposure : DefaultExposure,
                value =>
                {
                    _colorGrade?.SetExposure(value);
                    PlayerPrefs.SetFloat(ExposureKey, value);
                }
            );

            CreateLightingSlider(
                content,
                ContrastKey,
                "Contraste",
                row++,
                0.7f,
                1.3f,
                _colorGrade != null ? _colorGrade.Contrast : DefaultContrast,
                value =>
                {
                    _colorGrade?.SetContrast(value);
                    PlayerPrefs.SetFloat(ContrastKey, value);
                }
            );

            CreateLightingSlider(
                content,
                SaturationKey,
                "Saturación",
                row,
                0.7f,
                1.3f,
                _colorGrade != null ? _colorGrade.Saturation : DefaultSaturation,
                value =>
                {
                    _colorGrade?.SetSaturation(value);
                    PlayerPrefs.SetFloat(SaturationKey, value);
                }
            );

            CreateButton(
                "Reset Lighting",
                content,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-18f, 16f),
                new Vector2(176f, 38f),
                "RESTABLECER",
                ResetLightingSettings
            );
        }

        private void CreateLightingSlider(
            RectTransform parent,
            string key,
            string label,
            int row,
            float min,
            float max,
            float value,
            UnityAction<float> onChanged
        )
        {
            RectTransform rowRect = CreateRect(
                $"Row {label}",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(14f, -14f - row * 50f),
                new Vector2(436f, 44f)
            );

            CreateText(
                $"Label {label}",
                rowRect,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(142f, 30f),
                label,
                13,
                TextAnchor.MiddleLeft,
                Color.white,
                FontStyle.Normal
            );

            Slider slider = CreateSlider(
                $"Slider {label}",
                rowRect,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(146f, 0f),
                new Vector2(205f, 24f),
                min,
                max,
                value
            );

            Text valueText = CreateText(
                $"Value {label}",
                rowRect,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(76f, 30f),
                FormatValue(value),
                12,
                TextAnchor.MiddleRight,
                new Color(0.95f, 0.78f, 0.42f, 1f),
                FontStyle.Bold
            );

            slider.onValueChanged.AddListener(current =>
            {
                valueText.text = FormatValue(current);
                onChanged?.Invoke(current);
            });

            _sliders[key] = slider;
            _valueTexts[key] = valueText;
        }

        private void ResetLightingSettings()
        {
            SetSliderValue(AmbientKey, DefaultAmbient);
            SetSliderValue(KeyLightKey, DefaultKey);
            SetSliderValue(FaceFillKey, DefaultFaceFill);
            SetSliderValue(CoolFillKey, DefaultCoolFill);
            SetSliderValue(RimKey, DefaultRim);
            SetSliderValue(BackgroundKey, DefaultBackground);
            SetSliderValue(ExposureKey, DefaultExposure);
            SetSliderValue(ContrastKey, DefaultContrast);
            SetSliderValue(SaturationKey, DefaultSaturation);
            PlayerPrefs.Save();
        }

        private void SetSliderValue(string key, float value)
        {
            if (_sliders.TryGetValue(key, out Slider slider))
            {
                slider.value = value;
            }
        }

        private void ShowTab(string id)
        {
            foreach (KeyValuePair<string, GameObject> pair in _tabContents)
            {
                pair.Value.SetActive(pair.Key == id);
            }

            foreach (KeyValuePair<string, Image> pair in _tabImages)
            {
                pair.Value.color = pair.Key == id ? _primaryColor : _buttonColor;
            }
        }

        private RectTransform FindSettingsDrawer()
        {
            RectTransform[] rects = FindObjectsByType<RectTransform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (RectTransform rect in rects)
            {
                if (
                    rect.name == "Drawer"
                    && rect.parent != null
                    && rect.parent.name == "Menu Settings"
                )
                {
                    return rect;
                }
            }

            return null;
        }

        private RectTransform CreateRect(
            string objectName,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size
        )
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);

            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
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
            UnityAction action
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
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = _buttonColor;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            button.colors = colors;

            CreateText(
                "Label",
                buttonObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                size,
                label,
                13,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold
            );

            return button;
        }

        private Slider CreateSlider(
            string objectName,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            float min,
            float max,
            float value
        )
        {
            GameObject sliderObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Slider)
            );
            sliderObject.transform.SetParent(parent, false);

            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            RectTransform background = CreateRect(
                "Background",
                sliderObject.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(size.x, 6f)
            );
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.10f, 0.14f, 0.19f, 1f);

            RectTransform fillArea = CreateRect(
                "Fill Area",
                sliderObject.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(size.x - 10f, 6f)
            );

            RectTransform fill = CreateRect(
                "Fill",
                fillArea,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(size.x - 10f, 6f)
            );
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = _primaryColor;

            RectTransform handleArea = CreateRect(
                "Handle Slide Area",
                sliderObject.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(size.x - 12f, 24f)
            );

            RectTransform handle = CreateRect(
                "Handle",
                handleArea,
                new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(16f, 22f)
            );
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));

            return slider;
        }

        private Text CreateText(
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
            FontStyle style
        )
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text)
            );
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.00");
        }
    }
}
