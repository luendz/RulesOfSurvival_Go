using System;
using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Verifica la parte de Reload despues de materializar la arquitectura final.
    /// Corrige controllers que ya tenian las 5 capas pero conservaron un estado
    /// Reload sin Motion/transicion valida de una materializacion anterior.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstReloadUpperBodyRepair
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string StandingPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadStandingHip.fbx";

        private const string CrouchPath =
            "Assets/_Game/Animations/Characters/MainCharacter/Locomotion/Rifle/RifleReloadCrouchHip.fbx";

        static EditorFirstReloadUpperBodyRepair()
        {
            EditorApplication.delayCall += Repair;
        }

        public static bool Repair()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return false;

            EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            int weaponLayerIndex = FindLayer(
                controller,
                PlayerAnimationCoordinator.WeaponUpperBodyLayerName
            );
            if (weaponLayerIndex < 0)
                return false;

            AnimationClip standingClip = LoadClip(StandingPath);
            AnimationClip crouchClip = LoadClip(CrouchPath) ?? standingClip;

            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer weaponLayer = layers[weaponLayerIndex];
            AnimatorStateMachine machine = weaponLayer.stateMachine;
            if (machine == null)
                return false;

            bool changed = false;

            // Las capas superiores deben comenzar apagadas. El Coordinator las
            // enciende solo cuando existe arma/accion real.
            changed |= SetDefaultWeight(
                layers,
                weaponLayerIndex,
                0f
            );

            int actionsLayerIndex = FindLayer(
                controller,
                PlayerAnimationCoordinator.UpperBodyActionsLayerName
            );
            if (actionsLayerIndex >= 0)
                changed |= SetDefaultWeight(layers, actionsLayerIndex, 0f);

            int aimLayerIndex = FindLayer(
                controller,
                PlayerAnimationCoordinator.AimRecoilLayerName
            );
            if (aimLayerIndex >= 0)
                changed |= SetDefaultWeight(layers, aimLayerIndex, 0f);

            int fullBodyLayerIndex = FindLayer(
                controller,
                PlayerAnimationCoordinator.FullBodyOverrideLayerName
            );
            if (fullBodyLayerIndex >= 0)
                changed |= SetDefaultWeight(layers, fullBodyLayerIndex, 0f);

            controller.layers = layers;

            AnimatorState reloadStanding = FindState(machine, "ReloadStanding");
            AnimatorState reloadCrouch = FindState(machine, "ReloadCrouch");
            AnimatorState armed = FindState(machine, "ArmedLocomotion");
            AnimatorState armedCrouch = FindState(machine, "ArmedCrouch");

            if (reloadStanding == null || reloadCrouch == null)
            {
                Debug.LogWarning(
                    "[Editor First] WeaponUpperBody no contiene ReloadStanding/ReloadCrouch. " +
                    "Vuelve a ejecutar 'Consolidate Player Upper Lower Animation'."
                );
                return changed;
            }

            changed |= AssignMotion(reloadStanding, standingClip);
            changed |= AssignMotion(reloadCrouch, crouchClip);
            changed |= ConfigureReloadSpeed(reloadStanding);
            changed |= ConfigureReloadSpeed(reloadCrouch);

            changed |= EnsureReloadEntry(
                machine,
                reloadStanding,
                crouching: false
            );
            changed |= EnsureReloadEntry(
                machine,
                reloadCrouch,
                crouching: true
            );

            if (armed != null)
            {
                changed |= EnsureReloadExit(
                    reloadStanding,
                    armed,
                    crouching: false
                );
            }

            if (armedCrouch != null)
            {
                changed |= EnsureReloadExit(
                    reloadCrouch,
                    armedCrouch,
                    crouching: true
                );
            }

            if (changed)
            {
                EditorUtility.SetDirty(machine);
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Editor First] Reload reparado en WeaponUpperBody: " +
                    "clips standing/crouch, ReloadSpeed, entradas AnyState y pesos base."
                );
            }

            return changed;
        }

        private static bool SetDefaultWeight(
            AnimatorControllerLayer[] layers,
            int index,
            float weight)
        {
            if (index < 0 || index >= layers.Length)
                return false;

            if (Mathf.Approximately(layers[index].defaultWeight, weight))
                return false;

            layers[index].defaultWeight = weight;
            return true;
        }

        private static bool AssignMotion(AnimatorState state, Motion motion)
        {
            if (state == null || motion == null || state.motion == motion)
                return false;

            state.motion = motion;
            EditorUtility.SetDirty(state);
            return true;
        }

        private static bool ConfigureReloadSpeed(AnimatorState state)
        {
            if (state == null)
                return false;

            bool changed = false;
            if (!state.speedParameterActive)
            {
                state.speedParameterActive = true;
                changed = true;
            }

            if (state.speedParameter != "ReloadSpeed")
            {
                state.speedParameter = "ReloadSpeed";
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(state);

            return changed;
        }

        private static bool EnsureReloadEntry(
            AnimatorStateMachine machine,
            AnimatorState destination,
            bool crouching)
        {
            AnimatorStateTransition[] transitions = machine.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null || transition.destinationState != destination)
                    continue;

                if (HasCondition(transition, "Reloading", AnimatorConditionMode.If) &&
                    HasCondition(
                        transition,
                        "Crouch",
                        crouching ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot
                    ))
                {
                    return false;
                }
            }

            AnimatorStateTransition created = machine.AddAnyStateTransition(destination);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.04f;
            created.canTransitionToSelf = false;
            created.AddCondition(AnimatorConditionMode.If, 0f, "Reloading");
            created.AddCondition(
                crouching ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                "Crouch"
            );
            EditorUtility.SetDirty(created);
            return true;
        }

        private static bool EnsureReloadExit(
            AnimatorState from,
            AnimatorState to,
            bool crouching)
        {
            AnimatorStateTransition[] transitions = from.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null || transition.destinationState != to)
                    continue;

                if (HasCondition(transition, "Reloading", AnimatorConditionMode.IfNot))
                    return false;
            }

            AnimatorStateTransition created = from.AddTransition(to);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.05f;
            created.canTransitionToSelf = false;
            created.AddCondition(AnimatorConditionMode.IfNot, 0f, "Reloading");
            created.AddCondition(
                crouching ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                "Crouch"
            );
            EditorUtility.SetDirty(created);
            return true;
        }

        private static bool HasCondition(
            AnimatorStateTransition transition,
            string parameter,
            AnimatorConditionMode mode)
        {
            AnimatorCondition[] conditions = transition.conditions;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].parameter == parameter &&
                    conditions[i].mode == mode)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindLayer(
            AnimatorController controller,
            string name)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == name)
                    return i;
            }

            return -1;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string name)
        {
            if (machine == null)
                return null;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == name)
                    return states[i].state;
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorState found = FindState(children[i].stateMachine, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static AnimationClip LoadClip(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
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
    }
}
