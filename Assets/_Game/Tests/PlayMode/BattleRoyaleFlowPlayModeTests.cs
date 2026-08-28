using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.UI;
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

            firstPlayer.SetActive(false);
            secondPlayer.SetActive(false);

            PlayerInputReader firstInput =
                firstPlayer.AddComponent<PlayerInputReader>();

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

            InventoryComponent secondInventory =
                secondPlayer.AddComponent<InventoryComponent>();

            PlayerEliminationController elimination =
                AddConfiguredElimination(
                    firstPlayer,
                    firstHealth,
                    inventory
                );

            PlayerEliminationController secondElimination =
                AddConfiguredElimination(
                    secondPlayer,
                    secondHealth,
                    secondInventory
                );

            firstPlayer.SetActive(true);
            secondPlayer.SetActive(true);

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

            Assert.That(elimination, Is.Not.Null);
            Assert.That(elimination.IsEliminated, Is.True);
            Assert.That(
                firstInput.enabled,
                Is.False
            );
            Assert.That(interactor.enabled, Is.False);
            Assert.That(elimination.SpawnedLoot, Is.Not.Null);
            Assert.That(
                elimination.SpawnedLoot.ItemCount,
                Is.EqualTo(1)
            );
            Assert.That(elimination.SpawnedLoot.TotalUnitCount, Is.EqualTo(12));
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

        [UnityTest]
        public IEnumerator DeathLootContainer_AllowsPartialAndSelectedLoot()
        {
            GameObject sourceObject =
                new GameObject("LootBox_SourcePlayer");

            GameObject looterObject =
                new GameObject("LootBox_LooterPlayer");

            InventoryComponent source =
                sourceObject.AddComponent<
                    InventoryComponent
                >();

            InventoryComponent destination =
                looterObject.AddComponent<
                    InventoryComponent
                >();

            looterObject.AddComponent<Health>();

            PlayerInputReader input =
                looterObject.AddComponent<
                    PlayerInputReader
                >();

            InventoryItemDefinition item =
                ScriptableObject.CreateInstance<
                    InventoryItemDefinition
                >();

            item.itemId = "loot_box_ammo";
            item.displayName = "Munición de prueba";
            item.maxStack = 30;
            item.weight = 1f;

            source.SetCapacity(20f);
            destination.SetCapacity(3f);
            source.Add(item, 5);

            DeathLootContainer container =
                DeathLootContainer.Create(
                    Vector3.zero,
                    source
                );

            Assert.That(source.Stacks, Is.Empty);
            Assert.That(container.ItemCount, Is.EqualTo(1));
            Assert.That(container.TotalUnitCount, Is.EqualTo(5));
            Assert.That(
                container.SourcePlayerName,
                Is.EqualTo(sourceObject.name)
            );
            Assert.That(
                container.CanInteract(looterObject),
                Is.True
            );

            int firstTransfer =
                container.TryLoot(
                    item,
                    5,
                    destination
                );

            Assert.That(firstTransfer, Is.EqualTo(3));
            Assert.That(destination.GetAmount(item), Is.EqualTo(3));
            Assert.That(container.ItemCount, Is.EqualTo(1));
            Assert.That(container.TotalUnitCount, Is.EqualTo(2));

            destination.SetCapacity(10f);

            Assert.That(
                container.LootAllPossible(destination),
                Is.EqualTo(2)
            );

            Assert.That(input.UiBlocked, Is.False);
            Assert.That(destination.GetAmount(item), Is.EqualTo(5));

            yield return null;

            Assert.That(container == null, Is.True);

            Object.Destroy(sourceObject);
            Object.Destroy(looterObject);
            Object.Destroy(item);

            yield return null;
        }

        private static PlayerEliminationController AddConfiguredElimination(
            GameObject player,
            Health health,
            InventoryComponent inventory)
        {
            PlayerEliminationController elimination =
                player.AddComponent<PlayerEliminationController>();

            SetPrivate(elimination, "health", health);
            SetPrivate(elimination, "inventory", inventory);
            SetPrivate(elimination, "visualRoot", player.transform);
            SetPrivate(elimination, "input", player.GetComponent<PlayerInputReader>());
            SetPrivate(elimination, "interactor", player.GetComponent<PlayerInteractor>());
            return elimination;
        }

        private static void SetPrivate(
            Object target,
            string fieldName,
            Object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
