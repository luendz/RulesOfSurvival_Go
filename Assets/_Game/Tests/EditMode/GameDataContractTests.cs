using NUnit.Framework;
using ROS.Game.Core;
using ROS.Game.Inventory;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.Tests.EditMode
{
    public sealed class GameDataContractTests
    {
        [TestCase("weapon_m4a1")]
        [TestCase("ammo.rifle")]
        [TestCase("backpack-level-1")]
        public void StableId_AcceptsCanonicalValues(string stableId)
        {
            Assert.That(GameDataId.IsValid(stableId), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Weapon M4A1")]
        [TestCase("arma_áwm")]
        [TestCase("UPPER_CASE")]
        public void StableId_RejectsUnstableValues(string stableId)
        {
            Assert.That(GameDataId.IsValid(stableId), Is.False);
        }

        [Test]
        public void CurrentDefinitions_ExposeTheSharedContract()
        {
            InventoryItemDefinition item =
                ScriptableObject.CreateInstance<InventoryItemDefinition>();

            WeaponDefinition weapon =
                ScriptableObject.CreateInstance<WeaponDefinition>();

            try
            {
                item.itemId = "heal_bandage";
                weapon.weaponId = "weapon_m4a1";

                IGameDataDefinition itemContract = item;
                IGameDataDefinition weaponContract = weapon;

                Assert.That(itemContract.StableId, Is.EqualTo("heal_bandage"));
                Assert.That(weaponContract.StableId, Is.EqualTo("weapon_m4a1"));
                Assert.That(itemContract.Confidence, Is.EqualTo(DataConfidence.Prototype));
                Assert.That(weaponContract.Confidence, Is.EqualTo(DataConfidence.Prototype));
            }
            finally
            {
                Object.DestroyImmediate(item);
                Object.DestroyImmediate(weapon);
            }
        }

        [Test]
        public void WeaponDefinition_CyclesOnlySupportedFireModes()
        {
            WeaponDefinition weapon =
                ScriptableObject.CreateInstance<WeaponDefinition>();

            try
            {
                weapon.fireMode = WeaponFireMode.Auto;
                weapon.supportsSingle = true;
                weapon.supportsBurst = false;
                weapon.supportsAuto = true;

                Assert.That(
                    weapon.GetNextFireMode(WeaponFireMode.Auto),
                    Is.EqualTo(WeaponFireMode.Single)
                );
                Assert.That(
                    weapon.GetNextFireMode(WeaponFireMode.Single),
                    Is.EqualTo(WeaponFireMode.Auto)
                );
            }
            finally
            {
                Object.DestroyImmediate(weapon);
            }
        }

        [Test]
        public void WeaponDefinition_UsesLongerEmptyReload()
        {
            WeaponDefinition weapon =
                ScriptableObject.CreateInstance<WeaponDefinition>();

            try
            {
                weapon.reloadTime = 2.1f;
                weapon.emptyReloadTime = 2.8f;

                Assert.That(
                    weapon.GetReloadDuration(false),
                    Is.EqualTo(2.1f)
                );
                Assert.That(
                    weapon.GetReloadDuration(true),
                    Is.EqualTo(2.8f)
                );
            }
            finally
            {
                Object.DestroyImmediate(weapon);
            }
        }
    }
}
