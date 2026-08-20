using NUnit.Framework;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class InventoryComponentTests
    {
        private GameObject _owner;
        private InventoryComponent _inventory;
        private InventoryItemDefinition _item;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Inventory_TestOwner");
            _inventory = _owner.AddComponent<InventoryComponent>();

            _item =
                ScriptableObject.CreateInstance<InventoryItemDefinition>();

            _item.itemId = "ammo_rifle";
            _item.maxStack = 2;
            _item.weight = 2f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_item);
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void Add_RespectsCapacityWithoutPartiallyAdding()
        {
            _inventory.SetCapacity(5f);

            Assert.That(_inventory.Add(_item, 2), Is.True);
            Assert.That(_inventory.Add(_item, 1), Is.False);

            Assert.That(_inventory.UsedCapacity, Is.EqualTo(4f));
            Assert.That(_inventory.Stacks, Has.Count.EqualTo(1));
            Assert.That(_inventory.Stacks[0].amount, Is.EqualTo(2));
        }

        [Test]
        public void AddAndRemove_KeepStacksConsistent()
        {
            _inventory.SetCapacity(20f);

            Assert.That(_inventory.Add(_item, 5), Is.True);
            Assert.That(_inventory.Stacks, Has.Count.EqualTo(3));
            Assert.That(_inventory.Remove(_item, 3), Is.True);

            Assert.That(_inventory.Stacks, Has.Count.EqualTo(1));
            Assert.That(_inventory.Stacks[0].amount, Is.EqualTo(2));
            Assert.That(_inventory.UsedCapacity, Is.EqualTo(4f));
        }

        [Test]
        public void Remove_DoesNotMutateInventoryWhenAmountIsUnavailable()
        {
            _inventory.SetCapacity(20f);
            _inventory.Add(_item, 2);

            Assert.That(_inventory.Remove(_item, 3), Is.False);
            Assert.That(_inventory.Stacks[0].amount, Is.EqualTo(2));
        }
    }
}
