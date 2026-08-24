using ROS.Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    [InitializeOnLoad]
    public static class EditorFirstWeaponEffectsMaterializer
    {
        private const string MaterialPath =
            "Assets/_Game/Resources/EditorFirst/WeaponTracer.mat";

        static EditorFirstWeaponEffectsMaterializer()
        {
            EditorApplication.delayCall += EnsureEditableWeaponEffects;
        }

        [MenuItem("Rules Of Survival/Editor First/Ensure Editable Weapon Effects")]
        public static void EnsureEditableWeaponEffects()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            EditorFirstPresentationBuilder.EnsureMaterialized();
            Material defaultTracerMaterial = EnsureTracerMaterial();
            if (defaultTracerMaterial == null)
                return;

            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets/_Game/Prefabs" }
            );

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;

                WeaponEffects[] effects =
                    root.GetComponentsInChildren<WeaponEffects>(true);

                for (int e = 0; e < effects.Length; e++)
                {
                    WeaponEffects effect = effects[e];
                    if (effect == null ||
                        PrefabUtility.IsPartOfPrefabInstance(effect.gameObject))
                    {
                        continue;
                    }

                    SerializedObject serialized = new SerializedObject(effect);
                    SerializedProperty tracerProperty =
                        serialized.FindProperty("tracer");
                    SerializedProperty materialProperty =
                        serialized.FindProperty("tracerMaterial");

                    LineRenderer tracer = tracerProperty != null
                        ? tracerProperty.objectReferenceValue as LineRenderer
                        : null;

                    if (tracer == null)
                    {
                        Transform existing = Find(effect.transform, "Tracer") ??
                                             Find(effect.transform, "RuntimeTracer");
                        if (existing != null)
                            tracer = existing.GetComponent<LineRenderer>();
                    }

                    bool createdTracer = false;
                    bool tracerChanged = false;

                    if (tracer == null)
                    {
                        GameObject tracerObject = new GameObject("Tracer");
                        tracerObject.transform.SetParent(effect.transform, false);
                        tracer = tracerObject.AddComponent<LineRenderer>();
                        createdTracer = true;
                        tracerChanged = true;
                        changed = true;
                    }
                    else if (tracer.gameObject.name == "RuntimeTracer")
                    {
                        tracer.gameObject.name = "Tracer";
                        tracerChanged = true;
                        changed = true;
                    }

                    Material configuredMaterial = materialProperty != null
                        ? materialProperty.objectReferenceValue as Material
                        : null;

                    if (createdTracer)
                    {
                        tracer.useWorldSpace = true;
                        tracer.positionCount = 2;
                        tracer.startWidth = 0.012f;
                        tracer.endWidth = 0.0024f;
                        tracer.numCapVertices = 2;
                        tracer.shadowCastingMode =
                            UnityEngine.Rendering.ShadowCastingMode.Off;
                        tracer.receiveShadows = false;
                        tracer.enabled = false;
                        tracer.sharedMaterial = configuredMaterial != null
                            ? configuredMaterial
                            : defaultTracerMaterial;
                    }
                    else if (tracer.sharedMaterial == null)
                    {
                        tracer.sharedMaterial = configuredMaterial != null
                            ? configuredMaterial
                            : defaultTracerMaterial;
                        tracerChanged = true;
                        changed = true;
                    }

                    if (tracerProperty != null &&
                        tracerProperty.objectReferenceValue == null)
                    {
                        tracerProperty.objectReferenceValue = tracer;
                        changed = true;
                    }

                    if (materialProperty != null &&
                        materialProperty.objectReferenceValue == null)
                    {
                        materialProperty.objectReferenceValue =
                            tracer.sharedMaterial != null
                                ? tracer.sharedMaterial
                                : defaultTracerMaterial;
                        changed = true;
                    }

                    if (tracerChanged)
                        EditorUtility.SetDirty(tracer);

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        private static Material EnsureTracerMaterial()
        {
            Material existing =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            Material material = new Material(shader)
            {
                name = "WeaponTracer",
                color = new Color(1f, 0.85f, 0.35f, 1f)
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(1f, 0.85f, 0.35f, 1f)
                );
            }

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static Transform Find(Transform root, string objectName)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName)
                    return all[i];
            }
            return null;
        }
    }
}
