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

        private PlayerInputReader _localInput;
        private ThirdPersonCamera _thirdPersonCamera;
        private Camera _worldCamera;
        private Text _compassText;
        private Image _minimapArrow;
        private float _nextResolveTime;

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
                _compassText = null;
                _minimapArrow = null;
                return;
            }

            // El texto amarillo de distancia (por ejemplo "1068m") era un
            // waypoint provisional fijo creado por el primer prototipo del HUD.
            // No está conectado a ningún sistema de marcadores, por eso se oculta.
            Transform waypoint = hud.transform.Find("Canvas/Waypoint");
            if (waypoint != null && waypoint.gameObject.activeSelf)
            {
                waypoint.gameObject.SetActive(false);
            }

            if (_compassText == null)
            {
                Transform compass =
                    hud.transform.Find("Canvas/CompassStrip/CompassText");
                if (compass != null)
                {
                    _compassText = compass.GetComponent<Text>();
                }
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
            if (_compassText == null)
            {
                return;
            }

            // Ventana de 120 grados alrededor del centro, similar a ROS.
            string left60 = FormatCompassTick(heading - 60f);
            string left45 = FormatCompassTick(heading - 45f);
            string left30 = FormatCompassTick(heading - 30f);
            string left15 = FormatCompassTick(heading - 15f);
            string center = FormatCompassTick(heading);
            string right15 = FormatCompassTick(heading + 15f);
            string right30 = FormatCompassTick(heading + 30f);
            string right45 = FormatCompassTick(heading + 45f);
            string right60 = FormatCompassTick(heading + 60f);

            _compassText.text =
                $"LEFT REAR   {left60}  {left45}  {left30}  {left15}   " +
                $"{center}   {right15}  {right30}  {right45}  {right60}   RIGHT REAR";
        }

        private void UpdateMinimapArrow(float heading)
        {
            if (_minimapArrow == null)
            {
                return;
            }

            // El sprite triangular generado por RulesOfSurvivalHUD tiene su punta
            // base orientada hacia abajo. El +180 lógico corrige esa orientación.
            // El mapa permanece con norte arriba, por eso el yaw de cámara se
            // convierte directamente a una rotación clockwise en pantalla.
            float zRotation = Mathf.Repeat(180f - heading, 360f);
            _minimapArrow.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, zRotation);
        }

        private static string FormatCompassTick(float angle)
        {
            int value = Mathf.RoundToInt(
                Mathf.Repeat(angle, 360f) / 15f
            ) * 15;

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
