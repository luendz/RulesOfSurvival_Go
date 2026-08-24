using ROS.Game.Input;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstRosWeaponSlotSerializedRepair
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstRosWeaponSlotSerializedRepair()
        {
            EditorApplication.delayCall += Repair;
        }

        [MenuItem("Rules Of Survival/Editor First/Repair Serialized ROS Weapon Slots")]
        public static void Repair()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
            {
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            PlayerInputReader player = FindInScene<PlayerInputReader>(scene);
            PlayerLootEquipment equipment = player != null
                ? player.GetComponent<PlayerLootEquipment>()
                : null;
            WeaponEquipmentController weapons = player != null
                ? player.GetComponent<WeaponEquipmentController>()
                : null;

            bool changed = false;
            if (equipment != null)
            {
                SerializedObject serialized = new SerializedObject(equipment);
                SerializedProperty slots = serialized.FindProperty("weaponItems");
                if (slots != null)
                {
                    if (slots.arraySize != PlayerWeaponSlotRules.SlotCount)
                    {
                        slots.arraySize = PlayerWeaponSlotRules.SlotCount;
                        changed = true;
                    }

                    if (weapons != null)
                    {
                        changed |= AssignStartingWeaponItem(
                            slots,
                            1,
                            weapons.PrimarySlot1
                        );
                        changed |= AssignStartingWeaponItem(
                            slots,
                            2,
                            weapons.PrimarySlot2
                        );
                        changed |= AssignStartingWeaponItem(
                            slots,
                            3,
                            weapons.SidearmSlot
                        );
                    }

                    if (changed)
                    {
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(equipment);
                    }
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool AssignStartingWeaponItem(
            SerializedProperty slots,
            int slot,
            WeaponController weapon
        )
        {
            if (slots == null ||
                slot < 1 ||
                slot > slots.arraySize ||
                weapon == null ||
                weapon.Definition == null)
            {
                return false;
            }

            SerializedProperty element = slots.GetArrayElementAtIndex(slot - 1);
            if (element.objectReferenceValue != null)
                return false;

            InventoryItemDefinition item = FindItemForDefinition(weapon.Definition);
            if (item == null)
                return false;

            element.objectReferenceValue = item;
            return true;
        }

        private static InventoryItemDefinition FindItemForDefinition(
            WeaponDefinition definition
        )
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:InventoryItemDefinition",
                new[] { "Assets/_Game/Data/Weapons" }
            );

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                InventoryItemDefinition item =
                    AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);

                if (item != null && item.weaponDefinition == definition)
                    return item;
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T[] found = roots[i].GetComponentsInChildren<T>(true);
                if (found.Length > 0)
                    return found[0];
            }
            return null;
        }
    }
}
