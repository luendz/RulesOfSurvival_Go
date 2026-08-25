using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Migra la antigua definicion Item_556Ammo al asset canonico Rifle Ammo.
    /// La migracion trabaja sobre los archivos serializados fisicos de Unity para
    /// preservar referencias en escenas, prefabs, ScriptableObjects y controllers.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstRifleAmmoMigration
    {
        private const string LegacyAssetPath =
            "Assets/_Game/Data/Item_556Ammo.asset";

        private const string CanonicalAssetPath =
            "Assets/_Game/Data/Ammo/Item_RifleAmmo.asset";

        private const string LegacyGuid =
            "bdecb8b4df730704783d4d93c1ee6782";

        private static readonly HashSet<string> SerializedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".asset",
                ".prefab",
                ".unity",
                ".controller",
                ".overrideController",
                ".anim",
                ".playable"
            };

        static EditorFirstRifleAmmoMigration()
        {
            EditorApplication.delayCall += Migrate;
        }

        public static void Migrate()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            string canonicalGuid =
                AssetDatabase.AssetPathToGUID(CanonicalAssetPath);

            if (string.IsNullOrWhiteSpace(canonicalGuid))
            {
                Debug.LogError(
                    "[Editor First] No se encontro Rifle Ammo canonico en: " +
                    CanonicalAssetPath
                );
                return;
            }

            string legacyGuid =
                AssetDatabase.AssetPathToGUID(LegacyAssetPath);

            if (string.IsNullOrWhiteSpace(legacyGuid))
                legacyGuid = LegacyGuid;

            int changedFiles = ReplaceSerializedReferences(
                legacyGuid,
                canonicalGuid
            );

            bool deletedLegacy = false;
            if (AssetDatabase.LoadMainAssetAtPath(LegacyAssetPath) != null)
            {
                deletedLegacy = AssetDatabase.DeleteAsset(LegacyAssetPath);
            }
            else if (File.Exists(LegacyAssetPath))
            {
                File.Delete(LegacyAssetPath);

                string meta = LegacyAssetPath + ".meta";
                if (File.Exists(meta))
                    File.Delete(meta);

                deletedLegacy = true;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            int remaining = CountSerializedReferences(LegacyGuid);

            if (remaining > 0)
            {
                Debug.LogWarning(
                    "[Editor First] La migracion 5.56 termino, pero aun existen " +
                    remaining + " archivo(s) con el GUID legado."
                );
                return;
            }

            Debug.Log(
                "[Editor First] 5.56 legado eliminado. " +
                changedFiles + " archivo(s) migrados a Rifle Ammo" +
                (deletedLegacy ? "; asset Item_556Ammo eliminado." : ".")
            );
        }

        private static int ReplaceSerializedReferences(
            string oldGuid,
            string newGuid)
        {
            if (string.IsNullOrWhiteSpace(oldGuid) ||
                string.IsNullOrWhiteSpace(newGuid) ||
                string.Equals(oldGuid, newGuid, StringComparison.Ordinal))
            {
                return 0;
            }

            int changed = 0;

            foreach (string path in EnumerateSerializedAssetFiles())
            {
                string normalized = path.Replace('\\', '/');

                if (string.Equals(
                        normalized,
                        LegacyAssetPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string text;
                try
                {
                    text = File.ReadAllText(path);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[Editor First] No se pudo revisar " + normalized +
                        ": " + exception.Message
                    );
                    continue;
                }

                if (text.IndexOf(oldGuid, StringComparison.Ordinal) < 0)
                    continue;

                string migrated = text.Replace(oldGuid, newGuid);

                try
                {
                    File.WriteAllText(path, migrated);
                    changed++;
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        "[Editor First] No se pudo migrar " + normalized +
                        ": " + exception.Message
                    );
                }
            }

            return changed;
        }

        private static int CountSerializedReferences(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return 0;

            int count = 0;

            foreach (string path in EnumerateSerializedAssetFiles())
            {
                string normalized = path.Replace('\\', '/');

                if (string.Equals(
                        normalized,
                        LegacyAssetPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    string text = File.ReadAllText(path);
                    if (text.IndexOf(guid, StringComparison.Ordinal) >= 0)
                        count++;
                }
                catch
                {
                }
            }

            return count;
        }

        private static IEnumerable<string> EnumerateSerializedAssetFiles()
        {
            if (!Directory.Exists("Assets"))
                yield break;

            foreach (
                string path in Directory.EnumerateFiles(
                    "Assets",
                    "*.*",
                    SearchOption.AllDirectories
                ))
            {
                if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string extension = Path.GetExtension(path);
                if (SerializedExtensions.Contains(extension))
                    yield return path;
            }
        }
    }
}
