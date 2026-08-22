using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Presenta una retícula distinta según la familia del arma equipada por
    /// el jugador local. Las armas normales usan '+'. Las escopetas usan
    /// '( )' y la apertura lateral acompaña suavemente al spread real.
    /// </summary>
    [DefaultExecutionOrder(850)]
    public sealed class WeaponCrosshairPresenter : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const string HudRootName = "ROS_HUD_Runtime";

        [Header("Shotgun Reticle")]
        [SerializeField] private float shotgunBaseHalfGap = 17f;
        [SerializeField] private float shotgunMaxHalfGap = 34f;
        [SerializeField] private float spreadForMaxGap = 7f;

        private PlayerInputReader _localInput;
        private WeaponEquipmentController _equipment;
        private RectTransform _root;
        private Text _normalCrosshair;
        private Text _shotgunLeft;
        private Text _shotgunRight;
        private GameObject _legacyCrosshair;
        private float _nextResolveTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
                return;

            if (FindFirstObjectByType<WeaponCrosshairPresenter>() != null)
                return;

            new GameObject("ROS_WeaponCrosshair")
                .AddComponent<WeaponCrosshairPresenter>();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.15f;
                ResolveLocalEquipment();
                EnsureVisuals();
            }

            RefreshCrosshair();
        }

        private void OnDisable()
        {
            if (_legacyCrosshair != null)
                _legacyCrosshair.SetActive(true);
        }

        private void ResolveLocalEquipment()
        {
            if (!IsValidLocalInput(_localInput))
            {
                _localInput = FindLocalPlayerInput();
                _equipment = null;
            }

            if (_localInput != null)
            {
                WeaponEquipmentController localEquipment =
                    _localInput.GetComponent<WeaponEquipmentController>();

                if (localEquipment == null)
                {
                    localEquipment =
                        _localInput.GetComponentInChildren<WeaponEquipmentController>(true);
                }

                if (localEquipment == null)
                {
                    localEquipment =
                        _localInput.GetComponentInParent<WeaponEquipmentController>();
                }

                _equipment = localEquipment;
                return;
            }

            // Fallback únicamente si no se pudo resolver un PlayerInputReader.
            // Se evita seleccionar objetos cuyo nombre parezca pertenecer a bots.
            WeaponEquipmentController[] equipments =
                Resources.FindObjectsOfTypeAll<WeaponEquipmentController>();

            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < equipments.Length; i++)
            {
                WeaponEquipmentController candidate = equipments[i];
                if (candidate == null ||
                    candidate.gameObject.scene != activeScene ||
                    candidate.gameObject.name.StartsWith("Bot_"))
                {
                    continue;
                }

                _equipment = candidate;
                break;
            }
        }

        private void EnsureVisuals()
        {
            if (_root != null)
                return;

            GameObject hud = GameObject.Find(HudRootName);
            if (hud == null)
                return;

            Transform canvas = hud.transform.Find("Canvas");
            if (canvas == null)
                return;

            Transform legacy = canvas.Find("Crosshair");
            if (legacy != null)
            {
                _legacyCrosshair = legacy.gameObject;
                _legacyCrosshair.SetActive(false);
            }

            GameObject rootObject = new GameObject("WeaponCrosshair");
            rootObject.transform.SetParent(canvas, false);
            rootObject.transform.SetAsLastSibling();

            _root = rootObject.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = new Vector2(140f, 72f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _normalCrosshair = CreateText(
                "NormalCrosshair",
                _root,
                "+",
                font,
                27
            );

            _shotgunLeft = CreateText(
                "ShotgunLeft",
                _root,
                "(",
                font,
                38
            );

            _shotgunRight = CreateText(
                "ShotgunRight",
                _root,
                ")",
                font,
                38
            );
        }

        private void RefreshCrosshair()
        {
            if (_root == null)
                return;

            WeaponController weapon =
                _equipment != null
                    ? _equipment.EquippedWeapon
                    : null;

            bool hasWeapon = weapon != null && weapon.Definition != null;
            _root.gameObject.SetActive(hasWeapon);

            if (!hasWeapon)
                return;

            bool isShotgun =
                weapon.Definition.family == WeaponFamily.Shotgun;

            if (_normalCrosshair != null)
                _normalCrosshair.gameObject.SetActive(!isShotgun);

            if (_shotgunLeft != null)
                _shotgunLeft.gameObject.SetActive(isShotgun);

            if (_shotgunRight != null)
                _shotgunRight.gameObject.SetActive(isShotgun);

            if (!isShotgun)
                return;

            float normalizedSpread = Mathf.InverseLerp(
                0f,
                Mathf.Max(0.01f, spreadForMaxGap),
                weapon.CurrentSpread
            );

            float halfGap = Mathf.Lerp(
                shotgunBaseHalfGap,
                shotgunMaxHalfGap,
                normalizedSpread
            );

            SetAnchoredX(_shotgunLeft.rectTransform, -halfGap);
            SetAnchoredX(_shotgunRight.rectTransform, halfGap);
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
                   input.gameObject.scene == SceneManager.GetActiveScene() &&
                   !input.UsesExternalControl;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            Font font,
            int fontSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(42f, 58f);
            rect.anchoredPosition = Vector2.zero;

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.98f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            return text;
        }

        private static void SetAnchoredX(RectTransform rect, float x)
        {
            if (rect == null)
                return;

            Vector2 position = rect.anchoredPosition;
            position.x = x;
            position.y = 0f;
            rect.anchoredPosition = position;
        }
    }
}
