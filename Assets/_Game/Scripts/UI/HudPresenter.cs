using ROS.Game.AI;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Parachute;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Crosshair dinámico basado en Canvas. El radio se ajusta en tiempo real
    /// al spread real del arma equipada.
    /// El estado de vida, arma y partida lo muestran VitalsPanelUI,
    /// WeaponPanelUI y BattleRoyalePanelUI.
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private ParachuteController parachute;

        [Header("Crosshair")]
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private float crosshairArmLength = 8f;
        [SerializeField] private float crosshairThickness = 2f;
        [SerializeField] private float minCrosshairGap = 3f;
        [SerializeField] private float maxCrosshairGap = 45f;
        [SerializeField] private float pixelsPerSpreadDegree = 10f;
        [SerializeField] private float crosshairSmoothSpeed = 14f;

        [Header("Runtime Debug")]
        [SerializeField] private float debugCurrentSpread;
        [SerializeField] private float debugTargetGap;
        [SerializeField] private float debugCurrentGap;

        private float _currentCrosshairGap;

        // Canvas crosshair elements
        private GameObject _crosshairRoot;
        private RectTransform _left;
        private RectTransform _right;
        private RectTransform _up;
        private RectTransform _down;

        private void Awake()
        {
            EnsureReferences();
            _currentCrosshairGap = minCrosshairGap;
            BuildCrosshairCanvas();
        }

        private void Update()
        {
            EnsureReferences();
            UpdateCrosshair();
        }

        private void OnDestroy()
        {
            if (_crosshairRoot != null)
                Destroy(_crosshairRoot);
        }

        private void EnsureReferences()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>();

            if (input == null)
                input = BattleRoyaleBotController.FindLocalPlayerInput();

            if (equipment == null)
                equipment = GetComponent<WeaponEquipmentController>();

            if (equipment == null && input != null)
                equipment = input.GetComponent<WeaponEquipmentController>();

            if (equipment == null)
                equipment = FindFirstObjectByType<WeaponEquipmentController>();

            if (parachute == null && input != null)
                parachute = input.GetComponent<ParachuteController>();
        }

        private void UpdateCrosshair()
        {
            bool visible =
                showCrosshair &&
                IsCrosshairAllowed() &&
                equipment != null &&
                equipment.HasEquippedWeapon;

            if (_crosshairRoot != null)
                _crosshairRoot.SetActive(visible);

            if (!visible) return;

            WeaponController weapon = equipment.EquippedWeapon;
            float spread = weapon != null ? weapon.CurrentSpread : 0f;
            debugCurrentSpread = spread;

            float targetGap = Mathf.Clamp(
                minCrosshairGap + spread * pixelsPerSpreadDegree,
                minCrosshairGap,
                maxCrosshairGap
            );
            debugTargetGap = targetGap;

            _currentCrosshairGap = Mathf.Lerp(
                _currentCrosshairGap,
                targetGap,
                crosshairSmoothSpeed * Time.deltaTime
            );
            debugCurrentGap = _currentCrosshairGap;

            float gap = _currentCrosshairGap;
            float arm = crosshairArmLength;
            float half = arm * 0.5f;

            // Posición: el brazo empieza en gap y su centro está en gap + half
            if (_left  != null) _left.anchoredPosition  = new Vector2(-(gap + half), 0f);
            if (_right != null) _right.anchoredPosition = new Vector2(  gap + half,  0f);
            if (_up    != null) _up.anchoredPosition    = new Vector2(0f,   gap + half);
            if (_down  != null) _down.anchoredPosition  = new Vector2(0f, -(gap + half));
        }

        private bool IsCrosshairAllowed()
        {
            if (parachute == null) return true;
            return parachute.State != AirDropState.InPlane &&
                   parachute.State != AirDropState.FreeFall &&
                   parachute.State != AirDropState.Parachuting;
        }

        private void BuildCrosshairCanvas()
        {
            _crosshairRoot = new GameObject("CrosshairCanvas");

            Canvas canvas = _crosshairRoot.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = _crosshairRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Centro de pantalla
            GameObject center = new GameObject("Center");
            center.transform.SetParent(_crosshairRoot.transform, false);
            RectTransform cr = center.AddComponent<RectTransform>();
            cr.anchorMin        = new Vector2(0.5f, 0.5f);
            cr.anchorMax        = new Vector2(0.5f, 0.5f);
            cr.sizeDelta        = Vector2.zero;
            cr.anchoredPosition = Vector2.zero;

            _left  = MakeArm(center.transform, crosshairArmLength, crosshairThickness, false);
            _right = MakeArm(center.transform, crosshairArmLength, crosshairThickness, false);
            _up    = MakeArm(center.transform, crosshairThickness, crosshairArmLength, true);
            _down  = MakeArm(center.transform, crosshairThickness, crosshairArmLength, true);

            _crosshairRoot.SetActive(false);
        }

        private static RectTransform MakeArm(
            Transform parent, float w, float h, bool vertical)
        {
            GameObject go = new GameObject(vertical ? "Arm_V" : "Arm_H");
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.9f);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }
    }
}
