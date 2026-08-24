using ROS.Game.Core;
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
    public static class EditorFirstEmptyPlayerLoadoutMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private const string PlayerPrefabPath =
            "Assets/_Game/Prefabs/Player_Prototype.prefab";

        static EditorFirstEmptyPlayerLoadoutMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Reset Main Player To Empty Loadout")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            bool prefabChanged = ClearPlayerPrefab();
            if (prefabChanged)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    PlayerPrefabPath,
                    ImportAssetOptions.ForceUpdate
                );
            }

            if (!System.IO.File.Exists(ScenePath))
                return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive
                );
            }

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            PlayerInputReader player = FindMainPlayer(scene);
            bool sceneChanged = player != null &&
                                ClearPlayerState(player.gameObject, false);

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);

            if (prefabChanged || sceneChanged)
            {
                Debug.Log(
                    "[Editor First] Jugador principal restaurado a carga inicial vacia: " +
                    "sin armas equipadas, sin armas recogidas y slots 1-5 vacios."
                );
            }
        }

        private static bool ClearPlayerPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
                return false;

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == null)
                return false;

            bool changed = false;
            try
            {
                changed |= ClearPlayerState(root, true);

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return changed;
        }

        private static bool ClearPlayerState(
            GameObject player,
            bool removeWeaponObjects
        )
        {
            if (player == null)
                return false;

            bool changed = false;

            WeaponEquipmentController equipment =
                player.GetComponent<WeaponEquipmentController>();

            if (equipment != null)
            {
                SerializedObject serialized = new SerializedObject(equipment);
                changed |= SetObjectReference(serialized, "primarySlot1", null);
                changed |= SetObjectReference(serialized, "primarySlot2", null);
                changed |= SetObjectReference(serialized, "sidearmSlot", null);
                changed |= SetBool(serialized, "startWithSlot1Equipped", false);
                changed |= SetBool(serialized, "autoDiscoverWeapons", false);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(equipment);
            }

            PlayerLootEquipment loot = player.GetComponent<PlayerLootEquipment>();
            if (loot != null)
            {
                SerializedObject serialized = new SerializedObject(loot);
                SerializedProperty items = serialized.FindProperty("weaponItems");
                if (items != null)
                {
                    if (items.arraySize != PlayerWeaponSlotRules.SlotCount)
                    {
                        items.arraySize = PlayerWeaponSlotRules.SlotCount;
                        changed = true;
                    }

                    for (int i = 0; i < items.arraySize; i++)
                    {
                        SerializedProperty item = items.GetArrayElementAtIndex(i);
                        if (item.objectReferenceValue != null)
                        {
                            item.objectReferenceValue = null;
                            changed = true;
                        }
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(loot);
            }

            PlayerAuxiliaryWeaponSlots auxiliary =
                player.GetComponent<PlayerAuxiliaryWeaponSlots>();
            if (auxiliary != null)
            {
                SerializedObject serialized = new SerializedObject(auxiliary);
                SerializedProperty selected =
                    serialized.FindProperty("selectedAuxiliarySlot");
                if (selected != null && selected.intValue != 0)
                {
                    selected.intValue = 0;
                    changed = true;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(auxiliary);
            }

            InventoryComponent inventory = player.GetComponent<InventoryComponent>();
            if (inventory != null)
                changed |= RemoveStartingWeaponItems(inventory);

            if (removeWeaponObjects)
            {
                WeaponController[] weapons =
                    player.GetComponentsInChildren<WeaponController>(true);

                for (int i = weapons.Length - 1; i >= 0; i--)
                {
                    WeaponController weapon = weapons[i];
                    if (weapon == null)
                        continue;

                    Object.DestroyImmediate(weapon.gameObject);
                    changed = true;
                }
            }
            else
            {
                WeaponController[] weapons =
                    player.GetComponentsInChildren<WeaponController>(true);

                for (int i = 0; i < weapons.Length; i++)
                {
                    WeaponController weapon = weapons[i];
                    if (weapon == null)
                        continue;

                    if (PrefabUtility.IsPartOfPrefabInstance(weapon.gameObject))
                        continue;

                    Object.DestroyImmediate(weapon.gameObject);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool RemoveStartingWeaponItems(InventoryComponent inventory)
        {
            SerializedObject serialized = new SerializedObject(inventory);
            SerializedProperty stacks = serialized.FindProperty("stacks");
            if (stacks == null)
                return false;

            bool changed = false;
            for (int i = stacks.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty stack = stacks.GetArrayElementAtIndex(i);
                SerializedProperty itemProperty = stack.FindPropertyRelative("item");
                InventoryItemDefinition item =
                    itemProperty != null
                        ? itemProperty.objectReferenceValue as InventoryItemDefinition
                        : null;

                if (item == null)
                    continue;

                if (item.itemType != ItemType.Weapon &&
                    item.itemType != ItemType.Throwable)
                {
                    continue;
                }

                stacks.DeleteArrayElementAtIndex(i);
                changed = true;
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(inventory);
            }

            return changed;
        }

        private static PlayerInputReader FindMainPlayer(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                PlayerInputReader[] inputs =
                    roots[i].GetComponentsInChildren<PlayerInputReader>(true);

                for (int j = 0; j < inputs.Length; j++)
                {
                    PlayerInputReader input = inputs[j];
                    if (input == null)
                        continue;

                    if (input.gameObject.CompareTag("Player"))
                        return input;
                }
            }

            for (int i = 0; i < roots.Length; i++)
            {
                PlayerInputReader fallback =
                    roots[i].GetComponentInChildren<PlayerInputReader>(true);
                if (fallback != null)
                    return fallback;
            }

            return null;
        }

        private static bool SetObjectReference(
            SerializedObject serialized,
            string propertyName,
            Object value
        )
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
                return false;

            property.objectReferenceValue = value;
            return true;
        }

        private static bool SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value
        )
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.boolValue == value)
                return false;

            property.boolValue = value;
            return true;
        }
    }
}
