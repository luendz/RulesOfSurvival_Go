using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Materializa el apuntado agachado como una capa Upper Body real dentro
    /// del Animator Controller. La capa base conserva Crouch Idle/Walk en
    /// pelvis y piernas, mientras torso/brazos reutilizan la locomocion de Aim.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstCrouchAimUpperBodyMaterializer
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string UpperBodyMaskPath =
            "Assets/_Game/Animations/Masks/AM_WeaponUpperBody.mask";

        private const string LocomotionLayerName = "Locomotion";
        private const string SourceAimStateName = "BT_AimLocomotion";
        private const string LayerName = "CrouchAimUpperBody";
        private const string EmptyStateName = "Empty";
        private const string AimStateName = "CrouchAim";
        private const string ParameterName = "UpperBodyAim";

        static EditorFirstCrouchAimUpperBodyMaterializer()
        {
            EditorApplication.delayCall += EnsureCrouchAimUpperBody;
        }

        [MenuItem("Rules Of Survival/Editor First/Configure Crouch Aim Upper Body")]
        public static void EnsureCrouchAimUpperBody()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            AvatarMask upperBodyMask =
                AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);

            if (controller == null)
            {
                Debug.LogError(
                    "[Editor First] No se encontro el Animator Controller: " +
                    ControllerPath
                );
                return;
            }

            if (upperBodyMask == null)
            {
                Debug.LogError(
                    "[Editor First] No se encontro el AvatarMask Upper Body: " +
                    UpperBodyMaskPath
                );
                return;
            }

            AnimatorControllerLayer locomotionLayer =
                FindLayer(controller, LocomotionLayerName);

            if (locomotionLayer == null || locomotionLayer.stateMachine == null)
            {
                Debug.LogError(
                    "[Editor First] No se encontro la capa Locomotion en " +
                    ControllerPath
                );
                return;
            }

            AnimatorState sourceAimState =
                FindStateRecursive(
                    locomotionLayer.stateMachine,
                    SourceAimStateName
                );

            if (sourceAimState == null || sourceAimState.motion == null)
            {
                Debug.LogError(
                    "[Editor First] No se encontro el estado/motion '" +
                    SourceAimStateName + "' en Locomotion."
                );
                return;
            }

            bool changed = false;

            changed |= EnsureBoolParameter(controller, ParameterName);

            AnimatorControllerLayer crouchAimLayer =
                FindLayer(controller, LayerName);

            if (crouchAimLayer == null)
            {
                AnimatorStateMachine stateMachine =
                    new AnimatorStateMachine
                    {
                        name = LayerName
                    };

                AssetDatabase.AddObjectToAsset(stateMachine, controller);

                crouchAimLayer = new AnimatorControllerLayer
                {
                    name = LayerName,
                    defaultWeight = 1f,
                    avatarMask = upperBodyMask,
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    stateMachine = stateMachine
                };

                controller.AddLayer(crouchAimLayer);
                changed = true;
            }

            AnimatorStateMachine crouchAimMachine =
                crouchAimLayer.stateMachine;

            if (crouchAimMachine == null)
            {
                Debug.LogError(
                    "[Editor First] La capa CrouchAimUpperBody no tiene StateMachine."
                );
                return;
            }

            // Si la capa fue creada por una version anterior incompleta,
            // terminamos de configurarla. Una mascara manual distinta se respeta.
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name != LayerName)
                    continue;

                AnimatorControllerLayer layer = layers[i];

                if (layer.avatarMask == null)
                {
                    layer.avatarMask = upperBodyMask;
                    changed = true;
                }

                // Solo corregimos el peso cero heredado de una capa recien
                // creada/incompleta. Un peso manual distinto de cero se respeta.
                if (Mathf.Approximately(layer.defaultWeight, 0f))
                {
                    layer.defaultWeight = 1f;
                    changed = true;
                }

                layers[i] = layer;
                break;
            }

            controller.layers = layers;

            AnimatorState emptyState =
                FindDirectState(crouchAimMachine, EmptyStateName);

            if (emptyState == null)
            {
                emptyState = crouchAimMachine.AddState(
                    EmptyStateName,
                    new Vector3(250f, 80f, 0f)
                );
                emptyState.motion = null;
                emptyState.writeDefaultValues = false;
                changed = true;
            }

            AnimatorState crouchAimState =
                FindDirectState(crouchAimMachine, AimStateName);

            if (crouchAimState == null)
            {
                crouchAimState = crouchAimMachine.AddState(
                    AimStateName,
                    new Vector3(520f, 80f, 0f)
                );
                crouchAimState.motion = sourceAimState.motion;
                crouchAimState.writeDefaultValues = true;
                changed = true;
            }
            else if (crouchAimState.motion == null)
            {
                crouchAimState.motion = sourceAimState.motion;
                changed = true;
            }

            if (crouchAimMachine.defaultState == null)
            {
                crouchAimMachine.defaultState = emptyState;
                changed = true;
            }

            changed |= EnsureTransition(
                emptyState,
                crouchAimState,
                ParameterName,
                AnimatorConditionMode.If
            );

            changed |= EnsureTransition(
                crouchAimState,
                emptyState,
                ParameterName,
                AnimatorConditionMode.IfNot
            );

            if (!changed)
                return;

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(crouchAimMachine);
            EditorUtility.SetDirty(emptyState);
            EditorUtility.SetDirty(crouchAimState);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Editor First] Crouch Aim configurado: piernas en Locomotion " +
                "y apuntado en CrouchAimUpperBody."
            );
        }

        private static bool EnsureBoolParameter(
            AnimatorController controller,
            string parameterName
        )
        {
            AnimatorControllerParameter[] parameters = controller.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name != parameterName)
                    continue;

                if (parameters[i].type != AnimatorControllerParameterType.Bool)
                {
                    Debug.LogError(
                        "[Editor First] El parametro '" + parameterName +
                        "' existe pero no es Bool."
                    );
                }

                return false;
            }

            controller.AddParameter(
                parameterName,
                AnimatorControllerParameterType.Bool
            );
            return true;
        }

        private static AnimatorControllerLayer FindLayer(
            AnimatorController controller,
            string layerName
        )
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
            AnimatorStateMachine stateMachine,
            string stateName
        )
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null &&
                    states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            ChildAnimatorStateMachine[] children = stateMachine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorState result = FindStateRecursive(
                    children[i].stateMachine,
                    stateName
                );

                if (result != null)
                    return result;
            }

            return null;
        }

        private static AnimatorState FindDirectState(
            AnimatorStateMachine stateMachine,
            string stateName
        )
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null &&
                    states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            return null;
        }

        private static bool EnsureTransition(
            AnimatorState source,
            AnimatorState destination,
            string parameterName,
            AnimatorConditionMode mode
        )
        {
            AnimatorStateTransition[] transitions = source.transitions;

            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition.destinationState != destination)
                    continue;

                AnimatorCondition[] conditions = transition.conditions;
                for (int c = 0; c < conditions.Length; c++)
                {
                    if (conditions[c].parameter == parameterName &&
                        conditions[c].mode == mode)
                    {
                        return false;
                    }
                }
            }

            AnimatorStateTransition created =
                source.AddTransition(destination);

            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.08f;
            created.canTransitionToSelf = false;
            created.AddCondition(mode, 0f, parameterName);
            return true;
        }
    }
}
