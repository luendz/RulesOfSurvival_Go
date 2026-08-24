using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa los elementos de presentacion directamente dentro de
    /// 07_BattleRoyaleTest para que puedan editarse desde Hierarchy/Inspector.
    ///
    /// Los objetos se desempaquetan completamente: la escena se convierte en
    /// la fuente visual que usa Play Mode, mientras los prefabs EditorFirst se
    /// conservan como respaldo/origen para recrear elementos faltantes.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstBattleRoyaleSceneMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/07_BattleRoyaleTest.unity";

        private const string RootName = "__EDITOR_FIRST_PRESENTATION";

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

        static EditorFirstBattleRoyaleSceneMaterializer()
        {
            EditorApplication.delayCall += EnsureSceneOnce;
        }

        [MenuItem("Rules Of Survival/Editor First/Put Everything In Battle Royale Hierarchy")]
        public static void MaterializeInBattleRoyaleScene()
        {
            MaterializeScene(false);
        }

        [MenuItem("Rules Of Survival/Editor First/Open Battle Royale Editable Hierarchy")]
        public static void OpenEditableScene()
        {
            MaterializeScene(false);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            GameObject root = FindRoot(scene, RootName);
            if (root != null)
            {
                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);
            }
        }

        private static void EnsureSceneOnce()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            if (!System.IO.File.Exists(ScenePath))
                return;

            Scene loaded = SceneManager.GetSceneByPath(ScenePath);
            if (loaded.IsValid() && loaded.isLoaded && FindRoot(loaded, RootName) != null)
                return;

            MaterializeScene(true);
        }

        private static void MaterializeScene(bool onlyIfMissing)
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("[Editor First] No existe la escena: " + ScenePath);
                return;
            }

            // Primero se materializan/actualizan los prefabs fuente.
            EditorFirstPresentationBuilder.EnsureMaterialized();
            EditorFirstCrosshairMaterializer.EnsureCrosshair();
            EditorFirstLootViewsMaterializer.EnsureLootViews();

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            GameObject existingRoot = FindRoot(scene, RootName);
            if (existingRoot != null && onlyIfMissing)
            {
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            bool changed = false;
            GameObject presentationRoot = existingRoot;
            if (presentationRoot == null)
            {
                presentationRoot = new GameObject(RootName);
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
                    "[Editor First] 07_BattleRoyaleTest ya contiene la presentacion editable en Hierarchy."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
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
