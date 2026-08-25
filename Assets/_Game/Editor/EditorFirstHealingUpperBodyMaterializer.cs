using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Configura la capa Actions del Animator para que las acciones como Healing
    /// afecten solo al torso/brazos. La locomocion de la capa base sigue
    /// controlando cadera y piernas mientras el jugador se cura.
    ///
    /// Se aplica unicamente si Actions aun no tiene AvatarMask, de modo que una
    /// configuracion manual posterior del usuario no sea sobrescrita.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstHealingUpperBodyMaterializer
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        private const string UpperBodyMaskPath =
            "Assets/_Game/Animations/Masks/AM_WeaponUpperBody.mask";

        private const string ActionsLayerName = "Actions";
        private const string HealingParameterName = "Healing";
        private const string HealingStateName = "Healing";

        static EditorFirstHealingUpperBodyMaterializer()
        {
            EditorApplication.delayCall += EnsureHealingUpperBody;
        }

        [MenuItem("Rules Of Survival/Editor First/Configure Healing Upper Body")]
        public static void EnsureHealingUpperBody()
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
                    "[Editor First] No se encontro el Avatar Mask Upper Body: " +
                    UpperBodyMaskPath
                );
                return;
            }

            bool changed = false;
            AnimatorControllerLayer[] layers = controller.layers;
            int actionsLayerIndex = -1;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name != ActionsLayerName)
                    continue;

                actionsLayerIndex = i;

                // Solo migramos la configuracion antigua sin mascara. Si el
                // usuario asigna otra mascara manualmente, se respeta.
                if (layers[i].avatarMask == null)
                {
                    layers[i].avatarMask = upperBodyMask;
                    changed = true;
                }

                break;
            }

            if (actionsLayerIndex < 0)
            {
                Debug.LogError(
                    "[Editor First] AC_Player_Prototype no contiene la capa 'Actions'."
                );
                return;
            }

            if (!HasBoolParameter(controller, HealingParameterName))
            {
                Debug.LogError(
                    "[Editor First] AC_Player_Prototype no contiene el parametro bool 'Healing'."
                );
                return;
            }

            if (!ContainsState(
                    layers[actionsLayerIndex].stateMachine,
                    HealingStateName
                ))
            {
                Debug.LogError(
                    "[Editor First] La capa Actions no contiene el estado 'Healing'."
                );
                return;
            }

            if (!changed)
                return;

            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Editor First] Healing configurado como Upper Body. " +
                "La locomocion de piernas permanece activa durante la curacion."
            );
        }

        private static bool HasBoolParameter(
            AnimatorController controller,
            string parameterName
        )
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.name == parameterName &&
                    parameter.type == AnimatorControllerParameterType.Bool)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsState(
            AnimatorStateMachine stateMachine,
            string stateName
        )
        {
            if (stateMachine == null)
                return false;

            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null &&
                    states[i].state.name == stateName)
                {
                    return true;
                }
            }

            ChildAnimatorStateMachine[] children = stateMachine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                if (ContainsState(children[i].stateMachine, stateName))
                    return true;
            }

            return false;
        }
    }
}
