using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.Input;
using ROS.Game.Parachute;
using ROS.Game.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstBattleRoyaleBotMaterializer
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private const string ParachuteResource =
            "Parachute/PF_ParachuteVisual";

        static EditorFirstBattleRoyaleBotMaterializer()
        {
            EditorApplication.delayCall += Materialize;
        }

        [MenuItem("Rules Of Survival/Editor First/Materialize Battle Royale Bots")]
        public static void Materialize()
        {
            if (Application.isPlaying || EditorApplication.isCompiling ||
                !System.IO.File.Exists(ScenePath))
            {
                return;
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
                return;

            PlayerInputReader input = FindInScene<PlayerInputReader>(scene);
            BattleRoyaleManager manager = FindInScene<BattleRoyaleManager>(scene);
            MatchStartController sequence = FindInScene<MatchStartController>(scene);
            AirplaneController airplane = FindInScene<AirplaneController>(scene);

            if (input == null || manager == null || sequence == null || airplane == null)
            {
                Debug.LogError(
                    "[Editor First] No se pudo materializar BattleRoyaleBotDirector: " +
                    "faltan PlayerInputReader, BattleRoyaleManager, MatchStartController o AirplaneController."
                );

                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            BattleRoyaleBotDirector director =
                sequence.GetComponent<BattleRoyaleBotDirector>();

            bool changed = false;
            if (director == null)
            {
                director = sequence.gameObject.AddComponent<BattleRoyaleBotDirector>();
                changed = true;
            }

            GameObject parachutePrefab =
                Resources.Load<GameObject>(ParachuteResource);

            SerializedObject serialized = new SerializedObject(director);
            changed |= SetObjectReference(serialized, "sourcePlayer", input.gameObject);
            changed |= SetObjectReference(serialized, "airplane", airplane);
            changed |= SetObjectReference(serialized, "matchManager", manager);
            changed |= SetObjectReference(serialized, "parachutePrefab", parachutePrefab);
            changed |= SetObjectReference(serialized, "sequence", sequence);
            changed |= SetInt(
                serialized,
                "botCount",
                BattleRoyaleBotDirector.DefaultBotCount
            );
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log(
                $"[Editor First] BattleRoyaleBotDirector listo en escena 08 con {BattleRoyaleBotDirector.DefaultBotCount} bots. " +
                "Los bots se activan al pulsar INICIAR PARTIDA BR."
            );

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T[] found = roots[i].GetComponentsInChildren<T>(true);
                if (found.Length > 0)
                    return found[0];
            }

            return null;
        }

        private static bool SetObjectReference(
            SerializedObject serialized,
            string propertyName,
            Object value
        )
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
                return false;

            property.objectReferenceValue = value;
            return true;
        }

        private static bool SetInt(
            SerializedObject serialized,
            string propertyName,
            int value
        )
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.intValue == value)
                return false;

            property.intValue = value;
            return true;
        }
    }
}
