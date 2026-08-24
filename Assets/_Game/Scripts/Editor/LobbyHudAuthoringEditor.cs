using ROS.Game.Lobby;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Editor
{
    [CustomEditor(typeof(LobbySceneBootstrap))]
    public sealed class LobbySceneBootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            EditorGUILayout.HelpBox(
                "El HUD del lobby puede existir como objetos reales de la escena. " +
                "Así puedes mover, redimensionar y editar cada elemento directamente desde la Jerarquía y el Inspector.",
                MessageType.Info
            );

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Crear / localizar HUD editable", GUILayout.Height(32f)))
                {
                    LobbyHudAuthoring.Materialize((LobbySceneBootstrap)target, true);
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class LobbyHudAuthoring
    {
        private const string LobbySceneName = "08_Lobby";
        private const string MenuPath =
            "Tools/Rules of Survival/Lobby/Crear o localizar HUD editable";

        static LobbyHudAuthoring()
        {
            EditorSceneManager.sceneOpened -= HandleSceneOpened;
            EditorSceneManager.sceneOpened += HandleSceneOpened;
        }

        [MenuItem(MenuPath)]
        private static void MaterializeFromMenu()
        {
            LobbySceneBootstrap bootstrap =
                Object.FindFirstObjectByType<LobbySceneBootstrap>(FindObjectsInactive.Include);

            if (bootstrap == null)
            {
                EditorUtility.DisplayDialog(
                    "HUD del Lobby",
                    "No se encontró LobbySceneBootstrap en la escena actual.",
                    "Aceptar"
                );
                return;
            }

            Materialize(bootstrap, true);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateMaterializeFromMenu()
        {
            return !EditorApplication.isPlaying;
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (EditorApplication.isPlaying || scene.name != LobbySceneName)
            {
                return;
            }

            EditorApplication.delayCall += () => MaterializeIfMissing(scene);
        }

        private static void MaterializeIfMissing(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlaying)
            {
                return;
            }

            LobbyHudView existing =
                Object.FindFirstObjectByType<LobbyHudView>(FindObjectsInactive.Include);
            if (existing != null && existing.gameObject.scene == scene)
            {
                return;
            }

            LobbySceneBootstrap bootstrap =
                Object.FindFirstObjectByType<LobbySceneBootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null || bootstrap.gameObject.scene != scene)
            {
                return;
            }

            Materialize(bootstrap, false);
        }

        public static LobbyHudView Materialize(
            LobbySceneBootstrap bootstrap,
            bool selectHud
        )
        {
            if (bootstrap == null || EditorApplication.isPlaying)
            {
                return null;
            }

            LobbyHudView hud = bootstrap.CreateEditableHud();
            if (hud == null)
            {
                return null;
            }

            hud.CaptureReferences();
            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(hud);

            Scene scene = bootstrap.gameObject.scene;
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (selectHud)
            {
                Selection.activeGameObject = hud.gameObject;
                EditorGUIUtility.PingObject(hud.gameObject);
            }

            Debug.Log(
                "HUD del lobby materializado. Edita 'Lobby Canvas' directamente " +
                "desde la Jerarquía y guarda la escena para conservar los cambios."
            );

            return hud;
        }
    }
}
