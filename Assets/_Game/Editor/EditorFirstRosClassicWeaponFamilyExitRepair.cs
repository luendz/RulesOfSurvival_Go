using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Corrige el cambio entre familias de UpperBody_Weapon en controllers ROS Classic
    /// ya materializados.
    ///
    /// Las transiciones padre entre sub-state machines (Rifle, Pistol, Shotgun, etc.)
    /// solo pueden resolverse cuando el estado actual sale de su sub-state machine.
    /// Este reparador agrega una salida inmediata desde cada estado de la familia cuando
    /// WeaponType deja de coincidir con el valor de esa familia. El parent state machine
    /// ya decide despues a que familia entrar usando WeaponType.
    ///
    /// No asigna ni modifica Motion/AnimationClip.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicWeaponFamilyExitRepair
    {
        private const string WeaponType = "WeaponType";

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

        static EditorFirstRosClassicWeaponFamilyExitRepair()
        {
            EditorApplication.delayCall += ScheduleRepair;
        }

        private static void ScheduleRepair()
        {
            EditorApplication.delayCall -= Repair;
            EditorApplication.delayCall += Repair;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/96 - Reparar salida entre familias de arma")]
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

            AnimatorControllerLayer weaponLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.UpperBodyWeaponLayer
            );

            if (weaponLayer == null || weaponLayer.stateMachine == null)
                return;

            bool changed = false;
            AnimatorStateMachine root = weaponLayer.stateMachine;

            for (int i = 0; i < FamilyNames.Length; i++)
            {
                AnimatorStateMachine family = FindChildStateMachine(root, FamilyNames[i]);
                if (family == null)
                    continue;

                changed |= EnsureFamilyExitTransitions(family, FamilyValues[i]);
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] UpperBody_Weapon reparado: cada familia ahora " +
                "sale cuando WeaponType cambia, permitiendo Rifle/Pistol/Shotgun/etc."
            );
        }

        private static bool EnsureFamilyExitTransitions(
            AnimatorStateMachine family,
            int familyValue)
        {
            bool changed = false;

            ChildAnimatorState[] states = family.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null)
                    continue;

                if (!HasWeaponTypeExitTransition(state, familyValue))
                {
                    AnimatorStateTransition exit = state.AddExitTransition();
                    ConfigureTransition(exit);
                    exit.AddCondition(
                        AnimatorConditionMode.NotEqual,
                        familyValue,
                        WeaponType
                    );
                    EditorUtility.SetDirty(state);
                    changed = true;
                }
            }

            ChildAnimatorStateMachine[] children = family.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child != null)
                    changed |= EnsureFamilyExitTransitions(child, familyValue);
            }

            if (changed)
                EditorUtility.SetDirty(family);

            return changed;
        }

        private static bool HasWeaponTypeExitTransition(
            AnimatorState state,
            int familyValue)
        {
            AnimatorStateTransition[] transitions = state.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null || !transition.isExit)
                    continue;

                AnimatorCondition[] conditions = transition.conditions;
                for (int c = 0; c < conditions.Length; c++)
                {
                    AnimatorCondition condition = conditions[c];
                    if (condition.parameter == WeaponType &&
                        condition.mode == AnimatorConditionMode.NotEqual &&
                        Mathf.Approximately(condition.threshold, familyValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void ConfigureTransition(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.02f;
            transition.offset = 0f;
            transition.exitTime = 0f;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
            transition.orderedInterruption = false;
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
    }
}
