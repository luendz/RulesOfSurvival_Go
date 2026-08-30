#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ROS.Game.Editor
{
    public static class DedicatedServerBuild
    {
        [MenuItem("ROS Battle Royale/Build/Windows Dedicated Server")]
        public static void BuildWindowsServer()
        {
            string scenePath = "Assets/_Game/Scenes/08_EchoValley.unity";
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("ROS Battle Royale", "Primero genera las escenas con el menú 01.", "OK");
                return;
            }

            Directory.CreateDirectory("Builds/ServerWindows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = "Builds/ServerWindows/ROS_Server.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Dedicated Server build: {report.summary.result} - {report.summary.totalSize} bytes");
        }
    }
}
#endif
