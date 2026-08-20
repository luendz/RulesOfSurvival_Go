using ROS.Game.Character;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(PlayerMotor))]
    public sealed class FallDamageReceiver : MonoBehaviour
    {
        [SerializeField] private float safeImpactSpeed = 12f;
        [SerializeField] private float lethalImpactSpeed = 28f;
        [SerializeField] private float maximumDamage = 100f;

        private Health _health;
        private PlayerMotor _motor;
        private bool _wasGrounded;
        private float _peakDownwardSpeed;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _motor = GetComponent<PlayerMotor>();
            _wasGrounded = _motor != null && _motor.IsGrounded;
        }

        private void Update()
        {
            if (_health == null ||
                _motor == null ||
                !_health.IsAlive)
            {
                return;
            }

            bool grounded = _motor.IsGrounded;

            if (!grounded)
            {
                _peakDownwardSpeed = Mathf.Max(
                    _peakDownwardSpeed,
                    Mathf.Max(0f, -_motor.Velocity.y)
                );
            }
            else if (!_wasGrounded)
            {
                ApplyLandingDamage(_peakDownwardSpeed);
                _peakDownwardSpeed = 0f;
            }

            _wasGrounded = grounded;
        }

        public void ApplyLandingDamage(float impactSpeed)
        {
            if (_health == null || !_health.IsAlive)
            {
                return;
            }

            float amount = CalculateDamage(
                impactSpeed,
                safeImpactSpeed,
                lethalImpactSpeed,
                maximumDamage
            );

            if (amount <= 0f)
            {
                return;
            }

            _health.ApplyDamage(
                new DamageInfo(
                    amount,
                    transform.position,
                    Vector3.down,
                    null,
                    DamageType.Fall,
                    HitZone.Leg
                )
            );
        }

        public static float CalculateDamage(
            float impactSpeed,
            float safeSpeed,
            float lethalSpeed,
            float maximum)
        {
            if (impactSpeed <= safeSpeed || maximum <= 0f)
            {
                return 0f;
            }

            float safeRange = Mathf.Max(
                0.01f,
                lethalSpeed - safeSpeed
            );

            float normalized = Mathf.Clamp01(
                (impactSpeed - safeSpeed) / safeRange
            );

            return maximum * normalized * normalized;
        }
    }
}
