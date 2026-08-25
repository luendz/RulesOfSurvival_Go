using System;
using ROS.Game.Animation;
using ROS.Game.Character;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Hace que Fall dependa de ShouldFall, que a su vez se calcula por distancia
    /// real de caida. Los saltos bajos aterrizan sin pasar por Fall.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstFallAnimationThresholdMaterializer
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string FunctionalScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private const string ParameterName = "ShouldFall";
        private const string LocomotionLayerName = "Locomotion";
        private const string FallStateName = "Fall";

        static EditorFirstFallAnimationThresholdMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Configure Fall Distance Threshold")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            bool controllerChanged = ConfigureAnimator();
            bool sceneChanged = EnsureRuntimeGateInFunctionalScene();

            if (controllerChanged || sceneChanged)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "[Editor First] Fall configurado por distancia minima de caida. " +
                    "Los saltos bajos pueden ir de Jump a Landing sin reproducir Fall."
                );
            }
        }

        private static bool ConfigureAnimator()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
                return false;

            bool changed = EnsureBoolParameter(controller, ParameterName);

            AnimatorControllerLayer locomotion = FindLayer(controller, LocomotionLayerName);
            if (locomotion == null || locomotion.stateMachine == null)
                return changed;

            AnimatorState fallState = FindStateRecursive(locomotion.stateMachine, FallStateName);
            if (fallState == null)
                return changed;

            changed |= ReplaceTransitionsToFallRecursive(locomotion.stateMachine, fallState);

            if (changed)
                EditorUtility.SetDirty(controller);

            return changed;
        }

        private static bool EnsureBoolParameter(AnimatorController controller, string name)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                    return false;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Bool);
            return true;
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
                if (states[i].state != null && states[i].state.name == stateName)
                    return states[i].state;
            }

            ChildAnimatorStateMachine[] childMachines = machine.stateMachines;
            for (int i = 0; i < childMachines.Length; i++)
            {
                AnimatorState state = FindStateRecursive(childMachines[i].stateMachine, stateName);
                if (state != null)
                    return state;
            }

            return null;
        }

        private static bool ReplaceTransitionsToFallRecursive(
            AnimatorStateMachine machine,
            AnimatorState fallState)
        {
            bool changed = false;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null || state == fallState)
                    continue;

                AnimatorStateTransition[] transitions = state.transitions;
                for (int j = 0; j < transitions.Length; j++)
                {
                    AnimatorStateTransition transition = transitions[j];
                    if (transition == null || transition.destinationState != fallState)
                        continue;

                    if (IsOnlyShouldFallCondition(transition))
                        continue;

                    ClearConditions(transition);
                    transition.AddCondition(AnimatorConditionMode.If, 0f, ParameterName);
                    transition.hasExitTime = false;
                    transition.duration = Mathf.Min(transition.duration, 0.05f);
                    EditorUtility.SetDirty(transition);
                    changed = true;
                }
            }

            AnimatorStateTransition[] anyTransitions = machine.anyStateTransitions;
            for (int i = 0; i < anyTransitions.Length; i++)
            {
                AnimatorStateTransition transition = anyTransitions[i];
                if (transition == null || transition.destinationState != fallState)
                    continue;

                if (IsOnlyShouldFallCondition(transition))
                    continue;

                ClearConditions(transition);
                transition.AddCondition(AnimatorConditionMode.If, 0f, ParameterName);
                transition.hasExitTime = false;
                transition.duration = Mathf.Min(transition.duration, 0.05f);
                EditorUtility.SetDirty(transition);
                changed = true;
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                changed |= ReplaceTransitionsToFallRecursive(
                    children[i].stateMachine,
                    fallState
                );
            }

            return changed;
        }

        private static bool IsOnlyShouldFallCondition(AnimatorStateTransition transition)
        {
            AnimatorCondition[] conditions = transition.conditions;
            return conditions.Length == 1 &&
                   conditions[0].parameter == ParameterName &&
                   conditions[0].mode == AnimatorConditionMode.If;
        }

        private static void ClearConditions(AnimatorStateTransition transition)
        {
            AnimatorCondition[] conditions = transition.conditions;
            for (int i = conditions.Length - 1; i >= 0; i--)
                transition.RemoveCondition(conditions[i]);
        }

        private static bool EnsureRuntimeGateInFunctionalScene()
        {
            if (!System.IO.File.Exists(FunctionalScenePath))
                return false;

            Scene activeScene = SceneManager.GetActiveScene();
            string activePath = activeScene.path;
            bool activeDirty = activeScene.IsValid() && activeScene.isDirty;

            Scene scene = EditorSceneManager.OpenScene(
                FunctionalScenePath,
                OpenSceneMode.Single
            );

            bool changed = false;
            PlayerMotor[] motors = UnityEngine.Object.FindObjectsByType<PlayerMotor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < motors.Length; i++)
            {
                PlayerMotor motor = motors[i];
                if (motor == null || motor.gameObject.scene != scene)
                    continue;

                PlayerFallAnimationGate gate = motor.GetComponent<PlayerFallAnimationGate>();
                if (gate != null)
                    continue;

                Undo.AddComponent<PlayerFallAnimationGate>(motor.gameObject);
                changed = true;
            }

            if (changed)
                EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrWhiteSpace(activePath) && activePath != FunctionalScenePath)
            {
                Scene restored = EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
                if (activeDirty)
                    EditorSceneManager.MarkSceneDirty(restored);
            }

            return changed;
        }
    }
}
