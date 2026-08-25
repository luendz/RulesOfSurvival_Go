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

        [MenuItem("Rules Of Survival/Editor First/Configure Crouch Aim Upper Body")]
        public static void EnsureCrouchAimUpperBody()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();
        }
    }
}
