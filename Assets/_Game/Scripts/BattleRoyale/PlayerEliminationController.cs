using System;
using System.Collections;
using System.Collections.Generic;
using ROS.Game.Animation;
using ROS.Game.CameraSystem;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Core;
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
    public sealed class PlayerEliminationController :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private Transform visualRoot;

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
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        public void Bind(BattleRoyaleManager manager)
        {
            MatchManager = manager;
            EnsureReferences();
        }

        private void OnDied(DamageInfo damage)
        {
            if (IsEliminated)
            {
                return;
            }

            IsEliminated = true;

            // Antes de desactivar armas/componentes, convertir el equipamiento
            // jugable en entradas de inventario. Los bots clonados pueden tener
            // armas equipadas sin que esas armas existan como stacks, y eso
            // generaba una caja visible pero con ItemCount == 0.
            EnsureDeathInventorySnapshot();

            BlockGameplay();
            SpawnLootContainer();
            EnterDeathCamera();

            if (useFallbackPose && visualRoot != null)
            {
                StartCoroutine(ApplyFallbackPose());
            }

            Eliminated?.Invoke(damage);
        }

        private void EnsureReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (inventory == null)
            {
                inventory =
                    GetComponent<InventoryComponent>();
            }

            if (visualRoot == null)
            {
                PlayerVisualAdapter visual =
                    GetComponentInChildren<
                        PlayerVisualAdapter
                    >(true);

                if (visual != null)
                {
                    visualRoot = visual.transform;
                }
            }

            if (visualRoot == null)
            {
                visualRoot =
                    transform.Find("VisualRoot");
            }
        }

        /// <summary>
        /// Garantiza que la caja de muerte represente lo que el jugador/bot
        /// realmente llevaba. PlayerLootEquipment conserva referencias de ítems
        /// equipados cuando vienen del sistema de loot; para armas preinstaladas
        /// directamente en WeaponEquipmentController se crea una definición
        /// runtime equivalente y, además, una pila de su munición restante.
        /// </summary>
        private void EnsureDeathInventorySnapshot()
        {
            EnsureReferences();

            if (inventory == null)
            {
                return;
            }

            PlayerLootEquipment lootEquipment =
                GetComponent<PlayerLootEquipment>();

            HashSet<WeaponDefinition> representedWeapons =
                new HashSet<WeaponDefinition>();

            if (lootEquipment != null)
            {
                AddEquippedItemIfMissing(lootEquipment.HelmetItem);
                AddEquippedItemIfMissing(lootEquipment.VestItem);
                AddEquippedItemIfMissing(lootEquipment.BackpackItem);

                for (int slot = 1; slot <= 3; slot++)
                {
                    InventoryItemDefinition weaponItem =
                        lootEquipment.GetWeaponItem(slot);

                    if (weaponItem == null)
                    {
                        continue;
                    }

                    AddEquippedItemIfMissing(weaponItem);

                    if (weaponItem.weaponDefinition != null)
                    {
                        representedWeapons.Add(
                            weaponItem.weaponDefinition
                        );
                    }
                }
            }

            WeaponEquipmentController equipment =
                GetComponent<WeaponEquipmentController>();

            if (equipment == null)
            {
                return;
            }

            AddRuntimeWeaponSnapshot(
                equipment.PrimarySlot1,
                1,
                representedWeapons
            );
            AddRuntimeWeaponSnapshot(
                equipment.PrimarySlot2,
                2,
                representedWeapons
            );
            AddRuntimeWeaponSnapshot(
                equipment.SidearmSlot,
                3,
                representedWeapons
            );
        }

        private void AddEquippedItemIfMissing(
            InventoryItemDefinition item
        )
        {
            if (item == null || inventory == null)
            {
                return;
            }

            if (inventory.GetAmount(item) <= 0)
            {
                inventory.Add(item, 1);
            }
        }

        private void AddRuntimeWeaponSnapshot(
            WeaponController weapon,
            int slot,
            HashSet<WeaponDefinition> representedWeapons
        )
        {
            if (weapon == null || inventory == null)
            {
                return;
            }

            WeaponDefinition definition = weapon.Definition;

            if (definition != null &&
                !representedWeapons.Contains(definition))
            {
                InventoryItemDefinition weaponItem =
                    ScriptableObject.CreateInstance<
                        InventoryItemDefinition
                    >();

                weaponItem.name =
                    $"LootRuntime_{definition.displayName}";
                weaponItem.itemId =
                    $"runtime_death_weapon_{definition.weaponId}_{GetInstanceID()}_{slot}";
                weaponItem.displayName =
                    string.IsNullOrWhiteSpace(definition.displayName)
                        ? weapon.gameObject.name
                        : definition.displayName;
                weaponItem.itemType = ItemType.Weapon;
                weaponItem.dataConfidence = DataConfidence.Prototype;
                weaponItem.maxStack = 1;
                weaponItem.weight = 0f;
                weaponItem.weaponDefinition = definition;
                weaponItem.preferredWeaponSlot = slot;
                weaponItem.hideFlags = HideFlags.DontSave;

                inventory.Add(weaponItem, 1);
                representedWeapons.Add(definition);
            }

            int totalAmmo =
                Mathf.Max(0, weapon.AmmoInMagazine) +
                Mathf.Max(0, weapon.ReserveAmmo);

            if (definition == null ||
                definition.ammoType == AmmoType.None ||
                totalAmmo <= 0)
            {
                return;
            }

            InventoryItemDefinition ammoItem =
                ScriptableObject.CreateInstance<
                    InventoryItemDefinition
                >();

            ammoItem.name = $"LootRuntime_Ammo_{definition.ammoType}";
            ammoItem.itemId =
                $"runtime_death_ammo_{definition.ammoType}_{GetInstanceID()}_{slot}";
            ammoItem.displayName = $"Munición {definition.ammoType}";
            ammoItem.itemType = ItemType.Ammo;
            ammoItem.dataConfidence = DataConfidence.Prototype;
            ammoItem.maxStack = Mathf.Max(totalAmmo, 1);
            ammoItem.weight = 0f;
            ammoItem.ammoType = definition.ammoType;
            ammoItem.hideFlags = HideFlags.DontSave;

            inventory.Add(ammoItem, totalAmmo);
        }

        private void BlockGameplay()
        {
            SetEnabled(
                GetComponent<PlayerMotor>(),
                false
            );

            SetEnabled(
                GetComponent<PlayerInteractor>(),
                false
            );

            SetEnabled(
                GetComponent<PlayerAimController>(),
                false
            );

            WeaponEquipmentController equipment =
                GetComponent<WeaponEquipmentController>();

            if (equipment != null)
            {
                equipment.StopAllCoroutines();
                equipment.enabled = false;
            }

            foreach (
                WeaponController weapon
                in GetComponentsInChildren<
                    WeaponController
                >(true)
            )
            {
                if (weapon != null)
                {
                    weapon.DisableForElimination();
                }
            }

            SetEnabled(
                GetComponent<PlayerInputReader>(),
                false
            );
        }

        private void SpawnLootContainer()
        {
            SpawnedLoot =
                DeathLootContainer.Create(
                    transform.position,
                    inventory
                );
        }

        private void EnterDeathCamera()
        {
            ThirdPersonCamera camera =
                FindFirstObjectByType<ThirdPersonCamera>();

            if (
                camera != null &&
                camera.Target == transform
            )
            {
                camera.EnterDeathView(transform);
            }
        }

        private IEnumerator ApplyFallbackPose()
        {
            Vector3 startPosition =
                visualRoot.localPosition;

            Quaternion startRotation =
                visualRoot.localRotation;

            Vector3 targetPosition =
                startPosition + fallbackOffset;

            Quaternion targetRotation =
                startRotation *
                Quaternion.Euler(fallbackEuler);

            float elapsed = 0f;
            float duration =
                Mathf.Max(0.01f, fallbackDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        elapsed / duration
                    );

                visualRoot.localPosition =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        t
                    );

                visualRoot.localRotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        t
                    );

                yield return null;
            }
        }

        private static void SetEnabled(
            Behaviour behaviour,
            bool value
        )
        {
            if (behaviour != null)
            {
                behaviour.enabled = value;
            }
        }
    }
}
