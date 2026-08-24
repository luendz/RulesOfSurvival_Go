using ROS.Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstWeaponBackMountMaterializer
    {
        private const string WeaponsFolder =
            "Assets/_Game/Prefabs/Weapons";

        private static readonly Vector3 LegacyBack01Position =
            new Vector3(0.18f, 0.05f, -0.12f);
        private static readonly Vector3 LegacyBack01Euler =
            new Vector3(0f, 0f, 35f);
        private static readonly Vector3 LegacyBack02Position =
            new Vector3(-0.18f, 0.05f, -0.12f);
        private static readonly Vector3 LegacyBack02Euler =
            new Vector3(0f, 0f, -35f);

        // Convención física del personaje:
        // Back01 = lado derecho.
        private static readonly Vector3 RightBackPosition =
            new Vector3(0.01f, 0.08f, -0.036f);
        private static readonly Vector3 RightBackEuler =
            new Vector3(-180f, -180f, 50f);

        // Back02 = lado izquierdo.
        private static readonly Vector3 LeftBackPosition =
            new Vector3(-0.03f, 0.133f, -0.035f);
        private static readonly Vector3 LeftBackEuler =
            new Vector3(-180f, -180f, 120f);

        static EditorFirstWeaponBackMountMaterializer()
        {
            EditorApplication.delayCall += ApplyStandardBackMounts;
        }

        [MenuItem("Rules Of Survival/Editor First/Apply Standard Back Weapon Mounts")]
        public static void ApplyStandardBackMounts()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { WeaponsFolder }
            );

            int changedPrefabs = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                bool changed = false;
                try
                {
                    WeaponMount[] mounts =
                        root.GetComponentsInChildren<WeaponMount>(true);

                    for (int j = 0; j < mounts.Length; j++)
                        changed |= UpgradeLegacyMount(mounts[j]);

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changedPrefabs++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            if (changedPrefabs > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "[Editor First] Montajes de espalda actualizados en " +
                    changedPrefabs + " prefabs. Back01=derecha, Back02=izquierda."
                );
            }
        }

        private static bool UpgradeLegacyMount(WeaponMount mount)
        {
            if (mount == null)
                return false;

            SerializedObject serialized = new SerializedObject(mount);
            SerializedProperty back01Position =
                serialized.FindProperty("back01LocalPosition");
            SerializedProperty back01Euler =
                serialized.FindProperty("back01LocalEulerAngles");
            SerializedProperty back02Position =
                serialized.FindProperty("back02LocalPosition");
            SerializedProperty back02Euler =
                serialized.FindProperty("back02LocalEulerAngles");

            bool changed = false;

            if (back01Position != null &&
                Approximately(back01Position.vector3Value, LegacyBack01Position))
            {
                back01Position.vector3Value = RightBackPosition;
                changed = true;
            }

            if (back01Euler != null &&
                Approximately(back01Euler.vector3Value, LegacyBack01Euler))
            {
                back01Euler.vector3Value = RightBackEuler;
                changed = true;
            }

            if (back02Position != null &&
                Approximately(back02Position.vector3Value, LegacyBack02Position))
            {
                back02Position.vector3Value = LeftBackPosition;
                changed = true;
            }

            if (back02Euler != null &&
                Approximately(back02Euler.vector3Value, LegacyBack02Euler))
            {
                back02Euler.vector3Value = LeftBackEuler;
                changed = true;
            }

            if (!changed)
                return false;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mount);
            return true;
        }

        private static bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.0001f &&
                   Mathf.Abs(a.y - b.y) < 0.0001f &&
                   Mathf.Abs(a.z - b.z) < 0.0001f;
        }
    }
}
