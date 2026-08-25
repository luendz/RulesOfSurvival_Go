using ROS.Game.Combat;
using ROS.Game.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstPlayerDebugHealthMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        static EditorFirstPlayerDebugHealthMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        public static bool Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive
                );
            }

            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            PlayerInputReader input = FindLocalInput(scene);
            if (input == null)
            {
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return false;
            }

            bool changed = false;
            PlayerDebugHealthHotkeys hotkeys =
                input.GetComponent<PlayerDebugHealthHotkeys>();

            if (hotkeys == null)
            {
                hotkeys = input.gameObject.AddComponent<PlayerDebugHealthHotkeys>();
                EditorUtility.SetDirty(input.gameObject);
                changed = true;
            }
            else if (!hotkeys.enabled)
            {
                hotkeys.enabled = true;
                EditorUtility.SetDirty(hotkeys);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);

            return changed;
        }

        private static PlayerInputReader FindLocalInput(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            PlayerInputReader fallback = null;

            for (int i = 0; i < roots.Length; i++)
            {
                PlayerInputReader[] readers =
                    roots[i].GetComponentsInChildren<PlayerInputReader>(true);

                for (int r = 0; r < readers.Length; r++)
                {
                    PlayerInputReader reader = readers[r];
                    if (reader == null)
                        continue;

                    fallback ??= reader;
                    if (!reader.UsesExternalControl)
                        return reader;
                }
            }

            return fallback;
        }
    }
}
