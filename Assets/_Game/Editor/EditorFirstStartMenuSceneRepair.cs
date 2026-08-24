using ROS.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Conecta el menu fisico Editor First con el controlador funcional que ya
    /// existe en la escena Battle Royale. Tambien garantiza un EventSystem
    /// compatible con el nuevo Input System para que los Button reciban click.
    /// No modifica posiciones, colores ni layout del menu.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstStartMenuSceneRepair
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private const string MenuPath =
            "01_RUNTIME_UI/MENU_BATTLE_ROYALE_EDITABLE";

        static EditorFirstStartMenuSceneRepair()
        {
            EditorApplication.delayCall += RepairFunctionalSceneMenu;
        }

        [MenuItem("Rules Of Survival/Editor First/Repair Functional Test Menu")]
        public static void RepairFunctionalSceneMenu()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

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

            bool changed = false;

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);

            Transform physicalMenu =
                presentationRoot != null
                    ? presentationRoot.transform.Find(MenuPath)
                    : null;

            if (physicalMenu != null)
            {
                BattleRoyaleStartMenu controller = FindController(scene, physicalMenu);
                if (controller == null)
                {
                    controller = physicalMenu.gameObject.AddComponent<BattleRoyaleStartMenu>();
                    changed = true;
                }

                Button startButton = FindNamedButton(
                    physicalMenu,
                    "StartMatchButton"
                );
                Button freeroamButton = FindNamedButton(
                    physicalMenu,
                    "FreeroamButton"
                );

                SerializedObject serialized = new SerializedObject(controller);
                SerializedProperty viewRoot = serialized.FindProperty("viewRoot");
                SerializedProperty start = serialized.FindProperty("startMatchButton");
                SerializedProperty free = serialized.FindProperty("freeroamButton");

                if (viewRoot != null && viewRoot.objectReferenceValue != physicalMenu.gameObject)
                {
                    viewRoot.objectReferenceValue = physicalMenu.gameObject;
                    changed = true;
                }

                if (start != null && start.objectReferenceValue != startButton)
                {
                    start.objectReferenceValue = startButton;
                    changed = true;
                }

                if (free != null && free.objectReferenceValue != freeroamButton)
                {
                    free.objectReferenceValue = freeroamButton;
                    changed = true;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }

            changed |= EnsureEventSystem(scene);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[Editor First] Menu funcional reparado y conectado en 08_EditorFirstFunctionalTest."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static BattleRoyaleStartMenu FindController(
            Scene scene,
            Transform physicalMenu
        )
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                BattleRoyaleStartMenu[] candidates =
                    roots[i].GetComponentsInChildren<BattleRoyaleStartMenu>(true);

                for (int j = 0; j < candidates.Length; j++)
                {
                    BattleRoyaleStartMenu candidate = candidates[j];
                    if (candidate == null)
                        continue;

                    if (candidate.transform == physicalMenu ||
                        candidate.transform.IsChildOf(physicalMenu))
                    {
                        continue;
                    }

                    return candidate;
                }
            }

            BattleRoyaleStartMenu attached =
                physicalMenu.GetComponent<BattleRoyaleStartMenu>();

            return attached;
        }

        private static Button FindNamedButton(Transform root, string name)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == name)
                    return buttons[i];
            }
            return null;
        }

        private static bool EnsureEventSystem(Scene scene)
        {
            EventSystem existing = null;
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length && existing == null; i++)
            {
                existing = roots[i].GetComponentInChildren<EventSystem>(true);
            }

            GameObject eventObject;
            bool changed = false;

            if (existing == null)
            {
                eventObject = new GameObject("EventSystem_EditorFirst");
                SceneManager.MoveGameObjectToScene(eventObject, scene);
                existing = eventObject.AddComponent<EventSystem>();
                changed = true;
            }
            else
            {
                eventObject = existing.gameObject;
            }

            InputSystemUIInputModule inputModule =
                eventObject.GetComponent<InputSystemUIInputModule>();

            if (inputModule == null)
            {
                BaseInputModule[] oldModules =
                    eventObject.GetComponents<BaseInputModule>();

                for (int i = 0; i < oldModules.Length; i++)
                {
                    if (oldModules[i] != null &&
                        !(oldModules[i] is InputSystemUIInputModule))
                    {
                        Object.DestroyImmediate(oldModules[i]);
                    }
                }

                inputModule = eventObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
                changed = true;
            }

            EditorUtility.SetDirty(eventObject);
            return changed;
        }
    }
}
