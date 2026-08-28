using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.TestTools;

namespace ROS.Game.Tests.PlayMode
{
    public sealed class WeaponSwitchPlayModeTests
    {
        [UnityTest]
        public IEnumerator EquipFromEmpty_UsesFastAnimatedWindow()
        {
            GameObject player = new GameObject("WeaponSwitch_Player");
            GameObject weaponObject = new GameObject("WeaponSwitch_Target");
            weaponObject.transform.SetParent(player.transform, false);

            try
            {
                player.AddComponent<PlayerInputReader>();
                WeaponEquipmentController equipment =
                    player.AddComponent<WeaponEquipmentController>();
                SetStartEquipped(equipment, false);

                weaponObject.AddComponent<WeaponMount>();
                WeaponController weapon =
                    weaponObject.AddComponent<WeaponController>();
                LogAssert.Expect(
                    LogType.Error,
                    "WeaponSwitch_Target no tiene WeaponEffects configurado en su prefab."
                );
                LogAssert.Expect(
                    LogType.Error,
                    "WeaponSwitch_Target no tiene WeaponRecoil configurado en su prefab."
                );
                equipment.SetWeaponInSlot(1, weapon);

                equipment.RequestEquipSlot(1);

                Assert.That(equipment.IsSwitchingWeapon, Is.True);
                Assert.That(equipment.EquippedWeapon, Is.Null);

                yield return new WaitForSeconds(0.32f);

                Assert.That(equipment.EquippedWeapon, Is.EqualTo(weapon));
                Assert.That(equipment.IsSwitchingWeapon, Is.True);

                yield return new WaitForSeconds(0.47f);

                Assert.That(equipment.IsSwitchingWeapon, Is.False);
            }
            finally
            {
                Object.Destroy(player);
            }
        }

        [UnityTest]
        public IEnumerator AuxiliarySwitch_UsesSameAnimatedWindow()
        {
            GameObject player = new GameObject("AuxiliarySwitch_Player");

            try
            {
                player.AddComponent<PlayerInputReader>();
                WeaponEquipmentController equipment =
                    player.AddComponent<WeaponEquipmentController>();
                SetStartEquipped(equipment, false);

                bool swapped = false;
                Assert.That(
                    equipment.RequestAuxiliarySwitch(() => swapped = true),
                    Is.True
                );

                Assert.That(equipment.IsSwitchingWeapon, Is.True);
                Assert.That(swapped, Is.False);

                yield return new WaitForSeconds(0.32f);

                Assert.That(swapped, Is.True);
                Assert.That(equipment.IsSwitchingWeapon, Is.True);

                yield return new WaitForSeconds(0.47f);

                Assert.That(equipment.IsSwitchingWeapon, Is.False);
            }
            finally
            {
                Object.Destroy(player);
            }
        }

        private static void SetStartEquipped(
            WeaponEquipmentController equipment,
            bool value)
        {
            FieldInfo field = typeof(WeaponEquipmentController).GetField(
                "startWithSlot1Equipped",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.That(field, Is.Not.Null);
            field.SetValue(equipment, value);
        }
    }
}
