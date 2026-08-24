using ROS.Game.CameraSystem;
using ROS.Game.Input;
using ROS.Game.Parachute;
using ROS.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Garantiza que el menu fisico de inicio BR sea quien controle realmente
    /// el cursor y los botones en build. Evita controladores heredados en otros
    /// objetos que pueden estar desactivados y dejar el menu visible sin mouse.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstStartMenuControllerNormalizer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private const string MenuPath =
            "01_RUNTIME_UI/MENU_BATTLE_ROYALE_EDITABLE";

        static EditorFirstStartMenuControllerNormalizer()
        {
            EditorApplication.delayCall += Normalize;
        }

        [MenuItem("Rules Of Survival/Editor First/Fix BR Menu Cursor Controller")]
        public static void Normalize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            if (!System.IO.File.Exists(ScenePath))
                return;

            // Primero deja listas las referencias normales del flujo BR.
            EditorFirstStartMenuSceneRepair.RepairFunctionalSceneMenu();

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

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);

            Transform physicalMenu =
                presentationRoot != null
                    ? presentationRoot.transform.Find(MenuPath)
                    : null;

            if (physicalMenu == null)
            {
                Debug.LogError(
                    "[Editor First] No se encontro MENU_BATTLE_ROYALE_EDITABLE en la escena funcional."
                );
                CloseIfNeeded(scene, openedTemporarily);
                return;
            }

            bool changed = false;

            if (!physicalMenu.gameObject.activeSelf)
            {
                physicalMenu.gameObject.SetActive(true);
                changed = true;
            }

            BattleRoyaleStartMenu controller =
                physicalMenu.GetComponent<BattleRoyaleStartMenu>();

            if (controller == null)
            {
                controller = physicalMenu.gameObject.AddComponent<BattleRoyaleStartMenu>();
                changed = true;
            }

            if (!controller.enabled)
            {
                controller.enabled = true;
                changed = true;
            }

            // Deshabilita cualquier controlador heredado que no pertenezca al
            // menu fisico. Asi solo existe una autoridad para cursor/botones.
            BattleRoyaleStartMenu[] allControllers =
                FindAllInScene<BattleRoyaleStartMenu>(scene);

            for (int i = 0; i < allControllers.Length; i++)
            {
                BattleRoyaleStartMenu other = allControllers[i];
                if (other == null || other == controller)
                    continue;

                if (other.enabled)
                {
                    other.enabled = false;
                    EditorUtility.SetDirty(other);
                    changed = true;
                }
            }

            Button startButton = FindNamedButton(physicalMenu, "StartMatchButton");
            Button freeroamButton = FindNamedButton(physicalMenu, "FreeroamButton");
            MatchStartController sequence = FindInScene<MatchStartController>(scene);
            PlayerInputReader input = FindLocalPlayerInput(scene);
            ThirdPersonCamera camera = FindInScene<ThirdPersonCamera>(scene);

            SerializedObject serialized = new SerializedObject(controller);
            changed |= SetObjectReference(serialized, "viewRoot", physicalMenu.gameObject);
            changed |= SetObjectReference(serialized, "startMatchButton", startButton);
            changed |= SetObjectReference(serialized, "freeroamButton", freeroamButton);
            changed |= SetObjectReference(serialized, "sequence", sequence);
            changed |= SetObjectReference(serialized, "input", input);
            changed |= SetObjectReference(serialized, "playerCamera", camera);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log(
                "[Editor First] MENU_BATTLE_ROYALE_EDITABLE tiene ahora el unico BattleRoyaleStartMenu activo y controla el cursor."
            );

            CloseIfNeeded(scene, openedTemporarily);
        }

        private static PlayerInputReader FindLocalPlayerInput(Scene scene)
        {
            ThirdPersonCamera camera = FindInScene<ThirdPersonCamera>(scene);
            if (camera != null && camera.Target != null)
            {
                PlayerInputReader cameraInput =
                    camera.Target.GetComponent<PlayerInputReader>();
                if (cameraInput != null && !cameraInput.UsesExternalControl)
                    return cameraInput;
            }

            PlayerInputReader[] readers = FindAllInScene<PlayerInputReader>(scene);
            for (int i = 0; i < readers.Length; i++)
            {
                if (readers[i] != null && !readers[i].UsesExternalControl)
                    return readers[i];
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            T[] all = FindAllInScene<T>(scene);
            return all.Length > 0 ? all[0] : null;
        }

        private static T[] FindAllInScene<T>(Scene scene) where T : Component
        {
            System.Collections.Generic.List<T> result =
                new System.Collections.Generic.List<T>();

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T[] found = roots[i].GetComponentsInChildren<T>(true);
                if (found != null && found.Length > 0)
                    result.AddRange(found);
            }

            return result.ToArray();
        }

        private static Button FindNamedButton(Transform root, string objectName)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == objectName)
                    return buttons[i];
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

        private static void CloseIfNeeded(Scene scene, bool openedTemporarily)
        {
            if (openedTemporarily && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
