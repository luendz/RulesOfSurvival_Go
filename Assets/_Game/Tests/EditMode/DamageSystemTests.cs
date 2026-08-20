using System.Reflection;
using NUnit.Framework;
using ROS.Game.Combat;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class DamageSystemTests
    {
        private GameObject _target;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _target = new GameObject("DamageSystem_TestTarget");
            _health = _target.AddComponent<Health>();

            MethodInfo awake = typeof(Health).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.That(awake, Is.Not.Null);
            awake.Invoke(_health, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_target);
        }

        [TestCase(HitZone.Head, 40f)]
        [TestCase(HitZone.Torso, 20f)]
        [TestCase(HitZone.Arm, 15f)]
        [TestCase(HitZone.Leg, 13f)]
        public void FirearmDamage_AppliesHitZoneMultiplier(
            HitZone hitZone,
            float expectedDamage)
        {
            _health.ApplyDamage(
                CreateDamage(
                    20f,
                    DamageType.Firearm,
                    hitZone
                )
            );

            Assert.That(
                _health.CurrentHealth,
                Is.EqualTo(100f - expectedDamage)
                    .Within(0.001f)
            );
        }

        [Test]
        public void Level2Helmet_ReducesHeadshotAndLosesDurability()
        {
            ProtectiveEquipment protection =
                _target.AddComponent<ProtectiveEquipment>();

            protection.EquipHelmet(ProtectionLevel.Level2);

            _health.ApplyDamage(
                CreateDamage(
                    20f,
                    DamageType.Firearm,
                    HitZone.Head
                )
            );

            Assert.That(
                _health.CurrentHealth,
                Is.EqualTo(76f).Within(0.001f)
            );

            Assert.That(
                protection.HelmetDurability,
                Is.EqualTo(134f).Within(0.001f)
            );

            Assert.That(_health.LastDamageResult.IsHeadshot, Is.True);
            Assert.That(
                _health.LastDamageResult.AbsorbedDamage,
                Is.EqualTo(16f).Within(0.001f)
            );
        }

        [Test]
        public void Level2Vest_ReducesTorsoAndExplosionDamage()
        {
            ProtectiveEquipment protection =
                _target.AddComponent<ProtectiveEquipment>();

            protection.EquipVest(ProtectionLevel.Level2);

            _health.ApplyDamage(
                CreateDamage(
                    50f,
                    DamageType.Explosion,
                    HitZone.Torso
                )
            );

            Assert.That(
                _health.CurrentHealth,
                Is.EqualTo(70f).Within(0.001f)
            );

            Assert.That(
                protection.VestDurability,
                Is.EqualTo(160f).Within(0.001f)
            );
        }

        [Test]
        public void FallDamage_BypassesHelmetAndVest()
        {
            ProtectiveEquipment protection =
                _target.AddComponent<ProtectiveEquipment>();

            protection.EquipHelmet(ProtectionLevel.Level2);
            protection.EquipVest(ProtectionLevel.Level2);

            _health.ApplyDamage(
                CreateDamage(
                    40f,
                    DamageType.Fall,
                    HitZone.Leg
                )
            );

            Assert.That(
                _health.CurrentHealth,
                Is.EqualTo(60f).Within(0.001f)
            );

            Assert.That(
                protection.CurrentTotalDurability,
                Is.EqualTo(330f).Within(0.001f)
            );
        }

        [Test]
        public void DamageEvent_ReportsResolvedDamageAndFatalState()
        {
            DamageResult received = default;
            int eventCount = 0;

            _health.Damaged += result =>
            {
                received = result;
                eventCount++;
            };

            _health.ApplyDamage(
                CreateDamage(
                    60f,
                    DamageType.Firearm,
                    HitZone.Head
                )
            );

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(received.WasFatal, Is.True);
            Assert.That(received.HealthDamage, Is.EqualTo(100f));
        }

        [Test]
        public void FallDamage_UsesQuadraticVelocityCurve()
        {
            Assert.That(
                FallDamageReceiver.CalculateDamage(
                    12f,
                    12f,
                    28f,
                    100f
                ),
                Is.Zero
            );

            Assert.That(
                FallDamageReceiver.CalculateDamage(
                    20f,
                    12f,
                    28f,
                    100f
                ),
                Is.EqualTo(25f).Within(0.001f)
            );

            Assert.That(
                FallDamageReceiver.CalculateDamage(
                    28f,
                    12f,
                    28f,
                    100f
                ),
                Is.EqualTo(100f).Within(0.001f)
            );
        }

        [Test]
        public void ExplosionDamage_FallsOffFromInnerRadiusToEdge()
        {
            Assert.That(
                ExplosionDamageSource.CalculateDamage(
                    1f,
                    10f,
                    0.2f,
                    100f
                ),
                Is.EqualTo(100f).Within(0.001f)
            );

            Assert.That(
                ExplosionDamageSource.CalculateDamage(
                    6f,
                    10f,
                    0.2f,
                    100f
                ),
                Is.EqualTo(50f).Within(0.001f)
            );

            Assert.That(
                ExplosionDamageSource.CalculateDamage(
                    10f,
                    10f,
                    0.2f,
                    100f
                ),
                Is.Zero
            );
        }

        private static DamageInfo CreateDamage(
            float amount,
            DamageType damageType,
            HitZone hitZone)
        {
            return new DamageInfo(
                amount,
                Vector3.zero,
                Vector3.forward,
                null,
                damageType,
                hitZone
            );
        }
    }
}
