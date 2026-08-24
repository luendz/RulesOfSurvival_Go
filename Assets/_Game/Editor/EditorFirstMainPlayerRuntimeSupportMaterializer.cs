using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstMainPlayerRuntimeSupportMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstMainPlayerRuntimeSupportMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Materialize Main Player Runtime Support")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
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

            PlayerInputReader input = FindInScene<PlayerInputReader>(scene);
            if (input == null)
            {
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            bool changed = false;
            GameObject player = input.gameObject;

            changed |= EnsureComponent<PlayerAimController>(player);

            WeaponEquipmentController equipment =
                player.GetComponent<WeaponEquipmentController>();

            if (equipment != null)
            {
                WeaponController[] weapons =
                    player.GetComponentsInChildren<WeaponController>(true);

                for (int i = 0; i < weapons.Length; i++)
                {
                    WeaponController weapon = weapons[i];
                    if (weapon == null) continue;

                    changed |= EnsureComponent<WeaponMount>(weapon.gameObject);
                    changed |= EnsureComponent<WeaponEffects>(weapon.gameObject);
                    changed |= EnsureComponent<WeaponRecoil>(weapon.gameObject);
                }

                changed |= EnsureSocketFollower(equipment, "rightHandSocket");
                changed |= EnsureSocketFollower(equipment, "backSocket01");
                changed |= EnsureSocketFollower(equipment, "backSocket02");
                changed |= EnsureSocketFollower(equipment, "hipSocket");
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[Editor First] Soporte del jugador principal materializado fisicamente."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool EnsureSocketFollower(
            WeaponEquipmentController equipment,
            string propertyName)
        {
            SerializedObject serialized = new SerializedObject(equipment);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Transform socket = property != null
                ? property.objectReferenceValue as Transform
                : null;

            if (socket == null)
                return false;

            return EnsureComponent<BoneSocketFollower>(socket.gameObject);
        }

        private static bool EnsureComponent<T>(GameObject target)
            where T : Component
        {
            if (target.GetComponent<T>() != null)
                return false;

            target.AddComponent<T>();
            EditorUtility.SetDirty(target);
            return true;
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
