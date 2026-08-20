using NUnit.Framework;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class LootTableDefinitionTests
    {
        private LootTableDefinition _table;
        private InventoryItemDefinition _firstItem;
        private InventoryItemDefinition _secondItem;

        [SetUp]
        public void SetUp()
        {
            _table =
                ScriptableObject.CreateInstance<LootTableDefinition>();

            _firstItem = CreateItem("item_first");
            _secondItem = CreateItem("item_second");

            SerializedObject serializedTable =
                new SerializedObject(_table);

            SerializedProperty entries =
                serializedTable.FindProperty("entries");

            entries.arraySize = 2;
            ConfigureEntry(entries.GetArrayElementAtIndex(0), _firstItem, 1f);
            ConfigureEntry(entries.GetArrayElementAtIndex(1), _secondItem, 3f);

            serializedTable.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
            Object.DestroyImmediate(_firstItem);
            Object.DestroyImmediate(_secondItem);
        }

        [Test]
        public void DeterministicRoll_SelectsExpectedWeightedEntry()
        {
            Assert.That(
                _table.TryPickFromNormalizedRoll(0f, out LootEntry first),
                Is.True
            );

            Assert.That(
                _table.TryPickFromNormalizedRoll(0.75f, out LootEntry second),
                Is.True
            );

            Assert.That(first.item, Is.SameAs(_firstItem));
            Assert.That(second.item, Is.SameAs(_secondItem));
        }

        [Test]
        public void DeterministicRoll_RejectsNaN()
        {
            Assert.That(
                _table.TryPickFromNormalizedRoll(float.NaN, out _),
                Is.False
            );
        }

        private static InventoryItemDefinition CreateItem(string itemId)
        {
            InventoryItemDefinition item =
                ScriptableObject.CreateInstance<InventoryItemDefinition>();

            item.itemId = itemId;
            item.maxStack = 1;
            item.weight = 1f;

            return item;
        }

        private static void ConfigureEntry(
            SerializedProperty entry,
            InventoryItemDefinition item,
            float weight
        )
        {
            entry.FindPropertyRelative("item").objectReferenceValue = item;
            entry.FindPropertyRelative("weight").floatValue = weight;
            entry.FindPropertyRelative("minAmount").intValue = 1;
            entry.FindPropertyRelative("maxAmount").intValue = 1;
        }
    }
}
