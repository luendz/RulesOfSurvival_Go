using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Entrada de compatibilidad para el reparador anterior. La arquitectura y
    /// sus estados se reparan ahora desde un único materializador idempotente.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstUnifiedAnimationStateRepair
    {
        static EditorFirstUnifiedAnimationStateRepair()
        {
            EditorApplication.delayCall += Repair;
        }

        [MenuItem("Rules Of Survival/Editor First/Repair Consolidated Upper Body Motions")]
        public static void Repair()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstUnifiedAnimationMaterializer.Materialize();
        }
    }
}
