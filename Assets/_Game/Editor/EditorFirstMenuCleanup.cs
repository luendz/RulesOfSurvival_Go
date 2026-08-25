using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Expone únicamente las acciones manuales de mantenimiento que siguen
    /// siendo útiles para el flujo Editor First. Las herramientas internas
    /// permanecen disponibles como métodos estáticos, pero ya no crean entradas
    /// propias en el menú de Unity.
    /// </summary>
    public static class EditorFirstMenuCleanup
    {
        [MenuItem("Rules Of Survival/Editor First/Validate Editor First", false, 100)]
        public static void ValidateEditorFirst()
        {
            MethodInfo validateMethod = typeof(EditorFirstPresentationBuilder).GetMethod(
                "ValidateRuntimePresentationCreation",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (validateMethod == null)
            {
                Debug.LogError(
                    "[Editor First] No se encontro la validacion de presentacion Editor First."
                );
                return;
            }

            validateMethod.Invoke(null, null);
        }

        [MenuItem("Rules Of Survival/Editor First/Repair - Rebuild Editor First", false, 101)]
        public static void RepairOrRebuildEditorFirst()
        {
            EditorFirstFunctionalTestSceneBuilder.EnsureFunctionalTestScene();
        }
    }
}
