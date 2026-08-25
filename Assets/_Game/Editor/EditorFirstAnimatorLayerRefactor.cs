using System;
using System.Collections.Generic;
using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Normaliza el Animator del jugador para que Locomotion sea responsable
    /// solo del movimiento/postura del cuerpo base y WeaponUpperBody del torso.
    ///
    /// Primera fase deliberadamente conservadora:
    /// - elimina locomocion armada duplicada del layer base;
    /// - elimina ArmedCrouch del torso porque las piernas ya resuelven Crouch;
    /// - mantiene ReloadStanding/ReloadCrouch hasta unificar tambien el calculo
    ///   de duracion de recarga del runtime.
    ///
    /// Es idempotente: puede ejecutarse varias veces sin duplicar estados.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstAnimatorLayerRefactor
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private static readonly HashSet<string> LegacyLocomotionStates =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "BT_RifleLocomotion",
                "RifleIdle",
                "BT_AimLocomotion",
                "BT_RifleCrouch",
                "RifleJump",
                "RifleFall",
                "RifleLand",
                "RifleReload Crouch Hip",
                "RifleReload Standing Hip",
                "RifleReloadCrouchHip",
                "RifleReloadStandingHip"
            };

        static EditorFirstAnimatorLayerRefactor()
        {
            EditorApplication.delayCall += Refactor;
        }

        [MenuItem("Rules Of Survival/Editor First/Refactor Animator Layers")]
        public static void Refactor()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
                return;

            bool changed = false;
            changed |= RefactorLocomotion(controller);
            changed |= RefactorWeaponUpperBody(controller);
            changed |= NormalizeLayerSettings(controller);

            if (!changed)
                return;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Editor First] Animator refactorizado: Locomotion queda libre de estados de arma " +
                "y WeaponUpperBody usa Empty/Hip/Aim con recargas aisladas en el torso."
            );
        }

        private static bool RefactorLocomotion(AnimatorController controller)
        {
            AnimatorControllerLayer layer = FindLayer(
                controller,
                PlayerAnimationCoordinator.LocomotionLayerName
            );

            if (layer == null || layer.stateMachine == null)
                return false;

            bool changed = RemoveLegacyStatesRecursive(layer.stateMachine);

            // La capa base siempre gobierna el cuerpo completo. Las armas se
            // componen exclusivamente desde las capas superiores.
            if (layer.avatarMask != null)
            {
                layer.avatarMask = null;
                changed = true;
            }

            if (layer.blendingMode != AnimatorLayerBlendingMode.Override)
            {
                layer.blendingMode = AnimatorLayerBlendingMode.Override;
                changed = true;
            }

            if (!Mathf.Approximately(layer.defaultWeight, 1f))
            {
                layer.defaultWeight = 1f;
                changed = true;
            }

            return changed;
        }

        private static bool RemoveLegacyStatesRecursive(AnimatorStateMachine machine)
        {
            bool changed = false;

            ChildAnimatorState[] states = machine.states;
            for (int i = states.Length - 1; i >= 0; i--)
            {
                AnimatorState state = states[i].state;
                if (state != null && LegacyLocomotionStates.Contains(state.name))
                {
                    machine.RemoveState(state);
                    changed = true;
                }
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = children.Length - 1; i >= 0; i--)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child == null)
                    continue;

                changed |= RemoveLegacyStatesRecursive(child);

                if (child.states.Length == 0 && child.stateMachines.Length == 0)
                {
                    machine.RemoveStateMachine(child);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool RefactorWeaponUpperBody(AnimatorController controller)
        {
            AnimatorControllerLayer layer = FindLayer(
                controller,
                PlayerAnimationCoordinator.WeaponUpperBodyLayerName
            );

            if (layer == null || layer.stateMachine == null)
                return false;

            AnimatorStateMachine machine = layer.stateMachine;

            // El formato nuevo ya no duplica la postura de brazos por Crouch.
            if (FindState(machine, "Hip") != null &&
                FindState(machine, "Aim") != null &&
                FindState(machine, "ArmedLocomotion") == null &&
                FindState(machine, "ArmedCrouch") == null &&
                FindState(machine, "AimLocomotion") == null)
            {
                return false;
            }

            Motion hipMotion = FindMotion(machine, "Hip", "ArmedLocomotion", "ArmedCrouch");
            Motion aimMotion = FindMotion(machine, "Aim", "AimLocomotion");
            Motion reloadStandingMotion = FindMotion(machine, "ReloadStanding");
            Motion reloadCrouchMotion = FindMotion(machine, "ReloadCrouch");
            Motion switchMotion = FindMotion(machine, "WeaponSwitch");

            ClearStateMachine(machine);

            AnimatorState empty = machine.AddState("Empty", new Vector3(260f, 80f));
            AnimatorState hip = machine.AddState("Hip", new Vector3(540f, 20f));
            AnimatorState aim = machine.AddState("Aim", new Vector3(800f, 20f));
            AnimatorState reloadStanding = machine.AddState(
                "ReloadStanding",
                new Vector3(800f, 160f)
            );
            AnimatorState reloadCrouch = machine.AddState(
                "ReloadCrouch",
                new Vector3(800f, 270f)
            );
            AnimatorState weaponSwitch = machine.AddState(
                "WeaponSwitch",
                new Vector3(540f, 210f)
            );

            empty.writeDefaultValues = false;
            hip.writeDefaultValues = false;
            aim.writeDefaultValues = false;
            reloadStanding.writeDefaultValues = false;
            reloadCrouch.writeDefaultValues = false;
            weaponSwitch.writeDefaultValues = false;

            hip.motion = hipMotion;
            aim.motion = aimMotion;
            reloadStanding.motion = reloadStandingMotion;
            reloadCrouch.motion = reloadCrouchMotion != null
                ? reloadCrouchMotion
                : reloadStandingMotion;
            weaponSwitch.motion = switchMotion;

            if (reloadStanding.motion != null)
            {
                reloadStanding.speedParameterActive = true;
                reloadStanding.speedParameter = "ReloadSpeed";
            }

            if (reloadCrouch.motion != null)
            {
                reloadCrouch.speedParameterActive = true;
                reloadCrouch.speedParameter = "ReloadSpeed";
            }

            machine.defaultState = empty;

            // Crouch deja de decidir una pose de arma distinta: solamente cambia
            // las piernas desde Locomotion. Hip/Aim siguen siendo torso puro.
            AddBoolTransition(empty, hip, "UpperBodyArmed", true, 0.08f);
            AddBoolTransition(hip, empty, "UpperBodyArmed", false, 0.08f);
            AddBoolTransition(hip, aim, "UpperBodyAim", true, 0.05f);
            AddBoolTransition(aim, hip, "UpperBodyAim", false, 0.05f);
            AddBoolTransition(aim, empty, "UpperBodyArmed", false, 0.05f);

            if (reloadStanding.motion != null)
            {
                AnimatorStateTransition toReloadStanding =
                    machine.AddAnyStateTransition(reloadStanding);
                ConfigureTransition(toReloadStanding, 0.04f);
                toReloadStanding.AddCondition(AnimatorConditionMode.If, 0f, "Reloading");
                toReloadStanding.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch");

                AddBoolTransition(reloadStanding, hip, "Reloading", false, 0.05f);
            }

            if (reloadCrouch.motion != null)
            {
                AnimatorStateTransition toReloadCrouch =
                    machine.AddAnyStateTransition(reloadCrouch);
                ConfigureTransition(toReloadCrouch, 0.04f);
                toReloadCrouch.AddCondition(AnimatorConditionMode.If, 0f, "Reloading");
                toReloadCrouch.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");

                AddBoolTransition(reloadCrouch, hip, "Reloading", false, 0.05f);
            }

            if (switchMotion != null)
            {
                AnimatorStateTransition toSwitch = machine.AddAnyStateTransition(weaponSwitch);
                ConfigureTransition(toSwitch, 0.04f);
                toSwitch.AddCondition(AnimatorConditionMode.If, 0f, "WeaponSwitch");

                AnimatorStateTransition switchExit = weaponSwitch.AddTransition(empty);
                ConfigureTransition(switchExit, 0.06f);
                switchExit.hasExitTime = true;
                switchExit.exitTime = 0.95f;
            }

            return true;
        }

        private static void ClearStateMachine(AnimatorStateMachine machine)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = states.Length - 1; i >= 0; i--)
            {
                if (states[i].state != null)
                    machine.RemoveState(states[i].state);
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = children.Length - 1; i >= 0; i--)
            {
                if (children[i].stateMachine != null)
                    machine.RemoveStateMachine(children[i].stateMachine);
            }
        }

        private static bool NormalizeLayerSettings(AnimatorController controller)
        {
            bool changed = false;
            AnimatorControllerLayer[] layers = controller.layers;

            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorControllerLayer layer = layers[i];
                float expectedWeight = i == 0 ? 1f : 0f;

                if (!Mathf.Approximately(layer.defaultWeight, expectedWeight))
                {
                    layer.defaultWeight = expectedWeight;
                    changed = true;
                }

                if (layer.name == PlayerAnimationCoordinator.AimRecoilLayerName)
                {
                    if (layer.blendingMode != AnimatorLayerBlendingMode.Additive)
                    {
                        layer.blendingMode = AnimatorLayerBlendingMode.Additive;
                        changed = true;
                    }
                }
                else if (layer.blendingMode != AnimatorLayerBlendingMode.Override)
                {
                    layer.blendingMode = AnimatorLayerBlendingMode.Override;
                    changed = true;
                }
            }

            if (changed)
                controller.layers = layers;

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

        private static AnimatorState FindState(AnimatorStateMachine machine, string stateName)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.name == stateName)
                    return state;
            }

            return null;
        }

        private static Motion FindMotion(
            AnimatorStateMachine machine,
            params string[] stateNames)
        {
            for (int i = 0; i < stateNames.Length; i++)
            {
                AnimatorState state = FindState(machine, stateNames[i]);
                if (state != null && state.motion != null)
                    return state.motion;
            }

            return null;
        }

        private static void AddBoolTransition(
            AnimatorState source,
            AnimatorState destination,
            string parameter,
            bool value,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            ConfigureTransition(transition, duration);
            transition.AddCondition(
                value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                parameter
            );
        }

        private static void ConfigureTransition(
            AnimatorStateTransition transition,
            float duration)
        {
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.offset = 0f;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
        }
    }
}
