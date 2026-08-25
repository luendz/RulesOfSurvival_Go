using System;
using System.Collections.Generic;
using ROS.Game.Animation;
using ROS.Game.Input;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Migra el Animator del jugador a una composición clara y editable:
    ///
    /// Locomotion       -> cuerpo completo, sin arma.
    /// UpperBodyCombat  -> postura de arma / Aim, solo cintura hacia arriba.
    /// UpperBodyActions -> Heal / Reload / Switch / Pickup, solo arriba.
    /// Gestures         -> permanece full-body.
    /// Lean             -> PlayerLeanRigApplier lo aplica al final de la pose.
    ///
    /// El materializador crea las nuevas capas una única vez. Si ya existen,
    /// no reconstruye sus estados para conservar cualquier ajuste manual hecho
    /// después desde el Animator de Unity.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstUnifiedAnimationMaterializer
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string UpperBodyMaskPath =
            "Assets/_Game/Animations/Masks/AM_WeaponUpperBody.mask";

        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private const string ReloadStandingPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadStandingHip.fbx";

        private const string ReloadCrouchPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadCrouchHip.fbx";

        private const string WeaponSwitchPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleSwitch_UpperBody.fbx";

        private const string PickupPath =
            "Assets/_Game/Animations/Character/Locomotion/Ch28_nonPBR@Taking Item.fbx";

        static EditorFirstUnifiedAnimationMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Consolidate Player Upper Lower Animation")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            bool controllerChanged = EnsureAnimatorArchitecture();
            bool sceneChanged = MaterializeCoordinatorInFunctionalScene();

            if (controllerChanged || sceneChanged)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Editor First] Animacion consolidada: Locomotion + " +
                    "UpperBodyCombat + UpperBodyActions. Reload usa clips reales " +
                    "de pie/agachado y el PlayerAnimationCoordinator es la fuente unica."
                );
            }
        }

        public static bool EnsureAnimatorArchitecture()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            AvatarMask upperBodyMask =
                AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);

            if (controller == null || upperBodyMask == null)
            {
                Debug.LogWarning(
                    "[Editor First] No se pudo consolidar el Animator: falta " +
                    "AC_Player_Prototype.controller o AM_WeaponUpperBody.mask."
                );
                return false;
            }

            // Capturamos motions ya probados antes de desactivar las capas legacy.
            Motion rifleLocomotion = FindState(controller, "BT_RifleLocomotion")?.motion;
            Motion rifleCrouch = FindState(controller, "BT_RifleCrouch")?.motion;
            Motion aimLocomotion = FindState(controller, "BT_AimLocomotion")?.motion;
            Motion healing = FindState(controller, "Healing")?.motion;
            Motion takingItem = FindState(controller, "TakingItem")?.motion;

            AnimationClip reloadStanding = LoadFirstAnimationClip(ReloadStandingPath);
            AnimationClip reloadCrouch = LoadFirstAnimationClip(ReloadCrouchPath);
            AnimationClip weaponSwitch = LoadFirstAnimationClip(WeaponSwitchPath);
            AnimationClip pickup = LoadFirstAnimationClip(PickupPath);

            if (takingItem == null)
                takingItem = pickup;

            bool changed = false;

            changed |= EnsureParameter(
                controller,
                "UpperBodyArmed",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "UpperBodyAim",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "Reloading",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "ReloadSpeed",
                AnimatorControllerParameterType.Float
            );
            changed |= EnsureParameter(
                controller,
                "Healing",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "AimPitch",
                AnimatorControllerParameterType.Float
            );
            changed |= EnsureParameter(
                controller,
                "Crouch",
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                "WeaponSwitch",
                AnimatorControllerParameterType.Trigger
            );
            changed |= EnsureParameter(
                controller,
                "PickupItem",
                AnimatorControllerParameterType.Trigger
            );

            if (FindLayerIndex(controller, PlayerAnimationCoordinator.CombatLayerName) < 0)
            {
                CreateCombatLayer(
                    controller,
                    upperBodyMask,
                    rifleLocomotion,
                    rifleCrouch,
                    aimLocomotion
                );
                changed = true;
            }

            if (FindLayerIndex(controller, PlayerAnimationCoordinator.ActionsLayerName) < 0)
            {
                CreateActionsLayer(
                    controller,
                    upperBodyMask,
                    healing,
                    reloadStanding,
                    reloadCrouch,
                    weaponSwitch,
                    takingItem
                );
                changed = true;
            }

            // Las capas viejas permanecen dentro del asset como referencia y
            // para no destruir trabajo anterior, pero ya no participan en la pose.
            changed |= SetLegacyLayerWeight(controller, "Actions", 0f);
            changed |= SetLegacyLayerWeight(controller, "WeaponUpperBody", 0f);
            changed |= SetLegacyLayerWeight(controller, "CrouchAimUpperBody", 0f);

            // Las nuevas capas sí deben empezar activas. Luego el Coordinator
            // las silencia temporalmente durante Gestures o muerte.
            changed |= SetLayerWeight(
                controller,
                PlayerAnimationCoordinator.CombatLayerName,
                1f
            );
            changed |= SetLayerWeight(
                controller,
                PlayerAnimationCoordinator.ActionsLayerName,
                1f
            );

            if (changed)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }

            return changed;
        }

        private static void CreateCombatLayer(
            AnimatorController controller,
            AvatarMask mask,
            Motion rifleLocomotion,
            Motion rifleCrouch,
            Motion aimLocomotion)
        {
            AnimatorStateMachine machine = new AnimatorStateMachine
            {
                name = PlayerAnimationCoordinator.CombatLayerName
            };
            AssetDatabase.AddObjectToAsset(machine, controller);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = PlayerAnimationCoordinator.CombatLayerName,
                stateMachine = machine,
                avatarMask = mask,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                iKPass = true
            };

            AnimatorState empty = machine.AddState("Empty", new Vector3(250f, 20f));
            AnimatorState armed = machine.AddState(
                "ArmedLocomotion",
                new Vector3(520f, -70f)
            );
            AnimatorState crouch = machine.AddState(
                "ArmedCrouch",
                new Vector3(520f, 110f)
            );
            AnimatorState aim = machine.AddState(
                "AimLocomotion",
                new Vector3(800f, 20f)
            );

            empty.writeDefaultValues = false;
            armed.writeDefaultValues = false;
            crouch.writeDefaultValues = false;
            aim.writeDefaultValues = false;

            armed.motion = rifleLocomotion;
            crouch.motion = rifleCrouch != null ? rifleCrouch : rifleLocomotion;
            aim.motion = aimLocomotion;
            machine.defaultState = empty;

            AddTransition(
                empty,
                armed,
                0.08f,
                Condition("UpperBodyArmed", true),
                Condition("Crouch", false)
            );
            AddTransition(
                empty,
                crouch,
                0.08f,
                Condition("UpperBodyArmed", true),
                Condition("Crouch", true)
            );

            AddTransition(
                armed,
                empty,
                0.08f,
                Condition("UpperBodyArmed", false)
            );
            AddTransition(
                crouch,
                empty,
                0.08f,
                Condition("UpperBodyArmed", false)
            );
            AddTransition(
                aim,
                empty,
                0.06f,
                Condition("UpperBodyArmed", false)
            );

            AddTransition(
                armed,
                crouch,
                0.08f,
                Condition("Crouch", true),
                Condition("UpperBodyAim", false)
            );
            AddTransition(
                crouch,
                armed,
                0.08f,
                Condition("Crouch", false),
                Condition("UpperBodyAim", false)
            );

            AddTransition(
                armed,
                aim,
                0.06f,
                Condition("UpperBodyAim", true)
            );
            AddTransition(
                crouch,
                aim,
                0.06f,
                Condition("UpperBodyAim", true)
            );

            AddTransition(
                aim,
                armed,
                0.06f,
                Condition("UpperBodyAim", false),
                Condition("Crouch", false)
            );
            AddTransition(
                aim,
                crouch,
                0.06f,
                Condition("UpperBodyAim", false),
                Condition("Crouch", true)
            );

            AppendLayer(controller, layer);
        }

        private static void CreateActionsLayer(
            AnimatorController controller,
            AvatarMask mask,
            Motion healingMotion,
            AnimationClip reloadStanding,
            AnimationClip reloadCrouch,
            AnimationClip weaponSwitch,
            Motion takingItem)
        {
            AnimatorStateMachine machine = new AnimatorStateMachine
            {
                name = PlayerAnimationCoordinator.ActionsLayerName
            };
            AssetDatabase.AddObjectToAsset(machine, controller);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = PlayerAnimationCoordinator.ActionsLayerName,
                stateMachine = machine,
                avatarMask = mask,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                iKPass = true
            };

            AnimatorState empty = machine.AddState("Empty", new Vector3(250f, 20f));
            AnimatorState healing = machine.AddState("Healing", new Vector3(520f, -170f));
            AnimatorState reloadStandingState = machine.AddState(
                "ReloadStanding",
                new Vector3(540f, -40f)
            );
            AnimatorState reloadCrouchState = machine.AddState(
                "ReloadCrouch",
                new Vector3(540f, 90f)
            );
            AnimatorState switchState = machine.AddState(
                "WeaponSwitch",
                new Vector3(540f, 220f)
            );
            AnimatorState pickupState = machine.AddState(
                "TakingItem",
                new Vector3(540f, 340f)
            );

            empty.writeDefaultValues = false;
            healing.writeDefaultValues = false;
            reloadStandingState.writeDefaultValues = false;
            reloadCrouchState.writeDefaultValues = false;
            switchState.writeDefaultValues = false;
            pickupState.writeDefaultValues = false;

            healing.motion = healingMotion;
            reloadStandingState.motion = reloadStanding;
            reloadCrouchState.motion = reloadCrouch != null
                ? reloadCrouch
                : reloadStanding;
            switchState.motion = weaponSwitch;
            pickupState.motion = takingItem;

            reloadStandingState.speedParameterActive = true;
            reloadStandingState.speedParameter = "ReloadSpeed";
            reloadCrouchState.speedParameterActive = true;
            reloadCrouchState.speedParameter = "ReloadSpeed";

            machine.defaultState = empty;

            AnimatorStateTransition healTransition =
                machine.AddAnyStateTransition(healing);
            ConfigureTransition(healTransition, 0.08f);
            healTransition.canTransitionToSelf = false;
            healTransition.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Healing"
            );

            AnimatorStateTransition reloadStandingTransition =
                machine.AddAnyStateTransition(reloadStandingState);
            ConfigureTransition(reloadStandingTransition, 0.08f);
            reloadStandingTransition.canTransitionToSelf = false;
            reloadStandingTransition.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Reloading"
            );
            reloadStandingTransition.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                "Crouch"
            );
            reloadStandingTransition.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                "Healing"
            );

            AnimatorStateTransition reloadCrouchTransition =
                machine.AddAnyStateTransition(reloadCrouchState);
            ConfigureTransition(reloadCrouchTransition, 0.08f);
            reloadCrouchTransition.canTransitionToSelf = false;
            reloadCrouchTransition.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Reloading"
            );
            reloadCrouchTransition.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Crouch"
            );
            reloadCrouchTransition.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                "Healing"
            );

            if (weaponSwitch != null)
            {
                AnimatorStateTransition switchTransition =
                    machine.AddAnyStateTransition(switchState);
                ConfigureTransition(switchTransition, 0.05f);
                switchTransition.canTransitionToSelf = false;
                switchTransition.AddCondition(
                    AnimatorConditionMode.If,
                    0f,
                    "WeaponSwitch"
                );
                switchTransition.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "Healing"
                );
                switchTransition.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "Reloading"
                );
            }

            if (takingItem != null)
            {
                AnimatorStateTransition pickupTransition =
                    machine.AddAnyStateTransition(pickupState);
                ConfigureTransition(pickupTransition, 0.05f);
                pickupTransition.canTransitionToSelf = false;
                pickupTransition.AddCondition(
                    AnimatorConditionMode.If,
                    0f,
                    "PickupItem"
                );
                pickupTransition.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "Healing"
                );
                pickupTransition.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "Reloading"
                );
            }

            AddTransition(
                healing,
                empty,
                0.08f,
                Condition("Healing", false)
            );

            AddTransition(
                reloadStandingState,
                empty,
                0.06f,
                Condition("Reloading", false)
            );
            AddTransition(
                reloadCrouchState,
                empty,
                0.06f,
                Condition("Reloading", false)
            );

            AddTransition(
                reloadStandingState,
                reloadCrouchState,
                0.05f,
                Condition("Reloading", true),
                Condition("Crouch", true)
            );
            AddTransition(
                reloadCrouchState,
                reloadStandingState,
                0.05f,
                Condition("Reloading", true),
                Condition("Crouch", false)
            );

            AddExitTimeTransition(switchState, empty, 0.95f, 0.08f);
            AddExitTimeTransition(pickupState, empty, 0.95f, 0.08f);

            AppendLayer(controller, layer);
        }

        private static bool MaterializeCoordinatorInFunctionalScene()
        {
            if (!System.IO.File.Exists(ScenePath))
                return false;

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
                return false;

            PlayerInputReader input = FindLocalInput(scene);
            if (input == null)
            {
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return false;
            }

            bool changed = false;
            GameObject player = input.gameObject;

            PlayerAnimationCoordinator coordinator =
                player.GetComponent<PlayerAnimationCoordinator>();
            if (coordinator == null)
            {
                coordinator = player.AddComponent<PlayerAnimationCoordinator>();
                EditorUtility.SetDirty(player);
                changed = true;
            }
            else if (!coordinator.enabled)
            {
                coordinator.enabled = true;
                EditorUtility.SetDirty(coordinator);
                changed = true;
            }

            // El driver anterior queda físicamente en la escena como referencia
            // histórica, pero desactivado. Solo Coordinator escribe el Animator.
            PlayerAnimatorDriver[] oldDrivers =
                player.GetComponentsInChildren<PlayerAnimatorDriver>(true);
            for (int i = 0; i < oldDrivers.Length; i++)
            {
                if (oldDrivers[i] != null && oldDrivers[i].enabled)
                {
                    oldDrivers[i].enabled = false;
                    EditorUtility.SetDirty(oldDrivers[i]);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);

            return changed;
        }

        private static PlayerInputReader FindLocalInput(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            PlayerInputReader fallback = null;

            for (int i = 0; i < roots.Length; i++)
            {
                PlayerInputReader[] readers =
                    roots[i].GetComponentsInChildren<PlayerInputReader>(true);

                for (int r = 0; r < readers.Length; r++)
                {
                    PlayerInputReader reader = readers[r];
                    if (reader == null)
                        continue;

                    fallback ??= reader;
                    if (!reader.UsesExternalControl)
                        return reader;
                }
            }

            return fallback;
        }

        private static bool EnsureParameter(
            AnimatorController controller,
            string parameterName,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName)
                    return false;
            }

            controller.AddParameter(parameterName, type);
            return true;
        }

        private static int FindLayerIndex(
            AnimatorController controller,
            string layerName)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName)
                    return i;
            }

            return -1;
        }

        private static bool SetLegacyLayerWeight(
            AnimatorController controller,
            string layerName,
            float weight)
        {
            return SetLayerWeight(controller, layerName, weight);
        }

        private static bool SetLayerWeight(
            AnimatorController controller,
            string layerName,
            float weight)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name != layerName)
                    continue;

                if (Mathf.Approximately(layers[i].defaultWeight, weight))
                    return false;

                layers[i].defaultWeight = weight;
                controller.layers = layers;
                return true;
            }

            return false;
        }

        private static void AppendLayer(
            AnimatorController controller,
            AnimatorControllerLayer layer)
        {
            List<AnimatorControllerLayer> layers =
                new List<AnimatorControllerLayer>(controller.layers)
                {
                    layer
                };
            controller.layers = layers.ToArray();
        }

        private static AnimatorState FindState(
            AnimatorController controller,
            string stateName)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorState found = FindStateRecursive(
                    layers[i].stateMachine,
                    stateName
                );
                if (found != null)
                    return found;
            }

            return null;
        }

        private static AnimatorState FindStateRecursive(
            AnimatorStateMachine machine,
            string stateName)
        {
            if (machine == null)
                return null;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null &&
                    states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorState found = FindStateRecursive(
                    children[i].stateMachine,
                    stateName
                );
                if (found != null)
                    return found;
            }

            return null;
        }

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return null;
        }

        private readonly struct TransitionCondition
        {
            public TransitionCondition(string parameter, bool value)
            {
                Parameter = parameter;
                Value = value;
            }

            public string Parameter { get; }
            public bool Value { get; }
        }

        private static TransitionCondition Condition(string parameter, bool value)
        {
            return new TransitionCondition(parameter, value);
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            float duration,
            params TransitionCondition[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureTransition(transition, duration);

            for (int i = 0; i < conditions.Length; i++)
            {
                transition.AddCondition(
                    conditions[i].Value
                        ? AnimatorConditionMode.If
                        : AnimatorConditionMode.IfNot,
                    0f,
                    conditions[i].Parameter
                );
            }
        }

        private static void AddExitTimeTransition(
            AnimatorState from,
            AnimatorState to,
            float exitTime,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
        }

        private static void ConfigureTransition(
            AnimatorStateTransition transition,
            float duration)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.exitTime = 0f;
            transition.canTransitionToSelf = false;
        }
    }
}
