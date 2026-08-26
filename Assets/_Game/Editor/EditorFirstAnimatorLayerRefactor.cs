using System;
using System.Collections.Generic;
using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Normaliza el Animator del jugador con una arquitectura extensible:
    /// - Locomotion: movimiento/postura y flujo Jump/Fall/Landing.
    /// - WeaponUpperBody: armas de fuego y melee sin crear layers por arma.
    /// - FullBodyOverride: gestos y espacio reservado para ataques melee completos.
    ///
    /// Todo queda materializado como estados/parametros normales del Animator,
    /// visible y editable desde Unity. Una vez migrado, no vuelve a reconstruir
    /// los grupos principales para no pisar ajustes manuales posteriores.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstAnimatorLayerRefactor
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string ShouldFallParameter = "ShouldFall";
        private const string WeaponCategoryParameter = "WeaponCategory";
        private const string WeaponStyleParameter = "WeaponStyle";
        private const string MeleeAttackParameter = "MeleeAttack";
        private const string MeleeAttackIndexParameter = "MeleeAttackIndex";
        private const string AirborneFlowTag = "ROS_AirborneV2";

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
            // Se difiere un tick adicional para ejecutarse despues de los
            // materializadores legacy que aun reconstruyen partes del Animator.
            EditorApplication.delayCall += ScheduleAfterLegacyMaterializers;
        }

        private static void ScheduleAfterLegacyMaterializers()
        {
            EditorApplication.delayCall -= Refactor;
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
            changed |= EnsureParameters(controller);
            changed |= RefactorLocomotion(controller);
            changed |= RefactorAirborneFlow(controller);
            changed |= RefactorWeaponUpperBody(controller);
            changed |= EnsureMeleeFullBodyArea(controller);
            changed |= NormalizeLayerSettings(controller);

            if (!changed)
                return;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Editor First] Animator refactorizado: flujo Jump/Fall/Landing corregido, " +
                "WeaponUpperBody preparado para Firearm/Melee y FullBodyOverride preparado para ataques melee."
            );
        }

        private static bool EnsureParameters(AnimatorController controller)
        {
            bool changed = false;
            changed |= EnsureParameter(
                controller,
                ShouldFallParameter,
                AnimatorControllerParameterType.Bool
            );
            changed |= EnsureParameter(
                controller,
                WeaponCategoryParameter,
                AnimatorControllerParameterType.Int
            );
            changed |= EnsureParameter(
                controller,
                WeaponStyleParameter,
                AnimatorControllerParameterType.Int
            );
            changed |= EnsureParameter(
                controller,
                MeleeAttackParameter,
                AnimatorControllerParameterType.Trigger
            );
            changed |= EnsureParameter(
                controller,
                MeleeAttackIndexParameter,
                AnimatorControllerParameterType.Int
            );
            return changed;
        }

        private static bool EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name != name)
                    continue;

                if (parameters[i].type == type)
                    return false;

                controller.RemoveParameter(i);
                controller.AddParameter(name, type);
                return true;
            }

            controller.AddParameter(name, type);
            return true;
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

        /// <summary>
        /// Fuerza un flujo aereo determinista una sola vez:
        /// salto corto: Jump -> Landing -> BT_Locomotion
        /// caida real : Jump/Locomotion -> Fall -> Landing -> BT_Locomotion
        /// </summary>
        private static bool RefactorAirborneFlow(AnimatorController controller)
        {
            AnimatorControllerLayer layer = FindLayer(
                controller,
                PlayerAnimationCoordinator.LocomotionLayerName
            );

            if (layer == null || layer.stateMachine == null)
                return false;

            AnimatorStateMachine machine = layer.stateMachine;
            AnimatorState jump = FindStateRecursive(machine, "Jump");
            AnimatorState fall = FindStateRecursive(machine, "Fall");
            AnimatorState landing = FindStateRecursive(machine, "Landing");
            AnimatorState locomotion = FindStateRecursive(machine, "BT_Locomotion");

            if (jump == null || fall == null || landing == null || locomotion == null)
                return false;

            // Marca de migracion: desde aqui el usuario puede editar las
            // transiciones manualmente y no se volveran a pisar al abrir Unity.
            if (landing.tag == AirborneFlowTag)
                return false;

            RemoveAllTransitions(jump);
            RemoveAllTransitions(fall);
            RemoveAllTransitions(landing);

            AnimatorStateTransition jumpToFall = jump.AddTransition(fall);
            ConfigureTransition(jumpToFall, 0.05f);
            jumpToFall.AddCondition(
                AnimatorConditionMode.If,
                0f,
                ShouldFallParameter
            );

            AnimatorStateTransition jumpToLanding = jump.AddTransition(landing);
            ConfigureTransition(jumpToLanding, 0.04f);
            jumpToLanding.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Grounded"
            );

            AnimatorStateTransition fallToLanding = fall.AddTransition(landing);
            ConfigureTransition(fallToLanding, 0.04f);
            fallToLanding.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Grounded"
            );

            AnimatorStateTransition landingToLocomotion = landing.AddTransition(locomotion);
            ConfigureTransition(landingToLocomotion, 0.08f);
            landingToLocomotion.hasExitTime = true;
            landingToLocomotion.exitTime = 0.88f;

            // Si se camina fuera de una plataforma sin haber pasado por Jump,
            // cualquier transicion existente hacia Fall debe usar ShouldFall.
            ReplaceTransitionsToFallRecursive(machine, fall, jump);

            jump.writeDefaultValues = false;
            fall.writeDefaultValues = false;
            landing.writeDefaultValues = false;
            landing.tag = AirborneFlowTag;

            EditorUtility.SetDirty(jump);
            EditorUtility.SetDirty(fall);
            EditorUtility.SetDirty(landing);
            return true;
        }

        private static void ReplaceTransitionsToFallRecursive(
            AnimatorStateMachine machine,
            AnimatorState fall,
            AnimatorState jump)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null || state == fall || state == jump)
                    continue;

                AnimatorStateTransition[] transitions = state.transitions;
                for (int j = 0; j < transitions.Length; j++)
                {
                    AnimatorStateTransition transition = transitions[j];
                    if (transition == null || transition.destinationState != fall)
                        continue;

                    ClearConditions(transition);
                    ConfigureTransition(transition, 0.05f);
                    transition.AddCondition(
                        AnimatorConditionMode.If,
                        0f,
                        ShouldFallParameter
                    );
                    EditorUtility.SetDirty(transition);
                }
            }

            AnimatorStateTransition[] anyTransitions = machine.anyStateTransitions;
            for (int i = 0; i < anyTransitions.Length; i++)
            {
                AnimatorStateTransition transition = anyTransitions[i];
                if (transition == null || transition.destinationState != fall)
                    continue;

                ClearConditions(transition);
                ConfigureTransition(transition, 0.05f);
                transition.AddCondition(
                    AnimatorConditionMode.If,
                    0f,
                    ShouldFallParameter
                );
                EditorUtility.SetDirty(transition);
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].stateMachine != null)
                {
                    ReplaceTransitionsToFallRecursive(
                        children[i].stateMachine,
                        fall,
                        jump
                    );
                }
            }
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

            // Ya migrado: respetar cualquier ajuste manual posterior.
            if (FindState(machine, "Firearm_Hip") != null &&
                FindState(machine, "Firearm_Aim") != null &&
                FindState(machine, "Melee_Hold") != null &&
                FindState(machine, "ArmedLocomotion") == null &&
                FindState(machine, "ArmedCrouch") == null &&
                FindState(machine, "AimLocomotion") == null)
            {
                return false;
            }

            Motion hipMotion = FindMotion(
                machine,
                "Firearm_Hip",
                "Hip",
                "ArmedLocomotion",
                "ArmedCrouch"
            );
            Motion aimMotion = FindMotion(
                machine,
                "Firearm_Aim",
                "Aim",
                "AimLocomotion"
            );
            Motion reloadStandingMotion = FindMotion(
                machine,
                "Firearm_ReloadStanding",
                "ReloadStanding"
            );
            Motion reloadCrouchMotion = FindMotion(
                machine,
                "Firearm_ReloadCrouch",
                "ReloadCrouch"
            );
            Motion switchMotion = FindMotion(machine, "WeaponSwitch");
            Motion meleeHoldMotion = FindMotion(machine, "Melee_Hold");
            Motion meleeEquipMotion = FindMotion(machine, "Melee_Equip");

            ClearStateMachine(machine);

            AnimatorState empty = AddState(machine, "Empty", null, 240f, 80f);

            // Grupo visual Firearm.
            AnimatorState firearmHip = AddState(
                machine,
                "Firearm_Hip",
                hipMotion,
                540f,
                -80f
            );
            AnimatorState firearmAim = AddState(
                machine,
                "Firearm_Aim",
                aimMotion,
                820f,
                -80f
            );
            AnimatorState reloadStanding = AddState(
                machine,
                "Firearm_ReloadStanding",
                reloadStandingMotion,
                820f,
                60f
            );
            AnimatorState reloadCrouch = AddState(
                machine,
                "Firearm_ReloadCrouch",
                reloadCrouchMotion != null ? reloadCrouchMotion : reloadStandingMotion,
                820f,
                170f
            );
            AnimatorState weaponSwitch = AddState(
                machine,
                "WeaponSwitch",
                switchMotion,
                540f,
                90f
            );

            // Grupo visual Melee. Los Motion quedan configurables en el Animator;
            // no se crea KnifeLayer/HammerLayer/ChickenLayer.
            AnimatorState meleeHold = AddState(
                machine,
                "Melee_Hold",
                meleeHoldMotion,
                540f,
                320f
            );
            AnimatorState meleeEquip = AddState(
                machine,
                "Melee_Equip",
                meleeEquipMotion,
                820f,
                320f
            );

            firearmHip.tag = "Firearm";
            firearmAim.tag = "Firearm";
            reloadStanding.tag = "Firearm";
            reloadCrouch.tag = "Firearm";
            meleeHold.tag = "Melee";
            meleeEquip.tag = "Melee";

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

            AddBoolAndIntTransition(
                empty,
                firearmHip,
                "UpperBodyArmed",
                true,
                WeaponCategoryParameter,
                PlayerAnimationCoordinator.WeaponCategoryFirearm,
                0.08f
            );
            AddBoolAndIntTransition(
                empty,
                meleeHold,
                "UpperBodyArmed",
                true,
                WeaponCategoryParameter,
                PlayerAnimationCoordinator.WeaponCategoryMelee,
                0.08f
            );

            AddBoolTransition(firearmHip, empty, "UpperBodyArmed", false, 0.08f);
            AddBoolTransition(meleeHold, empty, "UpperBodyArmed", false, 0.08f);

            AddIntTransition(
                firearmHip,
                meleeHold,
                WeaponCategoryParameter,
                PlayerAnimationCoordinator.WeaponCategoryMelee,
                0.08f
            );
            AddIntTransition(
                meleeHold,
                firearmHip,
                WeaponCategoryParameter,
                PlayerAnimationCoordinator.WeaponCategoryFirearm,
                0.08f
            );

            AddBoolTransition(firearmHip, firearmAim, "UpperBodyAim", true, 0.05f);
            AddBoolTransition(firearmAim, firearmHip, "UpperBodyAim", false, 0.05f);
            AddBoolTransition(firearmAim, empty, "UpperBodyArmed", false, 0.05f);
            AddIntTransition(
                firearmAim,
                meleeHold,
                WeaponCategoryParameter,
                PlayerAnimationCoordinator.WeaponCategoryMelee,
                0.05f
            );

            if (reloadStanding.motion != null)
            {
                AnimatorStateTransition toReloadStanding =
                    machine.AddAnyStateTransition(reloadStanding);
                ConfigureTransition(toReloadStanding, 0.04f);
                toReloadStanding.AddCondition(AnimatorConditionMode.If, 0f, "Reloading");
                toReloadStanding.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch");
                toReloadStanding.AddCondition(
                    AnimatorConditionMode.Equals,
                    PlayerAnimationCoordinator.WeaponCategoryFirearm,
                    WeaponCategoryParameter
                );

                AddBoolTransition(reloadStanding, firearmHip, "Reloading", false, 0.05f);
            }

            if (reloadCrouch.motion != null)
            {
                AnimatorStateTransition toReloadCrouch =
                    machine.AddAnyStateTransition(reloadCrouch);
                ConfigureTransition(toReloadCrouch, 0.04f);
                toReloadCrouch.AddCondition(AnimatorConditionMode.If, 0f, "Reloading");
                toReloadCrouch.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
                toReloadCrouch.AddCondition(
                    AnimatorConditionMode.Equals,
                    PlayerAnimationCoordinator.WeaponCategoryFirearm,
                    WeaponCategoryParameter
                );

                AddBoolTransition(reloadCrouch, firearmHip, "Reloading", false, 0.05f);
            }

            if (switchMotion != null)
            {
                AnimatorStateTransition toSwitch = machine.AddAnyStateTransition(weaponSwitch);
                ConfigureTransition(toSwitch, 0.04f);
                toSwitch.AddCondition(AnimatorConditionMode.If, 0f, "WeaponSwitch");
                toSwitch.AddCondition(
                    AnimatorConditionMode.Equals,
                    PlayerAnimationCoordinator.WeaponCategoryFirearm,
                    WeaponCategoryParameter
                );

                AddExitTimeTransition(weaponSwitch, empty, 0.95f, 0.06f);
            }

            if (meleeEquip.motion != null)
            {
                AnimatorStateTransition toMeleeEquip =
                    machine.AddAnyStateTransition(meleeEquip);
                ConfigureTransition(toMeleeEquip, 0.04f);
                toMeleeEquip.AddCondition(AnimatorConditionMode.If, 0f, "WeaponSwitch");
                toMeleeEquip.AddCondition(
                    AnimatorConditionMode.Equals,
                    PlayerAnimationCoordinator.WeaponCategoryMelee,
                    WeaponCategoryParameter
                );

                AddExitTimeTransition(meleeEquip, meleeHold, 0.95f, 0.06f);
            }

            return true;
        }

        /// <summary>
        /// Crea un sub-state machine visible para ataques melee de cuerpo completo.
        /// No conecta estados sin Motion para evitar activar un Override vacio.
        /// </summary>
        private static bool EnsureMeleeFullBodyArea(AnimatorController controller)
        {
            AnimatorControllerLayer layer = FindLayer(
                controller,
                PlayerAnimationCoordinator.FullBodyOverrideLayerName
            );

            if (layer == null || layer.stateMachine == null)
                return false;

            AnimatorStateMachine root = layer.stateMachine;
            AnimatorStateMachine meleeMachine = FindStateMachine(root, "MeleeAttacks");
            bool changed = false;

            if (meleeMachine == null)
            {
                meleeMachine = root.AddStateMachine(
                    "MeleeAttacks",
                    new Vector3(900f, 330f)
                );
                changed = true;
            }

            changed |= EnsurePlaceholderState(
                meleeMachine,
                "Attack01",
                new Vector3(260f, -80f),
                "Melee"
            );
            changed |= EnsurePlaceholderState(
                meleeMachine,
                "Attack02",
                new Vector3(520f, -80f),
                "Melee"
            );
            changed |= EnsurePlaceholderState(
                meleeMachine,
                "HeavyAttack",
                new Vector3(520f, 80f),
                "Melee"
            );

            return changed;
        }

        private static bool EnsurePlaceholderState(
            AnimatorStateMachine machine,
            string name,
            Vector3 position,
            string tag)
        {
            AnimatorState state = FindState(machine, name);
            if (state != null)
                return false;

            state = machine.AddState(name, position);
            state.writeDefaultValues = false;
            state.tag = tag;
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

        private static AnimatorState FindStateRecursive(
            AnimatorStateMachine machine,
            string stateName)
        {
            AnimatorState local = FindState(machine, stateName);
            if (local != null)
                return local;

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child == null)
                    continue;

                AnimatorState found = FindStateRecursive(child, stateName);
                if (found != null)
                    return found;
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

        private static AnimatorStateMachine FindStateMachine(
            AnimatorStateMachine parent,
            string name)
        {
            ChildAnimatorStateMachine[] children = parent.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine machine = children[i].stateMachine;
                if (machine != null && machine.name == name)
                    return machine;
            }

            return null;
        }

        private static Motion FindMotion(
            AnimatorStateMachine machine,
            params string[] stateNames)
        {
            for (int i = 0; i < stateNames.Length; i++)
            {
                AnimatorState state = FindStateRecursive(machine, stateNames[i]);
                if (state != null && state.motion != null)
                    return state.motion;
            }

            return null;
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            string name,
            Motion motion,
            float x,
            float y)
        {
            AnimatorState state = machine.AddState(name, new Vector3(x, y));
            state.motion = motion;
            state.writeDefaultValues = false;
            return state;
        }

        private static void RemoveAllTransitions(AnimatorState state)
        {
            AnimatorStateTransition[] transitions = state.transitions;
            for (int i = transitions.Length - 1; i >= 0; i--)
                state.RemoveTransition(transitions[i]);
        }

        private static void ClearConditions(AnimatorStateTransition transition)
        {
            AnimatorCondition[] conditions = transition.conditions;
            for (int i = conditions.Length - 1; i >= 0; i--)
                transition.RemoveCondition(conditions[i]);
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

        private static void AddIntTransition(
            AnimatorState source,
            AnimatorState destination,
            string parameter,
            int value,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            ConfigureTransition(transition, duration);
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                value,
                parameter
            );
        }

        private static void AddBoolAndIntTransition(
            AnimatorState source,
            AnimatorState destination,
            string boolParameter,
            bool boolValue,
            string intParameter,
            int intValue,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            ConfigureTransition(transition, duration);
            transition.AddCondition(
                boolValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                boolParameter
            );
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                intValue,
                intParameter
            );
        }

        private static void AddExitTimeTransition(
            AnimatorState source,
            AnimatorState destination,
            float exitTime,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            ConfigureTransition(transition, duration);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
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
