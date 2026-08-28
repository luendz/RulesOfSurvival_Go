using System;
using System.Collections.Generic;
using System.IO;
using ROS.Game.AI;
using ROS.Game.Animation;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Gameplay;
using ROS.Game.Lobby;
using ROS.Game.Loot;
using ROS.Game.Parachute;
using ROS.Game.UI;
using ROS.Game.Weapons;
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
                foreach (BattleRoyaleBotController bot in
                         root.GetComponentsInChildren<BattleRoyaleBotController>(true))
                    RequireReferences(bot, errors, path, "_input", "_motor", "_parachute",
                        "_equipment", "_lootEquipment", "_inventory", "_consumables",
                        "_health", "_controlFrame");

                ValidatePlayerReferenceGraph(root, path, errors, false);
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
                    foreach (GameObject root in scene.GetRootGameObjects())
                        ValidatePlayerReferenceGraph(root, path, errors, true);

                    foreach (DamageNumberSpawner damageNumbers in
                             FindAllInScene<DamageNumberSpawner>(scene))
                        RequireReferences(damageNumbers, errors, path,
                            "_equipment", "_damageNumberPrefab", "_worldCamera");

                    foreach (PlayerEquipmentVisualPresenter equipmentVisuals in
                             FindAllInScene<PlayerEquipmentVisualPresenter>(scene))
                        RequireReferences(equipmentVisuals, errors, path,
                            "lootEquipment", "protection",
                            "helmetLevel1", "helmetLevel2", "helmetLevel3",
                            "vestLevel1", "vestLevel2", "vestLevel3",
                            "backpackLevel1Definition", "backpackLevel2Definition",
                            "backpackLevel3Definition", "backpackLevel1",
                            "backpackLevel2", "backpackLevel3");

                    foreach (CombatFeedbackPresenter feedback in
                             FindAllInScene<CombatFeedbackPresenter>(scene))
                    {
                        if (!feedback.enabled) continue;
                        RequireReferences(feedback, errors, path, "health", "directionReference",
                            "hitmarkerRoot", "hitmarkerParts.Array.data[0]",
                            "hitmarkerParts.Array.data[1]", "hitmarkerParts.Array.data[2]",
                            "hitmarkerParts.Array.data[3]", "headshotLabel",
                            "damageBars.Array.data[0]", "damageBars.Array.data[1]",
                            "damageBars.Array.data[2]", "damageBars.Array.data[3]");
                    }

                    foreach (MatchStartHud matchHud in FindAllInScene<MatchStartHud>(scene))
                        RequireReferences(matchHud, errors, path, "sequence", "parachute",
                            "panelRoot", "titleText", "detailText");

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

        private static IEnumerable<T> FindAllInScene<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (T value in root.GetComponentsInChildren<T>(true))
                    yield return value;
        }

        private static void ValidatePlayerReferenceGraph(
            GameObject root,
            string path,
            List<string> errors,
            bool requireSceneReferences)
        {
            foreach (PlayerLeanController lean in
                     root.GetComponentsInChildren<PlayerLeanController>(true))
            {
                RequireReferences(lean, errors, path, "input", "motor", "equipment",
                    "health", "parachute", "animator");
            }

            foreach (PlayerMotor motor in
                     root.GetComponentsInChildren<PlayerMotor>(true))
            {
                RequireReferences(motor, errors, path, "visualRoot", "equipment", "consumable",
                    "_controller", "_input");
                if (requireSceneReferences)
                    RequireReferences(motor, errors, path, "cameraTransform");
            }

            foreach (PlayerLeanRigApplier rig in
                     root.GetComponentsInChildren<PlayerLeanRigApplier>(true))
                RequireReferences(rig, errors, path, "leanController", "humanoidAnimator");

            foreach (ParachuteController parachute in
                     root.GetComponentsInChildren<ParachuteController>(true))
                RequireReferences(parachute, errors, path, "input", "motor", "equipment",
                    "controller");

            foreach (ConsumableController consumable in
                     root.GetComponentsInChildren<ConsumableController>(true))
            {
                RequireReferences(consumable, errors, path, "_health", "_inventory", "_input");
                ValidateConsumableHud(consumable, path, errors);
            }

            foreach (PlayerAimController aim in
                     root.GetComponentsInChildren<PlayerAimController>(true))
                if (requireSceneReferences)
                    RequireReferences(aim, errors, path, "aimCamera");

            foreach (PlayerGestureController gesture in
                     root.GetComponentsInChildren<PlayerGestureController>(true))
                RequireReferences(gesture, errors, path, "animationCoordinator", "input",
                    "motor", "equipment", "health", "parachute", "consumable");

            foreach (PlayerAuxiliaryWeaponSlots auxiliary in
                     root.GetComponentsInChildren<PlayerAuxiliaryWeaponSlots>(true))
                RequireReferences(auxiliary, errors, path, "input", "weapons", "lootEquipment",
                    "animator", "rightHandSocket", "aimController", "health", "consumable",
                    "gestureController", "parachute", "_characterController");

            foreach (PlayerAnimationCoordinator coordinator in
                     root.GetComponentsInChildren<PlayerAnimationCoordinator>(true))
                RequireReferences(coordinator, errors, path, "animator", "motor", "input",
                    "equipment", "auxiliarySlots", "aimController", "leanController", "health",
                    "parachute", "consumable", "gestureController", "interactor");

            foreach (PlayerLootEquipment lootEquipment in
                     root.GetComponentsInChildren<PlayerLootEquipment>(true))
                RequireReferences(lootEquipment, errors, path, "inventory", "protection", "weapons");

            foreach (LootDropController lootDrop in
                     root.GetComponentsInChildren<LootDropController>(true))
                RequireReferences(lootDrop, errors, path, "inventory", "input");

            ValidateNoDuplicates<PlayerLeanController>(root, path, errors);
            ValidateNoDuplicates<PlayerMotor>(root, path, errors);
            ValidateNoDuplicates<PlayerLeanRigApplier>(root, path, errors);
            ValidateNoDuplicates<ParachuteController>(root, path, errors);
            ValidateNoDuplicates<ConsumableController>(root, path, errors);
            ValidateNoDuplicates<PlayerAimController>(root, path, errors);
            ValidateNoDuplicates<PlayerGestureController>(root, path, errors);
            ValidateNoDuplicates<PlayerAuxiliaryWeaponSlots>(root, path, errors);
            ValidateNoDuplicates<PlayerAnimationCoordinator>(root, path, errors);
            ValidateNoDuplicates<PlayerLootEquipment>(root, path, errors);
            ValidateNoDuplicates<LootDropController>(root, path, errors);
        }

        private static void ValidateConsumableHud(
            ConsumableController consumable,
            string path,
            List<string> errors)
        {
            SerializedObject serialized = new SerializedObject(consumable);
            SerializedProperty showHud = serialized.FindProperty("showHud");
            if (showHud != null && showHud.boolValue)
                RequireReferences(consumable, errors, path, "barRoot", "fill", "label");
        }

        private static void ValidateNoDuplicates<T>(
            GameObject root,
            string path,
            List<string> errors)
            where T : Component
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.GetComponents<T>().Length > 1)
                    errors.Add($"{path}: {child.name} tiene {typeof(T).Name} duplicado.");
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
