using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Limpia del Canvas principal los bloques que ya no forman parte del HUD.
    /// Conserva exclusivamente el QuickConsumeRoot ubicado en Vitals/Meds.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstHudHierarchyCleanup
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstHudHierarchyCleanup()
        {
            EditorApplication.delayCall += Cleanup;
        }

        [MenuItem("Rules Of Survival/Editor First/Clean Obsolete HUD Hierarchy")]
        public static void Cleanup()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
            {
                return;
            }

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

            Transform canvas = presentationRoot != null
                ? presentationRoot.transform.Find(
                    "01_RUNTIME_UI/HUD_ROS_EDITABLE/Canvas"
                )
                : null;

            if (canvas == null)
            {
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            bool changed = false;

            changed |= DestroyDirectChild(canvas, "EquipmentStatusRoot");
            changed |= DestroyDirectChild(canvas, "MatchStatePanel");
            changed |= DestroyDirectChild(canvas, "InteractionHint");

            Transform vitals = canvas.Find("Vitals");
            Transform meds = vitals != null ? vitals.Find("Meds") : null;
            Transform nestedQuickConsume = meds != null
                ? meds.Find("QuickConsumeRoot")
                : null;
            Transform canvasQuickConsume = canvas.Find("QuickConsumeRoot");

            if (nestedQuickConsume != null)
            {
                if (canvasQuickConsume != null &&
                    canvasQuickConsume != nestedQuickConsume)
                {
                    Object.DestroyImmediate(canvasQuickConsume.gameObject);
                    changed = true;
                }
            }
            else if (canvasQuickConsume != null && meds != null)
            {
                // Salvaguarda para escenas antiguas: si todavía no existe el
                // QuickConsumeRoot dentro de Meds, reutiliza el existente en vez
                // de destruir la única vista funcional.
                canvasQuickConsume.SetParent(meds, false);
                nestedQuickConsume = canvasQuickConsume;
                changed = true;
            }
            else if (canvasQuickConsume != null)
            {
                Debug.LogWarning(
                    "[Editor First] No se encontro Vitals/Meds. Se conserva temporalmente " +
                    "el QuickConsumeRoot actual para no perder la vista de consumibles."
                );
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[Editor First] HUD limpio: EquipmentStatusRoot, MatchStatePanel e " +
                    "InteractionHint eliminados. QuickConsumeRoot se conserva solo en Vitals/Meds."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool DestroyDirectChild(Transform parent, string name)
        {
            if (parent == null)
                return false;

            Transform target = parent.Find(name);
            if (target == null || target.parent != parent)
                return false;

            Object.DestroyImmediate(target.gameObject);
            return true;
        }
    }
}
