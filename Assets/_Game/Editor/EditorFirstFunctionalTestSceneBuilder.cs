using System.Collections.Generic;
using ROS.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Crea una escena de prueba funcional a partir de 07_BattleRoyaleTest y
    /// agrega toda la presentacion Editor First como objetos reales de escena.
    /// La escena 08 se crea una sola vez y nunca se reemplaza automaticamente,
    /// de modo que los cambios manuales del usuario se preservan.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstFunctionalTestSceneBuilder
    {
        public const string SourceScenePath =
            "Assets/_Game/Scenes/07_BattleRoyaleTest.unity";

        public const string FunctionalScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstFunctionalTestSceneBuilder()
        {
            EditorApplication.delayCall += EnsureFunctionalTestScene;
        }

        [MenuItem("Rules Of Survival/Editor First/Create Or Repair Functional Test Scene")]
        public static void EnsureFunctionalTestScene()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            if (!System.IO.File.Exists(SourceScenePath))
            {
                Debug.LogError(
                    "[Editor First] No se encontro la escena base: " + SourceScenePath
                );
                return;
            }

            EnsureSourceAssets();

            bool created = false;
            if (!System.IO.File.Exists(FunctionalScenePath))
            {
                created = AssetDatabase.CopyAsset(
                    SourceScenePath,
                    FunctionalScenePath
                );

                if (!created)
                {
                    Debug.LogError(
                        "[Editor First] No se pudo crear la escena funcional: " +
                        FunctionalScenePath
                    );
                    return;
                }

                AssetDatabase.ImportAsset(
                    FunctionalScenePath,
                    ImportAssetOptions.ForceUpdate
                );
            }

            EditorFirstBattleRoyaleSceneMaterializer.MaterializeSceneAtPath(
                FunctionalScenePath,
                false
            );

            RepairFunctionalComponents();
            EditorFirstStartMenuSceneRepair.RepairFunctionalSceneMenu();
            EditorFirstHudAndPlayerMaterializer.Materialize();
            EditorFirstConsumableHudMaterializer.Materialize();
            EditorFirstMainPlayerRuntimeSupportMaterializer.Materialize();
            EditorFirstHudCompatibilityMaterializer.Materialize();
            EditorFirstBattleRoyaleBotMaterializer.Materialize();
            EditorFirstRosWeaponSlotsMaterializer.Materialize();
            EditorFirstRosWeaponSlotVisualMaterializer.Materialize();
            EditorFirstRosWeaponSlotSerializedRepair.Repair();
            EnsureInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                created
                    ? "[Editor First] Escena funcional creada: " + FunctionalScenePath
                    : "[Editor First] Escena funcional revisada sin sobrescribir tus cambios: " +
                      FunctionalScenePath
            );
        }

        [MenuItem("Rules Of Survival/Editor First/Open Functional Test Scene")]
        public static void OpenFunctionalTestScene()
        {
            EnsureFunctionalTestScene();

            if (!System.IO.File.Exists(FunctionalScenePath))
                return;

            Scene scene = EditorSceneManager.OpenScene(
                FunctionalScenePath,
                OpenSceneMode.Single
            );

            GameObject root =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);

            if (root != null)
            {
                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);
            }
        }

        private static void EnsureSourceAssets()
        {
            EditorFirstPresentationBuilder.EnsureMaterialized();
            EditorFirstCrosshairMaterializer.EnsureCrosshair();
            EditorFirstLootViewsMaterializer.EnsureLootViews();
            EditorFirstHudBehaviorMaterializer.EnsureHudBehaviors();
            EditorFirstWeaponEffectsMaterializer.EnsureEditableWeaponEffects();
        }

        private static void RepairFunctionalComponents()
        {
            Scene scene = SceneManager.GetSceneByPath(FunctionalScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
            {
                scene = EditorSceneManager.OpenScene(
                    FunctionalScenePath,
                    OpenSceneMode.Additive
                );
            }

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            bool changed = false;
            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);

            if (presentationRoot != null)
            {
                Transform hud = presentationRoot.transform.Find(
                    "01_RUNTIME_UI/HUD_ROS_EDITABLE"
                );

                if (hud != null)
                {
                    changed |= EnsureComponent<EditorFirstHudRuntimeRoot>(hud.gameObject);
                    changed |= EnsureComponent<RulesOfSurvivalHUD>(hud.gameObject);
                    changed |= EnsureComponent<RulesOfSurvivalHUDFunctionality>(hud.gameObject);
                    changed |= EnsureComponent<RulesOfSurvivalHUDNearbyLootPresenter>(hud.gameObject);
                    changed |= EnsureComponent<RulesOfSurvivalHUDRuntimePolish>(hud.gameObject);
                    changed |= EnsureComponent<WeaponCrosshairPresenter>(hud.gameObject);
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
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

        private static void EnsureInBuildSettings()
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;

            for (int i = 0; i < current.Length; i++)
            {
                if (current[i].path == FunctionalScenePath)
                {
                    if (!current[i].enabled)
                    {
                        current[i].enabled = true;
                        EditorBuildSettings.scenes = current;
                    }
                    return;
                }
            }

            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(current)
                {
                    new EditorBuildSettingsScene(FunctionalScenePath, true)
                };

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
