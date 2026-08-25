using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Compatibilidad con el reparador de Reload anterior. Reload pertenece a
    /// WeaponUpperBody y se materializa desde la arquitectura unificada.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstReloadUpperBodyRepair
    {
        static EditorFirstReloadUpperBodyRepair()
        {
            EditorApplication.delayCall += () => Repair();
        }

        public static bool Repair()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return false;

            return EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();
        }
    }
}
