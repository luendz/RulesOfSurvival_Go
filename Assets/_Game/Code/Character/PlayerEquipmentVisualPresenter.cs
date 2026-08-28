using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using UnityEngine;

namespace ROS.Game.Character
{
    /// <summary>
    /// Sincroniza equipamiento jugable con visuales fisicos ya existentes en el jugador.
    /// Nunca crea, instancia, mueve ni escala objetos en runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipmentVisualPresenter : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private PlayerLootEquipment lootEquipment;
        [SerializeField] private ProtectiveEquipment protection;

        [Header("Helmet Visuals")]
        [SerializeField] private GameObject helmetLevel1;
        [SerializeField] private GameObject helmetLevel2;
        [SerializeField] private GameObject helmetLevel3;

        [Header("Vest Visuals")]
        [SerializeField] private GameObject vestLevel1;
        [SerializeField] private GameObject vestLevel2;
        [SerializeField] private GameObject vestLevel3;

        [Header("Backpack Definitions")]
        [SerializeField] private InventoryItemDefinition backpackLevel1Definition;
        [SerializeField] private InventoryItemDefinition backpackLevel2Definition;
        [SerializeField] private InventoryItemDefinition backpackLevel3Definition;

        [Header("Backpack Visuals")]
        [SerializeField] private GameObject backpackLevel1;
        [SerializeField] private GameObject backpackLevel2;
        [SerializeField] private GameObject backpackLevel3;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                Debug.LogError(
                    $"[{nameof(PlayerEquipmentVisualPresenter)}] Referencias incompletas en '{name}'. Completa la instancia en el Editor.",
                    this
                );
                enabled = false;
                return;
            }

            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Refresh()
        {
            ProtectionLevel helmet = protection != null
                ? protection.HelmetLevel
                : lootEquipment != null && lootEquipment.HelmetItem != null
                    ? lootEquipment.HelmetItem.protectionLevel
                    : ProtectionLevel.None;

            ProtectionLevel vest = protection != null
                ? protection.VestLevel
                : lootEquipment != null && lootEquipment.VestItem != null
                    ? lootEquipment.VestItem.protectionLevel
                    : ProtectionLevel.None;

            SetLevelVisuals(
                helmet,
                helmetLevel1,
                helmetLevel2,
                helmetLevel3
            );

            SetLevelVisuals(
                vest,
                vestLevel1,
                vestLevel2,
                vestLevel3
            );

            InventoryItemDefinition backpack = lootEquipment != null
                ? lootEquipment.BackpackItem
                : null;

            int backpackLevel = ResolveBackpackLevel(backpack);
            SetActive(backpackLevel1, backpackLevel == 1);
            SetActive(backpackLevel2, backpackLevel == 2);
            SetActive(backpackLevel3, backpackLevel == 3);
        }

        private int ResolveBackpackLevel(InventoryItemDefinition item)
        {
            if (item == null)
                return 0;

            if (item == backpackLevel1Definition) return 1;
            if (item == backpackLevel2Definition) return 2;
            if (item == backpackLevel3Definition) return 3;

            // Respaldo para assets antiguos: las definiciones materializadas se
            // ordenan por capacidad, por lo que esta comparación sigue siendo estable.
            float capacity = item.backpackCapacity;
            if (backpackLevel3Definition != null &&
                capacity >= backpackLevel3Definition.backpackCapacity)
                return 3;
            if (backpackLevel2Definition != null &&
                capacity >= backpackLevel2Definition.backpackCapacity)
                return 2;
            if (backpackLevel1Definition != null &&
                capacity >= backpackLevel1Definition.backpackCapacity)
                return 1;

            return 0;
        }

        private static void SetLevelVisuals(
            ProtectionLevel level,
            GameObject level1,
            GameObject level2,
            GameObject level3)
        {
            SetActive(level1, level == ProtectionLevel.Level1);
            SetActive(level2, level == ProtectionLevel.Level2);
            SetActive(level3, level == ProtectionLevel.Level3);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private bool ValidateReferences()
        {
            return lootEquipment != null &&
                   protection != null &&
                   helmetLevel1 != null &&
                   helmetLevel2 != null &&
                   helmetLevel3 != null &&
                   vestLevel1 != null &&
                   vestLevel2 != null &&
                   vestLevel3 != null &&
                   backpackLevel1Definition != null &&
                   backpackLevel2Definition != null &&
                   backpackLevel3Definition != null &&
                   backpackLevel1 != null &&
                   backpackLevel2 != null &&
                   backpackLevel3 != null;
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (lootEquipment != null)
                lootEquipment.EquipmentChanged += Refresh;
            if (protection != null)
                protection.Changed += Refresh;
        }

        private void Unsubscribe()
        {
            if (lootEquipment != null)
                lootEquipment.EquipmentChanged -= Refresh;
            if (protection != null)
                protection.Changed -= Refresh;
        }

    }
}
