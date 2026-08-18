using System;
using System.Collections.Generic;
using ROS.Game.Character;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Combat;
using ROS.Game.UI;
using UnityEngine;

namespace ROS.Game.Weapons
{
    /// <summary>
    /// Handles weapon slots and moves weapons between character sockets.
    /// Slot 1 = primary/back 01, Slot 2 = primary/back 02, Slot 3 = sidearm/hip.
    /// Only the currently equipped weapon has combat logic enabled.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class WeaponEquipmentController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Animator animator;

        [Header("Weapon Slots")]
        [SerializeField] private WeaponController primarySlot1;
        [SerializeField] private WeaponController primarySlot2;
        [SerializeField] private WeaponController sidearmSlot;

        [Header("Character Sockets")]
        [SerializeField] private Transform rightHandSocket;
        [SerializeField] private Transform backSocket01;
        [SerializeField] private Transform backSocket02;
        [SerializeField] private Transform hipSocket;

        [Header("Startup")]
        [SerializeField] private bool startWithSlot1Equipped = true;
        [SerializeField] private bool autoDiscoverWeapons = true;
        [SerializeField] private bool autoBindSocketsToHumanoidBones = true;

        [Header("Runtime Debug")]
        [SerializeField] private int debugEquippedSlot;
        [SerializeField] private string debugEquippedWeapon;
        [SerializeField] private PlayerCombatState debugCombatState;

        public WeaponController PrimarySlot1 => primarySlot1;
        public WeaponController PrimarySlot2 => primarySlot2;
        public WeaponController SidearmSlot => sidearmSlot;

        public WeaponController EquippedWeapon { get; private set; }
        public int EquippedSlot { get; private set; }
        public bool HasEquippedWeapon => EquippedWeapon != null;

        public PlayerCombatState CombatState
        {
            get
            {
                if (EquippedWeapon == null)
                    return PlayerCombatState.Unarmed;

                if (EquippedWeapon.IsReloading)
                    return PlayerCombatState.Reloading;

                if (input != null && input.AimHeld)
                    return PlayerCombatState.Aiming;

                return PlayerCombatState.HipFire;
            }
        }

        public event Action<WeaponController, int> WeaponEquipped;
        public event Action<WeaponController, int> WeaponHolstered;
        public event Action<int, WeaponController> SlotChanged;

        private static readonly int HasRifle = Animator.StringToHash("HasRifle");

        private void Awake()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            FindSockets();

            if (autoBindSocketsToHumanoidBones)
                BindSocketsToBones();

            if (autoDiscoverWeapons)
                DiscoverWeapons();

