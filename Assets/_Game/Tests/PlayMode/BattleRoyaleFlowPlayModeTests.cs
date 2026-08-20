using System.Collections;
using NUnit.Framework;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace ROS.Game.Tests.PlayMode
{
    public sealed class BattleRoyaleFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator LethalDamage_UpdatesAliveCountAndFinishesMatch()
        {
            GameObject managerObject =
                new GameObject("BattleRoyale_TestManager");

            GameObject firstPlayer =
                new GameObject("BattleRoyale_TestPlayer01");

            GameObject secondPlayer =
                new GameObject("BattleRoyale_TestPlayer02");

            BattleRoyaleManager manager =
                managerObject.AddComponent<BattleRoyaleManager>();

            Health firstHealth =
                firstPlayer.AddComponent<Health>();

            Health secondHealth =
                secondPlayer.AddComponent<Health>();

            manager.RegisterPlayer(firstHealth);
            manager.RegisterPlayer(secondHealth);
            manager.BeginMatch();

            Assert.That(manager.State, Is.EqualTo(MatchState.Playing));
            Assert.That(manager.AliveCount, Is.EqualTo(2));

            firstHealth.ApplyDamage(
                new DamageInfo(
                    500f,
                    firstPlayer.transform.position,
                    Vector3.forward,
                    secondPlayer
                )
            );

            yield return null;

            Assert.That(manager.AliveCount, Is.EqualTo(1));
            Assert.That(manager.State, Is.EqualTo(MatchState.Finished));

            Object.Destroy(managerObject);
            Object.Destroy(firstPlayer);
            Object.Destroy(secondPlayer);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SafeZone_DamagesLivingPlayerOutsideItsRadius()
        {
            GameObject zoneObject =
                new GameObject("SafeZone_TestZone");

            GameObject playerObject =
                new GameObject("SafeZone_TestPlayer");

            SafeZoneController zone =
                zoneObject.AddComponent<SafeZoneController>();

            Health health =
                playerObject.AddComponent<Health>();

            playerObject.transform.position =
                new Vector3(20f, 0f, 0f);

            zone.Begin(Vector3.zero, 10f);

            yield return null;

            Assert.That(zone.IsOutside(playerObject.transform.position), Is.True);

            float healthBeforeDamage = health.CurrentHealth;
            zone.ApplyZoneDamage(health);

            Assert.That(health.CurrentHealth, Is.LessThan(healthBeforeDamage));

            Object.Destroy(zoneObject);
            Object.Destroy(playerObject);

            yield return null;
        }
    }
}
