using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Paso 13 del Animator ROS Classic.
    ///
    /// Materializa Aim_Offset como una capa Additive independiente de la pose
    /// permanente del arma y de la locomocion. AimYaw y AimPitch trabajan en
    /// rango normalizado -1..1 y alimentan un Blend Tree cartesiano 2D.
    ///
    /// El repositorio actual contiene poses de apuntado con rifle, pero no hay
    /// evidencia de clips autorados especificamente como offsets aditivos
    /// Center/Up/Down/Left/Right. Por eso no se reutilizan poses full-body como
    /// si fueran aditivas: los nueve slots se dejan Motion=None para asignarlos
    /// manualmente cuando existan clips correctos.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRosClassicAimOffset
    {
        static EditorFirstRosClassicAimOffset()
        {
            EditorApplication.delayCall += ScheduleAfterPreviousSteps;
        }

        private static void ScheduleAfterPreviousSteps()
        {
            EditorApplication.delayCall -= Materialize;
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/ROS Classic Animator/13 - Construir Aim Offset")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstRosClassicUpperBodyActions.Materialize();

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    EditorFirstRosClassicAnimatorBuilder.ControllerPath
                );

            if (controller == null)
                return;

            AnimatorControllerLayer aimLayer = FindLayer(
                controller,
                EditorFirstRosClassicAnimatorBuilder.AimOffsetLayer
            );

            if (aimLayer == null || aimLayer.stateMachine == null)
                return;

            EnsureParameter(controller, "AimYaw", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "AimPitch", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsAiming", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine root = aimLayer.stateMachine;
            if (FindState(root, "Aim_2D") != null)
                return;

            ClearRootStatesAndChildMachines(root);

            BlendTree aimTree = new BlendTree
            {
                name = "BT_AimOffset_2D",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "AimYaw",
                blendParameterY = "AimPitch",
                useAutomaticThresholds = false
            };

            AssetDatabase.AddObjectToAsset(aimTree, controller);

            // Layout cartesiano normalizado. Ningun motion se inventa ni se
            // reutiliza desde una pose full-body que no haya sido creada como
            // animacion aditiva.
            aimTree.AddChild(null, new Vector2(0f, 0f));    // Center
            aimTree.AddChild(null, new Vector2(0f, 1f));    // Up
            aimTree.AddChild(null, new Vector2(0f, -1f));   // Down
            aimTree.AddChild(null, new Vector2(-1f, 0f));   // Left
            aimTree.AddChild(null, new Vector2(1f, 0f));    // Right
            aimTree.AddChild(null, new Vector2(-1f, 1f));   // Up Left
            aimTree.AddChild(null, new Vector2(1f, 1f));    // Up Right
            aimTree.AddChild(null, new Vector2(-1f, -1f));  // Down Left
            aimTree.AddChild(null, new Vector2(1f, -1f));   // Down Right

            AnimatorState aimState = root.AddState(
                "Aim_2D",
                new Vector3(320f, 40f, 0f)
            );
            aimState.motion = aimTree;
            aimState.writeDefaultValues = false;
            root.defaultState = aimState;

            EditorUtility.SetDirty(aimTree);
            EditorUtility.SetDirty(aimState);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ROS Classic Animator] Aim_Offset materializado como Blend Tree 2D " +
                "Additive controlado por AimYaw/AimPitch."
            );

            Debug.LogWarning(
                "[ROS Classic Animator] Aim_Offset queda con 9 slots Motion=None. " +
                "No se encontraron clips confirmados como offsets aditivos; las " +
                "poses Aim Rifle existentes no se reutilizan para evitar deformar " +
                "el torso al mezclarlas en una capa Additive."
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
