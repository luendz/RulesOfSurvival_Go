using System.Collections.Generic;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Interaction;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Controlador del HUD de Rules of Survival.
    /// La jerarquia visual vive fisicamente en la escena/prefab editable; este
    /// componente solo enlaza referencias y actualiza datos en runtime.
    /// Nunca instancia ni construye el HUD por codigo.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class RulesOfSurvivalHUD : MonoBehaviour
    {
        private static readonly Color Dark = new Color(0.025f, 0.035f, 0.045f, 0.84f);
        private static readonly Color Yellow = new Color(0.96f, 0.86f, 0.02f, 0.96f);

        [Header("Runtime References")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private Health health;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private BattleRoyaleManager battleRoyale;
        [SerializeField] private PlayerInteractor interactor;

        [Header("HUD References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text compassText;
        [SerializeField] private Text killText;
        [SerializeField] private Text leftText;
        [SerializeField] private Text distanceText;
        [SerializeField] private Text playerNameText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image armorFill;
        [SerializeField] private Text healthValueText;
        [SerializeField] private Text zoneText;
        [SerializeField] private Text interactionText;
        [SerializeField] private RectTransform lootPanel;
        [SerializeField] private Text[] lootRows = new Text[7];
        [SerializeField] private Image minimapPlayerArrow;

        [System.Serializable]
        private sealed class WeaponSlotView
        {
            public Image background;
            public Text slot;
            public Text name;
            public Text ammo;
        }

        [SerializeField] private WeaponSlotView[] weaponSlots = new WeaponSlotView[3];

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    $"[{nameof(RulesOfSurvivalHUD)}] Referencias incompletas en '{name}'. " +
                    "Configura jugador, partida, cámaras y vista desde el Inspector.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            UpdateCompass();
            UpdateBattleRoyaleStats();
            UpdateVitals();
            UpdateWeapons();
            UpdateLootPanel();
            UpdateMinimap();
            UpdateZoneBanner();
        }

        private bool HasRequiredReferences()
        {
            return worldCamera != null && minimapCamera != null && health != null &&
                   equipment != null && battleRoyale != null && interactor != null &&
                   canvas != null && compassText != null && healthFill != null &&
                   armorFill != null && lootPanel != null && minimapPlayerArrow != null &&
                   lootRows != null && lootRows.Length == 7 &&
                   weaponSlots != null && weaponSlots.Length == 3;
        }

        private void UpdateCompass()
        {
            if (compassText == null || worldCamera == null)
                return;

            float heading = Mathf.Repeat(worldCamera.transform.eulerAngles.y, 360f);
            string cardinal = GetCardinal(heading);
            int left30 = Mathf.RoundToInt(Mathf.Repeat(heading - 30f, 360f) / 5f) * 5;
            int left15 = Mathf.RoundToInt(Mathf.Repeat(heading - 15f, 360f) / 5f) * 5;
            int center = Mathf.RoundToInt(heading / 5f) * 5;
            int right15 = Mathf.RoundToInt(Mathf.Repeat(heading + 15f, 360f) / 5f) * 5;
            int right30 = Mathf.RoundToInt(Mathf.Repeat(heading + 30f, 360f) / 5f) * 5;

            compassText.text =
                $"{left30:000}     {left15:000}     {cardinal} {center:000}     {right15:000}     {right30:000}";
        }

        private void UpdateBattleRoyaleStats()
        {
            if (battleRoyale == null)
                return;

            if (killText != null)
            {
                int kills = health != null ? battleRoyale.GetKillCount(health) : 0;
                killText.text = $"{kills} KILL";
            }

            if (leftText != null)
                leftText.text = $"{battleRoyale.AliveCount} LEFT";

            SafeZoneController zone = battleRoyale.SafeZone;
            if (distanceText == null || zone == null || health == null)
                return;

            Vector3 playerFlat = health.transform.position;
            playerFlat.y = 0f;
            Vector3 centerFlat = zone.Center;
            centerFlat.y = 0f;
            float fromCenter = Vector3.Distance(playerFlat, centerFlat);
            float toBorder = Mathf.Max(0f, fromCenter - zone.Radius);
            distanceText.text = toBorder > 0f
                ? $"ZONE\n{toBorder:0}m"
                : $"SAFE\n{zone.Radius:0}m";
        }

        private void UpdateVitals()
        {
            if (health == null)
                return;

            float healthNormalized = health.MaxHealth > 0f
                ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth)
                : 0f;
            float armorNormalized = health.MaxArmor > 0f
                ? Mathf.Clamp01(health.CurrentArmor / health.MaxArmor)
                : 0f;

            if (healthFill != null)
                SetWidth(healthFill.rectTransform, 250f * healthNormalized);
            if (armorFill != null)
                SetWidth(armorFill.rectTransform, 250f * armorNormalized);
            if (healthValueText != null)
                healthValueText.text = Mathf.CeilToInt(health.CurrentHealth).ToString();
            if (playerNameText != null)
                playerNameText.text = health.gameObject.name.Replace("_Prototype", string.Empty);
        }

        private void UpdateWeapons()
        {
            if (equipment == null || weaponSlots == null || weaponSlots.Length < 3)
                return;

            RefreshWeaponSlot(weaponSlots[0], 1, equipment.PrimarySlot1);
            RefreshWeaponSlot(weaponSlots[1], 2, equipment.PrimarySlot2);
            RefreshWeaponSlot(weaponSlots[2], 3, equipment.SidearmSlot);
        }

        private void RefreshWeaponSlot(WeaponSlotView view, int slotNumber, WeaponController weapon)
        {
            if (view == null)
                return;

            bool active = equipment != null &&
                          equipment.EquippedSlot == slotNumber &&
                          equipment.EquippedWeapon == weapon &&
                          weapon != null;

            if (view.background != null)
                view.background.color = active
                    ? new Color(0.08f, 0.10f, 0.12f, 0.96f)
                    : Dark;
            if (view.slot != null)
                view.slot.color = active ? Yellow : Color.white;

            if (weapon == null)
            {
                if (view.name != null)
                {
                    view.name.text = "EMPTY";
                    view.name.color = new Color(1f, 1f, 1f, 0.45f);
                }
                if (view.ammo != null)
                    view.ammo.text = "--/--";
                return;
            }

            if (view.name != null)
            {
                view.name.color = Color.white;
                view.name.text = weapon.Definition != null
                    ? weapon.Definition.displayName.ToUpperInvariant()
                    : weapon.name.ToUpperInvariant();
            }
            if (view.ammo != null)
                view.ammo.text = $"{weapon.AmmoInMagazine}/{weapon.ReserveAmmo}";
        }

        private void UpdateLootPanel()
        {
            if (interactor == null || lootPanel == null)
                return;

            IReadOnlyList<IInteractable> nearby = interactor.Nearby;
            bool visible = nearby != null && nearby.Count > 0;
            lootPanel.gameObject.SetActive(visible);

            if (!visible)
            {
                if (interactionText != null)
                    interactionText.text = string.Empty;
                return;
            }

            if (lootRows != null)
            {
                for (int i = 0; i < lootRows.Length; i++)
                {
                    Text row = lootRows[i];
                    if (row == null)
                        continue;

                    if (i < nearby.Count && nearby[i] != null)
                    {
                        IInteractable item = nearby[i];
                        row.text = "▣  " + item.InteractionLabel;
                        row.color = i == 0
                            ? Color.black
                            : new Color(0.08f, 0.08f, 0.08f, 0.88f);
                    }
                    else
                    {
                        row.text = string.Empty;
                    }
                }
            }

            if (interactionText != null)
            {
                IInteractable current = interactor.Current;
                interactionText.text = current != null
                    ? $"[F] {current.InteractionLabel}"
                    : string.Empty;
            }
        }

        private void UpdateMinimap()
        {
            if (minimapCamera == null || health == null)
                return;

            Vector3 player = health.transform.position;
            minimapCamera.transform.position = player + Vector3.up * 180f;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (minimapPlayerArrow != null)
            {
                minimapPlayerArrow.rectTransform.localEulerAngles =
                    new Vector3(0f, 0f, -health.transform.eulerAngles.y);
            }
        }

        private void UpdateZoneBanner()
        {
            if (zoneText == null || battleRoyale == null || battleRoyale.SafeZone == null)
                return;

            SafeZoneController zone = battleRoyale.SafeZone;
            if (zone.CurrentPhase < 0)
            {
                zoneText.text = string.Empty;
                return;
            }

            int seconds = Mathf.CeilToInt(zone.PhaseTimeRemaining);
            zoneText.text = zone.IsShrinking
                ? $"SAFE ZONE CLOSING  {seconds}s"
                : $"SAFE ZONE SHRINKS IN  {seconds}s";
        }

        private static string GetCardinal(float heading)
        {
            string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int index = Mathf.RoundToInt(heading / 45f) % names.Length;
            return names[index];
        }

        private static void SetWidth(RectTransform rect, float width)
        {
            Vector2 size = rect.sizeDelta;
            size.x = Mathf.Max(0f, width);
            rect.sizeDelta = size;
        }
    }
}
