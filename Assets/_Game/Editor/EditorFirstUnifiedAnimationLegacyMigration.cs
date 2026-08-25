using ROS.Game.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Migración de una sola vía para copias locales que llegaron a materializar
    /// el antiguo UpperBodyCombat (Empty + Aim) antes de la consolidación V2.
    /// Cuando detecta esa estructura incompleta, elimina únicamente la capa
    /// legacy y deja que EditorFirstUnifiedAnimationMaterializer cree la nueva.
    /// Una capa V2 válida nunca se toca, por lo que los ajustes manuales quedan
    /// preservados después de la migración.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstUnifiedAnimationLegacyMigration
    {
        private const string ControllerPath =
            "Assets/_Game/Animations/AC_Player_Prototype.controller";

        static EditorFirstUnifiedAnimationLegacyMigration()
        {
            EditorApplication.delayCall += MigrateIfNeeded;
        }

        [MenuItem("Rules Of Survival/Editor First/Migrate Legacy Upper Body Layers")]
        public static void MigrateIfNeeded()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return;

            bool changed = false;

            changed |= RemoveIncompleteLayer(
                controller,
                PlayerAnimationCoordinator.CombatLayerName,
                "ArmedLocomotion"
            );

            changed |= RemoveIncompleteLayer(
                controller,
                PlayerAnimationCoordinator.ActionsLayerName,
                "ReloadStanding"
            );

            if (changed)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }

            // Es idempotente: crea lo que falte y no reconstruye las capas V2.
            EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();
        }

        private static bool RemoveIncompleteLayer(
            AnimatorController controller,
            string layerName,
            string requiredState)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorControllerLayer layer = layers[i];
                if (layer.name != layerName)
                    continue;

                if (ContainsState(layer.stateMachine, requiredState))
                    return false;

                controller.RemoveLayer(i);
                Debug.Log(
                    "[Editor First] Capa legacy '" + layerName +
                    "' retirada para migrar a la arquitectura unificada."
                );
                return true;
            }

            return false;
        }

        private static bool ContainsState(
            AnimatorStateMachine machine,
            string stateName)
        {
            if (machine == null)
                return false;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                    return true;
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                if (ContainsState(children[i].stateMachine, stateName))
                    return true;
            }

            return false;
        }
    }
}
