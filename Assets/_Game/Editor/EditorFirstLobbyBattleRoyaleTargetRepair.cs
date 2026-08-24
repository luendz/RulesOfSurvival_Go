using ROS.Game.Lobby;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Garantiza que el boton JUGAR/INICIAR del lobby cargue siempre la escena
    /// funcional Editor First y no la antigua 07_BattleRoyaleTest.
    /// </summary>
    [InitializeOnLoad]
    public sealed class EditorFirstLobbyBattleRoyaleTargetRepair : IPreprocessBuildWithReport
    {
        private const string LobbyScenePath =
            "Assets/_Game/Scenes/08_Lobby.unity";

        private const string BattleRoyaleSceneName =
            "08_EditorFirstFunctionalTest";

        public int callbackOrder => -1000;

        static EditorFirstLobbyBattleRoyaleTargetRepair()
        {
            EditorApplication.delayCall += RepairLobbyTarget;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            RepairLobbyTarget();
        }

        [MenuItem("Rules Of Survival/Editor First/Fix Lobby BR Target")]
        public static void RepairLobbyTarget()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            if (!System.IO.File.Exists(LobbyScenePath))
            {
                Debug.LogWarning(
                    "[Editor First] No se encontro la escena de lobby: " +
                    LobbyScenePath
                );
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(LobbyScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
            {
                scene = EditorSceneManager.OpenScene(
                    LobbyScenePath,
                    OpenSceneMode.Additive
                );
            }

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            LobbySceneBootstrap bootstrap = FindBootstrap(scene);
            if (bootstrap == null)
            {
                Debug.LogError(
                    "[Editor First] 08_Lobby no contiene LobbySceneBootstrap."
                );

                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            SerializedObject serialized = new SerializedObject(bootstrap);
            SerializedProperty target =
                serialized.FindProperty("battleRoyaleSceneName");

            bool changed = false;
            if (target != null && target.stringValue != BattleRoyaleSceneName)
            {
                target.stringValue = BattleRoyaleSceneName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bootstrap);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[Editor First] Lobby corregido: JUGAR/INICIAR -> " +
                    BattleRoyaleSceneName
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static LobbySceneBootstrap FindBootstrap(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                LobbySceneBootstrap bootstrap =
                    roots[i].GetComponentInChildren<LobbySceneBootstrap>(true);

                if (bootstrap != null)
                    return bootstrap;
            }

            return null;
        }
    }
}
