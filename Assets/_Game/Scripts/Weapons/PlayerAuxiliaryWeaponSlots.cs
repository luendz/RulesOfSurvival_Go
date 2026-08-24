using System;
using ROS.Game.Input;
using ROS.Game.Loot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.Weapons
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-35)]
    public sealed class PlayerAuxiliaryWeaponSlots : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WeaponEquipmentController weapons;
        [SerializeField] private PlayerLootEquipment lootEquipment;
        [SerializeField] private PlayerWeaponSlot selectedAuxiliarySlot =
            PlayerWeaponSlot.None;

        public PlayerWeaponSlot SelectedAuxiliarySlot => selectedAuxiliarySlot;

        public event Action<PlayerWeaponSlot> AuxiliarySlotChanged;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (weapons != null)
            {
                weapons.WeaponEquipped -= HandleWeaponEquipped;
                weapons.WeaponEquipped += HandleWeaponEquipped;
            }
        }

        private void Update()
        {
            ResolveReferences();

            if (input == null || input.UiBlocked || input.UsesExternalControl)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                SelectMelee();
                return;
            }

            if (keyboard.digit5Key.wasPressedThisFrame)
            {
                SelectThrowable();
                return;
            }

            if (input.HolsterWeaponPressed)
                SelectMelee();
        }

        public void SelectMelee()
        {
            if (weapons != null)
                weapons.HolsterCurrentWeapon();

            SetAuxiliarySlot(PlayerWeaponSlot.Melee);
        }

        public bool SelectThrowable()
        {
            if (lootEquipment == null ||
                lootEquipment.GetWeaponItem(PlayerWeaponSlot.Throwable) == null)
            {
                return false;
            }

            if (weapons != null)
                weapons.HolsterCurrentWeapon();

            SetAuxiliarySlot(PlayerWeaponSlot.Throwable);
            return true;
        }

        private void HandleWeaponEquipped(WeaponController _, int __)
        {
            SetAuxiliarySlot(PlayerWeaponSlot.None);
        }

        private void SetAuxiliarySlot(PlayerWeaponSlot slot)
        {
            if (selectedAuxiliarySlot == slot)
                return;

            selectedAuxiliarySlot = slot;
            AuxiliarySlotChanged?.Invoke(slot);
        }

        private void ResolveReferences()
        {
            input ??= GetComponent<PlayerInputReader>();
            weapons ??= GetComponent<WeaponEquipmentController>();
            lootEquipment ??= GetComponent<PlayerLootEquipment>();
        }

        private void OnDisable()
        {
            if (weapons != null)
                weapons.WeaponEquipped -= HandleWeaponEquipped;
        }
    }
}
