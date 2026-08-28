using System;
using System.Collections.Generic;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class ExplosionDamageSource : MonoBehaviour
    {
        [SerializeField] private float maximumDamage = 100f;
        [SerializeField] private float radius = 6f;
        [Range(0f, 1f)]
        [SerializeField] private float fullDamageRadius = 0.2f;
        [SerializeField] private LayerMask affectedLayers = ~0;
        [SerializeField] private bool destroyAfterDetonation;

        public event Action Detonated;

        public void Detonate(GameObject instigator = null)
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                Mathf.Max(0f, radius),
                affectedLayers,
                QueryTriggerInteraction.Collide
            );

            Dictionary<Health, Vector3> closestPoints =
                new Dictionary<Health, Vector3>();

            Dictionary<Health, float> closestDistances =
                new Dictionary<Health, float>();

            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                Health health =
                    collider.GetComponentInParent<Health>();

                if (health == null || !health.IsAlive)
                {
                    continue;
                }

                Vector3 targetPoint =
                    collider.ClosestPoint(transform.position);

                float distance = Vector3.Distance(
                    transform.position,
                    targetPoint
                );

                if (closestDistances.TryGetValue(
                        health,
                        out float previousDistance) &&
                    previousDistance <= distance)
                {
                    continue;
                }

                closestPoints[health] = targetPoint;
                closestDistances[health] = distance;
            }

            foreach (
                KeyValuePair<Health, Vector3> target
                in closestPoints)
            {
                Health health = target.Key;
                Vector3 targetPoint = target.Value;
                float distance = closestDistances[health];

                float amount = CalculateDamage(
                    distance,
                    radius,
                    fullDamageRadius,
                    maximumDamage
                );

                if (amount <= 0f)
                {
                    continue;
                }

                Vector3 direction =
                    health.transform.position -
                    transform.position;

                health.ApplyDamage(
                    new DamageInfo(
                        amount,
                        targetPoint,
                        direction.normalized,
                        instigator != null
                            ? instigator
                            : gameObject,
                        DamageType.Explosion,
                        HitZone.Torso
                    )
                );
            }

            Detonated?.Invoke();

            if (destroyAfterDetonation)
            {
                Destroy(gameObject);
            }
        }

        public static float CalculateDamage(
            float distance,
            float effectRadius,
            float fullRadiusRatio,
            float maximum)
        {
            if (effectRadius <= 0f ||
                maximum <= 0f ||
                distance >= effectRadius)
            {
                return 0f;
            }

            float fullRadius =
                effectRadius *
                Mathf.Clamp01(fullRadiusRatio);

            if (distance <= fullRadius)
            {
                return maximum;
            }

            float falloffRange = Mathf.Max(
                0.001f,
                effectRadius - fullRadius
            );

            float normalized = Mathf.Clamp01(
                (distance - fullRadius) /
                falloffRange
            );

            return maximum * (1f - normalized);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
