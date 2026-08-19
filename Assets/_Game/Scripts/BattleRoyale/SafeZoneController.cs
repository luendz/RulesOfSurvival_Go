using System;
using System.Collections;
using ROS.Game.Combat;
using UnityEngine;

namespace ROS.Game.BattleRoyale
{
    [Serializable]
    public struct ZonePhase
    {
        [Min(1f)]
        public float radius;

        [Min(0f)]
        public float waitSeconds;

        [Min(0.1f)]
        public float shrinkSeconds;

        [Min(0f)]
        public float damagePerSecond;
    }

    public sealed class SafeZoneController : MonoBehaviour
    {
        [SerializeField]
        private ZonePhase[] phases =
        {
            new ZonePhase
            {
                radius = 250f,
                waitSeconds = 30f,
                shrinkSeconds = 45f,
                damagePerSecond = 1f
            },

            new ZonePhase
            {
                radius = 140f,
                waitSeconds = 25f,
                shrinkSeconds = 40f,
                damagePerSecond = 2f
            },

            new ZonePhase
            {
                radius = 70f,
                waitSeconds = 20f,
                shrinkSeconds = 35f,
                damagePerSecond = 4f
            },

            new ZonePhase
            {
                radius = 25f,
                waitSeconds = 15f,
                shrinkSeconds = 30f,
                damagePerSecond = 8f
            }
        };

        public Vector3 Center { get; private set; }

        public float Radius { get; private set; }

        public int CurrentPhase { get; private set; } = -1;

        public int PhaseCount =>
            phases != null
                ? phases.Length
                : 0;

        public float CurrentDamagePerSecond
        {
            get;
            private set;
        }

        public float PhaseTimeRemaining
        {
            get;
            private set;
        }

        public bool IsShrinking
        {
            get;
            private set;
        }

        public event Action<int, Vector3, float>
            PhaseChanged;

        private Coroutine _routine;

        public void Begin(
            Vector3 initialCenter,
            float initialRadius
        )
        {
            Center = initialCenter;
            Radius = initialRadius;

            CurrentPhase = -1;
            CurrentDamagePerSecond = 0f;
            PhaseTimeRemaining = 0f;
            IsShrinking = false;

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine =
                StartCoroutine(
                    RunPhases()
                );
        }

        private IEnumerator RunPhases()
        {
            if (phases == null ||
                phases.Length == 0)
            {
                yield break;
            }

            for (
                int i = 0;
                i < phases.Length;
                i++
            )
            {
                CurrentPhase = i;

                ZonePhase phase =
                    phases[i];

                CurrentDamagePerSecond =
                    phase.damagePerSecond;

                // -------------------------
                // ESPERA ANTES DEL CIERRE
                // -------------------------

                IsShrinking = false;

                PhaseTimeRemaining =
                    phase.waitSeconds;

                while (
                    PhaseTimeRemaining > 0f
                )
                {
                    PhaseTimeRemaining =
                        Mathf.Max(
                            0f,
                            PhaseTimeRemaining -
                            Time.deltaTime
                        );

                    yield return null;
                }

                // -------------------------
                // INICIO DEL CIERRE
                // -------------------------

                IsShrinking = true;

                Vector3 oldCenter =
                    Center;

                float oldRadius =
                    Radius;

                float maxOffset =
                    Mathf.Max(
                        0f,
                        oldRadius -
                        phase.radius
                    );

                Vector2 offset =
                    UnityEngine.Random
                        .insideUnitCircle *
                    maxOffset;

                Vector3 nextCenter =
                    oldCenter +
                    new Vector3(
                        offset.x,
                        0f,
                        offset.y
                    );

                float elapsed = 0f;

                PhaseTimeRemaining =
                    phase.shrinkSeconds;

                while (
                    elapsed <
                    phase.shrinkSeconds
                )
                {
                    elapsed +=
                        Time.deltaTime;

                    float t =
                        Mathf.Clamp01(
                            elapsed /
                            phase.shrinkSeconds
                        );

                    Center =
                        Vector3.Lerp(
                            oldCenter,
                            nextCenter,
                            t
                        );

                    Radius =
                        Mathf.Lerp(
                            oldRadius,
                            phase.radius,
                            t
                        );

                    PhaseTimeRemaining =
                        Mathf.Max(
                            0f,
                            phase.shrinkSeconds -
                            elapsed
                        );

                    yield return null;
                }

                Center = nextCenter;
                Radius = phase.radius;

                PhaseTimeRemaining = 0f;
                IsShrinking = false;

                PhaseChanged?.Invoke(
                    i,
                    Center,
                    Radius
                );
            }

            _routine = null;
        }

        public bool IsOutside(
            Vector3 position
        )
        {
            Vector2 playerPosition =
                new Vector2(
                    position.x,
                    position.z
                );

            Vector2 zonePosition =
                new Vector2(
                    Center.x,
                    Center.z
                );

            return Vector2.Distance(
                playerPosition,
                zonePosition
            ) > Radius;
        }

        public void ApplyZoneDamage(
            Health health
        )
        {
            if (
                health == null ||
                !health.IsAlive ||
                CurrentDamagePerSecond <= 0f
            )
            {
                return;
            }

            if (
                !IsOutside(
                    health.transform.position
                )
            )
            {
                return;
            }

            health.ApplyDamage(
                new DamageInfo(
                    CurrentDamagePerSecond *
                    Time.deltaTime,

                    health.transform.position,

                    Vector3.zero,

                    gameObject
                )
            );
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(
                Application.isPlaying
                    ? Center
                    : transform.position,

                Application.isPlaying
                    ? Radius
                    : 10f
            );
        }
    }
}