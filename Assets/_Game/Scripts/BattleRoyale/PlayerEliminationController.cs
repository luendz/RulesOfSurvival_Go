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
            // armas equipadas sin que esas armas existan como stacks.
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
        /// realmente llevaba. Cuando existe un InventoryItemDefinition real,
        /// se reutiliza directamente para conservar icono, rareza, modelo,
        /// peso y demás metadata. Solo se crea una definición runtime como
        /// respaldo cuando no existe una definición de datos equivalente.
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

            AddWeaponSnapshot(
                equipment.PrimarySlot1,
                1,
                representedWeapons
            );
            AddWeaponSnapshot(
                equipment.PrimarySlot2,
                2,
                representedWeapons
            );
            AddWeaponSnapshot(
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

        private void AddWeaponSnapshot(
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
                InventoryItemDefinition canonicalWeapon =
                    FindCanonicalWeaponItem(definition);

                if (canonicalWeapon != null)
                {
                    if (inventory.GetAmount(canonicalWeapon) <= 0)
                    {
                        inventory.Add(canonicalWeapon, 1);
                    }
                }
                else
                {
                    InventoryItemDefinition runtimeWeapon =
                        CreateRuntimeWeaponItem(
                            weapon,
                            definition,
                            slot
                        );

                    inventory.Add(runtimeWeapon, 1);
                }

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

            InventoryItemDefinition canonicalAmmo =
                FindCanonicalAmmoItem(definition.ammoType);

            if (canonicalAmmo != null)
            {
                inventory.Add(canonicalAmmo, totalAmmo);
                return;
            }

            InventoryItemDefinition runtimeAmmo =
                CreateRuntimeAmmoItem(
                    definition.ammoType,
                    totalAmmo,
                    slot
                );

            inventory.Add(runtimeAmmo, totalAmmo);
        }

        private InventoryItemDefinition CreateRuntimeWeaponItem(
            WeaponController weapon,
            WeaponDefinition definition,
            int slot
        )
        {
            InventoryItemDefinition item =
                ScriptableObject.CreateInstance<
                    InventoryItemDefinition
                >();

            item.name = $"LootRuntime_{definition.displayName}";
            item.itemId =
                $"runtime_death_weapon_{definition.weaponId}_{GetInstanceID()}_{slot}";
            item.displayName =
                string.IsNullOrWhiteSpace(definition.displayName)
                    ? weapon.gameObject.name
                    : definition.displayName;
            item.itemType = ItemType.Weapon;
            item.dataConfidence = DataConfidence.Prototype;
            item.maxStack = 1;
            item.weight = 0f;
            item.weaponDefinition = definition;
            item.preferredWeaponSlot = slot;

            // Si no hay definición exacta, intentar al menos conservar un icono
            // compatible ya cargado. Solo se usa cuando la coincidencia es única
            // para evitar mostrar un arma incorrecta.
            item.icon = FindCompatibleWeaponIcon(definition);
            item.hideFlags = HideFlags.DontSave;
            return item;
        }

        private InventoryItemDefinition CreateRuntimeAmmoItem(
            AmmoType ammoType,
            int totalAmmo,
            int slot
        )
        {
            InventoryItemDefinition item =
                ScriptableObject.CreateInstance<
                    InventoryItemDefinition
                >();

            item.name = $"LootRuntime_Ammo_{ammoType}";
            item.itemId =
                $"runtime_death_ammo_{ammoType}_{GetInstanceID()}_{slot}";
            item.displayName = $"Munición {ammoType}";
            item.itemType = ItemType.Ammo;
            item.dataConfidence = DataConfidence.Prototype;
            item.maxStack = Mathf.Max(totalAmmo, 1);
            item.weight = 0f;
            item.ammoType = ammoType;
            item.icon = FindCompatibleAmmoIcon(ammoType);
            item.hideFlags = HideFlags.DontSave;
            return item;
        }

        private static InventoryItemDefinition FindCanonicalWeaponItem(
            WeaponDefinition definition
        )
        {
            if (definition == null)
            {
                return null;
            }

            InventoryItemDefinition[] definitions =
                Resources.FindObjectsOfTypeAll<InventoryItemDefinition>();

            InventoryItemDefinition exactWithoutIcon = null;
            InventoryItemDefinition idMatch = null;
            InventoryItemDefinition nameMatch = null;

            string targetId = NormalizeKey(definition.weaponId);
            string targetName = NormalizeKey(definition.displayName);

            for (int i = 0; i < definitions.Length; i++)
            {
                InventoryItemDefinition candidate = definitions[i];
                if (!IsCanonicalAsset(candidate) ||
                    candidate.itemType != ItemType.Weapon ||
                    candidate.weaponDefinition == null)
                {
                    continue;
                }

                if (candidate.weaponDefinition == definition)
                {
                    if (candidate.icon != null)
                    {
                        return candidate;
                    }

                    exactWithoutIcon ??= candidate;
                    continue;
                }

                if (idMatch == null &&
                    !string.IsNullOrEmpty(targetId) &&
                    NormalizeKey(candidate.weaponDefinition.weaponId) == targetId)
                {
                    idMatch = candidate;
                }

                if (nameMatch == null &&
                    !string.IsNullOrEmpty(targetName) &&
                    NormalizeKey(candidate.displayName) == targetName)
                {
                    nameMatch = candidate;
                }
            }

            return idMatch ?? nameMatch ?? exactWithoutIcon;
        }

        private static InventoryItemDefinition FindCanonicalAmmoItem(
            AmmoType ammoType
        )
        {
            InventoryItemDefinition[] definitions =
                Resources.FindObjectsOfTypeAll<InventoryItemDefinition>();

            InventoryItemDefinition fallback = null;

            for (int i = 0; i < definitions.Length; i++)
            {
                InventoryItemDefinition candidate = definitions[i];
                if (!IsCanonicalAsset(candidate) ||
                    candidate.itemType != ItemType.Ammo ||
                    candidate.ammoType != ammoType)
                {
                    continue;
                }

                if (candidate.icon != null)
                {
                    return candidate;
                }

                fallback ??= candidate;
            }

            return fallback;
        }

        private static Sprite FindCompatibleWeaponIcon(
            WeaponDefinition definition
        )
        {
            InventoryItemDefinition exact =
                FindCanonicalWeaponItem(definition);

            if (exact != null && exact.icon != null)
            {
                return exact.icon;
            }

            InventoryItemDefinition[] definitions =
                Resources.FindObjectsOfTypeAll<InventoryItemDefinition>();

            Sprite uniqueIcon = null;
            int compatibleCount = 0;

            for (int i = 0; i < definitions.Length; i++)
            {
                InventoryItemDefinition candidate = definitions[i];
                if (!IsCanonicalAsset(candidate) ||
                    candidate.itemType != ItemType.Weapon ||
                    candidate.weaponDefinition == null ||
                    candidate.icon == null)
                {
                    continue;
                }

                WeaponDefinition candidateWeapon = candidate.weaponDefinition;
                if (candidateWeapon.family != definition.family ||
                    candidateWeapon.ammoType != definition.ammoType)
                {
                    continue;
                }

                compatibleCount++;
                uniqueIcon = candidate.icon;

                if (compatibleCount > 1)
                {
                    return null;
                }
            }

            return uniqueIcon;
        }

        private static Sprite FindCompatibleAmmoIcon(AmmoType ammoType)
        {
            InventoryItemDefinition ammo =
                FindCanonicalAmmoItem(ammoType);

            return ammo != null ? ammo.icon : null;
        }

        private static bool IsCanonicalAsset(
            InventoryItemDefinition item
        )
        {
            return item != null &&
                   (item.hideFlags & HideFlags.DontSave) == 0;
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
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
