using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Compatibilidad legacy. Los slots visuales de armas viven dentro de
    /// HUD_ROS_EDITABLE y RulesOfSurvivalHUD actualiza nombre, selección y
    /// munición. Este componente conserva Bind sin crear UI en runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponSlotsPresenter : MonoBehaviour
    {
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private PlayerLootEquipment lootEquipment;

        public void Bind(
            WeaponEquipmentController weaponEquipment,
            PlayerLootEquipment playerLootEquipment
        )
        {
            equipment = weaponEquipment;
            lootEquipment = playerLootEquipment;
        }
    }
}
