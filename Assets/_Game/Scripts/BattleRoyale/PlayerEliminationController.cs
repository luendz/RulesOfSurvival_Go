using System;
using System.Collections;
using ROS.Game.Animation;
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
