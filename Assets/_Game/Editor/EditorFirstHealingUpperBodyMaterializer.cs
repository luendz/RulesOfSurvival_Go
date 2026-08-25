using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Compatibilidad con la herramienta anterior. Healing ahora pertenece a
    /// UpperBodyActions dentro de la arquitectura unificada.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstHealingUpperBodyMaterializer
    {
        static EditorFirstHealingUpperBodyMaterializer()
        {
            EditorApplication.delayCall += EnsureHealingUpperBody;
        }

        public static void EnsureHealingUpperBody()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();
        }
    }
}
