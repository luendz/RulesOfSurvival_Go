using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Asegura que DamageNumber.prefab tenga un borde real compuesto por
    /// TextMesh físicos y editables. No crea elementos visuales durante Play.
    /// Una vez creados, sus posiciones pueden ajustarse manualmente sin que
    /// este materializador las sobrescriba.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstDamageNumberMaterializer
    {
        private const string PrefabPath =
            "Assets/_Game/Resources/EditorFirst/DamageNumber.prefab";

        private static readonly Vector3[] OutlineOffsets =
        {
            new Vector3(-0.010f,  0.000f, 0.001f),
            new Vector3( 0.010f,  0.000f, 0.001f),
            new Vector3( 0.000f, -0.010f, 0.001f),
            new Vector3( 0.000f,  0.010f, 0.001f),
            new Vector3(-0.007f, -0.007f, 0.001f),
            new Vector3(-0.007f,  0.007f, 0.001f),
            new Vector3( 0.007f, -0.007f, 0.001f),
            new Vector3( 0.007f,  0.007f, 0.001f)
        };

        static EditorFirstDamageNumberMaterializer()
        {
            EditorApplication.delayCall += EnsureDamageNumberStyle;
        }

        public static void EnsureDamageNumberStyle()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstPresentationBuilder.EnsureMaterialized();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
                return;

            bool changed = false;
            try
            {
                TextMesh main = root.GetComponent<TextMesh>();
                if (main == null)
                    main = root.GetComponentInChildren<TextMesh>(true);

                if (main == null)
                {
                    Debug.LogError(
                        "[Editor First] DamageNumber.prefab no contiene un TextMesh principal."
                    );
                    return;
                }

                MeshRenderer mainRenderer = main.GetComponent<MeshRenderer>();
                if (mainRenderer != null && mainRenderer.sortingOrder != 10)
                {
                    mainRenderer.sortingOrder = 10;
                    changed = true;
                }

                Transform outlineRoot = root.transform.Find("Outline");
                if (outlineRoot == null)
                {
                    GameObject outlineObject = new GameObject("Outline");
                    outlineRoot = outlineObject.transform;
                    outlineRoot.SetParent(root.transform, false);
                    changed = true;
                }

                for (int i = 0; i < OutlineOffsets.Length; i++)
                {
                    string childName = "Outline_" + i;
                    Transform existing = outlineRoot.Find(childName);
                    if (existing != null)
                        continue;

                    GameObject child = new GameObject(childName);
                    child.transform.SetParent(outlineRoot, false);
                    child.transform.localPosition = OutlineOffsets[i];
                    child.transform.localRotation = Quaternion.identity;
                    child.transform.localScale = Vector3.one;

                    TextMesh outline = child.AddComponent<TextMesh>();
                    CopyTextSettings(main, outline);
                    outline.color = new Color(0.08f, 0.48f, 1f, 1f);

                    MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                    if (renderer != null)
                        renderer.sortingOrder = 9;

                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "[Editor First] DamageNumber preparado con borde físico editable."
                );
            }
        }

        private static void CopyTextSettings(TextMesh source, TextMesh target)
        {
            target.text = source.text;
            target.font = source.font;
            target.fontSize = source.fontSize;
            target.fontStyle = FontStyle.Bold;
            target.characterSize = source.characterSize;
            target.lineSpacing = source.lineSpacing;
            target.tabSize = source.tabSize;
            target.anchor = source.anchor;
            target.alignment = source.alignment;
            target.richText = source.richText;
            target.offsetZ = source.offsetZ;
        }
    }
}
