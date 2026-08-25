using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa en el layer Locomotion un bloque de paracaidas visible y editable:
    /// FreeFall -> Parachuting -> ParachuteLanding -> BT_Locomotion.
    ///
    /// Usa un unico parametro int ParachuteState:
    /// 0 = None, 1 = FreeFall, 2 = Parachuting, 3 = Landing.
    ///
    /// Una vez creado el bloque completo, no vuelve a reconstruirlo para respetar
    /// cualquier ajuste manual posterior realizado desde el Animator.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstParachuteLocomotion
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string FreeFallClipPath =
            "Assets/_Game/Animations/Character Animator/03. Parachute/Ch28_nonPBR@Falling Parachutec.fbx";

        private const string ParameterName = "ParachuteState";
        private const string StateMachineName = "Parachute";

        static EditorFirstParachuteLocomotion()
        {
            EditorApplication.delayCall += ScheduleAfterAnimatorRefactors;
        }

        private static void ScheduleAfterAnimatorRefactors()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Configure Parachute Locomotion")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return;

            bool changed = EnsureIntParameter(controller, ParameterName);

            AnimatorControllerLayer locomotionLayer = FindLayer(
                controller,
                PlayerAnimationCoordinator.LocomotionLayerName
            );
            if (locomotionLayer == null || locomotionLayer.stateMachine == null)
                return;

            AnimatorStateMachine root = locomotionLayer.stateMachine;
            AnimatorState locomotion = FindStateRecursive(root, "BT_Locomotion");
            if (locomotion == null)
                return;

            AnimatorStateMachine parachute = FindChildStateMachine(root, StateMachineName);
            if (parachute == null)
            {
                parachute = root.AddStateMachine(
                    StateMachineName,
                    new Vector3(930f, 430f, 0f)
                );
                changed = true;
            }

            AnimatorState freeFall = FindState(parachute, "FreeFall");
            AnimatorState parachuting = FindState(parachute, "Parachuting");
            AnimatorState parachuteLanding = FindState(parachute, "ParachuteLanding");

            // Si los tres estados ya existen, se considera materializado y se
            // respetan desde aqui todos los cambios manuales del usuario.
            bool alreadyMaterialized =
                freeFall != null && parachuting != null && parachuteLanding != null;

            if (!alreadyMaterialized)
            {
                if (freeFall == null)
                {
                    freeFall = parachute.AddState(
                        "FreeFall",
                        new Vector3(260f, 40f, 0f)
                    );
                    freeFall.motion = LoadFirstAnimationClip(FreeFallClipPath);
                    freeFall.writeDefaultValues = false;
                    changed = true;
                }

                if (parachuting == null)
                {
                    parachuting = parachute.AddState(
                        "Parachuting",
                        new Vector3(520f, 40f, 0f)
                    );
                    parachuting.writeDefaultValues = false;
                    changed = true;
                }

                if (parachuteLanding == null)
                {
                    parachuteLanding = parachute.AddState(
                        "ParachuteLanding",
                        new Vector3(780f, 40f, 0f)
                    );
                    parachuteLanding.writeDefaultValues = false;
                    changed = true;
                }

                parachute.defaultState = freeFall;

                // Entramos al sub-state machine desde cualquier estado de
                // Locomotion cuando empieza la caida desde el avion.
                AnimatorStateTransition enterParachute =
                    root.AddAnyStateTransition(parachute);
                ConfigureStateTransition(enterParachute, 0.05f);
                enterParachute.AddCondition(
                    AnimatorConditionMode.Equals,
                    1f,
                    ParameterName
                );

                AnimatorStateTransition freeFallToParachuting =
                    freeFall.AddTransition(parachuting);
                ConfigureStateTransition(freeFallToParachuting, 0.08f);
                freeFallToParachuting.AddCondition(
                    AnimatorConditionMode.Equals,
                    2f,
                    ParameterName
                );

                AnimatorStateTransition parachutingToLanding =
                    parachuting.AddTransition(parachuteLanding);
                ConfigureStateTransition(parachutingToLanding, 0.06f);
                parachutingToLanding.AddCondition(
                    AnimatorConditionMode.Equals,
                    3f,
                    ParameterName
                );

                // El aterrizaje reproduce su clip y sale del sub-state machine.
                // Si aun no hay clip asignado, se puede configurar manualmente.
                AnimatorStateTransition landingToExit =
                    parachuteLanding.AddExitTransition();
                ConfigureStateTransition(landingToExit, 0.08f);
                landingToExit.hasExitTime = true;
                landingToExit.exitTime = 0.90f;

                // Al salir de Parachute siempre regresamos a la locomocion base.
                root.AddStateMachineTransition(parachute, locomotion);

                changed = true;
            }
            else if (freeFall.motion == null)
            {
                // Solo completa el clip conocido si el usuario aun no asigno uno.
                AnimationClip clip = LoadFirstAnimationClip(FreeFallClipPath);
                if (clip != null)
                {
                    freeFall.motion = clip;
                    changed = true;
                }
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(parachute);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Editor First] Parachute creado dentro de Locomotion: " +
                "FreeFall -> Parachuting -> ParachuteLanding -> BT_Locomotion."
            );
        }

        private static bool EnsureIntParameter(
            AnimatorController controller,
            string parameterName)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name != parameterName)
                    continue;

                if (parameters[i].type == AnimatorControllerParameterType.Int)
                    return false;

                controller.RemoveParameter(i);
                controller.AddParameter(
                    parameterName,
                    AnimatorControllerParameterType.Int
                );
                return true;
            }

            controller.AddParameter(
                parameterName,
                AnimatorControllerParameterType.Int
            );
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
            string stateName)
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

        private static AnimatorState FindStateRecursive(
            AnimatorStateMachine machine,
            string stateName)
        {
            AnimatorState state = FindState(machine, stateName);
            if (state != null)
                return state;

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorStateMachine child = children[i].stateMachine;
                if (child == null)
                    continue;

                state = FindStateRecursive(child, stateName);
                if (state != null)
                    return state;
            }

            return null;
        }

        private static void ConfigureStateTransition(
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

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__"))
                    continue;

                return clip;
            }

            return null;
        }
    }
}
