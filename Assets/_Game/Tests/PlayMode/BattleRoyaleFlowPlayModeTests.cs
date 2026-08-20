using System.Collections;
using NUnit.Framework;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
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

            firstPlayer.AddComponent<
                ROS.Game.Character.PlayerMotor
            >();

            PlayerInteractor interactor =
                firstPlayer.AddComponent<PlayerInteractor>();

            BattleRoyaleManager manager =
                managerObject.AddComponent<BattleRoyaleManager>();

            Health firstHealth =
                firstPlayer.AddComponent<Health>();

            Health secondHealth =
                secondPlayer.AddComponent<Health>();

            InventoryComponent inventory =
                firstPlayer.AddComponent<InventoryComponent>();

            int matchFinishedCount = 0;

            manager.MatchFinished +=
                _ => matchFinishedCount++;

            InventoryItemDefinition item =
                ScriptableObject.CreateInstance<
                    InventoryItemDefinition
                >();

            item.itemId = "ammo_test";
            item.maxStack = 30;
            item.weight = 0.1f;

            inventory.Add(item, 12);

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
            Assert.That(manager.Winner, Is.SameAs(secondHealth));
            Assert.That(matchFinishedCount, Is.EqualTo(1));
            Assert.That(manager.GetKillCount(secondHealth), Is.EqualTo(1));
            Assert.That(manager.LastElimination.Victim, Is.SameAs(firstHealth));
            Assert.That(manager.LastElimination.Killer, Is.SameAs(secondHealth));
            Assert.That(manager.LastElimination.Placement, Is.EqualTo(2));

            PlayerEliminationController elimination =
                firstPlayer.GetComponent<
                    PlayerEliminationController
                >();

            Assert.That(elimination, Is.Not.Null);
            Assert.That(elimination.IsEliminated, Is.True);
            Assert.That(
                firstPlayer.GetComponent<PlayerInputReader>().enabled,
                Is.False
            );
            Assert.That(interactor.enabled, Is.False);
            Assert.That(elimination.SpawnedLoot, Is.Not.Null);
            Assert.That(
                elimination.SpawnedLoot.ItemCount,
                Is.EqualTo(12)
            );
            Assert.That(inventory.Stacks, Is.Empty);

            secondHealth.ApplyDamage(
                new DamageInfo(
                    500f,
                    Vector3.zero,
                    Vector3.forward,
                    firstPlayer
                )
            );

            Assert.That(matchFinishedCount, Is.EqualTo(1));
            Assert.That(manager.Winner, Is.SameAs(secondHealth));

            PlayerEliminationController secondElimination =
                secondPlayer.GetComponent<
                    PlayerEliminationController
                >();

            Object.Destroy(managerObject);
            Object.Destroy(firstPlayer);
            Object.Destroy(secondPlayer);
            Object.Destroy(elimination.SpawnedLoot.gameObject);

            if (secondElimination.SpawnedLoot != null)
            {
                Object.Destroy(
                    secondElimination.SpawnedLoot.gameObject
                );
            }

            Object.Destroy(item);

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
