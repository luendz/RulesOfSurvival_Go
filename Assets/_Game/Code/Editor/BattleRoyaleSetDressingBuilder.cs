using UnityEditor;
using UnityEngine;

namespace ROS.Game.Editor
{
    public static class BattleRoyaleSetDressingBuilder
    {
        private const string SedanModelPath =
            "Assets/_Game/Art/Vehicles/Models/Sedan.fbx";
        private const string ResourcesFolder =
            "Assets/_Game/Resources/World";
        private const string SedanPrefabPath =
            ResourcesFolder + "/PF_SedanStatic.prefab";
        private const float SedanMaximumSize = 4.8f;

        [MenuItem("Rules Of Survival/Tools/World/Build Static Sedan")]
        public static void Build()
        {
            EnsureFolder(ResourcesFolder);

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                SedanModelPath
            );
            if (source == null)
            {
                Debug.LogError($"No se encontró el modelo 3D: {SedanModelPath}");
                return;
            }

            GameObject root = new GameObject("PF_SedanStatic");
            GameObject model = PrefabUtility.InstantiatePrefab(source) as
                GameObject;

            if (model == null)
            {
                Object.DestroyImmediate(root);
                Debug.LogError("No se pudo instanciar el modelo del Sedan.");
                return;
            }

            model.name = "Sedan";
            model.transform.SetParent(root.transform, false);
            NormalizeAndGroundModel(model);

            PrefabUtility.SaveAsPrefabAsset(root, SedanPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Validate();
        }

        [MenuItem("Rules Of Survival/Tools/World/Validate Static Sedan")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SedanPrefabPath
            );
            if (prefab == null)
            {
                Debug.LogError("Falta el prefab estático del Sedan.");
                return;
            }

            bool hasRenderer =
                prefab.GetComponentsInChildren<Renderer>(true).Length > 0;
            bool hasInteraction =
                prefab.GetComponentsInChildren<Collider>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0;

            if (!hasRenderer || hasInteraction)
            {
                Debug.LogError(
                    "El Sedan debe tener renderers y no debe contener " +
                    "colliders ni rigidbodies."
                );
                return;
            }

            Debug.Log(
                "Validación correcta: Sedan visual estático y sin interacción."
            );
        }

        private static void NormalizeAndGroundModel(GameObject model)
        {
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
                    SedanMaximumSize / maximumSize;
            }

            if (TryGetBounds(model, out bounds))
            {
                model.transform.position -= new Vector3(
                    bounds.center.x,
                    bounds.min.y,
                    bounds.center.z
                );
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
