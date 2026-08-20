using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Parachute;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// HUD de combate provisional del prototipo.
    /// Usa OnGUI para no depender todavía de un Canvas.
    ///
    /// El crosshair utiliza el spread real del arma equipada.
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerAimController aim;
        [SerializeField] private BattleRoyaleManager battleRoyale;
        [SerializeField] private ParachuteController parachute;

        [Header("Crosshair")]
        [SerializeField] private bool showCrosshair = true;

        [Tooltip("Longitud de cada línea del crosshair.")]
        [SerializeField] private float crosshairArmLength = 8f;

        [Tooltip("Grosor de las líneas.")]
        [SerializeField] private float crosshairThickness = 2f;

        [Tooltip("Separación mínima del centro.")]
        [SerializeField] private float minCrosshairGap = 3f;

        [Tooltip("Separación máxima del centro.")]
        [SerializeField] private float maxCrosshairGap = 45f;

        [Tooltip("Conversión aproximada de grados de spread a píxeles.")]
        [SerializeField] private float pixelsPerSpreadDegree = 10f;

        [Tooltip("Velocidad con la que se abre/cierra el crosshair.")]
        [SerializeField] private float crosshairSmoothSpeed = 14f;

        [Header("Debug HUD")]
        [SerializeField] private bool showStatus = true;
        [SerializeField] private bool showControls = true;

        [Header("Runtime Debug")]
        [SerializeField] private float debugCurrentSpread;
        [SerializeField] private float debugTargetGap;
        [SerializeField] private float debugCurrentGap;

        private GUIStyle _style;
        private GUIStyle _smallStyle;
        private Texture2D _whiteTexture;

        private float _currentCrosshairGap;

        private void Awake()
        {
            EnsureReferences();

            _whiteTexture = Texture2D.whiteTexture;
            _currentCrosshairGap = minCrosshairGap;
        }

        private void Update()
        {
            EnsureReferences();
            UpdateCrosshair();
        }

        private void EnsureReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health == null)
            {
                health = FindFirstObjectByType<Health>();
            }

            if (equipment == null)
            {
                equipment = GetComponent<WeaponEquipmentController>();
            }

            if (equipment == null)
            {
                equipment = FindFirstObjectByType<WeaponEquipmentController>();
            }

            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (input == null)
            {
                input = FindFirstObjectByType<PlayerInputReader>();
            }

            if (aim == null)
            {
                aim = GetComponent<PlayerAimController>();
            }

            if (aim == null)
            {
                aim = FindFirstObjectByType<PlayerAimController>();
            }

            if (battleRoyale == null)
            {
                battleRoyale =
                    FindFirstObjectByType<BattleRoyaleManager>();
            }

            if (parachute == null)
            {
                parachute = FindFirstObjectByType<ParachuteController>();
            }
        }

        private void UpdateCrosshair()
        {
            WeaponController weapon =
                equipment != null
                    ? equipment.EquippedWeapon
                    : null;

            if (weapon == null)
            {
                debugCurrentSpread = 0f;
                debugTargetGap = minCrosshairGap;

                _currentCrosshairGap =
                    Mathf.Lerp(
                        _currentCrosshairGap,
                        minCrosshairGap,
                        crosshairSmoothSpeed *
                        Time.deltaTime
                    );

                debugCurrentGap =
                    _currentCrosshairGap;

                return;
            }

            float spread =
                weapon.CurrentSpread;

            debugCurrentSpread =
                spread;

            float targetGap =
                minCrosshairGap +
                spread *
                pixelsPerSpreadDegree;

            targetGap =
                Mathf.Clamp(
                    targetGap,
                    minCrosshairGap,
                    maxCrosshairGap
                );

            debugTargetGap =
                targetGap;

            _currentCrosshairGap =
                Mathf.Lerp(
                    _currentCrosshairGap,
                    targetGap,
                    crosshairSmoothSpeed *
                    Time.deltaTime
                );

            debugCurrentGap =
                _currentCrosshairGap;
        }

        private void OnGUI()
        {
            EnsureReferences();

            if (
                showCrosshair &&
                IsCrosshairAllowed() &&
                equipment != null &&
                equipment.HasEquippedWeapon
            )
            {
                DrawCrosshair();
            }

            if (!showStatus &&
                !showControls)
            {
                return;
            }

            if (_style == null)
            {
                _style =
                    new GUIStyle(
                        GUI.skin.label
                    )
                    {
                        fontSize = 20,
                        fontStyle =
                            FontStyle.Bold
                    };
            }

            if (_smallStyle == null)
            {
                _smallStyle =
                    new GUIStyle(
                        GUI.skin.label
                    )
                    {
                        fontSize = 14
                    };
            }

            if (showStatus)
            {
                DrawStatus();
            }

            if (showControls)
            {
                GUI.Label(
                    new Rect(
                        16,
                        Screen.height - 125,
                        1100,
                        105
                    ),
                    "WASD mover | Shift sprint | Espacio saltar | C agachar | RMB apuntar | LMB disparar | R recargar | 1/2/3 armas | Rueda cambiar arma | X guardar | Alt free-look | V hombro",
                    _smallStyle
                );
            }
        }

        private bool IsCrosshairAllowed()
        {
            if (parachute == null)
            {
                return true;
            }

            return parachute.State != AirDropState.InPlane &&
                   parachute.State != AirDropState.FreeFall &&
                   parachute.State != AirDropState.Parachuting;
        }

        private void DrawStatus()
        {
            float y = 16f;

            if (health != null)
            {
                GUI.Label(
                    new Rect(
                        16,
                        y,
                        520,
                        30
                    ),
                    $"VIDA {health.CurrentHealth:0}/{health.MaxHealth:0}   ARMOR {health.CurrentArmor:0}/{health.MaxArmor:0}",
                    _style
                );

                y += 30f;
            }

            WeaponController weapon =
                equipment != null
                    ? equipment.EquippedWeapon
                    : null;

            if (weapon != null)
            {
                string weaponName =
                    weapon.Definition != null
                        ? weapon.Definition.displayName
                        : weapon.name;

                string reloadText =
                    weapon.IsReloading
                        ? "   RECARGANDO"
                        : string.Empty;

                GUI.Label(
                    new Rect(
                        16,
                        y,
                        700,
                        30
                    ),
                    $"{weaponName}   MUNICION {weapon.AmmoInMagazine} / {weapon.ReserveAmmo}{reloadText}",
                    _style
                );

                y += 30f;

                GUI.Label(
                    new Rect(
                        16,
                        y,
                        700,
                        25
                    ),
                    $"SLOT {equipment.EquippedSlot}   SPREAD {weapon.CurrentSpread:0.00}°",
                    _smallStyle
                );

                y += 25f;
            }
            else
            {
                GUI.Label(
                    new Rect(
                        16,
                        y,
                        420,
                        30
                    ),
                    "SIN ARMA EQUIPADA",
                    _style
                );

                y += 30f;
            }

            if (battleRoyale != null)
            {
                GUI.Label(
                    new Rect(
                        16,
                        y,
                        420,
                        30
                    ),
                    $"VIVOS {battleRoyale.AliveCount}   ESTADO {battleRoyale.State}",
                    _style
                );
            }
        }

        private void DrawCrosshair()
        {
            if (_whiteTexture == null)
            {
                _whiteTexture =
                    Texture2D.whiteTexture;
            }

            float centerX =
                Screen.width *
                0.5f;

            float centerY =
                Screen.height *
                0.5f;

            float gap =
                _currentCrosshairGap;

            float armLength =
                crosshairArmLength;

            float thickness =
                crosshairThickness;

            Color previous =
                GUI.color;

            GUI.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.9f
                );

            // Izquierda
            GUI.DrawTexture(
                new Rect(
                    centerX -
                    gap -
                    armLength,
                    centerY -
                    thickness *
                    0.5f,
                    armLength,
                    thickness
                ),
                _whiteTexture
            );

            // Derecha
            GUI.DrawTexture(
                new Rect(
                    centerX +
                    gap,
                    centerY -
                    thickness *
                    0.5f,
                    armLength,
                    thickness
                ),
                _whiteTexture
            );

            // Arriba
            GUI.DrawTexture(
                new Rect(
                    centerX -
                    thickness *
                    0.5f,
                    centerY -
                    gap -
                    armLength,
                    thickness,
                    armLength
                ),
                _whiteTexture
            );

            // Abajo
            GUI.DrawTexture(
                new Rect(
                    centerX -
                    thickness *
                    0.5f,
                    centerY +
                    gap,
                    thickness,
                    armLength
                ),
                _whiteTexture
            );

            GUI.color =
                previous;
        }
    }
}
