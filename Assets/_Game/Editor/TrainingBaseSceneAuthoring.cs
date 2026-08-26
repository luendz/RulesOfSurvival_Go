using System;
using System.Collections.Generic;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Autoría automática de 09_TrainingBase: muestra el entorno en Scene View,
    /// garantiza el Player_Prototype y coloca todos los InventoryItemDefinition
    /// existentes como LootPickup interactivos para pruebas.
    /// </summary>
    [InitializeOnLoad]
    public static class TrainingBaseSceneAuthoring
    {
        private const string ScenePath = "Assets/_Game/Scenes/09_TrainingBase.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player_Prototype.prefab";
        private const string LootRootName = "__TrainingBase_AllLoot";

        static TrainingBaseSceneAuthoring()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += EnsureActiveScene;
        }

        [MenuItem("Rules Of Survival/Training Base/Recrear escena completa")]
        public static void RebuildTrainingBase()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsTrainingBase(scene))
            {
                if (!System.IO.File.Exists(ScenePath))
                {
                    Debug.LogWarning("No existe la escena 09_TrainingBase.");
                    return;
                }
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            DestroyGenerated(LootRootName);
            TrainingBaseRuntimeBootstrap.BuildForActiveScene(true);
            EnsurePlayer(scene);
            BuildAllLoot(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log("Training Base recreado: entorno, Player_Prototype y catálogo completo de loot listos para probar.");
        }

        [MenuItem("Rules Of Survival/Training Base/Guardar escena materializada")]
        public static void SaveMaterializedTrainingBase()
        {
            RebuildTrainingBase();
            Scene scene = SceneManager.GetActiveScene();
            if (IsTrainingBase(scene))
            {
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("09_TrainingBase guardada con la geometría y los loots materializados.");
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!IsTrainingBase(scene)) return;
            EditorApplication.delayCall += EnsureActiveScene;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                EnsureActiveScene();
        }

        private static void EnsureActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!IsTrainingBase(scene)) return;

            if (GameObject.Find(TrainingBaseRuntimeBootstrap.GeneratedRootName) == null)
                TrainingBaseRuntimeBootstrap.BuildForActiveScene(false);

            EnsurePlayer(scene);
            if (FindSceneObject(LootRootName, scene) == null)
                BuildAllLoot(scene);

            SceneView.RepaintAll();
        }

        private static bool IsTrainingBase(Scene scene)
        {
            return scene.IsValid() &&
                   (scene.name == TrainingBaseRuntimeBootstrap.SceneName || scene.path == ScenePath);
        }

        private static void EnsurePlayer(Scene scene)
        {
            GameObject player = FindSceneObject("Player_Prototype", scene);
            if (player == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (prefab != null)
                {
                    player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                }
            }

            if (player == null)
            {
                Debug.LogWarning("Training Base: no se pudo localizar Player_Prototype.prefab.");
                return;
            }

            player.SetActive(true);
            player.transform.SetPositionAndRotation(TrainingBaseRuntimeBootstrap.PlayerSpawn, Quaternion.identity);
        }

        private static void BuildAllLoot(Scene scene)
        {
            DestroyGenerated(LootRootName);

            GameObject root = new GameObject(LootRootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            List<InventoryItemDefinition> items = LoadAllLootDefinitions();
            items.Sort((a, b) =>
            {
                int type = string.Compare(a.itemType.ToString(), b.itemType.ToString(), StringComparison.Ordinal);
                return type != 0 ? type : string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
            });

            const int columns = 7;
            const float xSpacing = 3.3f;
            const float zSpacing = 4.3f;
            Vector3 origin = new Vector3(-10f, 0.35f, -48f);

            string previousType = null;
            int row = 0;
            int column = 0;
            int total = 0;

            foreach (InventoryItemDefinition item in items)
            {
                if (item == null) continue;
                string typeName = item.itemType.ToString();
                if (previousType != null && previousType != typeName)
                {
                    row++;
                    column = 0;
                }
                previousType = typeName;

                if (column >= columns)
                {
                    row++;
                    column = 0;
                }

                Vector3 position = origin + new Vector3(column * xSpacing, 0f, row * zSpacing);
                int amount = item.IsEquippable ? 1 : Mathf.Clamp(item.maxStack, 1, 30);

                LootPickup pickup = LootPickup.SpawnRuntime(item, amount, position, null, 0f);
                if (pickup != null)
                {
                    pickup.name = string.Format("Loot_{0}_{1}", typeName, item.displayName);
                    pickup.transform.SetParent(root.transform, true);
                    CreateLabel(pickup.transform, item, amount);
                    CreatePad(root.transform, position, typeName);
                    total++;
                }
                column++;
            }

            CreateSectionTitle(root.transform, new Vector3(-12.5f, 2.8f, -49f), "TODOS LOS LOOTS");
            Debug.Log(string.Format("Training Base: {0} definiciones de loot colocadas para prueba.", total));
        }

        private static List<InventoryItemDefinition> LoadAllLootDefinitions()
        {
            List<InventoryItemDefinition> result = new List<InventoryItemDefinition>();
            HashSet<InventoryItemDefinition> unique = new HashSet<InventoryItemDefinition>();

            string[] guids = AssetDatabase.FindAssets("t:InventoryItemDefinition", new[] { "Assets/_Game/Data" });
            if (guids.Length == 0)
                guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Game/Data" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                InventoryItemDefinition definition = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);
                if (definition != null && unique.Add(definition))
                    result.Add(definition);
            }
            return result;
        }

        private static void CreateLabel(Transform parent, InventoryItemDefinition item, int amount)
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.text = item.displayName + (amount > 1 ? " x" + amount : "");
            text.fontSize = 42;
            text.characterSize = 0.075f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
        }

        private static void CreateSectionTitle(Transform parent, Vector3 position, string value)
        {
            GameObject go = new GameObject("Titulo_Loot");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            TextMesh text = go.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = 60;
            text.characterSize = 0.12f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.25f);
        }

        private static void CreatePad(Transform parent, Vector3 position, string typeName)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Base_" + typeName;
            pad.transform.SetParent(parent, true);
            pad.transform.position = position + new Vector3(0f, -0.27f, 0f);
            pad.transform.localScale = new Vector3(2.7f, 0.12f, 2.7f);
            Renderer renderer = pad.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                Material material = new Material(shader);
                Color color = ColorForType(typeName);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                renderer.sharedMaterial = material;
            }
        }

        private static Color ColorForType(string type)
        {
            int hash = type != null ? type.GetHashCode() : 0;
            float h = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(h, 0.34f, 0.42f);
        }

        private static GameObject FindSceneObject(string name, Scene scene)
        {
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in all)
            {
                if (go != null && go.scene == scene && go.name == name)
                    return go;
            }
            return null;
        }

        private static void DestroyGenerated(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject go = FindSceneObject(name, scene);
            if (go != null)
                UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
