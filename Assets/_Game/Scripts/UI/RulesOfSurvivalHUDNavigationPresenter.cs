using ROS.Game.CameraSystem;
using ROS.Game.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Controla los elementos de navegación del HUD reconstruido:
    /// brújula superior y orientación del jugador en el minimapa.
    /// Se ejecuta en LateUpdate para leer el yaw final calculado por
    /// ThirdPersonCamera durante el mismo frame.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class RulesOfSurvivalHUDNavigationPresenter : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const int TickCount = 15;
        private const float TickStepDegrees = 15f;
        private const float VisibleHalfDegrees = 92f;
        private const float PixelsPerDegree = 2.25f;

        private static readonly Color DarkTag =
            new Color(0.025f, 0.035f, 0.045f, 0.88f);

        private PlayerInputReader _localInput;
        private ThirdPersonCamera _thirdPersonCamera;
        private Camera _worldCamera;
        private Image _minimapArrow;
        private float _nextResolveTime;

        private RectTransform _compassStrip;
        private Text _legacyCompassText;
        private RectTransform _compassVisualRoot;
        private Text _centerMarker;
        private readonly CompassTick[] _ticks = new CompassTick[TickCount];

        private sealed class CompassTick
        {
            public RectTransform Root;
            public Image Line;
            public Text Label;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDNavigationPresenter>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_Navigation")
                .AddComponent<RulesOfSurvivalHUDNavigationPresenter>();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.25f;
                ResolveReferences();
            }

            float heading = ResolveHeading();
            UpdateCompass(heading);
            UpdateMinimapArrow(heading);
        }

        private void ResolveReferences()
        {
            if (!IsValidLocalInput(_localInput))
            {
                _localInput = FindLocalPlayerInput();
                _thirdPersonCamera = null;
            }

            if (_thirdPersonCamera == null)
            {
                ThirdPersonCamera[] cameras =
                    Resources.FindObjectsOfTypeAll<ThirdPersonCamera>();

                Scene activeScene = SceneManager.GetActiveScene();

                for (int i = 0; i < cameras.Length; i++)
                {
                    ThirdPersonCamera candidate = cameras[i];
                    if (candidate == null ||
                        candidate.gameObject.scene != activeScene)
                    {
                        continue;
                    }

                    if (_localInput != null &&
                        candidate.Target == _localInput.transform)
                    {
                        _thirdPersonCamera = candidate;
                        break;
                    }

                    if (_thirdPersonCamera == null &&
                        candidate.gameObject.activeInHierarchy)
                    {
                        _thirdPersonCamera = candidate;
                    }
                }
            }

            if (_worldCamera == null ||
                !_worldCamera.gameObject.scene.IsValid())
            {
                _worldCamera = Camera.main;
            }

            GameObject hud = GameObject.Find("ROS_HUD_Runtime");
            if (hud == null)
            {
                ClearHudReferences();
                return;
            }

            Transform waypoint = hud.transform.Find("Canvas/Waypoint");
            if (waypoint != null && waypoint.gameObject.activeSelf)
            {
                waypoint.gameObject.SetActive(false);
            }

            Transform compassTransform =
                hud.transform.Find("Canvas/CompassStrip");

            if (compassTransform != null)
            {
                _compassStrip = compassTransform as RectTransform;

                Transform legacyText =
                    compassTransform.Find("CompassText");
                if (legacyText != null)
                {
                    _legacyCompassText = legacyText.GetComponent<Text>();
                }

                EnsureCompassVisual();
            }

            if (_minimapArrow == null)
            {
                Transform arrow =
                    hud.transform.Find("Canvas/MinimapFrame/PlayerArrow");
                if (arrow != null)
                {
                    _minimapArrow = arrow.GetComponent<Image>();
                }
            }
        }

        private void ClearHudReferences()
        {
            _compassStrip = null;
            _legacyCompassText = null;
            _compassVisualRoot = null;
            _centerMarker = null;
            _minimapArrow = null;

            for (int i = 0; i < _ticks.Length; i++)
            {
                _ticks[i] = null;
            }
        }

        private void EnsureCompassVisual()
        {
            if (_compassStrip == null)
            {
                return;
            }

            // En la referencia el centro de la brújula no tiene una barra oscura
            // continua. Solo LEFT REAR y RIGHT REAR usan cajas oscuras.
            Image stripBackground = _compassStrip.GetComponent<Image>();
            if (stripBackground != null)
            {
                stripBackground.color = Color.clear;
            }

            _compassStrip.sizeDelta = new Vector2(600f, 52f);

            if (_legacyCompassText != null)
            {
                _legacyCompassText.enabled = false;
            }

            Transform existing = _compassStrip.Find("CompassFidelity");
            if (existing != null)
            {
                _compassVisualRoot = existing as RectTransform;
                CacheExistingCompassVisual();
                return;
            }

            GameObject rootObject = new GameObject("CompassFidelity");
            rootObject.transform.SetParent(_compassStrip, false);
            _compassVisualRoot = rootObject.AddComponent<RectTransform>();
            _compassVisualRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _compassVisualRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _compassVisualRoot.pivot = new Vector2(0.5f, 0.5f);
            _compassVisualRoot.anchoredPosition = Vector2.zero;
            _compassVisualRoot.sizeDelta = new Vector2(600f, 52f);

            CreateRearTag("LeftRearTag", "LEFT REAR", -258f);
            CreateRearTag("RightRearTag", "RIGHT REAR", 258f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < TickCount; i++)
            {
                GameObject tickObject = new GameObject($"Tick_{i:00}");
                tickObject.transform.SetParent(_compassVisualRoot, false);

                RectTransform tickRect = tickObject.AddComponent<RectTransform>();
                tickRect.anchorMin = new Vector2(0.5f, 0.5f);
                tickRect.anchorMax = new Vector2(0.5f, 0.5f);
                tickRect.pivot = new Vector2(0.5f, 0.5f);
                tickRect.sizeDelta = new Vector2(46f, 44f);

                GameObject lineObject = new GameObject("TickLine");
                lineObject.transform.SetParent(tickRect, false);
                RectTransform lineRect = lineObject.AddComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.pivot = new Vector2(0.5f, 0f);
                lineRect.anchoredPosition = new Vector2(0f, 8f);
                lineRect.sizeDelta = new Vector2(2f, 11f);
                Image line = lineObject.AddComponent<Image>();
                line.color = new Color(1f, 1f, 1f, 0.95f);

                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(tickRect, false);
                RectTransform labelRect = labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = new Vector2(0f, -6f);
                labelRect.sizeDelta = new Vector2(46f, 22f);

                Text label = labelObject.AddComponent<Text>();
                label.font = font;
                label.fontSize = 12;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;

                Outline outline = labelObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
                outline.effectDistance = new Vector2(1f, -1f);

                _ticks[i] = new CompassTick
                {
                    Root = tickRect,
                    Line = line,
                    Label = label
                };
            }

            GameObject markerObject = new GameObject("CenterMarker");
            markerObject.transform.SetParent(_compassVisualRoot, false);
            RectTransform markerRect = markerObject.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = new Vector2(0f, 19f);
            markerRect.sizeDelta = new Vector2(24f, 20f);

            _centerMarker = markerObject.AddComponent<Text>();
            _centerMarker.font = font;
            _centerMarker.text = "▼";
            _centerMarker.fontSize = 17;
            _centerMarker.fontStyle = FontStyle.Bold;
            _centerMarker.alignment = TextAnchor.MiddleCenter;
            _centerMarker.color = Color.white;

            Outline markerOutline = markerObject.AddComponent<Outline>();
            markerOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            markerOutline.effectDistance = new Vector2(1f, -1f);
        }

        private void CacheExistingCompassVisual()
        {
            if (_compassVisualRoot == null)
            {
                return;
            }

            for (int i = 0; i < TickCount; i++)
            {
                Transform tick =
                    _compassVisualRoot.Find($"Tick_{i:00}");
                if (tick == null)
                {
                    continue;
                }

                Transform line = tick.Find("TickLine");
                Transform label = tick.Find("Label");

                _ticks[i] = new CompassTick
                {
                    Root = tick as RectTransform,
                    Line = line != null ? line.GetComponent<Image>() : null,
                    Label = label != null ? label.GetComponent<Text>() : null
                };
            }

            Transform marker = _compassVisualRoot.Find("CenterMarker");
            if (marker != null)
            {
                _centerMarker = marker.GetComponent<Text>();
            }
        }

        private void CreateRearTag(string name, string label, float x)
        {
            GameObject tagObject = new GameObject(name);
            tagObject.transform.SetParent(_compassVisualRoot, false);

            RectTransform tagRect = tagObject.AddComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(0.5f, 0.5f);
            tagRect.anchorMax = new Vector2(0.5f, 0.5f);
            tagRect.pivot = new Vector2(0.5f, 0.5f);
            tagRect.anchoredPosition = new Vector2(x, -5f);
            tagRect.sizeDelta = new Vector2(82f, 24f);

            Image tagBackground = tagObject.AddComponent<Image>();
            tagBackground.color = DarkTag;

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(tagRect, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 1f);
            textRect.offsetMax = new Vector2(-4f, -1f);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 11;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private float ResolveHeading()
        {
            if (_thirdPersonCamera != null)
            {
                return Mathf.Repeat(_thirdPersonCamera.CameraYaw, 360f);
            }

            if (_worldCamera != null)
            {
                return Mathf.Repeat(
                    _worldCamera.transform.eulerAngles.y,
                    360f
                );
            }

            if (_localInput != null)
            {
                return Mathf.Repeat(
                    _localInput.transform.eulerAngles.y,
                    360f
                );
            }

            return 0f;
        }

        private void UpdateCompass(float heading)
        {
            if (_compassVisualRoot == null)
            {
                return;
            }

            float baseAngle =
                Mathf.Floor(heading / TickStepDegrees) * TickStepDegrees;

            int centerIndex = TickCount / 2;

            for (int i = 0; i < TickCount; i++)
            {
                CompassTick tick = _ticks[i];
                if (tick == null || tick.Root == null)
                {
                    continue;
                }

                float rawAngle =
                    baseAngle + (i - centerIndex) * TickStepDegrees;
                float normalizedAngle = Mathf.Repeat(rawAngle, 360f);
                float delta = Mathf.DeltaAngle(heading, normalizedAngle);

                bool visible = Mathf.Abs(delta) <= VisibleHalfDegrees;
                tick.Root.gameObject.SetActive(visible);

                if (!visible)
                {
                    continue;
                }

                tick.Root.anchoredPosition =
                    new Vector2(delta * PixelsPerDegree, -2f);

                int roundedAngle =
                    Mathf.RoundToInt(normalizedAngle) % 360;
                bool major = roundedAngle % 45 == 0;

                if (tick.Label != null)
                {
                    tick.Label.text = FormatCompassTick(roundedAngle);
                    tick.Label.fontSize = major ? 15 : 12;
                    tick.Label.fontStyle = FontStyle.Bold;
                    tick.Label.color = major
                        ? Color.white
                        : new Color(0.93f, 0.93f, 0.93f, 0.96f);
                }

                if (tick.Line != null)
                {
                    RectTransform lineRect = tick.Line.rectTransform;
                    lineRect.sizeDelta = new Vector2(
                        major ? 2.5f : 2f,
                        major ? 17f : 10f
                    );
                    tick.Line.color = major
                        ? Color.white
                        : new Color(1f, 1f, 1f, 0.88f);
                }
            }
        }

        private void UpdateMinimapArrow(float heading)
        {
            if (_minimapArrow == null)
            {
                return;
            }

            // El sprite triangular base apunta hacia abajo. 180 grados corrige
            // su orientación para que la punta coincida con el frente de cámara.
            float zRotation = Mathf.Repeat(180f - heading, 360f);
            _minimapArrow.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, zRotation);
        }

        private static string FormatCompassTick(float angle)
        {
            int value = Mathf.RoundToInt(Mathf.Repeat(angle, 360f));
            value %= 360;

            return value switch
            {
                0 => "N",
                45 => "NE",
                90 => "E",
                135 => "SE",
                180 => "S",
                225 => "SW",
                270 => "W",
                315 => "NW",
                _ => value.ToString()
            };
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
    }
}
