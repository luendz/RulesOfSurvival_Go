using System.Reflection;
using NUnit.Framework;
using ROS.Game.Combat;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class HealthTests
    {
        private GameObject _target;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _target = new GameObject("Health_TestTarget");
            _health = _target.AddComponent<Health>();

            MethodInfo awake = typeof(Health).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(awake, Is.Not.Null);
            awake.Invoke(_health, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void ApplyDamage_ConsumesArmorBeforeHealth()
        {
            _health.AddArmor(100f);

            _health.ApplyDamage(
                new DamageInfo(
                    40f,
                    Vector3.zero,
                    Vector3.forward,
                    null
                )
            );

            Assert.That(_health.CurrentArmor, Is.EqualTo(78f).Within(0.001f));
            Assert.That(_health.CurrentHealth, Is.EqualTo(82f).Within(0.001f));
        }

        [Test]
        public void LethalDamage_RaisesDeathOnlyOnce()
        {
            int deathCount = 0;
            _health.Died += _ => deathCount++;

            DamageInfo lethalDamage =
                new DamageInfo(
                    500f,
                    Vector3.zero,
                    Vector3.forward,
                    null
                );

            _health.ApplyDamage(lethalDamage);
            _health.ApplyDamage(lethalDamage);

            Assert.That(_health.IsAlive, Is.False);
            Assert.That(_health.CurrentHealth, Is.Zero);
            Assert.That(deathCount, Is.EqualTo(1));
        }
    }
}
