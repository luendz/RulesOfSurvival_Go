using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Ajuste final del flujo aereo:
    /// - salto corto: Jump -> BT_Locomotion
    /// - caida real: Jump -> Fall -> Landing -> BT_Locomotion
    /// - Landing solo puede alcanzarse desde Fall.
    ///
    /// Se materializa una sola vez y luego respeta cualquier ajuste manual.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstAirborneFlowV3
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string MigrationTag = "ROS_AirborneV3";

        static EditorFirstAirborneFlowV3()
        {
            // Se ejecuta despues del refactor principal para no competir con el
            // materializador anterior del Animator.
            EditorApplication.delayCall += ScheduleAfterMainRefactor;
        }

        private static void ScheduleAfterMainRefactor()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Configure Airborne Flow V3")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
                return;

            AnimatorControllerLayer locomotionLayer = FindLayer(
                controller,
                PlayerAnimationCoordinator.LocomotionLayerName
            );

            if (locomotionLayer == null || locomotionLayer.stateMachine == null)
                return;

            AnimatorStateMachine machine = locomotionLayer.stateMachine;
            AnimatorState jump = FindStateRecursive(machine, "Jump");
            AnimatorState fall = FindStateRecursive(machine, "Fall");
            AnimatorState landing = FindStateRecursive(machine, "Landing");
            AnimatorState locomotion = FindStateRecursive(machine, "BT_Locomotion");

            if (jump == null || fall == null || landing == null || locomotion == null)
                return;

            // El refactor anterior deja ROS_AirborneV2 en Landing. No se cambia
            // ese tag para que dicho materializador permanezca inactivo.
            if (jump.tag == MigrationTag)
                return;

            RemoveAllTransitions(jump);
            RemoveAllTransitions(fall);
            RemoveAllTransitions(landing);
            RemoveTransitionsTo(locotion: locomotion, destination: landing);

            // Jump -> Fall solo cuando la distancia real de caida lo justifica.
            AnimatorStateTransition jumpToFall = jump.AddTransition(fall);
            ConfigureTransition(jumpToFall, 0.05f);
            jumpToFall.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "ShouldFall"
            );

            // Salto corto: al tocar suelo vuelve directamente a locomocion.
            AnimatorStateTransition jumpToLocomotion = jump.AddTransition(locomotion);
            ConfigureTransition(jumpToLocomotion, 0.05f);
            jumpToLocomotion.AddCondition(
                AnimatorConditionMode.If,
                0f,
                "Grounded"
            );
            jumpToLocomotion.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                "ShouldFall"
            );

            // Landing queda reservado exclusivamente para una caida real.
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

            jump.writeDefaultValues = false;
            fall.writeDefaultValues = false;
            landing.writeDefaultValues = false;
            jump.tag = MigrationTag;

            EditorUtility.SetDirty(jump);
            EditorUtility.SetDirty(fall);
            EditorUtility.SetDirty(landing);
            EditorUtility.SetDirty(locomotion);
            EditorUtility.SetDirty(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Editor First] Airborne V3 aplicado: Jump vuelve directo a Locomotion en saltos cortos y Landing solo se usa despues de Fall."
            );
        }

        private static void RemoveTransitionsTo(
            AnimatorState locotion,
            AnimatorState destination)
        {
            AnimatorStateTransition[] transitions = locotion.transitions;
            for (int i = transitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition != null && transition.destinationState == destination)
                    locotion.RemoveTransition(transition);
            }
        }

        private static void RemoveAllTransitions(AnimatorState state)
        {
            AnimatorStateTransition[] transitions = state.transitions;
            for (int i = transitions.Length - 1; i >= 0; i--)
            {
                if (transitions[i] != null)
                    state.RemoveTransition(transitions[i]);
            }
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
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.name == stateName)
                    return state;
            }

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
