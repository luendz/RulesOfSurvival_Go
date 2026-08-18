#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.Editor
{
    [InitializeOnLoad]
    public static class ROSFirstRunInitializer
    {
        private const string Marker = "ProjectSettings/ROSBaseInitialized.txt";

        static ROSFirstRunInitializer()
        {
            if (Application.isBatchMode || File.Exists(Marker)) return;
            EditorApplication.delayCall += InitializeOnce;
        }

        private static void InitializeOnce()
        {
            if (File.Exists(Marker)) return;
            ROSProjectSetup.CreateDemoProject();
            File.WriteAllText(Marker, "RulesOfSurvival_Go! base initialized by editor setup.\n");
            AssetDatabase.Refresh();
        }
    }
}
#endif
