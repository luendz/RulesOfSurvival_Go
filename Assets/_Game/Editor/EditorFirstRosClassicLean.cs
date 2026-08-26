using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 14 del Animator ROS Classic.
    ///
    /// Materializa Lean como una capa Additive independiente de Aim_Offset.
    /// El parametro Lean usa el mismo rango logico del sistema jugable existente:
    /// -1 = izquierda, 0 = centro, +1 = derecha.
    ///
    /// El proyecto ya dispone de PlayerLeanController + PlayerLeanRigApplier para
    /// resolver seleccion persistente, restricciones, colision y distribucion del
    /// torso. Mientras no existan clips especificamente autorados como offsets
    /// aditivos de lean, este Blend Tree queda con Motion=None para no duplicar la
    /// inclinacion procedural ni reutilizar una pose full-body incorrectamente.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicLean
    {
        static EditorFirstRosClassicLean()
        {
            EditorApplication.delayCall += ScheduleAfterPreviousSteps;
        }

        private static void ScheduleAfterPreviousSteps()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/14 - Construir Lean")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstRosClassicAimOffset.Materialize();

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
                return;

            AnimatorControllerLayer leanLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.LeanLayer
            );

            if (leanLayer == null || leanLayer.stateMachine == null)
                return;

            EnsureParameter(controller, "Lean", AnimatorControllerParameterType.Float);

            AnimatorStateMachine root = leanLayer.stateMachine;
            if (FindState(root, "Lean_1D") != null)
                return;

            ClearRootStatesAndChildMachines(root);

            BlendTree leanTree = new BlendTree
            {
                name = "BT_Lean_1D",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Lean",
                useAutomaticThresholds = false
            };

            AssetDatabase.AddObjectToAsset(leanTree, controller);

            // No se asignan motions falsos. El efecto visual actual sigue en el
            // rig procedural hasta disponer de clips aditivos Left/Center/Right.
            leanTree.AddChild(null, -1f); // Left
            leanTree.AddChild(null, 0f);  // Center
            leanTree.AddChild(null, 1f);  // Right

            AnimatorState leanState = root.AddState(
                "Lean_1D",
                new Vector3(320f, 40f, 0f)
            );
            leanState.motion = leanTree;
            leanState.writeDefaultValues = false;
            root.defaultState = leanState;

            EditorUtility.SetDirty(leanTree);
            EditorUtility.SetDirty(leanState);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Lean materializado como layer Additive " +
                "independiente con BT_Lean_1D (-1/0/+1)."
            );

            Debug.LogWarning(
                "[ROS Classic Animator] Lean_1D queda con Motion=None en Left, " +
                "Center y Right. PlayerLeanRigApplier conserva el efecto visual " +
                "procedural para evitar duplicarlo hasta contar con offsets " +
                "aditivos reales."
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

        private static void ClearRootStatesAndChildMachines(AnimatorStateMachine root)
        {
            ChildAnimatorState[] states = root.states;
            for (int i = states.Length - 1; i >= 0; i--)
            {
                if (states[i].state != null)
                    root.RemoveState(states[i].state);
            }

            ChildAnimatorStateMachine[] machines = root.stateMachines;
            for (int i = machines.Length - 1; i >= 0; i--)
            {
                if (machines[i].stateMachine != null)
                    root.RemoveStateMachine(machines[i].stateMachine);
            }
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                    return;
            }

            controller.AddParameter(name, type);
        }
    }
}
