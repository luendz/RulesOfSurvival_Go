using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Compatibilidad con la herramienta anterior. Healing ahora pertenece a
    /// la arquitectura consolidada de capas Upper Body.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstHealingUpperBodyMaterializer
    {
        static EditorFirstHealingUpperBodyMaterializer()
        {
            EditorApplication.delayCall += EnsureHealingUpperBody;
        }

        [MenuItem("Rules Of Survival/Editor First/Configure Healing Upper Body")]
        public static void EnsureHealingUpperBody()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstAnimationLayerConsolidator.EnsureConsolidatedAnimationLayers();
        }
    }
}
