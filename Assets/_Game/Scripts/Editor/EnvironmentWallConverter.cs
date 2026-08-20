#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Editor
{
    public static class EnvironmentWallConverter
    {
        [MenuItem("ROS Battle Royale/03 - Convertir Placeholders a Muros")]
        public static void ConvertPlaceholders()
        {
            GameObject[] all =
                Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            List<GameObject> targets = new List<GameObject>();
            foreach (GameObject go in all)
            {
                if (go.name.Contains("EnvironmentPlaceholder"))
                    targets.Add(go);
            }

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Convertir Muros",
                    "No se encontraron objetos con 'EnvironmentPlaceholder' en la escena.",
                    "OK"
                );
                return;
            }

            // Malla de cubo reutilizable
            GameObject tempCube =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh cubeMesh =
                tempCube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempCube);

            // Material gris mate
            Material wallMat = new Material(
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard")
            );
            wallMat.color = new Color(0.55f, 0.54f, 0.52f);
            AssetDatabase.CreateAsset(
                wallMat,
                "Assets/_Game/Materials/Environment/M_Wall_Placeholder.mat"
            );

            System.Random rng = new System.Random(42);

            for (int i = 0; i < targets.Count; i++)
            {
                GameObject go = targets[i];
                Undo.RecordObject(go, "Convertir a Muro");
                Undo.RecordObject(go.transform, "Convertir a Muro");

                go.name = $"Muro_{i + 1:00}";

                // Proporciones de pared
                float largo  = (float)(rng.NextDouble() * 5f + 4f);  // 4–9
                float alto   = 2.8f;
                float grosor = 0.5f;
                bool  rotar  = rng.NextDouble() > 0.5;

                go.transform.localScale = rotar
                    ? new Vector3(grosor, alto, largo)
                    : new Vector3(largo,  alto, grosor);

                Vector3 pos = go.transform.position;
                pos.y = alto * 0.5f;
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;

                // Reemplazar malla si es cilindro
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null)
                    mf.sharedMesh = cubeMesh;

                // Aplicar material
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sharedMaterial = wallMat;

                // CapsuleCollider → BoxCollider
                CapsuleCollider cap = go.GetComponent<CapsuleCollider>();
                if (cap != null)
                {
                    Undo.DestroyObjectImmediate(cap);
                    Undo.AddComponent<BoxCollider>(go);
                }

                // Asegurar que el BoxCollider no sea trigger
                BoxCollider box = go.GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.isTrigger = false;
                    box.center    = Vector3.zero;
                    box.size      = Vector3.one;
                }

                EditorUtility.SetDirty(go);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene()
            );

            EditorUtility.DisplayDialog(
                "Convertir Muros",
                $"{targets.Count} placeholders convertidos a muros.\n" +
                "Guarda la escena con Ctrl+S.",
                "OK"
            );
        }
    }
}
#endif
