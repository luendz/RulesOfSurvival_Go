using System;
using System.Collections.Generic;
using UnityEngine;

namespace ROS.Game.Loot
{
    public sealed class LootSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private LootPickup pickupPrefab;

        [SerializeField]
        private LootTableDefinition lootTable;

        [SerializeField]
        private Transform spawnedRoot;

        [Header("Spawn Points")]
        [SerializeField]
        private Transform[] spawnPoints;

        [SerializeField]
        [Min(0f)]
        private float pointJitterRadius = 0.35f;

        [Header("Random Area")]
        [SerializeField]
        [Min(0f)]
        private float radius = 8f;

        [Header("Spawn")]
        [SerializeField]
        private bool spawnOnStart = true;

        [SerializeField]
        [Min(1)]
        private int spawnCount = 10;

        [SerializeField]
        [Min(0f)]
        private float minimumSpacing = 1.2f;

        [SerializeField]
        [Min(1)]
        private int positionAttempts = 20;

        [SerializeField]
        private bool randomizeRotationY = true;

        [Header("Ground Alignment")]
        [SerializeField]
        private bool alignToGround = true;

        [SerializeField]
        private LayerMask groundMask = ~0;

        [SerializeField]
        [Min(0.1f)]
        private float rayStartHeight = 8f;

        [SerializeField]
        [Min(0.1f)]
        private float rayDistance = 20f;

        [SerializeField]
        private float groundOffset = 0.05f;

        private readonly List<LootPickup> _spawnedPickups =
            new List<LootPickup>();

        private readonly List<Vector3> _spawnedPositions =
            new List<Vector3>();

        private void Start()
        {
            if (spawnOnStart)
            {
                Spawn();
            }
        }

        public void Spawn()
        {
            if (!CanSpawn())
            {
                return;
            }

            RemoveDestroyedReferences();
            _spawnedPositions.Clear();

            for (int i = 0; i < spawnCount; i++)
            {
                if (!lootTable.TryPick(out LootEntry entry))
                {
                    break;
                }

                if (!TryFindSpawnPosition(out Vector3 spawnPosition))
                {
                    continue;
                }

                Quaternion rotation =
                    randomizeRotationY
                        ? Quaternion.Euler(
                            0f,
                            UnityEngine.Random.Range(0f, 360f),
                            0f
                        )
                        : Quaternion.identity;

                Transform parent =
                    spawnedRoot != null
                        ? spawnedRoot
                        : transform;

                LootPickup pickup =
                    Instantiate(
                        pickupPrefab,
                        spawnPosition,
                        rotation,
                        parent
                    );

                pickup.Configure(
                    entry.item,
                    entry.GetRandomAmount()
                );

                pickup.name =
                    $"Loot_{entry.item.displayName}";

                _spawnedPickups.Add(pickup);
                _spawnedPositions.Add(spawnPosition);
            }
        }

        public void ClearSpawnedLoot()
        {
            for (int i = _spawnedPickups.Count - 1; i >= 0; i--)
            {
                LootPickup pickup = _spawnedPickups[i];

                if (pickup != null)
                {
                    Destroy(pickup.gameObject);
                }
            }

            _spawnedPickups.Clear();
            _spawnedPositions.Clear();
        }

        private bool CanSpawn()
        {
            if (pickupPrefab == null)
            {
                Debug.LogWarning(
                    $"{name}: LootSpawner no tiene un prefab de loot asignado.",
                    this
                );

                return false;
            }

            if (lootTable == null)
            {
                Debug.LogWarning(
                    $"{name}: LootSpawner no tiene una tabla de loot asignada.",
                    this
                );

                return false;
            }

            if (!lootTable.HasValidEntries())
            {
                Debug.LogWarning(
                    $"{name}: La tabla de loot no contiene entradas válidas.",
                    this
                );

                return false;
            }

            return true;
        }

        private bool TryFindSpawnPosition(out Vector3 position)
        {
            int attempts = Mathf.Max(1, positionAttempts);

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Vector3 candidate = GetCandidatePosition();

                if (alignToGround)
                {
                    if (!TryProjectToGround(candidate, out candidate))
                    {
                        continue;
                    }
                }
                else
                {
                    candidate.y += groundOffset;
                }

                if (!HasEnoughSpacing(candidate))
                {
                    continue;
                }

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private Vector3 GetCandidatePosition()
        {
            if (HasSpawnPoints())
            {
                Transform point =
                    spawnPoints[
                        UnityEngine.Random.Range(0, spawnPoints.Length)
                    ];

                if (point != null)
                {
                    Vector2 jitter =
                        UnityEngine.Random.insideUnitCircle *
                        pointJitterRadius;

                    return
                        point.position +
                        new Vector3(jitter.x, 0f, jitter.y);
                }
            }

            Vector2 randomPoint =
                UnityEngine.Random.insideUnitCircle * radius;

            return
                transform.position +
                new Vector3(randomPoint.x, 0f, randomPoint.y);
        }

        private bool HasSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return false;
            }

            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryProjectToGround(
            Vector3 candidate,
            out Vector3 groundedPosition
        )
        {
            Vector3 origin =
                candidate +
                Vector3.up * rayStartHeight;

            float distance =
                rayStartHeight + rayDistance;

            RaycastHit[] hits =
                Physics.RaycastAll(
                    origin,
                    Vector3.down,
                    distance,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                );

            if (hits.Length == 0)
            {
                groundedPosition = default;
                return false;
            }

            Array.Sort(
                hits,
                (a, b) => a.distance.CompareTo(b.distance)
            );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                if (
                    hit.collider.GetComponentInParent<LootPickup>() != null ||
                    hit.collider.transform.IsChildOf(transform)
                )
                {
                    continue;
                }

                groundedPosition =
                    hit.point +
                    Vector3.up * groundOffset;

                return true;
            }

            groundedPosition = default;
            return false;
        }

        private bool HasEnoughSpacing(Vector3 candidate)
        {
            if (minimumSpacing <= 0f)
            {
                return true;
            }

            float minimumSqrDistance =
                minimumSpacing * minimumSpacing;

            foreach (Vector3 existing in _spawnedPositions)
            {
                Vector2 horizontalDelta =
                    new Vector2(
                        candidate.x - existing.x,
                        candidate.z - existing.z
                    );

                if (horizontalDelta.sqrMagnitude < minimumSqrDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveDestroyedReferences()
        {
            for (int i = _spawnedPickups.Count - 1; i >= 0; i--)
            {
                if (_spawnedPickups[i] == null)
                {
                    _spawnedPickups.RemoveAt(i);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (HasSpawnPoints())
            {
                foreach (Transform point in spawnPoints)
                {
                    if (point == null)
                    {
                        continue;
                    }

                    Gizmos.DrawWireSphere(
                        point.position,
                        Mathf.Max(0.1f, pointJitterRadius)
                    );
                }

                return;
            }

            Gizmos.DrawWireSphere(
                transform.position,
                radius
            );
        }
    }
}
