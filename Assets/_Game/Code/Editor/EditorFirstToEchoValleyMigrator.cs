using System;
using System.Collections.Generic;
using System.IO;
using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Parachute;
using ROS.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROS.Game.Editor
{
    public static class EditorFirstToEchoValleyMigrator
    {
        private const string LobbyPath =
            "Assets/_Game/Scenes/08_Lobby.unity";
        private const string EditorFirstPath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";
        private const string EchoValleyPath =
            "Assets/_Game/Scenes/08_EchoValley.unity";

        // Solo se traslada la capa funcional. Echo Valley conserva su terreno,
        // iluminación, vegetación y demás escenografía original.
        private static readonly string[] GameplayRootNames =
        {
            "__EDITOR_FIRST_PRESENTATION",
            "EventSystem_EditorFirst",
            "CrosshairHUD",
            "Main Camera",
            "Loot",
            "BattleRoyaleSystem",
            "BattleRoyaleMatchStart",
            "Blade_Liger",
            "Airplane_BattleRoyale",
            "Player_Prototype"
        };

        [MenuItem(
            "Rules Of Survival/Tools/Scenes/Migrar EditorFirst → EchoValley"
        )]
        public static void Migrate()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Migrar sistemas a Echo Valley",
                "Se reemplazarán en Echo Valley las raíces funcionales " +
                "equivalentes usando la configuración de EditorFirst. " +
                "El terreno, la iluminación y la escenografía de Echo Valley " +
                "se conservarán.",
                "Migrar",
                "Cancelar"
            );

            if (confirmed)
            {
                MigrateCore();
                EditorUtility.DisplayDialog(
                    "Migración completa",
                    "Echo Valley conserva su mapa y ya contiene la capa " +
                    "funcional de EditorFirst.",
                    "OK"
                );
            }
        }

        public static void MigrateBatch()
        {
            MigrateCore();
        }

        public static void AuditBatch()
        {
            ValidateScenePaths();

            Scene echoValley = EditorSceneManager.OpenScene(
                EchoValleyPath,
                OpenSceneMode.Single
            );
            Scene editorFirst = EditorSceneManager.OpenScene(
                EditorFirstPath,
                OpenSceneMode.Additive
            );

            Debug.Log(
                "[EchoValleyMigrator] Raíces Echo Valley:\n- " +
                string.Join("\n- ", GetRootNames(echoValley))
            );
            Debug.Log(
                "[EchoValleyMigrator] Raíces EditorFirst:\n- " +
                string.Join("\n- ", GetRootNames(editorFirst))
            );

            Dictionary<string, GameObject> sourceRoots =
                IndexRoots(editorFirst);
            List<GameObject> gameplayRoots = ResolveGameplayRoots(sourceRoots);
            List<string> externalReferences = FindExternalSceneReferences(
                gameplayRoots,
                editorFirst
            );

            if (externalReferences.Count == 0)
            {
                Debug.Log(
                    "[EchoValleyMigrator] La capa funcional no depende de " +
                    "raíces excluidas."
                );
            }
            else
            {
                Debug.LogWarning(
                    "[EchoValleyMigrator] Referencias hacia raíces excluidas:\n- " +
                    string.Join("\n- ", externalReferences)
                );
            }

            EditorSceneManager.CloseScene(editorFirst, true);
        }

        private static void MigrateCore()
        {
            ValidateScenePaths();

            Scene echoValley = EditorSceneManager.OpenScene(
                EchoValleyPath,
                OpenSceneMode.Single
            );
            Scene editorFirst = EditorSceneManager.OpenScene(
                EditorFirstPath,
                OpenSceneMode.Additive
            );

            Dictionary<string, GameObject> sourceRoots =
                IndexRoots(editorFirst);
            List<GameObject> gameplayRoots = ResolveGameplayRoots(sourceRoots);
            List<string> externalReferences = FindExternalSceneReferences(
                gameplayRoots,
                editorFirst
            );

            if (externalReferences.Count > 0)
            {
                throw new InvalidOperationException(
                    "La migración se detuvo porque existen referencias hacia " +
                    "raíces que no se trasladarán:\n- " +
                    string.Join("\n- ", externalReferences)
                );
            }

            RemovePreviousGameplayRoots(echoValley);

            for (int i = 0; i < gameplayRoots.Count; i++)
            {
                SceneManager.MoveGameObjectToScene(
                    gameplayRoots[i],
                    echoValley
                );
            }

            EditorSceneManager.SetActiveScene(echoValley);
            ValidateMigratedSystems(echoValley);
            EditorSceneManager.MarkSceneDirty(echoValley);

            if (!EditorSceneManager.SaveScene(echoValley, EchoValleyPath))
            {
                throw new InvalidOperationException(
                    $"No se pudo guardar {EchoValleyPath}."
                );
            }

            EditorSceneManager.CloseScene(editorFirst, true);
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[EchoValleyMigrator] Migración completada. " +
                $"Raíces funcionales trasladadas: {gameplayRoots.Count}. " +
                "El mapa y la iluminación de Echo Valley se conservaron."
            );
        }

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LobbyPath, true),
                new EditorBuildSettingsScene(EchoValleyPath, true),
                new EditorBuildSettingsScene(EditorFirstPath, false)
            };
        }

        private static void ValidateScenePaths()
        {
            if (!File.Exists(EditorFirstPath) || !File.Exists(EchoValleyPath))
            {
                throw new FileNotFoundException(
                    "No se encontraron las escenas requeridas para migrar."
                );
            }
        }

        private static Dictionary<string, GameObject> IndexRoots(Scene scene)
        {
            Dictionary<string, GameObject> roots =
                new Dictionary<string, GameObject>(StringComparer.Ordinal);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!roots.TryAdd(root.name, root))
                {
                    throw new InvalidOperationException(
                        $"{scene.path} contiene más de una raíz '{root.name}'."
                    );
                }
            }

            return roots;
        }

        private static List<GameObject> ResolveGameplayRoots(
            IReadOnlyDictionary<string, GameObject> sourceRoots
        )
        {
            List<GameObject> resolved = new List<GameObject>();
            List<string> missing = new List<string>();

            for (int i = 0; i < GameplayRootNames.Length; i++)
            {
                string rootName = GameplayRootNames[i];
                if (sourceRoots.TryGetValue(rootName, out GameObject root))
                {
                    resolved.Add(root);
                }
                else
                {
                    missing.Add(rootName);
                }
            }

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    "EditorFirst no contiene las raíces requeridas:\n- " +
                    string.Join("\n- ", missing)
                );
            }

            return resolved;
        }

        private static void RemovePreviousGameplayRoots(Scene echoValley)
        {
            HashSet<string> names = new HashSet<string>(
                GameplayRootNames,
                StringComparer.Ordinal
            );

            foreach (GameObject root in echoValley.GetRootGameObjects())
            {
                if (names.Contains(root.name))
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static List<string> FindExternalSceneReferences(
            IReadOnlyCollection<GameObject> gameplayRoots,
            Scene sourceScene
        )
        {
            HashSet<GameObject> selected = new HashSet<GameObject>(
                gameplayRoots
            );
            HashSet<string> references = new HashSet<string>();

            foreach (GameObject root in gameplayRoots)
            {
                Component[] components =
                    root.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component == null)
                    {
                        continue;
                    }

                    SerializedObject serialized = new SerializedObject(
                        component
                    );
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType !=
                            SerializedPropertyType.ObjectReference)
                        {
                            continue;
                        }

                        Object referenced = property.objectReferenceValue;
                        GameObject referencedObject = referenced switch
                        {
                            GameObject gameObject => gameObject,
                            Component referencedComponent =>
                                referencedComponent.gameObject,
                            _ => null
                        };

                        if (referencedObject == null ||
                            referencedObject.scene != sourceScene)
                        {
                            continue;
                        }

                        GameObject referencedRoot =
                            referencedObject.transform.root.gameObject;
                        if (selected.Contains(referencedRoot))
                        {
                            continue;
                        }

                        references.Add(
                            $"{GetHierarchyPath(component.transform)} / " +
                            $"{component.GetType().Name}.{property.propertyPath} " +
                            $"→ {GetHierarchyPath(referencedObject.transform)}"
                        );
                    }
                }
            }

            List<string> result = new List<string>(references);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static void ValidateMigratedSystems(Scene scene)
        {
            RequireInScene<BattleRoyaleManager>(scene);
            RequireInScene<BattleRoyaleBotDirector>(scene);
            RequireInScene<MatchStartController>(scene);
            RequireInScene<RulesOfSurvivalHUD>(scene);
            RequireInScene<ThirdPersonCamera>(scene);
            RequireInScene<EventSystem>(scene);
        }

        private static T RequireInScene<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            throw new InvalidOperationException(
                $"Echo Valley no contiene {typeof(T).Name} después de migrar."
            );
        }

        private static List<string> GetRootNames(Scene scene)
        {
            List<string> names = new List<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                names.Add(root.name);
            }

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
