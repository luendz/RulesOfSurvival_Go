using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Compatibilidad con el menu/herramienta anterior. Crouch Aim ya no usa
    /// una capa exclusiva: ahora forma parte de UpperBodyCombat, compartida con
    /// el apuntado de pie y en movimiento.
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

            EditorFirstAnimationLayerConsolidator.EnsureConsolidatedAnimationLayers();
        }
    }
}