            EnsureCombatRuntimeSupport();
            PrepareAllWeapons();
            UpdateRuntimeDebug();
        }

        private void Start()
        {
            if (startWithSlot1Equipped && primarySlot1 != null)
                EquipSlot(1);
            else
                HolsterCurrentWeapon();
        }

        private void Update()
        {
            if (input != null)
            {
                if (input.WeaponSlot1Pressed)
                    EquipSlot(1);
                else if (input.WeaponSlot2Pressed)
                    EquipSlot(2);
                else if (input.WeaponSlot3Pressed)
                    EquipSlot(3);
                else if (input.HolsterWeaponPressed)
                    HolsterCurrentWeapon();
            }

            UpdateRuntimeDebug();
        }

        public void EquipSlot(int slot)
        {
            WeaponController targetWeapon = GetWeaponForSlot(slot);
            if (targetWeapon == null)
                return;

            if (EquippedWeapon == targetWeapon)
                return;

            if (EquippedWeapon != null)
            {
                WeaponController previous = EquippedWeapon;
                int previousSlot = EquippedSlot;

                SetWeaponCombatEnabled(previous, false);
                MoveWeaponToHolster(previous, previousSlot);
                WeaponHolstered?.Invoke(previous, previousSlot);
            }

            EquippedWeapon = targetWeapon;
            EquippedSlot = slot;

            MoveWeaponToRightHand(targetWeapon);
            SetWeaponCombatEnabled(targetWeapon, true);
            UpdateAnimatorWeaponState();
            UpdateRuntimeDebug();

            WeaponEquipped?.Invoke(targetWeapon, slot);
        }

        public void HolsterCurrentWeapon()
        {
            if (EquippedWeapon == null)
            {
                EquippedSlot = 0;
                UpdateAnimatorWeaponState();
                UpdateRuntimeDebug();
                return;
            }

            WeaponController previous = EquippedWeapon;
            int previousSlot = EquippedSlot;

            SetWeaponCombatEnabled(previous, false);
            MoveWeaponToHolster(previous, previousSlot);

            EquippedWeapon = null;
            EquippedSlot = 0;

            UpdateAnimatorWeaponState();
            UpdateRuntimeDebug();

            WeaponHolstered?.Invoke(previous, previousSlot);
        }

        public WeaponController GetWeaponForSlot(int slot)
        {
            return slot switch
            {
                1 => primarySlot1,
                2 => primarySlot2,
                3 => sidearmSlot,
                _ => null
            };
        }

        public bool HasWeaponInSlot(int slot)
        {
            return GetWeaponForSlot(slot) != null;
        }

        /// <summary>
        /// Assigns or replaces a weapon in a slot. This is intended for future loot/inventory
        /// integration and can already be used by test scenes.
        /// </summary>
        public void SetWeaponInSlot(int slot, WeaponController weapon, bool equipImmediately = false)
        {
            if (slot < 1 || slot > 3)
                return;

            WeaponController oldWeapon = GetWeaponForSlot(slot);
            bool replacingEquippedWeapon = oldWeapon != null && oldWeapon == EquippedWeapon;

            if (replacingEquippedWeapon)
                HolsterCurrentWeapon();

            switch (slot)
            {
                case 1:
                    primarySlot1 = weapon;
                    break;
                case 2:
                    primarySlot2 = weapon;
                    break;
                case 3:
                    sidearmSlot = weapon;
                    break;
            }

            if (weapon != null)
            {
                PrepareWeapon(weapon, slot);
            }

            SlotChanged?.Invoke(slot, weapon);

            if (equipImmediately && weapon != null)
                EquipSlot(slot);

            UpdateRuntimeDebug();
        }

        private void DiscoverWeapons()
        {
            WeaponController[] discovered = GetComponentsInChildren<WeaponController>(true);
            if (discovered.Length == 0)
                return;

            var assigned = new HashSet<WeaponController>();
            if (primarySlot1 != null) assigned.Add(primarySlot1);
            if (primarySlot2 != null) assigned.Add(primarySlot2);
            if (sidearmSlot != null) assigned.Add(sidearmSlot);

            foreach (WeaponController weapon in discovered)
            {
                if (weapon == null || assigned.Contains(weapon))
                    continue;

                if (primarySlot1 == null)
                {
                    primarySlot1 = weapon;
                    assigned.Add(weapon);
                    continue;
                }

                if (primarySlot2 == null)
                {
                    primarySlot2 = weapon;
                    assigned.Add(weapon);
                    continue;
                }

                if (sidearmSlot == null)
                {
                    sidearmSlot = weapon;
                    assigned.Add(weapon);
                }
            }
        }

        private void PrepareAllWeapons()
        {
            PrepareWeapon(primarySlot1, 1);
            PrepareWeapon(primarySlot2, 2);
            PrepareWeapon(sidearmSlot, 3);
            UpdateAnimatorWeaponState();
        }

        private void PrepareWeapon(WeaponController weapon, int slot)
        {
            if (weapon == null)
                return;

            EnsureMountComponent(weapon);
            EnsureCombatComponents(weapon);
            SetWeaponCombatEnabled(weapon, false);
            MoveWeaponToHolster(weapon, slot);
        }

        private void MoveWeaponToRightHand(WeaponController weapon)
        {
            if (weapon == null || rightHandSocket == null)
                return;

            Transform weaponTransform = weapon.transform;
            weaponTransform.SetParent(rightHandSocket, false);

            WeaponMount mount = EnsureMountComponent(weapon);
            mount.Apply(WeaponMountPoint.RightHand);
        }

        private void MoveWeaponToHolster(WeaponController weapon, int slot)
        {
            if (weapon == null)
                return;

            Transform socket;
            WeaponMountPoint mountPoint;

            switch (slot)
            {
                case 2:
                    socket = backSocket02 != null ? backSocket02 : backSocket01;
                    mountPoint = WeaponMountPoint.Back02;
                    break;
                case 3:
                    socket = hipSocket != null ? hipSocket : backSocket01;
                    mountPoint = WeaponMountPoint.Hip;
                    break;
                default:
                    socket = backSocket01;
                    mountPoint = WeaponMountPoint.Back01;
                    break;
            }

            if (socket == null)
                return;

            Transform weaponTransform = weapon.transform;
            weaponTransform.SetParent(socket, false);

            WeaponMount mount = EnsureMountComponent(weapon);
            mount.Apply(mountPoint);
        }

        private static WeaponMount EnsureMountComponent(WeaponController weapon)
        {
            WeaponMount mount = weapon.GetComponent<WeaponMount>();
            if (mount == null)
                mount = weapon.gameObject.AddComponent<WeaponMount>();
            return mount;
        }

        private static void SetWeaponCombatEnabled(WeaponController weapon, bool enabled)
        {
            if (weapon == null)
                return;

            weapon.enabled = enabled;

            WeaponEffects effects = weapon.GetComponent<WeaponEffects>();
            if (effects != null)
                effects.enabled = enabled;
        }

        private void EnsureCombatRuntimeSupport()
        {
            PlayerAimController aim = GetComponent<PlayerAimController>();
            if (aim == null)
                aim = gameObject.AddComponent<PlayerAimController>();

            if (Camera.main != null)
                aim.SetCamera(Camera.main);

            HudPresenter hud = GetComponent<HudPresenter>();
            if (hud == null)
                gameObject.AddComponent<HudPresenter>();
        }

        private static void EnsureCombatComponents(WeaponController weapon)
        {
            if (weapon == null)
                return;

            WeaponEffects effects = weapon.GetComponent<WeaponEffects>();
            if (effects == null)
                effects = weapon.gameObject.AddComponent<WeaponEffects>();

            effects.EnsureRuntimeSetup();

            if (weapon.GetComponent<WeaponRecoil>() == null)
                weapon.gameObject.AddComponent<WeaponRecoil>();
        }

        private void FindSockets()
        {
            if (rightHandSocket == null)
                rightHandSocket = FindChildRecursive(transform, "Weapon_RightHand");
            if (backSocket01 == null)
                backSocket01 = FindChildRecursive(transform, "Weapon_Back_01");
            if (backSocket02 == null)
                backSocket02 = FindChildRecursive(transform, "Weapon_Back_02");
            if (hipSocket == null)
                hipSocket = FindChildRecursive(transform, "Weapon_Hip");
        }

        private void BindSocketsToBones()
        {
            if (animator == null || !animator.isHuman)
                return;

            BindSocket(rightHandSocket, HumanBodyBones.RightHand);
            BindSocket(backSocket01, HumanBodyBones.Chest, HumanBodyBones.Spine);
            BindSocket(backSocket02, HumanBodyBones.Chest, HumanBodyBones.Spine);
            BindSocket(hipSocket, HumanBodyBones.Hips);
        }

        private void BindSocket(Transform socket, params HumanBodyBones[] candidates)
        {
            if (socket == null || animator == null)
                return;

            Transform bone = null;
            foreach (HumanBodyBones candidate in candidates)
            {
                bone = animator.GetBoneTransform(candidate);
                if (bone != null)
                    break;
            }

            if (bone == null)
                return;

            BoneSocketFollower follower = socket.GetComponent<BoneSocketFollower>();
            if (follower == null)
                follower = socket.gameObject.AddComponent<BoneSocketFollower>();

            follower.Bind(bone);
        }

        private void UpdateAnimatorWeaponState()
        {
            if (animator != null)
                animator.SetBool(HasRifle, HasEquippedWeapon);
        }

        private void UpdateRuntimeDebug()
        {
            debugEquippedSlot = EquippedSlot;
            debugEquippedWeapon = EquippedWeapon != null ? EquippedWeapon.name : string.Empty;
            debugCombatState = CombatState;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
