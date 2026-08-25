using System;
using System.Collections.Generic;
using ROS.Game.Animation;
using ROS.Game.Character;
using ROS.Game.Input;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa de forma idempotente la arquitectura final del Animator:
    /// 0 Locomotion
    /// 1 WeaponUpperBody      (Override + upper-body mask)
    /// 2 UpperBodyActions     (Override + upper-body mask)
    /// 3 AimRecoil            (Additive + upper-body mask)
    /// 4 FullBodyOverride     (Override, sin mascara)
    ///
    /// Los clips existentes se reutilizan; no se fabrican motions nuevos.
    /// Las capas superiores nacen con peso 0: PlayerAnimationCoordinator las
    /// activa solo cuando existe un arma o accion real. Esto evita que un Empty
    /// Override neutralice la animacion natural de brazos/manos sin arma.
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

        private static readonly string[] UpperBodyGestureStates =
        {
            "Gesture_Salute",
            "Gesture_Talking_On_Phone",
            "Gesture_Waving_Gesture"
        };

        private static readonly string[] FullBodyGestureStates =
        {
            "Gesture_Dancing",
            "Gesture_Fishing_Cast",
            "Gesture_Hip_Hop_Dancing",
            "Gesture_Joyful_Jump",
            "Gesture_Opening",
            "Gesture_Rumba_Dancing"
        };

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
                    "[Editor First] Animator consolidado en 5 capas: " +
                    "Locomotion, WeaponUpperBody, UpperBodyActions, AimRecoil y FullBodyOverride."
                );
            }
        }

        internal static void ScheduleMaterialize()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
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

            EnsureParameters(controller);

            AnimatorControllerLayer locomotion =
                FindLayer(controller, PlayerAnimationCoordinator.LocomotionLayerName);
            if (locomotion == null)
            {
                Debug.LogError(
                    "[Editor First] El Animator no contiene Locomotion. Se cancela la migracion para no perder la locomocion base."
                );
                return false;
            }

            if (HasExactFinalLayerOrder(controller))
            {
                bool changed = ConfigureFinalLayerSettings(controller, upperBodyMask);
                if (changed)
                {
                    EditorUtility.SetDirty(controller);
                    AssetDatabase.SaveAssets();
                }
                return changed;
            }

            // Capturar Motion references antes de retirar las capas legacy.
            Motion armedLocomotion = FindMotion(
                controller,
                "ArmedLocomotion",
                "BT_RifleLocomotion"
            );
            Motion armedCrouch = FindMotion(
                controller,
                "ArmedCrouch",
                "BT_RifleCrouch"
            );
            Motion aimLocomotion = FindMotion(
                controller,
                "AimLocomotion",
                "BT_AimLocomotion"
            );
            Motion healing = FindMotion(controller, "Healing");
            Motion takingItem = FindMotion(controller, "TakingItem");

            AnimationClip reloadStanding =
                FindMotion(controller, "ReloadStanding") as AnimationClip ??
                LoadFirstAnimationClip(ReloadStandingPath);
            AnimationClip reloadCrouch =
                FindMotion(controller, "ReloadCrouch") as AnimationClip ??
                LoadFirstAnimationClip(ReloadCrouchPath);
            AnimationClip weaponSwitch =
                FindMotion(controller, "WeaponSwitch", "RifleSwitch_UpperBody") as AnimationClip ??
                LoadFirstAnimationClip(WeaponSwitchPath);

            Dictionary<string, Motion> gestureMotions =
                CaptureGestureMotions(controller);

            AnimatorControllerLayer weaponLayer = CreateWeaponUpperBodyLayer(
                controller,
                upperBodyMask,
                armedLocomotion,
                armedCrouch,
                aimLocomotion,
                reloadStanding,
                reloadCrouch,
                weaponSwitch
            );

            AnimatorControllerLayer actionsLayer = CreateUpperBodyActionsLayer(
                controller,
                upperBodyMask,
                healing,
                takingItem,
                gestureMotions
            );

            AnimatorControllerLayer aimRecoilLayer = CreateEmptyLayer(
                controller,
                PlayerAnimationCoordinator.AimRecoilLayerName,
                upperBodyMask,
                AnimatorLayerBlendingMode.Additive,
                0f,
                false
            );

            AnimatorControllerLayer fullBodyLayer = CreateFullBodyOverrideLayer(
                controller,
                gestureMotions
            );

            locomotion.name = PlayerAnimationCoordinator.LocomotionLayerName;
            locomotion.avatarMask = null;
            locomotion.defaultWeight = 1f;
            locomotion.blendingMode = AnimatorLayerBlendingMode.Override;
            locomotion.iKPass = false;

            controller.layers = new[]
            {
                locomotion,
                weaponLayer,
                actionsLayer,
                aimRecoilLayer,
                fullBodyLayer
            };

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void EnsureParameters(AnimatorController controller)
        {
            EnsureParameter(controller, "UpperBodyArmed", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "UpperBodyAim", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Reloading", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "ReloadSpeed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Healing", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "AimPitch", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Crouch", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "WeaponSwitch", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "PickupItem", AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorControllerLayer CreateWeaponUpperBodyLayer(
            AnimatorController controller,
            AvatarMask mask,
            Motion armedLocomotion,
            Motion armedCrouch,
            Motion aimLocomotion,
            AnimationClip reloadStanding,
            AnimationClip reloadCrouch,
            AnimationClip weaponSwitch)
        {
            AnimatorControllerLayer layer = CreateLayer(
                controller,
                PlayerAnimationCoordinator.WeaponUpperBodyLayerName,
                mask,
                AnimatorLayerBlendingMode.Override,
                0f,
                true
            );

            AnimatorStateMachine machine = layer.stateMachine;
            AnimatorState empty = AddState(machine, "Empty", null, 220f, 20f);
            AnimatorState armed = AddState(machine, "ArmedLocomotion", armedLocomotion, 500f, -120f);
            AnimatorState crouch = AddState(
                machine,
                "ArmedCrouch",
                armedCrouch != null ? armedCrouch : armedLocomotion,
                500f,
                20f
            );
            AnimatorState aim = AddState(machine, "AimLocomotion", aimLocomotion, 780f, -40f);
            AnimatorState reloadStandingState = AddState(
                machine,
                "ReloadStanding",
                reloadStanding,
                760f,
                120f
            );
            AnimatorState reloadCrouchState = AddState(
                machine,
                "ReloadCrouch",
                reloadCrouch != null ? reloadCrouch : reloadStanding,
                760f,
                230f
            );
            AnimatorState switchState = AddState(
                machine,
                "WeaponSwitch",
                weaponSwitch,
                500f,
                250f
            );

            machine.defaultState = empty;

            AddTransition(empty, armed, 0.08f,
                Condition("UpperBodyArmed", true), Condition("Crouch", false));
            AddTransition(empty, crouch, 0.08f,
                Condition("UpperBodyArmed", true), Condition("Crouch", true));
            AddTransition(armed, empty, 0.08f, Condition("UpperBodyArmed", false));
            AddTransition(crouch, empty, 0.08f, Condition("UpperBodyArmed", false));

            AddTransition(armed, crouch, 0.08f,
                Condition("Crouch", true), Condition("UpperBodyAim", false));
            AddTransition(crouch, armed, 0.08f,
                Condition("Crouch", false), Condition("UpperBodyAim", false));

            AddTransition(armed, aim, 0.05f, Condition("UpperBodyAim", true));
            AddTransition(crouch, aim, 0.05f, Condition("UpperBodyAim", true));
            AddTransition(aim, armed, 0.05f,
                Condition("UpperBodyAim", false), Condition("Crouch", false));
            AddTransition(aim, crouch, 0.05f,
                Condition("UpperBodyAim", false), Condition("Crouch", true));
            AddTransition(aim, empty, 0.05f, Condition("UpperBodyArmed", false));

            if (reloadStanding != null)
            {
                reloadStandingState.speedParameterActive = true;
                reloadStandingState.speedParameter = "ReloadSpeed";
                AnimatorStateTransition transition = machine.AddAnyStateTransition(reloadStandingState);
                ConfigureTransition(transition, 0.05f);
                transition.AddCondition(AnimatorConditionMode.If, 0f, "Reloading");
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch");
                AddTransition(reloadStandingState, armed, 0.05f,
                    Condition("Reloading", false), Condition("UpperBodyArmed", true));
                AddTransition(reloadStandingState, empty, 0.05f,
                    Condition("Reloading", false), Condition("UpperBodyArmed", false));
            }

            if (reloadCrouchState.motion != null)
            {
                reloadCrouchState.speedParameterActive = true;
                reloadCrouchState.speedParameter = "ReloadSpeed";
                AnimatorStateTransition transition = machine.AddAnyStateTransition(reloadCrouchState);
                ConfigureTransition(transition, 0.05f);
                transition.AddCondition(AnimatorConditionMode.If, 0f, "Reloading");
                transition.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
                AddTransition(reloadCrouchState, crouch, 0.05f,
                    Condition("Reloading", false), Condition("UpperBodyArmed", true));
                AddTransition(reloadCrouchState, empty, 0.05f,
                    Condition("Reloading", false), Condition("UpperBodyArmed", false));
            }

            if (weaponSwitch != null)
            {
                AnimatorStateTransition transition = machine.AddAnyStateTransition(switchState);
                ConfigureTransition(transition, 0.04f);
                transition.AddCondition(AnimatorConditionMode.If, 0f, "WeaponSwitch");
                AddExitTimeTransition(switchState, empty, 0.95f, 0.06f);
            }

            return layer;
        }

        private static AnimatorControllerLayer CreateUpperBodyActionsLayer(
            AnimatorController controller,
            AvatarMask mask,
            Motion healingMotion,
            Motion takingItemMotion,
            Dictionary<string, Motion> gestures)
        {
            AnimatorControllerLayer layer = CreateLayer(
                controller,
                PlayerAnimationCoordinator.UpperBodyActionsLayerName,
                mask,
                AnimatorLayerBlendingMode.Override,
                0f,
                true
            );

            AnimatorStateMachine machine = layer.stateMachine;
            AnimatorState empty = AddState(machine, "Empty", null, 220f, 20f);
            machine.defaultState = empty;

            AnimatorState healing = AddState(machine, "Healing", healingMotion, 520f, -100f);
            if (healingMotion != null)
            {
                AnimatorStateTransition healTransition = machine.AddAnyStateTransition(healing);
                ConfigureTransition(healTransition, 0.06f);
                healTransition.AddCondition(AnimatorConditionMode.If, 0f, "Healing");
                AddTransition(healing, empty, 0.06f, Condition("Healing", false));
            }

            AnimatorState takingItem = AddState(machine, "TakingItem", takingItemMotion, 520f, 40f);
            if (takingItemMotion != null)
            {
                AnimatorStateTransition pickupTransition = machine.AddAnyStateTransition(takingItem);
                ConfigureTransition(pickupTransition, 0.05f);
                pickupTransition.AddCondition(AnimatorConditionMode.If, 0f, "PickupItem");
                AddExitTimeTransition(takingItem, empty, 0.95f, 0.06f);
            }

            for (int i = 0; i < UpperBodyGestureStates.Length; i++)
            {
                string stateName = UpperBodyGestureStates[i];
                gestures.TryGetValue(stateName, out Motion motion);
                AddGestureState(machine, empty, stateName, motion, 620f, 180f + i * 100f);
            }

            return layer;
        }

        private static AnimatorControllerLayer CreateFullBodyOverrideLayer(
            AnimatorController controller,
            Dictionary<string, Motion> gestures)
        {
            AnimatorControllerLayer layer = CreateLayer(
                controller,
                PlayerAnimationCoordinator.FullBodyOverrideLayerName,
                null,
                AnimatorLayerBlendingMode.Override,
                0f,
                false
            );

            AnimatorStateMachine machine = layer.stateMachine;
            AnimatorState empty = AddState(machine, "Empty", null, 220f, 20f);
            machine.defaultState = empty;

            for (int i = 0; i < FullBodyGestureStates.Length; i++)
            {
                string stateName = FullBodyGestureStates[i];
                gestures.TryGetValue(stateName, out Motion motion);
                AddGestureState(
                    machine,
                    empty,
                    stateName,
                    motion,
                    560f + (i % 2) * 300f,
                    -180f + (i / 2) * 130f
                );
            }

            return layer;
        }

        private static AnimatorControllerLayer CreateEmptyLayer(
            AnimatorController controller,
            string name,
            AvatarMask mask,
            AnimatorLayerBlendingMode blendingMode,
            float defaultWeight,
            bool ikPass)
        {
            AnimatorControllerLayer layer = CreateLayer(
                controller,
                name,
                mask,
                blendingMode,
                defaultWeight,
                ikPass
            );
            AnimatorState empty = AddState(layer.stateMachine, "Empty", null, 250f, 20f);
            layer.stateMachine.defaultState = empty;
            return layer;
        }

        private static AnimatorControllerLayer CreateLayer(
            AnimatorController controller,
            string name,
            AvatarMask mask,
            AnimatorLayerBlendingMode blendingMode,
            float defaultWeight,
            bool ikPass)
        {
            AnimatorStateMachine machine = new AnimatorStateMachine { name = name };
            AssetDatabase.AddObjectToAsset(machine, controller);

            return new AnimatorControllerLayer
            {
                name = name,
                stateMachine = machine,
                avatarMask = mask,
                defaultWeight = defaultWeight,
                blendingMode = blendingMode,
                iKPass = ikPass
            };
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            string name,
            Motion motion,
            float x,
            float y)
        {
            AnimatorState state = machine.AddState(name, new Vector3(x, y, 0f));
            state.motion = motion;
            state.writeDefaultValues = false;
            return state;
        }

        private static void AddGestureState(
            AnimatorStateMachine machine,
            AnimatorState empty,
            string stateName,
            Motion motion,
            float x,
            float y)
        {
            if (motion == null)
                return;

            AnimatorState state = AddState(machine, stateName, motion, x, y);
            state.tag = "Gesture";
            state.speed = 1f;
            AddExitTimeTransition(state, empty, 0.98f, 0.12f);
        }

        private static Dictionary<string, Motion> CaptureGestureMotions(
            AnimatorController controller)
        {
            Dictionary<string, Motion> result =
                new Dictionary<string, Motion>(StringComparer.Ordinal);

            for (int i = 0; i < UpperBodyGestureStates.Length; i++)
            {
                Motion motion = FindMotion(controller, UpperBodyGestureStates[i]);
                if (motion != null)
                    result[UpperBodyGestureStates[i]] = motion;
            }

            for (int i = 0; i < FullBodyGestureStates.Length; i++)
            {
                Motion motion = FindMotion(controller, FullBodyGestureStates[i]);
                if (motion != null)
                    result[FullBodyGestureStates[i]] = motion;
            }

            return result;
        }

        private static bool HasExactFinalLayerOrder(AnimatorController controller)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            if (layers.Length != 5)
                return false;

            return layers[0].name == PlayerAnimationCoordinator.LocomotionLayerName &&
                   layers[1].name == PlayerAnimationCoordinator.WeaponUpperBodyLayerName &&
                   layers[2].name == PlayerAnimationCoordinator.UpperBodyActionsLayerName &&
                   layers[3].name == PlayerAnimationCoordinator.AimRecoilLayerName &&
                   layers[4].name == PlayerAnimationCoordinator.FullBodyOverrideLayerName;
        }

        private static bool ConfigureFinalLayerSettings(
            AnimatorController controller,
            AvatarMask upperBodyMask)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            bool changed = false;

            changed |= ConfigureLayer(
                layers[0], null, AnimatorLayerBlendingMode.Override, 1f, false);
            changed |= ConfigureLayer(
                layers[1], upperBodyMask, AnimatorLayerBlendingMode.Override, 0f, true);
            changed |= ConfigureLayer(
                layers[2], upperBodyMask, AnimatorLayerBlendingMode.Override, 0f, true);
            changed |= ConfigureLayer(
                layers[3], upperBodyMask, AnimatorLayerBlendingMode.Additive, 0f, false);
            changed |= ConfigureLayer(
                layers[4], null, AnimatorLayerBlendingMode.Override, 0f, false);

            if (changed)
                controller.layers = layers;

            return changed;
        }

        private static bool ConfigureLayer(
            AnimatorControllerLayer layer,
            AvatarMask mask,
            AnimatorLayerBlendingMode blendingMode,
            float defaultWeight,
            bool ikPass)
        {
            bool changed = false;
            if (layer.avatarMask != mask)
            {
                layer.avatarMask = mask;
                changed = true;
            }
            if (layer.blendingMode != blendingMode)
            {
                layer.blendingMode = blendingMode;
                changed = true;
            }
            if (!Mathf.Approximately(layer.defaultWeight, defaultWeight))
            {
                layer.defaultWeight = defaultWeight;
                changed = true;
            }
            if (layer.iKPass != ikPass)
            {
                layer.iKPass = ikPass;
                changed = true;
            }
            return changed;
        }

        private static AnimatorControllerLayer FindLayer(
            AnimatorController controller,
            string layerName)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName)
                    return layers[i];
            }
            return null;
        }

        private static Motion FindMotion(
            AnimatorController controller,
            params string[] stateNames)
        {
            for (int n = 0; n < stateNames.Length; n++)
            {
                AnimatorState state = FindState(controller, stateNames[n]);
                if (state != null && state.motion != null)
                    return state.motion;
            }
            return null;
        }

        private static AnimatorState FindState(
            AnimatorController controller,
            string stateName)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorState found = FindStateRecursive(layers[i].stateMachine, stateName);
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
                if (states[i].state != null && states[i].state.name == stateName)
                    return states[i].state;
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorState found = FindStateRecursive(children[i].stateMachine, stateName);
                if (found != null)
                    return found;
            }

            return null;
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

        private static bool MaterializeCoordinatorInFunctionalScene()
        {
            if (!System.IO.File.Exists(ScenePath))
                return false;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
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
            PlayerMotor motor =
                input.GetComponent<PlayerMotor>() ?? input.GetComponentInParent<PlayerMotor>();
            GameObject player = motor != null ? motor.gameObject : input.gameObject;

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

#pragma warning disable CS0618
            PlayerAnimatorDriver[] oldDrivers =
                player.GetComponentsInChildren<PlayerAnimatorDriver>(true);
#pragma warning restore CS0618
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
