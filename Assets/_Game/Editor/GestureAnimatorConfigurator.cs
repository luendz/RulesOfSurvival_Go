using System;
using System.Linq;
using ROS.Game.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.Editor
{
    /// <summary>
    /// Compatibilidad con el configurador de gestos original.
    /// Ya no crea una capa Gestures: delega al materializador unificado, que
    /// distribuye los clips entre UpperBodyActions y FullBodyOverride.
    /// </summary>
    [InitializeOnLoad]
    public static class GestureAnimatorConfigurator
    {
        internal const string GestureFolderPrefix =
            "Assets/_Game/Animations/Character/Gestures/";

        private const string CurrentGestureFolderPrefix =
            "Assets/_Game/Animations/Character Animator/08. Emotes/";

        static GestureAnimatorConfigurator()
        {
            EditorApplication.delayCall += ConfigureIfNeeded;
        }

        [MenuItem("Tools/Rules of Survival/Configurar sistema de gestos")]
        public static void ConfigureFromMenu()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstUnifiedAnimationMaterializer.Materialize();
        }

        internal static void ScheduleConfiguration()
        {
            EditorApplication.delayCall -= ConfigureIfNeeded;
            EditorApplication.delayCall += ConfigureIfNeeded;
        }

        private static void ConfigureIfNeeded()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstUnifiedAnimationMaterializer.Materialize();
        }

        internal static bool IsGestureAsset(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   (path.StartsWith(GestureFolderPrefix, StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(CurrentGestureFolderPrefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed class GestureAnimatorAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool gestureAssetChanged =
                importedAssets.Any(GestureAnimatorConfigurator.IsGestureAsset) ||
                movedAssets.Any(GestureAnimatorConfigurator.IsGestureAsset);

            if (gestureAssetChanged)
                GestureAnimatorConfigurator.ScheduleConfiguration();
        }
    }
}
