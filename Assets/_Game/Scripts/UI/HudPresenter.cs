using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Runtime combat HUD used by the current prototype scenes.
    /// It intentionally uses OnGUI so it works without requiring a scene Canvas.
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerAimController aim;
        [SerializeField] private BattleRoyaleManager battleRoyale;

        [Header("Crosshair")]
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private float crosshairSize = 18f;
        [SerializeField] private float crosshairThickness = 2f;
        [SerializeField] private float crosshairGap = 4f;

        [Header("Debug HUD")]
        [SerializeField] private bool showStatus = true;
        [SerializeField] private bool showControls = true;

        private GUIStyle _style;
        private GUIStyle _smallStyle;
        private Texture2D _whiteTexture;

        private void Awake()
        {
            EnsureReferences();
            _whiteTexture = Texture2D.whiteTexture;
        }

        private void EnsureReferences()
        {
            if (health == null) health = GetComponent<Health>();
            if (health == null) health = FindFirstObjectByType<Health>();

            if (equipment == null) equipment = GetComponent<WeaponEquipmentController>();
            if (equipment == null) equipment = FindFirstObjectByType<WeaponEquipmentController>();

            if (input == null) input = GetComponent<PlayerInputReader>();
            if (input == null) input = FindFirstObjectByType<PlayerInputReader>();

            if (aim == null) aim = GetComponent<PlayerAimController>();
            if (aim == null) aim = FindFirstObjectByType<PlayerAimController>();

            if (battleRoyale == null) battleRoyale = FindFirstObjectByType<BattleRoyaleManager>();
        }

        private void OnGUI()
        {
            EnsureReferences();

            if (showCrosshair && equipment != null && equipment.HasEquippedWeapon)
                DrawCrosshair();

            if (!showStatus && !showControls)
                return;

            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };

            if (_smallStyle == null)
                _smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };

            if (showStatus)
                DrawStatus();

            if (showControls)
            {
                GUI.Label(
                    new Rect(16, Screen.height - 125, 900, 105),
                    "WASD mover | Shift sprint | Espacio saltar | C agachar | RMB apuntar | LMB disparar | R recargar | 1/2 armas | X guardar | Alt free-look | V hombro",
                    _smallStyle
                );
            }
        }

        private void DrawStatus()
        {
            float y = 16f;

            if (health != null)
            {
                GUI.Label(
                    new Rect(16, y, 420, 30),
                    $"VIDA {health.CurrentHealth:0}/{health.MaxHealth:0}   ARMOR {health.CurrentArmor:0}/{health.MaxArmor:0}",
                    _style
                );
                y += 30f;
            }

            WeaponController weapon = equipment != null ? equipment.EquippedWeapon : null;
            if (weapon != null)
            {
                string weaponName = weapon.Definition != null ? weapon.Definition.displayName : weapon.name;
                GUI.Label(
                    new Rect(16, y, 520, 30),
                    $"{weaponName}   MUNICION {weapon.AmmoInMagazine} / {weapon.ReserveAmmo}",
                    _style
                );
                y += 30f;
            }
            else
            {
                GUI.Label(new Rect(16, y, 420, 30), "SIN ARMA EQUIPADA", _style);
                y += 30f;
            }

            if (battleRoyale != null)
            {
                GUI.Label(
                    new Rect(16, y, 420, 30),
                    $"VIVOS {battleRoyale.AliveCount}   ESTADO {battleRoyale.State}",
                    _style
                );
            }
        }

        private void DrawCrosshair()
        {
            if (_whiteTexture == null)
                _whiteTexture = Texture2D.whiteTexture;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            float armLength = Mathf.Max(2f, (crosshairSize - crosshairGap * 2f) * 0.5f);

            Color previous = GUI.color;
            GUI.color = aim != null && aim.HasHit
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.9f);

            GUI.DrawTexture(
                new Rect(centerX - crosshairGap - armLength, centerY - crosshairThickness * 0.5f, armLength, crosshairThickness),
                _whiteTexture
            );
            GUI.DrawTexture(
                new Rect(centerX + crosshairGap, centerY - crosshairThickness * 0.5f, armLength, crosshairThickness),
                _whiteTexture
            );
            GUI.DrawTexture(
                new Rect(centerX - crosshairThickness * 0.5f, centerY - crosshairGap - armLength, crosshairThickness, armLength),
                _whiteTexture
            );
            GUI.DrawTexture(
                new Rect(centerX - crosshairThickness * 0.5f, centerY + crosshairGap, crosshairThickness, armLength),
                _whiteTexture
            );

            GUI.color = previous;
        }
    }
}
