using System;
using System.Collections;
using ROS.Game.CameraSystem;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.BattleRoyale
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerEliminationController : MonoBehaviour
    {
        [Header("Data References")]
        [SerializeField] private Health health;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private PlayerLootEquipment lootEquipment;
        [SerializeField] private DeathLootVisualDefinition lootVisualDefinition;

        [Header("Gameplay References")]
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private WeaponEquipmentController weaponEquipment;
        [SerializeField] private WeaponController[] weapons;
        [SerializeField] private PlayerInputReader input;

        [Header("Presentation References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private ThirdPersonCamera deathCamera;

        [Header("Provisional Death Visual")]
        [SerializeField] private bool useFallbackPose = true;
        [SerializeField] private float fallbackDuration = 0.45f;
        [SerializeField] private Vector3 fallbackEuler =
            new Vector3(0f, 0f, 82f);
        [SerializeField] private Vector3 fallbackOffset =
            new Vector3(0f, 0.25f, 0f);

        public bool IsEliminated { get; private set; }
        public DeathLootContainer SpawnedLoot { get; private set; }
        public BattleRoyaleManager MatchManager { get; private set; }

        public event Action<DamageInfo> Eliminated;

        private void Awake()
        {
            if (health == null || inventory == null || visualRoot == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerEliminationController)}] Referencias incompletas en '{name}'.",
                    this
                );
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (health != null)
                health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnDied;
        }

        public void Bind(BattleRoyaleManager manager)
        {
            MatchManager = manager;
        }

        private void OnDied(DamageInfo damage)
        {
            if (IsEliminated)
                return;

            IsEliminated = true;
            CopyConfiguredEquipmentToInventory();
            BlockGameplay();
            SpawnLootContainer();
            EnterDeathCamera();

            if (useFallbackPose && visualRoot != null)
                StartCoroutine(ApplyFallbackPose());

            Eliminated?.Invoke(damage);
        }

        private void CopyConfiguredEquipmentToInventory()
        {
            if (inventory == null || lootEquipment == null)
                return;

            AddEquippedItemIfMissing(lootEquipment.HelmetItem);
            AddEquippedItemIfMissing(lootEquipment.VestItem);
            AddEquippedItemIfMissing(lootEquipment.BackpackItem);

            for (int slot = 1; slot <= 3; slot++)
                AddEquippedItemIfMissing(lootEquipment.GetWeaponItem(slot));
        }

        private void AddEquippedItemIfMissing(InventoryItemDefinition item)
        {
            if (item != null && inventory.GetAmount(item) <= 0)
                inventory.Add(item, 1);
        }

        private void BlockGameplay()
        {
            SetEnabled(motor, false);
            SetEnabled(interactor, false);
            SetEnabled(aimController, false);

            if (weaponEquipment != null)
            {
                weaponEquipment.StopAllCoroutines();
                weaponEquipment.enabled = false;
            }

            if (weapons != null)
            {
                for (int i = 0; i < weapons.Length; i++)
                {
                    if (weapons[i] != null)
                        weapons[i].DisableForElimination();
                }
            }

            SetEnabled(input, false);
        }

        private void SpawnLootContainer()
        {
            SpawnedLoot = DeathLootContainer.Create(
                transform.position,
                inventory,
                lootVisualDefinition
            );
        }

        private void EnterDeathCamera()
        {
            if (deathCamera != null && deathCamera.Target == transform)
                deathCamera.EnterDeathView(transform);
        }

        private IEnumerator ApplyFallbackPose()
        {
            Vector3 startPosition = visualRoot.localPosition;
            Quaternion startRotation = visualRoot.localRotation;
            Vector3 targetPosition = startPosition + fallbackOffset;
            Quaternion targetRotation =
                startRotation * Quaternion.Euler(fallbackEuler);

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, fallbackDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                visualRoot.localPosition =
                    Vector3.Lerp(startPosition, targetPosition, t);
                visualRoot.localRotation =
                    Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }
        }

        private static void SetEnabled(Behaviour behaviour, bool value)
        {
            if (behaviour != null)
                behaviour.enabled = value;
        }
    }
}
