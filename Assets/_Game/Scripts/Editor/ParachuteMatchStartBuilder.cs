using ROS.Game.Parachute;
using ROS.Game.World;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.Editor
{
    public static class ParachuteMatchStartBuilder
    {
        private const string ParachuteModelPath =
            "Assets/_Game/Art/Parachute/Models/Parachute.fbx";
        private const string AirplaneModelPath =
            "Assets/_Game/Art/Vehicles/Aircraft/Airplane_Starfighter/" +
            "Airplane_Starfighter.fbx";
        private const string ResourcesFolder =
            "Assets/_Game/Resources/Parachute";
        private const string ParachutePrefabPath =
            ResourcesFolder + "/PF_ParachuteVisual.prefab";
        private const string AirplanePrefabPath =
            ResourcesFolder + "/PF_AirplaneStart.prefab";

        [MenuItem("Rules Of Survival/Tools/Parachute/Build Match Start")]
        public static void Build()
        {
            EnsureFolder(ResourcesFolder);

            bool parachuteCreated = CreateVisualPrefab(
                ParachuteModelPath,
                ParachutePrefabPath,
                "PF_ParachuteVisual",
                5.5f,
                ParachuteController.ModelEulerAngles,
                false
            );
            bool airplaneCreated = CreateVisualPrefab(
                AirplaneModelPath,
                AirplanePrefabPath,
                "PF_AirplaneStart",
                18f,
                AirplaneController.ModelEulerAngles,
                true
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (parachuteCreated && airplaneCreated)
            {
                Debug.Log(
                    "Inicio de partida generado con avión y paracaídas reales."
                );
            }

            Validate();
        }

        [MenuItem("Rules Of Survival/Tools/Parachute/Validate Match Start")]
        public static void Validate()
        {
            GameObject parachute = AssetDatabase.LoadAssetAtPath<GameObject>(
                ParachutePrefabPath
            );
            GameObject airplane = AssetDatabase.LoadAssetAtPath<GameObject>(
                AirplanePrefabPath
            );

            bool valid = ValidateVisual(
                parachute,
                "paracaídas"
            );
            valid &= ValidateVisual(airplane, "avión");
            valid &= airplane != null &&
                     airplane.GetComponent<AirplaneController>() != null;
            valid &= ValidateModelRotation(
                parachute,
                ParachuteController.ModelEulerAngles,
                "paracaídas"
            );
            valid &= ValidateModelRotation(
                airplane,
                AirplaneController.ModelEulerAngles,
                "avión"
            );

            if (!valid)
            {
                Debug.LogError(
                    "La validación del inicio de partida encontró referencias faltantes."
                );
                return;
            }

            Debug.Log(
                "Validación de inicio de partida correcta: " +
                "avión, ruta, salto y paracaídas listos."
            );
        }

        private static bool CreateVisualPrefab(
            string modelPath,
            string prefabPath,
            string prefabName,
            float targetMaximumSize,
            Vector3? modelEulerAngles,
            bool addAirplaneController
        )
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                modelPath
            );

            if (source == null)
            {
                Debug.LogError($"No se encontró el modelo 3D: {modelPath}");
                return false;
            }

            GameObject root = new GameObject(prefabName);
            GameObject model = PrefabUtility.InstantiatePrefab(source) as
                GameObject;

            if (model == null)
            {
                Object.DestroyImmediate(root);
                Debug.LogError($"No se pudo instanciar: {modelPath}");
                return false;
            }

            model.name = source.name;
            model.transform.SetParent(root.transform, false);
            NormalizeModel(
                model,
                targetMaximumSize,
                modelEulerAngles
            );

            if (addAirplaneController)
            {
                root.AddComponent<AirplaneController>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return true;
        }

        private static void NormalizeModel(
            GameObject model,
            float targetMaximumSize,
            Vector3? modelEulerAngles
        )
        {
            if (modelEulerAngles.HasValue)
            {
                model.transform.localRotation = Quaternion.Euler(
                    modelEulerAngles.Value
                );
            }

            if (!TryGetBounds(model, out Bounds bounds))
            {
                return;
            }

            float maximumSize = Mathf.Max(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z
            );

            if (maximumSize > 0.0001f)
            {
                model.transform.localScale *=
                    targetMaximumSize / maximumSize;
            }

            if (TryGetBounds(model, out bounds))
            {
                model.transform.position -= bounds.center;
            }
        }

        private static bool TryGetBounds(
            GameObject root,
            out Bounds bounds
        )
        {
            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(true);
            bounds = default;

            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static bool ValidateModelRotation(
            GameObject prefab,
            Vector3 expectedEulerAngles,
            string label
        )
        {
            if (prefab == null || prefab.transform.childCount == 0)
            {
                Debug.LogError(
                    $"El prefab de {label} no tiene modelo 3D."
                );
                return false;
            }

            Quaternion expected = Quaternion.Euler(
                expectedEulerAngles
            );
            Quaternion actual = prefab.transform.GetChild(0).localRotation;

            if (Quaternion.Angle(expected, actual) <= 0.1f)
            {
                return true;
            }

            Debug.LogError(
                $"La rotación local del modelo de {label} no coincide " +
                $"con {expectedEulerAngles}."
            );
            return false;
        }

        private static bool ValidateVisual(
            GameObject prefab,
            string label
        )
        {
            if (prefab == null)
            {
                Debug.LogError($"Falta el prefab de {label}.");
                return false;
            }

            if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Debug.LogError($"El prefab de {label} no tiene renderers.");
                return false;
            }

            return true;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
