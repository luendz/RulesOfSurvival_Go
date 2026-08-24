using ROS.Game.Input;
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

            bool changed = false;
            if (equipment != null)
            {
                SerializedObject serialized = new SerializedObject(equipment);
                SerializedProperty slots = serialized.FindProperty("weaponItems");
                if (slots != null && slots.arraySize != PlayerWeaponSlotRules.SlotCount)
                {
                    slots.arraySize = PlayerWeaponSlotRules.SlotCount;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(equipment);
                    changed = true;
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
