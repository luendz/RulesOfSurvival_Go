using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Compatibilidad con instalaciones que aún invocan la migración anterior.
    /// Toda la migración real está centralizada en EditorFirstUnifiedAnimationMaterializer.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstUnifiedAnimationLegacyMigration
    {
        static EditorFirstUnifiedAnimationLegacyMigration()
        {
            EditorApplication.delayCall += MigrateIfNeeded;
        }

        public static void MigrateIfNeeded()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstUnifiedAnimationMaterializer.EnsureAnimatorArchitecture();
        }
    }
}