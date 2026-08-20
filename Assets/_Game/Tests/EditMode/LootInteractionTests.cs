using NUnit.Framework;
using ROS.Game.Core;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class LootInteractionTests
    {
        private readonly System.Collections.Generic.List<Object> _created =
            new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void Pickup_CollectsOnlyTheAmountThatFits()
        {
            GameObject player = CreatePlayer(5f);
            InventoryItemDefinition item = CreateItem(
                "ammo_partial",
                ItemType.Ammo,
                2f
            );

            LootPickup pickup = LootPickup.SpawnRuntime(
                item,
                5,
                Vector3.zero,
                null,
                0f
            );
            _created.Add(pickup.gameObject);

            Assert.That(pickup.TryCollect(player), Is.True);
            Assert.That(
                player.GetComponent<InventoryComponent>().GetAmount(item),
                Is.EqualTo(2)
            );
            Assert.That(pickup.Amount, Is.EqualTo(3));
            Assert.That(pickup.IsConsumed, Is.False);
        }

        [Test]
        public void Backpack_UpdatesInventoryCapacityWhenEquipped()
        {
            GameObject player = CreatePlayer(100f);
            PlayerLootEquipment equipment =
                player.AddComponent<PlayerLootEquipment>();

            InventoryItemDefinition backpack = CreateItem(
                "backpack_test",
                ItemType.Backpack,
                1f
            );
            backpack.pickupMode = LootPickupMode.EquipOnPickup;
            backpack.backpackCapacity = 180f;

            Assert.That(
                equipment.TryEquip(backpack, out InventoryItemDefinition replaced),
                Is.True
            );
            Assert.That(replaced, Is.Null);
            Assert.That(
                player.GetComponent<InventoryComponent>().Capacity,
                Is.EqualTo(180f)
            );
            Assert.That(equipment.BackpackItem, Is.SameAs(backpack));
        }

        [Test]
        public void Equipment_RejectsSameOrLowerLevels()
        {
            GameObject player = CreatePlayer(100f);
            PlayerLootEquipment equipment =
                player.AddComponent<PlayerLootEquipment>();

            InventoryItemDefinition backpack1 = CreateItem(
                "backpack_level_1",
                ItemType.Backpack,
                1f
            );
            backpack1.backpackCapacity = 140f;

            InventoryItemDefinition backpack2 = CreateItem(
                "backpack_level_2",
                ItemType.Backpack,
                1f
            );
            backpack2.backpackCapacity = 180f;

            Assert.That(
                equipment.TryEquip(backpack2, out _),
                Is.True
            );
            Assert.That(equipment.CanEquip(backpack1), Is.False);
            Assert.That(
                equipment.TryEquip(backpack1, out _),
                Is.False
            );
            Assert.That(
                equipment.BackpackItem,
                Is.SameAs(backpack2)
            );

            LootPickup lowerBackpackPickup =
                LootPickup.SpawnRuntime(
                    backpack1,
                    1,
                    Vector3.zero,
                    null,
                    0f
                );
            _created.Add(lowerBackpackPickup.gameObject);

            Assert.That(
                lowerBackpackPickup.CanInteract(player),
                Is.False
            );
            Assert.That(
                lowerBackpackPickup.IsBlockedByEquipmentLevel(
                    player
                ),
                Is.True
            );

            InventoryItemDefinition helmet1 = CreateItem(
                "helmet_level_1",
                ItemType.Helmet,
                1f
            );
            helmet1.protectionLevel = ProtectionLevel.Level1;

            InventoryItemDefinition helmet2 = CreateItem(
                "helmet_level_2",
                ItemType.Helmet,
                1f
            );
            helmet2.protectionLevel = ProtectionLevel.Level2;

            Assert.That(
                equipment.TryEquip(helmet2, out _),
                Is.True
            );
            Assert.That(equipment.CanEquip(helmet1), Is.False);

            InventoryItemDefinition vest2 = CreateItem(
                "vest_level_2",
                ItemType.Armor,
                1f
            );
            vest2.protectionLevel = ProtectionLevel.Level2;

            InventoryItemDefinition vest2Duplicate = CreateItem(
                "vest_level_2_duplicate",
                ItemType.Armor,
                1f
            );
            vest2Duplicate.protectionLevel = ProtectionLevel.Level2;

            Assert.That(
                equipment.TryEquip(vest2, out _),
                Is.True
            );
            Assert.That(
                equipment.CanEquip(vest2Duplicate),
                Is.False
            );
        }

        [Test]
        public void Drop_RemovesRequestedAmountAndCreatesWorldPickup()
        {
            GameObject player = CreatePlayer(20f);
            InventoryComponent inventory =
                player.GetComponent<InventoryComponent>();
            LootDropController drop =
                player.AddComponent<LootDropController>();
            InventoryItemDefinition item = CreateItem(
                "heal_drop",
                ItemType.Healing,
                1f
            );

            inventory.Add(item, 3);

            Assert.That(drop.Drop(item, 2), Is.True);
            Assert.That(inventory.GetAmount(item), Is.EqualTo(1));

            LootPickup[] pickups =
                Object.FindObjectsByType<LootPickup>(
                    FindObjectsSortMode.None
                );

            Assert.That(pickups, Has.Length.EqualTo(1));
            Assert.That(pickups[0].Item, Is.SameAs(item));
            Assert.That(pickups[0].Amount, Is.EqualTo(2));
            _created.Add(pickups[0].gameObject);
        }

        [Test]
        public void RuntimePickup_UsesAssignedWorldModelWithoutRootCube()
        {
            InventoryItemDefinition item = CreateItem(
                "backpack_visual",
                ItemType.Backpack,
                1f
            );
            GameObject model = new GameObject("Backpack_Model");
            item.worldModel = model;
            _created.Add(model);

            LootPickup pickup = LootPickup.SpawnRuntime(
                item,
                1,
                Vector3.zero,
                null,
                0f
            );
            _created.Add(pickup.gameObject);

            Assert.That(pickup.RuntimeVisual, Is.Not.Null);
            Assert.That(pickup.IsUsingFallbackVisual, Is.False);
            Assert.That(
                pickup.GetComponent<MeshRenderer>(),
                Is.Null
            );
            Assert.That(
                pickup.transform.localScale,
                Is.EqualTo(Vector3.one)
            );
        }

        [Test]
        public void RuntimePickup_UsesFallbackOnlyWhenWorldModelIsMissing()
        {
            InventoryItemDefinition item = CreateItem(
                "item_without_art",
                ItemType.Misc,
                1f
            );

            LootPickup pickup = LootPickup.SpawnRuntime(
                item,
                1,
                Vector3.zero,
                null,
                0f
            );
            _created.Add(pickup.gameObject);

            Assert.That(pickup.RuntimeVisual, Is.Null);
            Assert.That(pickup.IsUsingFallbackVisual, Is.True);
            Assert.That(
                pickup.GetComponent<MeshRenderer>(),
                Is.Null
            );
        }

        [Test]
        public void DeathContainer_UsesConfiguredModelAndBlueHalo()
        {
            GameObject sourcePlayer = CreatePlayer(20f);
            InventoryComponent sourceInventory =
                sourcePlayer.GetComponent<InventoryComponent>();
            InventoryItemDefinition item = CreateItem(
                "death_loot_visual",
                ItemType.Ammo,
                0.1f
            );

            sourceInventory.Add(item, 1);

            DeathLootContainer container =
                DeathLootContainer.Create(
                    Vector3.zero,
                    sourceInventory
                );
            _created.Add(container.gameObject);

            Assert.That(
                container.transform.Find("CajaLoot_Visual3D"),
                Is.Not.Null
            );
            Assert.That(
                container.GetComponentInChildren<DeathLootHalo>(true),
                Is.Not.Null
            );
            Assert.That(
                container.GetComponentInChildren<DeathLootHalo>(true)
                    .HasFloatingModel,
                Is.True
            );
            Assert.That(
                container.GetComponentsInChildren<Renderer>(true),
                Is.Not.Empty
            );
        }

        private GameObject CreatePlayer(float capacity)
        {
            GameObject player = new GameObject("Loot_TestPlayer");
            InventoryComponent inventory =
                player.AddComponent<InventoryComponent>();
            inventory.SetCapacity(capacity);
            _created.Add(player);
            return player;
        }

        private InventoryItemDefinition CreateItem(
            string id,
            ItemType type,
            float weight)
        {
            InventoryItemDefinition item =
                ScriptableObject.CreateInstance<InventoryItemDefinition>();
            item.itemId = id;
            item.displayName = id;
            item.itemType = type;
            item.weight = weight;
            item.maxStack = 10;
            _created.Add(item);
            return item;
        }
    }
}
