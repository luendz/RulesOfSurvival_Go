using ROS.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstHudCompatibilityMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstHudCompatibilityMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
                return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);
            Transform hud = presentationRoot != null
                ? presentationRoot.transform.Find("01_RUNTIME_UI/HUD_ROS_EDITABLE")
                : null;

            bool changed = false;
            if (hud != null)
            {
                changed |= EnsureComponent<DeathLootPanelPresenter>(hud.gameObject);
                changed |= EnsureComponent<CompassUI>(hud.gameObject);
                changed |= EnsureComponent<MinimapSystem>(hud.gameObject);
                changed |= EnsureComponent<WeaponSlotsPresenter>(hud.gameObject);
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

        private static bool EnsureComponent<T>(GameObject target)
            where T : Component
        {
            if (target.GetComponent<T>() != null)
                return false;

            target.AddComponent<T>();
            EditorUtility.SetDirty(target);
            return true;
        }
    }
}
