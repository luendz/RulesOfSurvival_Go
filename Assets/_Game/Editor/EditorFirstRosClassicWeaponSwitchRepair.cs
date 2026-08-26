using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Reparacion compatible para controllers ROS Classic ya materializados.
    ///
    /// Soluciona dos problemas de migracion:
    /// 1) WeaponEquipmentController legacy aun escribe HasRifle. Mientras ese
    ///    runtime siga existiendo, el parametro se conserva solo para evitar el
    ///    warning "Parameter Hash ... does not exist". La arquitectura nueva no
    ///    usa HasRifle para seleccionar poses.
    /// 2) Garantiza rutas entre familias de UpperBody_Weapon y la ruta visual de
    ///    Weapon Switch en UpperBody_Actions, incluso si el controller local fue
    ///    generado antes de que esas conexiones existieran en el materializador.
    ///
    /// No elimina ni reconstruye states existentes y no pisa motions manuales.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicWeaponSwitchRepair
    {
        private const string WeaponType = "WeaponType";
        private const string IsSwitchingWeapon = "IsSwitchingWeapon";
        private const string WeaponSwitch = "WeaponSwitch";
        private const string LegacyHasRifle = "HasRifle";

        private static readonly string[] FamilyNames =
        {
            "Unarmed",
            "Rifle",
            "Pistol",
            "Shotgun",
            "Sniper",
            "Melee",
            "Throwable"
        };

        private static readonly int[] FamilyValues =
        {
            0, 1, 2, 3, 4, 5, 6
        };

        static EditorFirstRosClassicWeaponSwitchRepair()
        {
            EditorApplication.delayCall += ScheduleRepair;
        }

        private static void ScheduleRepair()
        {
            EditorApplication.delayCall -= Repair;
            EditorApplication.delayCall += Repair;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/98 - Reparar cambio de arma")]
        public static void Repair()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
                return;

            bool changed = false;

            // Compatibilidad temporal con WeaponEquipmentController legacy.
            // PlayerAnimationCoordinator mantiene este bool apagado en ROS
            // Classic, por lo que no participa en la seleccion de animaciones.
            changed |= EnsureParameter(
                controller,
                LegacyHasRifle,
                AnimatorControllerParameterType.Bool
            );

            changed |= EnsureParameter(
                controller,
                WeaponType,
                AnimatorControllerParameterType.Int
            );

            changed |= EnsureParameter(
                controller,
                IsSwitchingWeapon,
                AnimatorControllerParameterType.Bool
            );

            changed |= EnsureParameter(
                controller,
                WeaponSwitch,
                AnimatorControllerParameterType.Trigger
            );

            AnimatorControllerLayer weaponLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.UpperBodyWeaponLayer
            );

            if (weaponLayer != null && weaponLayer.stateMachine != null)
            {
                changed |= RepairWeaponFamilyRoutes(weaponLayer.stateMachine);
            }

            AnimatorControllerLayer actionsLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.UpperBodyActionsLayer
            );

            if (actionsLayer != null && actionsLayer.stateMachine != null)
            {
                changed |= RepairWeaponSwitchAction(actionsLayer.stateMachine);
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Cambio de arma reparado: compatibilidad " +
                "HasRifle, rutas WeaponType y accion IsSwitchingWeapon verificadas."
            );
        }

        private static bool RepairWeaponFamilyRoutes(AnimatorStateMachine root)
        {
            bool changed = false;

            AnimatorState selector = FindState(root, "Weapon Selector");
            AnimatorStateMachine[] families = new AnimatorStateMachine[FamilyNames.Length];

            for (int i = 0; i < FamilyNames.Length; i++)
            {
                families[i] = FindChildStateMachine(root, FamilyNames[i]);
            }

            if (selector != null)
            {
                for (int i = 0; i < families.Length; i++)
                {
                    AnimatorStateMachine destination = families[i];
                    if (destination == null ||
                        HasStateTransition(selector, destination, WeaponType, FamilyValues[i]))
                    {
                        continue;
                    }

                    AnimatorStateTransition transition = selector.AddTransition(destination);
                    ConfigureStateTransition(transition, 0.03f);
                    transition.AddCondition(
                        AnimatorConditionMode.Equals,
                        FamilyValues[i],
                        WeaponType
                    );
                    changed = true;
                }
            }

            for (int from = 0; from < families.Length; from++)
            {
                AnimatorStateMachine source = families[from];
                if (source == null)
                    continue;

                for (int to = 0; to < families.Length; to++)
                {
                    if (from == to)
                        continue;

                    AnimatorStateMachine destination = families[to];
                    if (destination == null ||
                        HasStateMachineTransition(
                            root,
                            source,
                            destination,
                            WeaponType,
                            FamilyValues[to]
                        ))
                    {
                        continue;
                    }

                    AnimatorTransition transition =
                        root.AddStateMachineTransition(source, destination);
                    transition.AddCondition(
                        AnimatorConditionMode.Equals,
                        FamilyValues[to],
                        WeaponType
                    );
                    changed = true;
                }
            }

            return changed;
        }

        private static bool RepairWeaponSwitchAction(AnimatorStateMachine root)
        {
            AnimatorState empty = FindState(root, "Empty");
            AnimatorStateMachine weapon = FindChildStateMachine(root, "Weapon");

            if (empty == null || weapon == null)
                return false;

            bool changed = false;

            if (!HasStateBoolTransition(
                    empty,
                    weapon,
                    IsSwitchingWeapon,
                    true))
            {
                AnimatorStateTransition toWeapon = empty.AddTransition(weapon);
                ConfigureStateTransition(toWeapon, 0.04f);
                toWeapon.AddCondition(
                    AnimatorConditionMode.If,
                    0f,
                    IsSwitchingWeapon
                );
                changed = true;
            }

            if (!HasStateMachineBoolTransition(
                    root,
                    weapon,
                    empty,
                    IsSwitchingWeapon,
                    false))
            {
                AnimatorTransition toEmpty =
                    root.AddStateMachineTransition(weapon, empty);
                toEmpty.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    IsSwitchingWeapon
                );
                changed = true;
            }

            return changed;
        }

        private static bool HasStateTransition(
            AnimatorState source,
            AnimatorStateMachine destination,
            string parameter,
            float threshold)
        {
            AnimatorStateTransition[] transitions = source.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null || transition.destinationStateMachine != destination)
                    continue;

                if (HasCondition(transition.conditions, parameter, threshold))
                    return true;
            }

            return false;
        }

        private static bool HasStateBoolTransition(
            AnimatorState source,
            AnimatorStateMachine destination,
            string parameter,
            bool value)
        {
            AnimatorConditionMode expectedMode = value
                ? AnimatorConditionMode.If
                : AnimatorConditionMode.IfNot;

            AnimatorStateTransition[] transitions = source.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null || transition.destinationStateMachine != destination)
                    continue;

                AnimatorCondition[] conditions = transition.conditions;
                for (int conditionIndex = 0; conditionIndex < conditions.Length; conditionIndex++)
                {
                    AnimatorCondition condition = conditions[conditionIndex];
                    if (condition.parameter == parameter && condition.mode == expectedMode)
                        return true;
                }
            }

            return false;
        }

        private static bool HasStateMachineTransition(
            AnimatorStateMachine root,
            AnimatorStateMachine source,
            AnimatorStateMachine destination,
            string parameter,
            float threshold)
        {
            AnimatorTransition[] transitions = root.GetStateMachineTransitions(source);
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorTransition transition = transitions[i];
                if (transition == null || transition.destinationStateMachine != destination)
                    continue;

                if (HasCondition(transition.conditions, parameter, threshold))
                    return true;
            }

            return false;
        }

        private static bool HasStateMachineBoolTransition(
            AnimatorStateMachine root,
            AnimatorStateMachine source,
            AnimatorState destination,
            string parameter,
            bool value)
        {
            AnimatorTransition[] transitions = root.GetStateMachineTransitions(source);
            AnimatorConditionMode expectedMode = value
                ? AnimatorConditionMode.If
                : AnimatorConditionMode.IfNot;

            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorTransition transition = transitions[i];
                if (transition == null || transition.destinationState != destination)
                    continue;

                AnimatorCondition[] conditions = transition.conditions;
                for (int conditionIndex = 0; conditionIndex < conditions.Length; conditionIndex++)
                {
                    AnimatorCondition condition = conditions[conditionIndex];
                    if (condition.parameter == parameter && condition.mode == expectedMode)
                        return true;
                }
            }

            return false;
        }

        private static bool HasCondition(
            AnimatorCondition[] conditions,
            string parameter,
            float threshold)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                AnimatorCondition condition = conditions[i];
                if (condition.parameter == parameter &&
                    Mathf.Approximately(condition.threshold, threshold))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConfigureStateTransition(
            AnimatorStateTransition transition,
            float duration)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
            transition.exitTime = 0f;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
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

        private static AnimatorStateMachine FindChildStateMachine(
            AnimatorStateMachine parent,
            string name)
        {
            ChildAnimatorStateMachine[] children = parent.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child != null && child.name == name)
                    return child;
            }

            return null;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string name)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.name == name)
                    return state;
            }

            return null;
        }

        private static bool EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                    return false;
            }

            controller.AddParameter(name, type);
            return true;
        }
    }
}
