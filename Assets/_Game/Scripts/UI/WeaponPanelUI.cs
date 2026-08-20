using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    public sealed class WeaponPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponEquipmentController equipment;

        [Header("UI - Legacy Text")]
        [SerializeField] private Text ammoCurrent;
        [SerializeField] private Text weaponName;
        [SerializeField] private Text ammoReserve;
        [SerializeField] private Text reloadingText;
        [SerializeField] private Text slotText;

        private void Awake()
        {
            EnsureReferences();
            UpdateUI();
        }

        private void Update()
        {
            EnsureReferences();
            UpdateUI();
        }

        private void EnsureReferences()
        {
            if (equipment == null)
            {
                equipment = FindFirstObjectByType<WeaponEquipmentController>();
            }
        }

        private void UpdateUI()
        {
            if (equipment == null)
            {
                ClearUI();
                return;
            }

            WeaponController weapon = equipment.EquippedWeapon;

            if (weapon == null)
            {
                ClearUI();
                return;
            }

            if (ammoCurrent != null)
            {
                ammoCurrent.text = weapon.AmmoInMagazine.ToString();
            }

            if (ammoReserve != null)
            {
                ammoReserve.text = $"/ {weapon.ReserveAmmo}";
            }

            if (weaponName != null)
            {
                weaponName.text =
                    weapon.Definition != null
                        ? $"{weapon.Definition.displayName} [{GetFireModeLabel(weapon.CurrentFireMode)}]"
                        : weapon.name;
            }

            if (reloadingText != null)
            {
                reloadingText.text =
                    weapon.IsReloading
                        ? "RECARGANDO"
                        : string.Empty;
            }

            if (slotText != null)
            {
                slotText.text =
                    $"SLOT {equipment.EquippedSlot} | {GetFireModeLabel(weapon.CurrentFireMode)}";
            }
        }

        private void ClearUI()
        {
            if (ammoCurrent != null)
                ammoCurrent.text = "--";

            if (ammoReserve != null)
                ammoReserve.text = "/ --";

            if (weaponName != null)
                weaponName.text = "SIN ARMA";

            if (reloadingText != null)
                reloadingText.text = string.Empty;

            if (slotText != null)
                slotText.text = string.Empty;
        }

        private static string GetFireModeLabel(
            ROS.Game.Core.WeaponFireMode mode
        )
        {
            return mode switch
            {
                ROS.Game.Core.WeaponFireMode.Single => "SEMI",
                ROS.Game.Core.WeaponFireMode.Burst => "RAFAGA",
                _ => "AUTO"
            };
        }
    }
}
