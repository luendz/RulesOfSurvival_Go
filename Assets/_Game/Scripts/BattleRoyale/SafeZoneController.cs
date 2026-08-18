using System;
using System.Collections;
using ROS.Game.Combat;
using UnityEngine;

namespace ROS.Game.BattleRoyale
{
    [Serializable]
    public struct ZonePhase
    {
        [Min(1f)] public float radius;
        [Min(0f)] public float waitSeconds;
        [Min(0.1f)] public float shrinkSeconds;
        [Min(0f)] public float damagePerSecond;
    }

    public sealed class SafeZoneController : MonoBehaviour
    {
        [SerializeField] private ZonePhase[] phases =
        {
            new ZonePhase { radius = 250f, waitSeconds = 30f, shrinkSeconds = 45f, damagePerSecond = 1f },
            new ZonePhase { radius = 140f, waitSeconds = 25f, shrinkSeconds = 40f, damagePerSecond = 2f },
            new ZonePhase { radius = 70f, waitSeconds = 20f, shrinkSeconds = 35f, damagePerSecond = 4f },
            new ZonePhase { radius = 25f, waitSeconds = 15f, shrinkSeconds = 30f, damagePerSecond = 8f }
        };

        public Vector3 Center { get; private set; }
        public float Radius { get; private set; }
        public int CurrentPhase { get; private set; } = -1;
        public float CurrentDamagePerSecond { get; private set; } 
        public event Action<int, Vector3, float> PhaseChanged;

        private Coroutine _routine;

        public void Begin(Vector3 initialCenter, float initialRadius)
        {
            Center = initialCenter;
            Radius = initialRadius;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(RunPhases());
        }

        private IEnumerator RunPhases()
        {
            for (int i = 0; i < phases.Length; i++)
            {
                CurrentPhase = i;
                var phase = phases[i];
                CurrentDamagePerSecond = phase.damagePerSecond;
                yield return new WaitForSeconds(phase.waitSeconds);

                Vector3 oldCenter = Center;
                float oldRadius = Radius;
                float maxOffset = Mathf.Max(0f, oldRadius - phase.radius);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * maxOffset;
                Vector3 nextCenter = oldCenter + new Vector3(offset.x, 0f, offset.y);

                float elapsed = 0f;
                while (elapsed < phase.shrinkSeconds)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / phase.shrinkSeconds);
                    Center = Vector3.Lerp(oldCenter, nextCenter, t);
                    Radius = Mathf.Lerp(oldRadius, phase.radius, t);
                    yield return null;
                }
                PhaseChanged?.Invoke(i, Center, Radius);
            }
        }

        public bool IsOutside(Vector3 position)
        {
            Vector2 a = new Vector2(position.x, position.z);
            Vector2 b = new Vector2(Center.x, Center.z);
            return Vector2.Distance(a, b) > Radius;
        }

        public void ApplyZoneDamage(Health health)
        {
            if (health != null && health.IsAlive && IsOutside(health.transform.position) && CurrentDamagePerSecond > 0f)
                health.ApplyDamage(new DamageInfo(CurrentDamagePerSecond * Time.deltaTime, health.transform.position, Vector3.zero, gameObject));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(Application.isPlaying ? Center : transform.position, Application.isPlaying ? Radius : 10f);
        }
    }
}
