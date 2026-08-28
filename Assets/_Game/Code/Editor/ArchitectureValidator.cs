using System;
using System.Collections.Generic;
using System.IO;
using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Lobby;
using ROS.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Editor
{
    public static class ArchitectureValidator
    {
        private static readonly string[] CoreScenes =
        {
            "Assets/_Game/Scenes/07_BattleRoyaleTest.unity",
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity",
            "Assets/_Game/Scenes/08_Lobby.unity"
        };

        [MenuItem("Rules Of Survival/Validation/Validate Explicit Architecture")]
        public static void ValidateProject()
        {
            List<string> errors = CollectErrors();
            if (errors.Count == 0)
            {
                Debug.Log("[ArchitectureValidator] Arquitectura explícita válida.");
                return;
            }

            Debug.LogError("[ArchitectureValidator]\n- " + string.Join("\n- ", errors));
        }

        public static void ValidateProjectOrThrow()
        {
            List<string> errors = CollectErrors();
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            Debug.Log("[ArchitectureValidator] Arquitectura explícita válida.");
        }

        private static List<string> CollectErrors()
        {
            List<string> errors = new List<string>();
            ValidateSources(errors);
            ValidatePrefabs(errors);
            ValidateScenes(errors);
            return errors;
        }

        private static void ValidateSources(List<string> errors)
        {
            string root = Path.Combine(Application.dataPath, "_Game", "Code");
            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');
                if (normalized.Contains("/Editor/")) continue;

                string source = File.ReadAllText(path);
                RequireAbsent(source, normalized, "RuntimeInitializeOnLoadMethod", errors);
                RequireAbsent(source, normalized, "Resources.Load", errors);
                RequireAbsent(source, normalized, "Resources.FindObjectsOfTypeAll", errors);
                RequireAbsent(source, normalized, "ScriptableObject.CreateInstance", errors);
                RequireAbsent(source, normalized, "FindFirstObjectByType", errors);
                RequireAbsent(source, normalized, "FindAnyObjectByType", errors);
                RequireAbsent(source, normalized, "GameObject.Find(", errors);
                RequireAbsent(source, normalized, "Camera.main", errors);

                bool allowedFactory = normalized.EndsWith("/Loot/DeathLootContainer.cs") ||
                                      normalized.EndsWith("/Loot/LootPickup.cs") ||
                                      normalized.EndsWith("/World/EchoValleyMapAuthoring.cs");
                if (!allowedFactory && source.Contains("AddComponent<"))
                    errors.Add($"AddComponent runtime fuera de fábrica permitida: {normalized}");

                bool writesAnimator = source.Contains("animator.SetBool(") ||
                                      source.Contains("animator.SetFloat(") ||
                                      source.Contains("animator.SetInteger(") ||
                                      source.Contains("animator.SetTrigger(") ||
                                      source.Contains("animator.CrossFade(") ||
                                      source.Contains("animator.SetLayerWeight(");
                if (writesAnimator && !normalized.EndsWith("/Animation/PlayerAnimationCoordinator.cs"))
                    errors.Add($"Escritor de Animator no autorizado: {normalized}");
            }
        }

        private static void ValidatePrefabs(List<string> errors)
        {
            string[] folders = { "Assets/_Game/Prefabs", "Assets/_Game/Resources" };
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                    if (missing > 0) errors.Add($"{path}: {child.name} tiene {missing} script(s) faltante(s).");
                }

                foreach (PlayerEliminationController elimination in
                         root.GetComponentsInChildren<PlayerEliminationController>(true))
                {
                    RequireReferences(
                        elimination,
                        errors,
                        path,
                        "health",
                        "inventory",
                        "lootEquipment",
                        "visualRoot",
                        "input"
                    );
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateScenes(List<string> errors)
        {
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in CoreScenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                            errors.Add($"{path}: {child.name} tiene scripts faltantes.");

                if (path.EndsWith("08_Lobby.unity"))
                {
                    LobbySceneBootstrap lobby = FindInScene<LobbySceneBootstrap>(scene);
                    RequireReferences(lobby, errors, path, "authoredHud", "_canvas", "_navigation",
                        "_rotator", "_cameraController", "_character");
                }
                else
                {
                    RulesOfSurvivalHUD hud = FindInScene<RulesOfSurvivalHUD>(scene);
                    RequireReferences(hud, errors, path, "worldCamera", "minimapCamera", "health",
                        "equipment", "battleRoyale", "interactor", "canvas");
                    ThirdPersonCamera camera = FindInScene<ThirdPersonCamera>(scene);
                    RequireReferences(camera, errors, path, "target", "input", "equipment",
                        "leanController", "_parachute");
                    WeaponCrosshairPresenter crosshair =
                        FindInScene<WeaponCrosshairPresenter>(scene);
                    RequireReferences(
                        crosshair,
                        errors,
                        path,
                        "equipment",
                        "root",
                        "normalRoot",
                        "normalLeft",
                        "normalRight",
                        "normalUp",
                        "normalDown",
                        "shotgunLeft",
                        "shotgunRight",
                        "reloadTimerText"
                    );
                    GestureWheelUI gestures = FindInScene<GestureWheelUI>(scene);
                    RequireReferences(
                        gestures,
                        errors,
                        path,
                        "canvas",
                        "wheelOverlay",
                        "wheelCenter",
                        "selectionLabel",
                        "hintRoot",
                        "_input",
                        "_gestureController"
                    );
                    BattleRoyaleBotDirector bots = FindInScene<BattleRoyaleBotDirector>(scene);
                    if (bots != null)
                        RequireReferences(bots, errors, path, "sourcePlayer", "airplane", "matchManager",
                            "sequence", "botHealthBarPrefab", "gameplayCamera");
                }

                EditorSceneManager.CloseScene(scene, true);
            }

            if (original.IsValid()) SceneManager.SetActiveScene(original);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T value = root.GetComponentInChildren<T>(true);
                if (value != null) return value;
            }
            return null;
        }

        private static void RequireReferences(
            UnityEngine.Object target,
            List<string> errors,
            string assetPath,
            params string[] properties)
        {
            if (target == null)
            {
                errors.Add($"{assetPath}: falta {typeof(UnityEngine.Object).Name} esperado.");
                return;
            }

            SerializedObject serialized = new SerializedObject(target);
            foreach (string name in properties)
            {
                SerializedProperty property = serialized.FindProperty(name);
                if (property == null || property.objectReferenceValue == null)
                    errors.Add($"{assetPath}: {target.GetType().Name}.{name} no está asignado.");
            }
        }

        private static void RequireAbsent(
            string source,
            string path,
            string token,
            List<string> errors)
        {
            if (source.Contains(token)) errors.Add($"Uso prohibido '{token}': {path}");
        }
    }
}
