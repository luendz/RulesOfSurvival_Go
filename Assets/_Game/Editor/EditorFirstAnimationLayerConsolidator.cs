using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Consolida el Animator del jugador en una arquitectura composable:
    /// Locomotion (cuerpo completo) + UpperBodyCombat + Actions +
    /// WeaponUpperBody. Las capas superiores arrancan con peso cero y el
    /// PlayerAnimatorDriver decide cuando participan.
    ///
    /// Todo queda fisicamente guardado en AC_Player_Prototype.controller y es
    /// editable desde la ventana Animator.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstAnimationLayerConsolidator
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string UpperBodyMaskPath =
            "Assets/_Game/Animations/Masks/AM_WeaponUpperBody.mask";

        private const string LocomotionLayerName = "Locomotion";
        private const string CombatLayerName = "UpperBodyCombat";
        private const string ActionsLayerName = "Actions";
        private const string WeaponLayerName = "WeaponUpperBody";
        private const string LegacyCrouchAimLayerName = "CrouchAimUpperBody";

        private const string AimParameterName = "UpperBodyAim";
        private const string SourceAimStateName = "BT_AimLocomotion";
        private const string EmptyStateName = "Empty";
        private const string AimStateName = "Aim";

        static EditorFirstAnimationLayerConsolidator()
        {
            EditorApplication.delayCall += EnsureConsolidatedAnimationLayers;
        }

        [MenuItem("Rules Of Survival/Editor First/Consolidate Player Animation Layers")]
        public static void EnsureConsolidatedAnimationLayers()
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
                    "[Editor First] No se encontro la mascara Upper Body: " +
                    UpperBodyMaskPath
                );
                return;
            }

            AnimatorControllerLayer locomotion =
                FindLayer(controller, LocomotionLayerName);
            if (locomotion == null || locomotion.stateMachine == null)
            {
                Debug.LogError(
                    "[Editor First] No existe la capa Locomotion en " +
                    ControllerPath
                );
                return;
            }

            AnimatorState sourceAim = FindStateRecursive(
                locomotion.stateMachine,
                SourceAimStateName
            );

            if (sourceAim == null || sourceAim.motion == null)
            {
                Debug.LogError(
                    "[Editor First] No se encontro el motion fuente '" +
                    SourceAimStateName + "'."
                );
                return;
            }

            bool changed = false;

            changed |= EnsureBoolParameter(controller, AimParameterName);
            changed |= RemoveLayerIfPresent(controller, LegacyCrouchAimLayerName);
            changed |= EnsureExistingUpperBodyLayer(
                controller,
                ActionsLayerName,
                upperBodyMask
            );
            changed |= EnsureExistingUpperBodyLayer(
                controller,
                WeaponLayerName,
                upperBodyMask
            );
            changed |= EnsureCombatLayer(
                controller,
                upperBodyMask,
                sourceAim.motion
            );
            changed |= EnsureLocomotionWeight(controller);

            if (!changed)
                return;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Editor First] Animator consolidado: Locomotion cuerpo completo + " +
                "UpperBodyCombat + Actions + WeaponUpperBody. CrouchAimUpperBody " +
                "legacy fue retirado."
            );
        }

        private static bool EnsureCombatLayer(
            AnimatorController controller,
            AvatarMask mask,
            Motion aimMotion
        )
        {
            bool changed = false;
            AnimatorControllerLayer layer = FindLayer(controller, CombatLayerName);

            if (layer == null)
            {
                AnimatorStateMachine machine = new AnimatorStateMachine
                {
                    name = CombatLayerName
                };
                AssetDatabase.AddObjectToAsset(machine, controller);

                layer = new AnimatorControllerLayer
                {
                    name = CombatLayerName,
                    avatarMask = mask,
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    defaultWeight = 0f,
                    stateMachine = machine
                };

                controller.AddLayer(layer);
                changed = true;
            }

            AnimatorControllerLayer[] layers = controller.layers;
            int layerIndex = FindLayerIndex(layers, CombatLayerName);
            if (layerIndex < 0)
                return changed;

            AnimatorControllerLayer configured = layers[layerIndex];
            if (configured.avatarMask != mask)
            {
                configured.avatarMask = mask;
                changed = true;
            }

            if (configured.blendingMode != AnimatorLayerBlendingMode.Override)
            {
                configured.blendingMode = AnimatorLayerBlendingMode.Override;
                changed = true;
            }

            if (!Mathf.Approximately(configured.defaultWeight, 0f))
            {
                configured.defaultWeight = 0f;
                changed = true;
            }

            AnimatorStateMachine stateMachine = configured.stateMachine;
            if (stateMachine == null)
                return changed;

            AnimatorState empty = FindDirectState(stateMachine, EmptyStateName);
            if (empty == null)
            {
                empty = stateMachine.AddState(
                    EmptyStateName,
                    new Vector3(240f, 80f, 0f)
                );
                empty.motion = null;
                empty.writeDefaultValues = false;
                changed = true;
            }
            else
            {
                if (empty.motion != null)
                {
                    empty.motion = null;
                    changed = true;
                }
                if (empty.writeDefaultValues)
                {
                    empty.writeDefaultValues = false;
                    changed = true;
                }
            }

            AnimatorState aim = FindDirectState(stateMachine, AimStateName);
            if (aim == null)
            {
                aim = stateMachine.AddState(
                    AimStateName,
                    new Vector3(520f, 80f, 0f)
                );
                aim.motion = aimMotion;
                aim.writeDefaultValues = false;
                changed = true;
            }
            else
            {
                if (aim.motion != aimMotion)
                {
                    aim.motion = aimMotion;
                    changed = true;
                }
                if (aim.writeDefaultValues)
                {
                    aim.writeDefaultValues = false;
                    changed = true;
                }
            }

            if (stateMachine.defaultState != empty)
            {
                stateMachine.defaultState = empty;
                changed = true;
            }

            changed |= EnsureTransition(
                empty,
                aim,
                AimParameterName,
                AnimatorConditionMode.If
            );
            changed |= EnsureTransition(
                aim,
                empty,
                AimParameterName,
                AnimatorConditionMode.IfNot
            );

            layers[layerIndex] = configured;
            controller.layers = layers;

            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(empty);
            EditorUtility.SetDirty(aim);
            return changed;
        }

        private static bool EnsureExistingUpperBodyLayer(
            AnimatorController controller,
            string layerName,
            AvatarMask mask
        )
        {
            AnimatorControllerLayer[] layers = controller.layers;
            int index = FindLayerIndex(layers, layerName);
            if (index < 0)
                return false;

            bool changed = false;
            AnimatorControllerLayer layer = layers[index];

            if (layer.avatarMask != mask)
            {
                layer.avatarMask = mask;
                changed = true;
            }

            if (layer.blendingMode != AnimatorLayerBlendingMode.Override)
            {
                layer.blendingMode = AnimatorLayerBlendingMode.Override;
                changed = true;
            }

            // Nunca dejar una capa Override superior encendida sobre un estado
            // Empty. PlayerAnimatorDriver activa su peso solo durante la accion.
            if (!Mathf.Approximately(layer.defaultWeight, 0f))
            {
                layer.defaultWeight = 0f;
                changed = true;
            }

            if (layer.stateMachine != null)
            {
                AnimatorState empty = FindStateRecursive(
                    layer.stateMachine,
                    EmptyStateName
                );
                if (empty != null)
                {
                    if (empty.motion != null)
                    {
                        empty.motion = null;
                        changed = true;
                    }

                    if (empty.writeDefaultValues)
                    {
                        empty.writeDefaultValues = false;
                        changed = true;
                    }

                    EditorUtility.SetDirty(empty);
                }
            }

            layers[index] = layer;
            controller.layers = layers;
            return changed;
        }

        private static bool EnsureLocomotionWeight(AnimatorController controller)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            int index = FindLayerIndex(layers, LocomotionLayerName);
            if (index < 0 || Mathf.Approximately(layers[index].defaultWeight, 1f))
                return false;

            AnimatorControllerLayer layer = layers[index];
            layer.defaultWeight = 1f;
            layers[index] = layer;
            controller.layers = layers;
            return true;
        }

        private static bool RemoveLayerIfPresent(
            AnimatorController controller,
            string layerName
        )
        {
            AnimatorControllerLayer[] layers = controller.layers;
            int index = FindLayerIndex(layers, layerName);
            if (index < 0)
                return false;

            controller.RemoveLayer(index);
            return true;
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

        private static bool EnsureTransition(
            AnimatorState source,
            AnimatorState destination,
            string parameter,
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
                if (conditions.Length == 1 &&
                    conditions[0].parameter == parameter &&
                    conditions[0].mode == mode)
                {
                    return false;
                }
            }

            AnimatorStateTransition created = source.AddTransition(destination);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.08f;
            created.canTransitionToSelf = false;
            created.AddCondition(mode, 0f, parameter);
            return true;
        }

        private static AnimatorControllerLayer FindLayer(
            AnimatorController controller,
            string layerName
        )
        {
            AnimatorControllerLayer[] layers = controller.layers;
            int index = FindLayerIndex(layers, layerName);
            return index >= 0 ? layers[index] : null;
        }

        private static int FindLayerIndex(
            AnimatorControllerLayer[] layers,
            string layerName
        )
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName)
                    return i;
            }
            return -1;
        }

        private static AnimatorState FindStateRecursive(
            AnimatorStateMachine machine,
            string stateName
        )
        {
            if (machine == null)
                return null;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                    return states[i].state;
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                AnimatorState found = FindStateRecursive(
                    children[i].stateMachine,
                    stateName
                );
                if (found != null)
                    return found;
            }

            return null;
        }

        private static AnimatorState FindDirectState(
            AnimatorStateMachine machine,
            string stateName
        )
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                    return states[i].state;
            }
            return null;
        }
    }
}
