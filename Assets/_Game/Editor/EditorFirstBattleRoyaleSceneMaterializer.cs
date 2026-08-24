using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Inserta la presentacion Editor First dentro de cualquier escena Battle Royale.
    /// Los objetos se desempaquetan completamente para poder editarlos directamente
    /// desde Hierarchy/Inspector sin que el prefab bloquee los cambios de escena.
    /// </summary>
    public static class EditorFirstBattleRoyaleSceneMaterializer
    {
        public const string PresentationRootName = "__EDITOR_FIRST_PRESENTATION";

        private const string DefaultScenePath =
            "Assets/_Game/Scenes/07_BattleRoyaleTest.unity";

        private const string HudPath =
            "Assets/_Game/Resources/EditorFirst/ROS_HUD_Editable.prefab";
        private const string StartMenuPath =
            "Assets/_Game/Resources/EditorFirst/BattleRoyaleStartMenuView.prefab";
        private const string GestureWheelPath =
            "Assets/_Game/Resources/EditorFirst/GestureWheelUI.prefab";
        private const string BotHealthBarPath =
            "Assets/_Game/Resources/EditorFirst/BotHealthBar.prefab";
        private const string DamageNumberPath =
            "Assets/_Game/Resources/EditorFirst/DamageNumber.prefab";

        [MenuItem("Rules Of Survival/Editor First/Put Everything In Original Battle Royale Hierarchy")]
        public static void MaterializeInBattleRoyaleScene()
        {
            MaterializeSceneAtPath(DefaultScenePath, false);
        }

        [MenuItem("Rules Of Survival/Editor First/Open Original Battle Royale Editable Hierarchy")]
        public static void OpenEditableScene()
        {
            MaterializeSceneAtPath(DefaultScenePath, false);
            EditorSceneManager.OpenScene(DefaultScenePath, OpenSceneMode.Single);
            SelectPresentationRoot(DefaultScenePath);
        }

        public static bool MaterializeSceneAtPath(
            string scenePath,
            bool onlyIfMissing = false
        )
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return false;

            if (string.IsNullOrWhiteSpace(scenePath) ||
                !System.IO.File.Exists(scenePath))
            {
                Debug.LogError("[Editor First] No existe la escena: " + scenePath);
                return false;
            }

            EditorFirstPresentationBuilder.EnsureMaterialized();
            EditorFirstCrosshairMaterializer.EnsureCrosshair();
            EditorFirstLootViewsMaterializer.EnsureLootViews();
            EditorFirstHudBehaviorMaterializer.EnsureHudBehaviors();

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            GameObject existingRoot = FindRoot(scene, PresentationRootName);
            if (existingRoot != null && onlyIfMissing)
            {
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return false;
            }

            bool changed = false;
            GameObject presentationRoot = existingRoot;
            if (presentationRoot == null)
            {
                presentationRoot = new GameObject(PresentationRootName);
                SceneManager.MoveGameObjectToScene(presentationRoot, scene);
                changed = true;
            }

            Transform runtimeUi = EnsureGroup(
                presentationRoot.transform,
                "01_RUNTIME_UI",
                ref changed
            );

            Transform previewOnly = EnsureGroup(
                presentationRoot.transform,
                "99_PREVIEW_ONLY_DISABLED",
                ref changed
            );

            changed |= EnsureUnpackedSceneObject(
                scene,
                runtimeUi,
                HudPath,
                "HUD_ROS_EDITABLE",
                true
            );

            changed |= EnsureUnpackedSceneObject(
                scene,
                runtimeUi,
                StartMenuPath,
                "MENU_BATTLE_ROYALE_EDITABLE",
                true
            );

            changed |= EnsureUnpackedSceneObject(
                scene,
                runtimeUi,
                GestureWheelPath,
                "GESTURE_WHEEL_EDITABLE",
                true
            );

            changed |= EnsureUnpackedSceneObject(
                scene,
                previewOnly,
                BotHealthBarPath,
                "BOT_HEALTH_BAR_PREVIEW",
                false
            );

            changed |= EnsureUnpackedSceneObject(
                scene,
                previewOnly,
                DamageNumberPath,
                "DAMAGE_NUMBER_PREVIEW",
                false
            );

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[Editor First] Presentacion editable materializada en: " + scenePath
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);

            return changed;
        }

        public static GameObject FindPresentationRoot(Scene scene)
        {
            return FindRoot(scene, PresentationRootName);
        }

        private static void SelectPresentationRoot(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            GameObject root = FindPresentationRoot(scene);
            if (root == null)
                return;

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        private static Transform EnsureGroup(
            Transform parent,
            string name,
            ref bool changed
        )
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing;

            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            changed = true;
            return group.transform;
        }

        private static bool EnsureUnpackedSceneObject(
            Scene scene,
            Transform parent,
            string prefabPath,
            string sceneObjectName,
            bool active
        )
        {
            Transform existing = parent.Find(sceneObjectName);
            if (existing != null)
                return false;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[Editor First] No se encontro prefab para la escena: " + prefabPath
                );
                return false;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                return false;

            PrefabUtility.UnpackPrefabInstance(
                instance,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction
            );

            instance.name = sceneObjectName;
            instance.transform.SetParent(parent, false);
            instance.SetActive(active);
            EditorUtility.SetDirty(instance);
            return true;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == name)
                    return roots[i];
            }

            return null;
        }
    }
}
