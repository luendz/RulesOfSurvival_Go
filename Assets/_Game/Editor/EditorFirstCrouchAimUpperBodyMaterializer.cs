using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Compatibilidad con el menú anterior. Crouch Aim ya no usa una capa
    /// exclusiva: se compone dentro de UpperBodyCombat.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstCrouchAimUpperBodyMaterializer
    {
        static EditorFirstCrouchAimUpperBodyMaterializer()
        {
            EditorApplication.delayCall += EnsureCrouchAimUpperBody;
        }

        public static void EnsureCrouchAimUpperBody()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();
        }
    }
}
