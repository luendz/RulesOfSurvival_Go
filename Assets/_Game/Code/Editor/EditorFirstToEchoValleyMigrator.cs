using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Editor
{
    public static class EditorFirstToEchoValleyMigrator
    {
        private const string EditorFirstPath = "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";
        private const string EchoValleyPath  = "Assets/_Game/Scenes/08_EchoValley.unity";

        // Nombres de raíz a NO copiar (muros, piso, elementos estructurales de escenografía)
        private static readonly HashSet<string> Exclude = new()
        {
            "Muros",
            "Ground",
            "SafeZoneWall",
            "__EDITOR_FIRST_PRESENTATION",
        };

        [MenuItem("Rules Of Survival/Tools/Scenes/Migrar EditorFirst → EchoValley")]
        public static void Migrate()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Migrar objetos",
                "Se copiarán todos los GameObjects raíz de EditorFirst a EchoValley, " +
                "excepto Muros, Ground, SafeZoneWall y __EDITOR_FIRST_PRESENTATION.\n\n" +
                "¿Continuar?",
                "Migrar", "Cancelar");

            if (!confirmed) return;

            // Guardar la escena activa actual para restaurarla al final
            var previousScene = EditorSceneManager.GetActiveScene();

            // Abrir EchoValley y EditorFirst de forma aditiva
            var echoValleyScene    = EditorSceneManager.OpenScene(EchoValleyPath,  OpenSceneMode.Additive);
            var editorFirstScene   = EditorSceneManager.OpenScene(EditorFirstPath, OpenSceneMode.Additive);

            // Recopilar nombres ya existentes en EchoValley para detectar duplicados
            var existingNames = new HashSet<string>();
            foreach (var go in echoValleyScene.GetRootGameObjects())
                existingNames.Add(go.name);

            var toMove    = new List<GameObject>();
            var skipped   = new List<string>();
            var duplicates = new List<string>();

            foreach (var go in editorFirstScene.GetRootGameObjects())
            {
                if (ShouldExclude(go.name))
                {
                    skipped.Add(go.name);
                    continue;
                }

                if (existingNames.Contains(go.name))
                {
                    duplicates.Add(go.name);
                    // Aun así lo movemos; el usuario puede limpiar después
                }

                toMove.Add(go);
            }

            if (duplicates.Count > 0)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Duplicados detectados",
                    $"Los siguientes objetos ya existen en EchoValley:\n" +
                    $"{string.Join(", ", duplicates)}\n\n" +
                    "Se agregarán de todas formas. ¿Continuar?",
                    "Continuar", "Cancelar");

                if (!proceed)
                {
                    EditorSceneManager.CloseScene(editorFirstScene, true);
                    EditorSceneManager.CloseScene(echoValleyScene,  false);
                    return;
                }
            }

            // Mover los GameObjects a EchoValley
            foreach (var go in toMove)
                SceneManager.MoveGameObjectToScene(go, echoValleyScene);

            EditorSceneManager.SaveScene(echoValleyScene);
            EditorSceneManager.CloseScene(editorFirstScene, false); // no guardar EditorFirst

            Debug.Log(
                $"[Migrador] Completado. " +
                $"Movidos: {toMove.Count} | " +
                $"Excluidos (muros/piso): {string.Join(", ", skipped)}");

            EditorUtility.DisplayDialog(
                "Migración completa",
                $"Se movieron {toMove.Count} objeto(s) a EchoValley.\n" +
                $"Excluidos: {string.Join(", ", skipped)}",
                "OK");
        }

        private static bool ShouldExclude(string name)
        {
            if (Exclude.Contains(name)) return true;
            // Excluir Muro_01 … Muro_XX
            if (name.StartsWith("Muro_")) return true;
            return false;
        }
    }
}
