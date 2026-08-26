using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 04 del Animator ROS Classic.
    ///
    /// Materializa Grounded/Standing/Sprint como un Blend Tree direccional 2D
    /// orientado hacia delante. La arquitectura reserva Forward Left, Forward y
    /// Forward Right, tal como se definio para el movimiento clasico.
    ///
    /// Actualmente el repositorio solo contiene un clip neutro de Sprint hacia
    /// delante. Las diagonales quedan visibles como Motion=None para completarlas
    /// posteriormente con clips reales, sin fabricar direcciones falsas.
    ///
    /// IsSprinting e IsAutoRunning pueden activar el mismo estado Sprint.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicSprint
    {
        private const string SprintForwardClipPath =
            "Assets/_Game/Animations/Character Animator/01. Locomotion/Sprint/Ch28_nonPBR@Sprint.fbx";

        private const float MovingSpeed = 0.05f;
        private const float RunSpeed = 0.55f;

        static EditorFirstRosClassicSprint()
        {
            EditorApplication.delayCall += ScheduleAfterRun;
        }

        private static void ScheduleAfterRun()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/04 - Construir Sprint")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
            {
                EditorFirstRosClassicAnimatorBuilder.CreateIfMissing();
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );
            }

            if (controller == null)
                return;

            // Run es el paso anterior. La llamada es segura porque su
            // materializador no reconstruye un bloque que ya existe.
            EditorFirstRosClassicRun2D.Materialize();

            AnimatorControllerLayer baseLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.BaseLocomotionLayer
            );

            if (baseLayer == null || baseLayer.stateMachine == null)
                return;

            AnimatorStateMachine grounded =
                FindChildStateMachine(baseLayer.stateMachine, "Grounded");
            AnimatorStateMachine standing =
                grounded != null ? FindChildStateMachine(grounded, "Standing") : null;

            if (standing == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No existe Grounded/Standing. " +
                    "Ejecuta primero '01 - Crear arquitectura base'."
                );
                return;
            }

            if (FindState(standing, "Sprint") != null)
                return;

            AnimationClip sprintForward = LoadFirstAnimationClip(SprintForwardClipPath);

            BlendTree sprintTree = new BlendTree
            {
                name = "BT_Sprint_Forward",
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(sprintTree, controller);

            // Sprint clasico: solo se reservan las tres direcciones frontales.
            sprintTree.AddChild(null, new Vector2(-1f, 1f));           // Forward Left
            sprintTree.AddChild(sprintForward, new Vector2(0f, 1f)); // Forward
            sprintTree.AddChild(null, new Vector2(1f, 1f));           // Forward Right

            AnimatorState sprint = standing.AddState(
                "Sprint",
                new Vector3(1140f, 40f, 0f)
            );
            sprint.motion = sprintTree;
            sprint.writeDefaultValues = false;

            AnimatorState idle = FindState(standing, "Idle");
            AnimatorState walk = FindState(standing, "Walk_2D");
            AnimatorState run = FindState(standing, "Run_2D");

            AddSprintEntry(run, sprint, "IsSprinting");
            AddSprintEntry(run, sprint, "IsAutoRunning");
            AddSprintEntry(walk, sprint, "IsSprinting");
            AddSprintEntry(walk, sprint, "IsAutoRunning");

            if (idle != null)
            {
                AnimatorStateTransition sprintToIdle = sprint.AddTransition(idle);
                ConfigureTransition(sprintToIdle, 0.08f);
                sprintToIdle.AddCondition(
                    AnimatorConditionMode.Less,
                    MovingSpeed,
                    "Speed"
                );
            }

            if (run != null)
            {
                AnimatorStateTransition sprintToRun = sprint.AddTransition(run);
                ConfigureTransition(sprintToRun, 0.10f);
                sprintToRun.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsSprinting"
                );
                sprintToRun.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsAutoRunning"
                );
                sprintToRun.AddCondition(
                    AnimatorConditionMode.Greater,
                    RunSpeed,
                    "Speed"
                );
            }

            if (walk != null)
            {
                AnimatorStateTransition sprintToWalk = sprint.AddTransition(walk);
                ConfigureTransition(sprintToWalk, 0.10f);
                sprintToWalk.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsSprinting"
                );
                sprintToWalk.AddCondition(
                    AnimatorConditionMode.IfNot,
                    0f,
                    "IsAutoRunning"
                );
                sprintToWalk.AddCondition(
                    AnimatorConditionMode.Greater,
                    MovingSpeed,
                    "Speed"
                );
                sprintToWalk.AddCondition(
                    AnimatorConditionMode.Less,
                    RunSpeed,
                    "Speed"
                );
            }

            EditorUtility.SetDirty(sprintTree);
            EditorUtility.SetDirty(sprint);
            EditorUtility.SetDirty(standing);
            EditorUtility.SetDirty(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (sprintForward == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] Sprint fue creado, pero no se encontro el clip Forward: " +
                    SprintForwardClipPath
                );
            }
            else
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] Sprint creado. Forward usa Ch28_nonPBR@Sprint; " +
                    "Forward Left y Forward Right quedan como Motion=None hasta disponer de clips reales."
                );
            }
        }

        private static void AddSprintEntry(
            AnimatorState source,
            AnimatorState sprint,
            string boolParameter)
        {
            if (source == null || sprint == null)
                return;

            AnimatorStateTransition transition = source.AddTransition(sprint);
            ConfigureTransition(transition, 0.08f);
            transition.AddCondition(
                AnimatorConditionMode.If,
                0f,
                boolParameter
            );
            transition.AddCondition(
                AnimatorConditionMode.Greater,
                MovingSpeed,
                "Speed"
            );
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
            if (parent == null)
                return null;

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
            string stateName)
        {
            if (machine == null)
                return null;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && state.name == stateName)
                    return state;
            }

            return null;
        }

        private static void ConfigureTransition(
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

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null ||
                    clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return clip;
            }

            return null;
        }
    }
}
