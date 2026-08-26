using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 05 del Animator ROS Classic.
    ///
    /// Materializa Grounded/Crouch con:
    /// Crouch Enter -> Crouch Idle / Crouch Move_2D -> Crouch Exit.
    ///
    /// Usa Stance = 1 para Crouch.
    /// El repositorio contiene Crouch Idle, Crouch Walk Forward y un clip
    /// Crouch To Standing Idle. Igual que el Animator prototype existente,
    /// ese ultimo clip se reutiliza en reversa para Crouch Enter.
    ///
    /// Crouch Move_2D queda preparado en 8 direcciones con MoveX/MoveY, pero
    /// solo Forward recibe motion porque es el unico clip neutro disponible.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicCrouch
    {
        private const string CrouchIdleClipPath =
            "Assets/_Game/Animations/Character Animator/01. Locomotion/Crouch/Ch28_nonPBR@Crouch Idle.fbx";

        private const string CrouchForwardClipPath =
            "Assets/_Game/Animations/Character Animator/01. Locomotion/Crouch/Ch28_nonPBR@Crouch Walk Forward.fbx";

        private const string CrouchTransitionClipPath =
            "Assets/_Game/Animations/Character Animator/01. Locomotion/Crouch/Ch28_nonPBR@Crouch To Standing Idle.fbx";

        private const int CrouchStance = 1;
        private const float MoveThreshold = 0.05f;

        static EditorFirstRosClassicCrouch()
        {
            EditorApplication.delayCall += ScheduleAfterPreviousSteps;
        }

        private static void ScheduleAfterPreviousSteps()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/05 - Construir Crouch")]
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

            if (grounded == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No existe Grounded. " +
                    "Ejecuta primero '01 - Crear arquitectura base'."
                );
                return;
            }

            AnimatorStateMachine standing =
                FindChildStateMachine(grounded, "Standing");

            if (standing == null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] No existe Grounded/Standing."
                );
                return;
            }

            if (FindChildStateMachine(grounded, "Crouch") != null)
                return;

            AnimationClip crouchIdleClip =
                LoadFirstAnimationClip(CrouchIdleClipPath);
            AnimationClip crouchForwardClip =
                LoadFirstAnimationClip(CrouchForwardClipPath);
            AnimationClip crouchTransitionClip =
                LoadFirstAnimationClip(CrouchTransitionClipPath);

            AnimatorStateMachine crouch = grounded.AddStateMachine(
                "Crouch",
                new Vector3(650f, 330f, 0f)
            );

            AnimatorState crouchEnter = crouch.AddState(
                "Crouch Enter",
                new Vector3(260f, 20f, 0f)
            );
            crouchEnter.motion = crouchTransitionClip;
            crouchEnter.speed = -1f;
            crouchEnter.cycleOffset = 1f;
            crouchEnter.writeDefaultValues = false;

            AnimatorState crouchIdle = crouch.AddState(
                "Crouch Idle",
                new Vector3(520f, -40f, 0f)
            );
            crouchIdle.motion = crouchIdleClip;
            crouchIdle.writeDefaultValues = false;

            BlendTree crouchMoveTree = new BlendTree
            {
                name = "BT_Crouch_Move_8D",
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(crouchMoveTree, controller);

            crouchMoveTree.AddChild(crouchForwardClip, new Vector2(0f, 1f));
            crouchMoveTree.AddChild(null, new Vector2(-1f, 1f));
            crouchMoveTree.AddChild(null, new Vector2(-1f, 0f));
            crouchMoveTree.AddChild(null, new Vector2(-1f, -1f));
            crouchMoveTree.AddChild(null, new Vector2(0f, -1f));
            crouchMoveTree.AddChild(null, new Vector2(1f, -1f));
            crouchMoveTree.AddChild(null, new Vector2(1f, 0f));
            crouchMoveTree.AddChild(null, new Vector2(1f, 1f));

            AnimatorState crouchMove = crouch.AddState(
                "Crouch Move_2D",
                new Vector3(520f, 100f, 0f)
            );
            crouchMove.motion = crouchMoveTree;
            crouchMove.writeDefaultValues = false;

            AnimatorState crouchExit = crouch.AddState(
                "Crouch Exit",
                new Vector3(790f, 20f, 0f)
            );
            crouchExit.motion = crouchTransitionClip;
            crouchExit.speed = 1f;
            crouchExit.writeDefaultValues = false;

            crouch.defaultState = crouchEnter;

            // Standing sale hacia el parent cuando Stance cambia a Crouch.
            AddStandingExitTransition(FindState(standing, "Idle"));
            AddStandingExitTransition(FindState(standing, "Walk_2D"));
            AddStandingExitTransition(FindState(standing, "Run_2D"));
            AddStandingExitTransition(FindState(standing, "Sprint"));

            AnimatorTransition standingToCrouch =
                grounded.AddStateMachineTransition(standing, crouch);
            standingToCrouch.AddCondition(
                AnimatorConditionMode.Equals,
                CrouchStance,
                "Stance"
            );

            // Entrada. Reutilizamos el mismo clip de salida en reversa.
            AnimatorStateTransition enterToIdle = crouchEnter.AddTransition(crouchIdle);
            ConfigureExitTimeTransition(enterToIdle, 0.02f, 0.05f);

            AnimatorStateTransition enterCancel = crouchEnter.AddTransition(crouchExit);
            ConfigureTransition(enterCancel, 0.04f);
            enterCancel.AddCondition(
                AnimatorConditionMode.NotEqual,
                CrouchStance,
                "Stance"
            );

            // Idle <-> locomocion agachada.
            AnimatorStateTransition idleToMove = crouchIdle.AddTransition(crouchMove);
            ConfigureTransition(idleToMove, 0.10f);
            idleToMove.AddCondition(
                AnimatorConditionMode.Greater,
                MoveThreshold,
                "Speed"
            );

            AnimatorStateTransition moveToIdle = crouchMove.AddTransition(crouchIdle);
            ConfigureTransition(moveToIdle, 0.10f);
            moveToIdle.AddCondition(
                AnimatorConditionMode.Less,
                MoveThreshold,
                "Speed"
            );

            // Salida desde cualquiera de los estados estables de Crouch.
            AddExitToCrouchExit(crouchIdle, crouchExit);
            AddExitToCrouchExit(crouchMove, crouchExit);

            AnimatorStateTransition exitToParent = crouchExit.AddExitTransition();
            ConfigureExitTimeTransition(exitToParent, 0.90f, 0.05f);

            // Cuando Crouch termina, vuelve siempre al state machine Standing.
            grounded.AddStateMachineTransition(crouch, standing);

            EditorUtility.SetDirty(crouchMoveTree);
            EditorUtility.SetDirty(crouchEnter);
            EditorUtility.SetDirty(crouchIdle);
            EditorUtility.SetDirty(crouchMove);
            EditorUtility.SetDirty(crouchExit);
            EditorUtility.SetDirty(crouch);
            EditorUtility.SetDirty(grounded);
            EditorUtility.SetDirty(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Crouch creado: " +
                "Enter -> Idle/Move_2D -> Exit. Stance=1 activa Crouch."
            );

            if (crouchForwardClip != null)
            {
                Debug.LogWarning(
                    "[ROS Classic Animator] Crouch Move_2D solo tiene clip Forward. " +
                    "Faltan 7 direcciones neutras para completar el Blend Tree."
                );
            }

            if (crouchTransitionClip != null)
            {
                Debug.Log(
                    "[ROS Classic Animator] Crouch Enter reutiliza 'Crouch To Standing Idle' " +
                    "a velocidad -1, igual que el flujo existente del Animator prototype."
                );
            }
        }

        private static void AddStandingExitTransition(AnimatorState state)
        {
            if (state == null)
                return;

            AnimatorStateTransition transition = state.AddExitTransition();
            ConfigureTransition(transition, 0.05f);
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                CrouchStance,
                "Stance"
            );
        }

        private static void AddExitToCrouchExit(
            AnimatorState from,
            AnimatorState crouchExit)
        {
            if (from == null || crouchExit == null)
                return;

            AnimatorStateTransition transition = from.AddTransition(crouchExit);
            ConfigureTransition(transition, 0.05f);
            transition.AddCondition(
                AnimatorConditionMode.NotEqual,
                CrouchStance,
                "Stance"
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

        private static void ConfigureExitTimeTransition(
            AnimatorStateTransition transition,
            float exitTime,
            float duration)
        {
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
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
