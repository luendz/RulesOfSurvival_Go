using System.Collections.Generic;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Controla una reticula fisica incluida dentro del prefab editable del HUD.
    /// No crea ningun elemento visual en tiempo de ejecucion.
    /// </summary>
    [DefaultExecutionOrder(850)]
    public sealed class WeaponCrosshairPresenter : MonoBehaviour
    {
        [Header("Normal Reticle")]
        [SerializeField] private float normalArmLength = 8f;
        [SerializeField] private float normalThickness = 2f;
        [SerializeField] private float normalMinGap = 3f;
        [SerializeField] private float normalMaxGap = 45f;
        [SerializeField] private float normalPixelsPerSpreadDegree = 10f;
        [SerializeField] private float normalSmoothSpeed = 14f;

        [Header("Shotgun Reticle")]
        [SerializeField] private float shotgunBaseHalfGap = 17f;
        [SerializeField] private float shotgunMaxHalfGap = 34f;
        [SerializeField] private float spreadForMaxGap = 7f;

        [Header("Reload Timer")]
        [Tooltip("Velocidad del parpadeo del contador de recarga. El texto permanece exactamente en el centro de la mirilla.")]
        [Min(0f)]
        [SerializeField] private float reloadBlinkSpeed = 4.5f;
        [Range(0f, 1f)]
        [SerializeField] private float reloadMinimumAlpha = 0.42f;

        [Header("Editable View")]
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform normalRoot;
        [SerializeField] private RectTransform normalLeft;
        [SerializeField] private RectTransform normalRight;
        [SerializeField] private RectTransform normalUp;
        [SerializeField] private RectTransform normalDown;
        [SerializeField] private Text shotgunLeft;
        [SerializeField] private Text shotgunRight;
        [SerializeField] private Text reloadTimerText;

        private readonly HashSet<GameObject> _suppressedCrosshairs =
            new HashSet<GameObject>();

        private PlayerInputReader _localInput;
        private WeaponEquipmentController _equipment;
        private float _currentNormalGap;
        private float _nextResolveTime;

        private WeaponController _trackedReloadWeapon;
        private float _reloadObservedAt;
        private float _reloadObservedDuration;

        private void Awake()
        {
            BindViewFromHierarchy();
            _currentNormalGap = Mathf.Max(0f, normalMinGap);
            ApplyNormalGap(_currentNormalGap);
            HideReloadTimer();
        }

        [ContextMenu("Rebind Weapon Crosshair")]
        public void BindViewFromHierarchy()
        {
            root = FindNamed<RectTransform>("WeaponCrosshair");
            normalRoot = FindNamed<RectTransform>("NormalCrosshairRoot");
            normalLeft = FindNamed<RectTransform>("NormalLeft");
            normalRight = FindNamed<RectTransform>("NormalRight");
            normalUp = FindNamed<RectTransform>("NormalUp");
            normalDown = FindNamed<RectTransform>("NormalDown");
            shotgunLeft = FindNamed<Text>("ShotgunLeft");
            shotgunRight = FindNamed<Text>("ShotgunRight");
            reloadTimerText = FindNamed<Text>("ReloadTimerText");
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.15f;
                ResolveLocalEquipment();
                if (root == null || reloadTimerText == null)
                    BindViewFromHierarchy();
            }

            RefreshCrosshair();
            SuppressCompetingCrosshairs();
        }

        private void OnDisable()
        {
            HideReloadTimer();
            RestoreSuppressedCrosshairs();
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

        private void RefreshCrosshair()
        {
            if (root == null)
                return;

            WeaponController weapon =
                _equipment != null ? _equipment.EquippedWeapon : null;

            bool hasWeapon = weapon != null && weapon.Definition != null;
            root.gameObject.SetActive(hasWeapon);
            if (!hasWeapon)
            {
                HideReloadTimer();
                return;
            }

            bool isShotgun = weapon.Definition.family == WeaponFamily.Shotgun;

            if (normalRoot != null)
                normalRoot.gameObject.SetActive(!isShotgun);
            if (shotgunLeft != null)
                shotgunLeft.gameObject.SetActive(isShotgun);
            if (shotgunRight != null)
                shotgunRight.gameObject.SetActive(isShotgun);

            if (isShotgun)
                UpdateShotgunCrosshair(weapon);
            else
                UpdateNormalCrosshair(weapon);

            UpdateReloadTimer(weapon);
        }

        private void UpdateReloadTimer(WeaponController weapon)
        {
            if (reloadTimerText == null || weapon == null)
            {
                HideReloadTimer();
                return;
            }

            if (!weapon.IsReloading || weapon.ActiveReloadDuration <= 0f)
            {
                HideReloadTimer();
                return;
            }

            if (_trackedReloadWeapon != weapon ||
                !reloadTimerText.gameObject.activeSelf)
            {
                _trackedReloadWeapon = weapon;
                _reloadObservedAt = Time.time;
                _reloadObservedDuration = Mathf.Max(
                    0.01f,
                    weapon.ActiveReloadDuration
                );
            }

            float elapsed = Mathf.Max(0f, Time.time - _reloadObservedAt);
            float remaining = Mathf.Max(0f, _reloadObservedDuration - elapsed);

            // Una decimal da lectura clara sin saturar el centro de la reticula.
            reloadTimerText.text = remaining.ToString("0.0");

            float pulse = 0.5f + 0.5f * Mathf.Sin(
                Time.unscaledTime * Mathf.Max(0f, reloadBlinkSpeed) * Mathf.PI * 2f
            );
            float alpha = Mathf.Lerp(
                Mathf.Clamp01(reloadMinimumAlpha),
                1f,
                pulse
            );

            Color color = reloadTimerText.color;
            color.a = alpha;
            reloadTimerText.color = color;

            if (!reloadTimerText.gameObject.activeSelf)
                reloadTimerText.gameObject.SetActive(true);
        }

        private void HideReloadTimer()
        {
            _trackedReloadWeapon = null;
            _reloadObservedAt = 0f;
            _reloadObservedDuration = 0f;

            if (reloadTimerText == null)
                return;

            if (reloadTimerText.gameObject.activeSelf)
                reloadTimerText.gameObject.SetActive(false);
        }

        private void UpdateNormalCrosshair(WeaponController weapon)
        {
            if (weapon == null || normalRoot == null)
                return;

            float targetGap = Mathf.Clamp(
                normalMinGap + Mathf.Max(0f, weapon.CurrentSpread) * normalPixelsPerSpreadDegree,
                normalMinGap,
                normalMaxGap
            );

            float interpolation = 1f - Mathf.Exp(
                -Mathf.Max(0.01f, normalSmoothSpeed) * Time.unscaledDeltaTime
            );

            _currentNormalGap = Mathf.Lerp(_currentNormalGap, targetGap, interpolation);
            ApplyNormalGap(_currentNormalGap);
        }

        private void ApplyNormalGap(float gap)
        {
            float halfArm = normalArmLength * 0.5f;
            float offset = gap + halfArm;

            if (normalLeft != null)
                normalLeft.anchoredPosition = new Vector2(-offset, 0f);
            if (normalRight != null)
                normalRight.anchoredPosition = new Vector2(offset, 0f);
            if (normalUp != null)
                normalUp.anchoredPosition = new Vector2(0f, offset);
            if (normalDown != null)
                normalDown.anchoredPosition = new Vector2(0f, -offset);
        }

        private void UpdateShotgunCrosshair(WeaponController weapon)
        {
            float normalizedSpread = Mathf.InverseLerp(
                0f,
                Mathf.Max(0.01f, spreadForMaxGap),
                Mathf.Max(0f, weapon.CurrentSpread)
            );

            float halfGap = Mathf.Lerp(
                shotgunBaseHalfGap,
                shotgunMaxHalfGap,
                normalizedSpread
            );

            if (shotgunLeft != null)
                SetAnchoredX(shotgunLeft.rectTransform, -halfGap);
            if (shotgunRight != null)
                SetAnchoredX(shotgunRight.rectTransform, halfGap);
        }

        private void SuppressCompetingCrosshairs()
        {
            if (root == null)
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            Graphic[] graphics = Resources.FindObjectsOfTypeAll<Graphic>();

            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null ||
                    graphic.gameObject.scene != activeScene ||
                    graphic.transform.IsChildOf(root))
                {
                    continue;
                }

                bool looksLikeCrosshair = ContainsCrosshairNameInHierarchy(graphic.transform);
                if (graphic is Text text)
                {
                    string value = text.text != null ? text.text.Trim() : string.Empty;
                    looksLikeCrosshair |= value == "+";
                }

                if (!looksLikeCrosshair || !graphic.gameObject.activeSelf)
                    continue;

                _suppressedCrosshairs.Add(graphic.gameObject);
                graphic.gameObject.SetActive(false);
            }
        }

        private static bool ContainsCrosshairNameInHierarchy(Transform transformToCheck)
        {
            Transform current = transformToCheck;
            while (current != null)
            {
                string name = current.gameObject.name;
                if (!string.IsNullOrEmpty(name))
                {
                    string normalized = name.ToLowerInvariant();
                    if (normalized.Contains("crosshair") || normalized.Contains("reticle"))
                        return true;
                }
                current = current.parent;
            }
            return false;
        }

        private void RestoreSuppressedCrosshairs()
        {
            foreach (GameObject suppressed in _suppressedCrosshairs)
            {
                if (suppressed != null)
                    suppressed.SetActive(true);
            }
            _suppressedCrosshairs.Clear();
        }

        private static PlayerInputReader FindLocalPlayerInput()
        {
            PlayerInputReader[] inputs = Resources.FindObjectsOfTypeAll<PlayerInputReader>();
            Scene activeScene = SceneManager.GetActiveScene();
            PlayerInputReader fallback = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInputReader candidate = inputs[i];
                if (!IsValidLocalInput(candidate) || candidate.gameObject.scene != activeScene)
                    continue;

                if (candidate.gameObject.name == "Player_Prototype" ||
                    candidate.gameObject.name.StartsWith("Player_"))
                {
                    return candidate;
                }

                if (fallback == null && !candidate.gameObject.name.StartsWith("Bot_"))
                    fallback = candidate;
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

        private T FindNamed<T>(string objectName) where T : Component
        {
            T[] all = GetComponentsInChildren<T>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName)
                    return all[i];
            }
            return null;
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
