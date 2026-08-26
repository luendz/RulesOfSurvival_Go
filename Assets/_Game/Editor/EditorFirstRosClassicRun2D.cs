using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 03 del Animator ROS Classic.
    ///
    /// Materializa Grounded/Standing/Run_2D con un Blend Tree direccional 2D
    /// controlado por MoveX y MoveY. Mantiene la locomocion del cuerpo separada
    /// de las poses de arma.
    ///
    /// Actualmente el repositorio solo contiene un clip neutro de carrera hacia
    /// delante. Las otras siete posiciones quedan visibles como Motion=None para
    /// que puedan completarse con clips reales sin fabricar direcciones falsas.
    ///
    /// Los umbrales de Speed usados para Walk <-> Run son valores provisionales
    /// de configuracion del Animator, no valores medidos del Rules of Survival
    /// original. Se calibraran cuando conectemos y comparemos el runtime final.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicRun2D
    {
        private const string RunForwardClipPath =
            "Assets/_Game/Animations/Character Animator/01. Locomotion/Run/Ch28_nonPBR@Running.fbx";

        private const float RunEnterSpeed = 0.55f;
        private const float RunExitSpeed = 0.45f;
        private const float IdleSpeed = 0.05f;

        static EditorFirstRosClassicRun2D()
        {
            // Se agenda un paso despues del builder base para permitir que este
            // cree primero AC_Player_ROS_Classic en un proyecto limpio.
            EditorApplication.delayCall += ScheduleAfterBaseBuilder;
        }

        private static void ScheduleAfterBaseBuilder()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/03 - Construir Run 8 direcciones")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
                return;

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

            if (FindState(standing, "Run_2D") != null)
                return;

            AnimationClip runForward = LoadFirstAnimationClip(RunForwardClipPath);

            BlendTree runTree = new BlendTree
            {
                name = "BT_Run_8D",
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(runTree, controller);

            runTree.AddChild(runForward, new Vector2(0f, 1f));       // Forward
            runTree.AddChild(null, new Vector2(-1f, 1f));           // Forward Left
            runTree.AddChild(null, new Vector2(-1f, 0f));           // Left
            runTree.AddChild(null, new Vector2(-1f, -1f));          // Backward Left
            runTree.AddChild(null, new Vector2(0f, -1f));           // Backward
            runTree.AddChild(null, new Vector2(1f, -1f));           // Backward Right
            runTree.AddChild(null, new Vector2(1f, 0f));            // Right
            runTree.AddChild(null, new Vector2(1f, 1f));            // Forward Right

            AnimatorState run = standing.AddState(
                "Run_2D",
                new Vector3(860f, 40f, 0f)
            );
            run.motion = runTree;
            run.writeDefaultValues = false;

            AnimatorState walk = FindState(standing, "Walk_2D");
            AnimatorState idle = FindState(standing, "Idle");

            if (walk != null)
            {
                AnimatorStateTransition walkToRun = walk.AddTransition(run);
                ConfigureTransition(walkToRun, 0.10f);
                walkToRun.AddCondition(
                    AnimatorConditionMode.Greater,
                    RunEnterSpeed,
                    "Speed"
                );

                AnimatorStateTransition runToWalk = run.AddTransition(walk);
                ConfigureTransition(runToWalk, 0.10f);
                runToWalk.AddCondition(
                    AnimatorConditionMode.Less,
                    RunExitSpeed,
                    "Speed"
                );
            }

            if (idle != null)
            {
                AnimatorStateTransition runToIdle = run.AddTransition(idle);
                ConfigureTransition(runToIdle, 0.08f);
                runToIdle.AddCondition(
                    AnimatorConditionMode.Less,
                    IdleSpeed,
                    "Speed"
                );
            }

            EditorUtility.SetDirty(runTree);
            EditorUtility.SetDirty(run);
            EditorUtility.SetDirty(standing);
            EditorUtility.SetDirty(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (runForward == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] Run_2D fue creado, pero no se encontro el clip Forward: " +
                    RunForwardClipPath
                );
            }
            else
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] Run_2D creado. Faltan clips neutros para 7 direcciones: " +
                    "Forward Left, Left, Backward Left, Backward, Backward Right, Right y Forward Right."
                );
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
