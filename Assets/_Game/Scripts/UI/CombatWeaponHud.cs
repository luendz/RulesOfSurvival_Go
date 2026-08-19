//using ROS.Game.Weapons;
//using TMPro;
//using UnityEngine;

//namespace ROS.Game.UI
//{
//    public sealed class CombatWeaponHud : MonoBehaviour
//    {
//        [Header("References")]
//        [SerializeField]
//        private WeaponEquipmentController equipment;

//        [Header("Weapon UI")]
//        [SerializeField]
//        private TextMeshProUGUI weaponName;
         
//        [SerializeField]
//        private TextMeshProUGUI ammoCurrent;

//        [SerializeField]
//        private TextMeshProUGUI ammoReserve;

//        [SerializeField]
//        private TextMeshProUGUI reloadingText;

//        [SerializeField]
//        private TextMeshProUGUI slotText;

//        [Header("Settings")]
//        [SerializeField]
//        private string reloadingLabel = "RECARGANDO...";

//        private void Awake()
//        {
//            FindEquipmentIfNeeded();
//            Refresh();
//        }

//        private void Update()
//        {
//            FindEquipmentIfNeeded();
//            Refresh();
//        }

//        private void FindEquipmentIfNeeded()
//        {
//            if (equipment != null)
//            {
//                return;
//            }

//            equipment =
//                FindFirstObjectByType<WeaponEquipmentController>();
//        }

//        private void Refresh()
//        {
//            if (equipment == null)
//            {
//                ShowNoWeapon();
//                return;
//            }

//            WeaponController weapon =
//                equipment.EquippedWeapon;

//            if (weapon == null)
//            {
//                ShowNoWeapon();
//                return;
//            }

//            if (weaponName != null)
//            {
//                weaponName.text =
//                    weapon.Definition != null
//                        ? weapon.Definition.displayName
//                        : weapon.name;
//            }

//            if (ammoCurrent != null)
//            {
//                ammoCurrent.text =
//                    weapon.AmmoInMagazine.ToString();
//            }

//            if (ammoReserve != null)
//            {
//                ammoReserve.text =
//                    $"/ {weapon.ReserveAmmo}";
//            }

//            if (slotText != null)
//            {
//                slotText.text =
//                    $"SLOT {equipment.EquippedSlot}";
//            }

//            if (reloadingText != null)
//            {
//                reloadingText.gameObject.SetActive(
//                    weapon.IsReloading
//                );

//                if (weapon.IsReloading)
//                {
//                    reloadingText.text =
//                        reloadingLabel;
//                }
//            }
//        }

//        private void ShowNoWeapon()
//        {
//            if (weaponName != null)
//            {
//                weaponName.text = string.Empty;
//            }

//            if (ammoCurrent != null)
//            {
//                ammoCurrent.text = string.Empty;
//            }

//            if (ammoReserve != null)
//            {
//                ammoReserve.text = string.Empty;
//            }

//            if (slotText != null)
//            {
//                slotText.text = string.Empty;
//            }

//            if (reloadingText != null)
//            {
//                reloadingText.gameObject.SetActive(false);
//            }
//        }
//    }
//}