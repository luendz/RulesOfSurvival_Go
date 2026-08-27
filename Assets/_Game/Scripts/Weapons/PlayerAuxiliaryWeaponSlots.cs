using System;
using ROS.Game.Animation;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Gameplay;
using ROS.Game.Input;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Parachute;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ROS.Game.Weapons
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-45)]
    public sealed class PlayerAuxiliaryWeaponSlots : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WeaponEquipmentController weapons;
        [SerializeField] private PlayerLootEquipment lootEquipment;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform rightHandSocket;
        [SerializeField] private PlayerWeaponSlot selectedAuxiliarySlot =
            PlayerWeaponSlot.None;

        [Header("Raw Model Hand Mount")]
        [Tooltip(
            "Transform provisional para modelos FBX usados directamente como weaponPrefab. " +
            "Los prefabs que ya incluyen WeaponMount conservan sus propios offsets."
        )]
        [SerializeField] private Vector3 rawModelLocalPosition =
            new Vector3(-0.044f, 0.228f, 0.021f);
        [SerializeField] private Vector3 rawModelLocalEulerAngles =
            new Vector3(1.246f, -88.748f, 89.526f);
        [SerializeField] private Vector3 rawModelLocalScale =
            new Vector3(40f, 40f, 40f);

        [Header("Melee Combat")]
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private Health health;
        [SerializeField] private ConsumableController consumable;
        [SerializeField] private PlayerGestureController gestureController;
        [SerializeField] private ParachuteController parachute;
        [SerializeField, Min(0.05f)] private float meleeCastRadius = 0.35f;
        [SerializeField] private LayerMask meleeHitMask = ~0;
        [SerializeField] private QueryTriggerInteraction meleeTriggerInteraction =
            QueryTriggerInteraction.Collide;

        [Header("Unarmed Combat")]
        [SerializeField, Min(0f)] private float unarmedDamage = 15f;
        [SerializeField, Min(0.1f)] private float unarmedRange = 1.6f;
        [SerializeField, Min(0.01f)] private float unarmedAttacksPerSecond = 1.5f;
        [SerializeField, Min(0f)] private float unarmedImpactForce = 3f;

        private GameObject _heldVisualInstance;
        private CharacterController _characterController;
        private bool _meleeFireLatched;
        private float _nextMeleeAttackTime;

        public PlayerWeaponSlot SelectedAuxiliarySlot => selectedAuxiliarySlot;
        public InventoryItemDefinition SelectedItem =>
            lootEquipment != null
                ? lootEquipment.GetWeaponItem(selectedAuxiliarySlot)
                : null;
        public WeaponDefinition SelectedWeaponDefinition =>
            SelectedItem != null ? SelectedItem.weaponDefinition : null;
        public GameObject HeldVisualInstance => _heldVisualInstance;

        public event Action<PlayerWeaponSlot> AuxiliarySlotChanged;
        public event Action<float> MeleeAttacked;
        public event Action<float> UnarmedAttacked;

        private void Awake()
        {
            ResolveReferences();
            RefreshHeldVisual();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (weapons != null)
            {
                weapons.WeaponEquipped -= HandleWeaponEquipped;
                weapons.WeaponEquipped += HandleWeaponEquipped;
            }

            if (lootEquipment != null)
            {
                lootEquipment.EquipmentChanged -= HandleEquipmentChanged;
                lootEquipment.EquipmentChanged += HandleEquipmentChanged;
            }

            RefreshHeldVisual();
        }

        private void Update()
        {
            ResolveReferences();

            if (input == null || input.UiBlocked)
            {
                _meleeFireLatched = false;
                return;
            }

            HandleMeleeInput();

            if (input.UsesExternalControl)
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

        private void HandleMeleeInput()
        {
            if (!input.FireHeld)
            {
                _meleeFireLatched = false;
                return;
            }

            if (_meleeFireLatched)
                return;

            _meleeFireLatched = true;
            if (selectedAuxiliarySlot == PlayerWeaponSlot.Melee)
                TryMeleeAttack();
            else if (selectedAuxiliarySlot == PlayerWeaponSlot.None)
                TryUnarmedAttack();
        }

        public bool TryMeleeAttack()
        {
            ResolveReferences();

            WeaponDefinition definition = SelectedWeaponDefinition;
            if (selectedAuxiliarySlot != PlayerWeaponSlot.Melee ||
                definition == null ||
                definition.family != WeaponFamily.Melee ||
                IsCloseRangeActionBlocked() ||
                Time.time < _nextMeleeAttackTime)
            {
                return false;
            }

            float attacksPerSecond = Mathf.Max(0.01f, definition.shotsPerSecond);
            float attackDuration = 1f / attacksPerSecond;
            _nextMeleeAttackTime = Time.time + attackDuration;

            MeleeAttacked?.Invoke(attackDuration);
            ApplyCloseRangeHit(
                definition.damage,
                definition.range,
                definition.impactForce,
                definition
            );
            return true;
        }

        public bool TryUnarmedAttack()
        {
            ResolveReferences();

            if (selectedAuxiliarySlot != PlayerWeaponSlot.None ||
                IsCloseRangeActionBlocked() ||
                Time.time < _nextMeleeAttackTime)
            {
                return false;
            }

            float attacksPerSecond = Mathf.Max(0.01f, unarmedAttacksPerSecond);
            float attackDuration = 1f / attacksPerSecond;
            _nextMeleeAttackTime = Time.time + attackDuration;

            UnarmedAttacked?.Invoke(attackDuration);
            ApplyCloseRangeHit(
                unarmedDamage,
                unarmedRange,
                unarmedImpactForce,
                null
            );
            return true;
        }

        private bool IsCloseRangeActionBlocked()
        {
            return (health != null && !health.IsAlive) ||
                   (weapons != null &&
                    (weapons.EquippedWeapon != null || weapons.IsSwitchingWeapon)) ||
                   (consumable != null && consumable.IsUsing) ||
                   (gestureController != null && gestureController.IsPlaying) ||
                   (parachute != null && parachute.IsAirbornePhase);
        }

        private void ApplyCloseRangeHit(
            float damage,
            float range,
            float impactForce,
            WeaponDefinition definition)
        {
            Vector3 origin = ResolveMeleeOrigin();
            Vector3 direction = ResolveMeleeDirection(origin);
            float safeRange = Mathf.Max(0.1f, range);

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                Mathf.Max(0.05f, meleeCastRadius),
                direction,
                safeRange,
                meleeHitMask,
                meleeTriggerInteraction
            );

            Array.Sort(hits, (left, right) =>
                left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null ||
                    hitCollider.transform.root == transform.root)
                {
                    continue;
                }

                DamageHitbox hitbox =
                    hitCollider.GetComponentInParent<DamageHitbox>();
                Health targetHealth = hitbox != null
                    ? hitbox.Owner
                    : hitCollider.GetComponentInParent<Health>();
                IDamageable damageable = targetHealth;

                if (damageable == null)
                    damageable = hitCollider.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.IsAlive)
                    continue;

                HitZone hitZone = hitbox != null
                    ? hitbox.HitZone
                    : HitZone.Torso;
                Vector3 hitPoint = hits[i].point.sqrMagnitude > 0.0001f
                    ? hits[i].point
                    : hitCollider.ClosestPoint(origin + direction * safeRange);

                DamageInfo damageInfo = definition != null
                    ? new DamageInfo(
                        damage,
                        hitPoint,
                        direction,
                        gameObject,
                        definition,
                        hitZone
                    )
                    : new DamageInfo(
                        damage,
                        hitPoint,
                        direction,
                        gameObject,
                        DamageType.Generic,
                        hitZone
                    );

                damageable.ApplyDamage(damageInfo);

                Rigidbody body = hitCollider.attachedRigidbody;
                if (body != null && !body.isKinematic && impactForce > 0f)
                {
                    body.AddForceAtPosition(
                        direction * impactForce,
                        hitPoint,
                        ForceMode.Impulse
                    );
                }

                return;
            }
        }

        private Vector3 ResolveMeleeOrigin()
        {
            if (_characterController != null)
                return _characterController.bounds.center;

            return transform.position + transform.up;
        }

        private Vector3 ResolveMeleeDirection(Vector3 origin)
        {
            if (aimController != null &&
                aimController.AimDirection.sqrMagnitude > 0.0001f)
            {
                return aimController.GetDirectionFrom(origin);
            }

            Vector3 direction = transform.forward;
            return direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;
        }

        public void SelectMelee()
        {
            ResolveReferences();

            bool hasMelee = lootEquipment != null &&
                            lootEquipment.GetWeaponItem(PlayerWeaponSlot.Melee) != null;
            RequestAuxiliarySelection(
                hasMelee ? PlayerWeaponSlot.Melee : PlayerWeaponSlot.None
            );
        }

        public bool SelectThrowable()
        {
            ResolveReferences();

            if (lootEquipment == null ||
                lootEquipment.GetWeaponItem(PlayerWeaponSlot.Throwable) == null)
            {
                return false;
            }

            return RequestAuxiliarySelection(PlayerWeaponSlot.Throwable);
        }

        private bool RequestAuxiliarySelection(PlayerWeaponSlot targetSlot)
        {
            bool firearmEquipped = weapons != null && weapons.HasEquippedWeapon;
            if (!firearmEquipped && selectedAuxiliarySlot == targetSlot)
            {
                RefreshHeldVisual();
                return false;
            }

            if (weapons == null)
            {
                SetAuxiliarySlot(targetSlot);
                return true;
            }

            return weapons.RequestAuxiliarySwitch(
                () => SetAuxiliarySlot(targetSlot)
            );
        }

        private void HandleWeaponEquipped(WeaponController _, int __)
        {
            SetAuxiliarySlot(PlayerWeaponSlot.None);
        }

        private void SetAuxiliarySlot(PlayerWeaponSlot slot)
        {
            if (selectedAuxiliarySlot == slot)
            {
                RefreshHeldVisual();
                return;
            }

            selectedAuxiliarySlot = slot;
            RefreshHeldVisual();
            AuxiliarySlotChanged?.Invoke(slot);
        }

        private void HandleEquipmentChanged()
        {
            RefreshHeldVisual();
        }

        private void RefreshHeldVisual()
        {
            DestroyHeldVisual();

            InventoryItemDefinition item = SelectedItem;
            if (item == null)
                return;

            ResolveRightHandSocket();
            if (rightHandSocket == null)
                return;

            GameObject visualPrefab = item.weaponPrefab != null
                ? item.weaponPrefab
                : item.worldModel;

            if (visualPrefab == null)
                return;

            _heldVisualInstance = Instantiate(visualPrefab, rightHandSocket);
            _heldVisualInstance.name = $"Auxiliary_{item.displayName}";

            Transform visualTransform = _heldVisualInstance.transform;
            WeaponMount mount = _heldVisualInstance.GetComponent<WeaponMount>();
            if (mount != null)
            {
                visualTransform.localPosition = Vector3.zero;
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = Vector3.one;
                mount.Apply(WeaponMountPoint.RightHand);
            }
            else
            {
                visualTransform.localPosition = rawModelLocalPosition;
                visualTransform.localRotation =
                    Quaternion.Euler(rawModelLocalEulerAngles);
                visualTransform.localScale = rawModelLocalScale;
            }

            DisableVisualPhysics(_heldVisualInstance);
        }

        private void DestroyHeldVisual()
        {
            if (_heldVisualInstance == null)
                return;

            if (Application.isPlaying)
                Destroy(_heldVisualInstance);
            else
                DestroyImmediate(_heldVisualInstance);

            _heldVisualInstance = null;
        }

        private void ResolveRightHandSocket()
        {
            if (rightHandSocket != null)
                return;

            rightHandSocket = FindChildRecursive(transform, "Weapon_RightHand");
            if (rightHandSocket != null)
                return;

            if (animator != null && animator.isHuman)
                rightHandSocket = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        private static void DisableVisualPhysics(GameObject visual)
        {
            if (visual == null)
                return;

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            Rigidbody[] bodies = visual.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].detectCollisions = false;
            }
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == name)
                    return children[i];
            }

            return null;
        }

        private void ResolveReferences()
        {
            input ??= GetComponent<PlayerInputReader>();
            weapons ??= GetComponent<WeaponEquipmentController>();
            lootEquipment ??= GetComponent<PlayerLootEquipment>();
            animator ??= GetComponentInChildren<Animator>(true);
            aimController ??= GetComponent<PlayerAimController>();
            health ??= GetComponent<Health>();
            consumable ??= GetComponent<ConsumableController>();
            gestureController ??= GetComponent<PlayerGestureController>();
            parachute ??= GetComponent<ParachuteController>();
            _characterController ??= GetComponent<CharacterController>();
            ResolveRightHandSocket();
        }

        private void OnDisable()
        {
            if (weapons != null)
                weapons.WeaponEquipped -= HandleWeaponEquipped;

            if (lootEquipment != null)
                lootEquipment.EquipmentChanged -= HandleEquipmentChanged;

            DestroyHeldVisual();
        }
    }
}
