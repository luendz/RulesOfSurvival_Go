#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ROS.Game.Core;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Editor
{
    /// <summary>
    /// Herramientas reproducibles para inspeccionar y vestir Echo Valley sin
    /// depender de cambios hechos durante Play Mode.
    /// </summary>
    public static class EchoValleyWorldBuilder
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EchoValley.unity";

        private const string GeneratedRootPath =
            "EchoValley_Generated";
        private const string TreeModelPath =
            "Assets/_Game/Art/Environment/Vegetation/Oak_Tree_01/Oak_Tree_01.fbx";
        private const string SmallHouseModelPath =
            "Assets/_Game/Art/Environment/Buildings/House_Small_01/House_Small_01.fbx";
        private const string LargeHouseModelPath =
            "Assets/_Game/Art/Environment/Buildings/House_Large_01/House_Large_01.fbx";
        private const string LootPickupPrefabPath =
            "Assets/_Game/Prefabs/LootPickup_Prototype.prefab";
        private const string PodiumMaterialPath =
            "Assets/_Game/Materials/Environment/M_EchoValley_BladePodium.mat";

        private static readonly Vector3 TreeLocalPosition =
            new Vector3(0f, 2.87f, 0f);
        private static readonly Quaternion TreeLocalRotation =
            Quaternion.Euler(-90f, 16f, 0f);
        private static readonly Vector3 TreeLocalScale =
            new Vector3(300f, 300f, 300f);
        private const StaticEditorFlags TreeStaticFlags =
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        private static readonly string[] ModelPaths =
        {
            "Assets/_Game/Art/Environment/Vegetation/Oak_Tree_01/Oak_Tree_01.fbx",
            "Assets/_Game/Art/Environment/Buildings/House_Small_01/House_Small_01.fbx",
            "Assets/_Game/Art/Environment/Buildings/House_Large_01/House_Large_01.fbx"
        };

        [MenuItem(
            "Rules Of Survival/Tools/World/Aplicar arte BR a Echo Valley"
        )]
        public static void Build()
        {
            BuildInternal();
        }

        public static void BuildBatch()
        {
            BuildInternal();
        }

        [MenuItem(
            "Rules Of Survival/Tools/World/Aplicar configuración de árboles"
        )]
        public static void ApplyTreeConfiguration()
        {
            Scene scene = GetOrOpenScene();
            GameObject echoRoot = scene.GetRootGameObjects().FirstOrDefault(
                root => root.name == "EchoValley"
            );
            Transform generated = echoRoot != null
                ? echoRoot.transform.Find(GeneratedRootPath)
                : null;
            if (generated == null)
            {
                throw new InvalidOperationException(
                    "Echo Valley no contiene su jerarquía generada."
                );
            }

            GameObject treeModel = LoadRequired<GameObject>(TreeModelPath);
            int configured = ConfigureExistingTrees(generated, treeModel);
            if (configured != 72)
            {
                throw new InvalidOperationException(
                    $"Se esperaban 72 árboles y se configuraron {configured}."
                );
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"No se pudo guardar {ScenePath}."
                );
            }

            Debug.Log(
                "[EchoValleyWorldBuilder] 72 árboles configurados: " +
                "posición=(0, 2.87, 0), rotación=(-90, 16, 0), " +
                "escala=(300, 300, 300) y Static."
            );
        }

        private static void BuildInternal()
        {
            WeaponFamilyLootBuilder.BuildWeaponFamilyLoot();

            Scene scene = GetOrOpenScene();
            GameObject echoRoot = scene.GetRootGameObjects().FirstOrDefault(
                root => root.name == "EchoValley"
            );
            if (echoRoot == null)
            {
                throw new InvalidOperationException(
                    "Echo Valley no contiene la raíz 'EchoValley'."
                );
            }

            Transform generated = echoRoot.transform.Find(GeneratedRootPath);
            if (generated == null)
            {
                throw new InvalidOperationException(
                    "Echo Valley no contiene su jerarquía generada."
                );
            }

            GameObject treeModel = LoadRequired<GameObject>(TreeModelPath);
            GameObject smallHouse = LoadRequired<GameObject>(
                SmallHouseModelPath
            );
            GameObject largeHouse = LoadRequired<GameObject>(
                LargeHouseModelPath
            );

            ValidateTriangleBudget(treeModel, 120000);
            ValidateTriangleBudget(smallHouse, 200000);
            ValidateTriangleBudget(largeHouse, 200000);

            int trees = ReplaceTrees(generated, treeModel);
            int houses = ReplaceHouses(
                generated,
                smallHouse,
                largeHouse
            );
            BuildBladeLigerExhibit(scene, generated);
            int loot = BuildStaticLoot(scene);

            ValidateBuiltWorld(scene, trees, houses, loot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"No se pudo guardar {ScenePath}."
                );
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[EchoValleyWorldBuilder] Arte BR aplicado y guardado. " +
                $"Árboles={trees}, casas={houses}, loot={loot}, " +
                "Blade Liger en exhibición y 9 armas integradas."
            );
        }

        private static Scene GetOrOpenScene()
        {
            Scene loaded = SceneManager.GetSceneByPath(ScenePath);
            if (loaded.IsValid() && loaded.isLoaded)
            {
                return loaded;
            }

            return EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single
            );
        }

        private static int ReplaceTrees(
            Transform generated,
            GameObject treeModel
        )
        {
            Transform nature = generated.Find("07_Nature");
            if (nature == null)
            {
                throw new InvalidOperationException(
                    "Falta EchoValley_Generated/07_Nature."
                );
            }

            List<Transform> trees = new List<Transform>();
            for (int i = 0; i < nature.childCount; i++)
            {
                Transform child = nature.GetChild(i);
                if (child.name.StartsWith("Tree_", StringComparison.Ordinal))
                {
                    trees.Add(child);
                }
            }

            for (int i = 0; i < trees.Count; i++)
            {
                Transform tree = trees[i];
                ClearChildren(tree);
                RemoveComponents<Collider>(tree.gameObject);

                GameObject instance = InstantiatePrefab(treeModel, tree);
                instance.name = "Oak_Tree_01";
                ConfigureTreeTransform(instance.transform);

                CapsuleCollider trunk =
                    tree.gameObject.AddComponent<CapsuleCollider>();
                trunk.radius = 0.48f;
                trunk.height = 6.1f;
                trunk.center = Vector3.up * trunk.height * 0.5f;
                SetTreeStaticRecursively(tree.gameObject);
            }

            return trees.Count;
        }

        private static int ConfigureExistingTrees(
            Transform generated,
            GameObject treeModel
        )
        {
            Transform nature = generated.Find("07_Nature");
            if (nature == null)
            {
                throw new InvalidOperationException(
                    "Falta EchoValley_Generated/07_Nature."
                );
            }

            int configured = 0;
            for (int i = 0; i < nature.childCount; i++)
            {
                Transform tree = nature.GetChild(i);
                if (!tree.name.StartsWith("Tree_", StringComparison.Ordinal))
                {
                    continue;
                }

                Transform instance = tree.Find("Oak_Tree_01");
                if (instance == null)
                {
                    ClearChildren(tree);
                    GameObject created = InstantiatePrefab(treeModel, tree);
                    created.name = "Oak_Tree_01";
                    instance = created.transform;
                }

                ConfigureTreeTransform(instance);
                SetTreeStaticRecursively(tree.gameObject);
                configured++;
            }

            return configured;
        }

        private static void ConfigureTreeTransform(Transform tree)
        {
            tree.localPosition = TreeLocalPosition;
            tree.localRotation = TreeLocalRotation;
            tree.localScale = TreeLocalScale;
        }

        private static int ReplaceHouses(
            Transform generated,
            GameObject smallHouse,
            GameObject largeHouse
        )
        {
            int replaced = 0;
            Transform core = generated.Find("02_Core_Compound");
            Transform twoStory = core != null
                ? core.Find("Two_Story_House")
                : null;
            if (twoStory == null)
            {
                throw new InvalidOperationException(
                    "Falta la casa de dos pisos de Echo Valley."
                );
            }

            ReplaceHouseModel(
                twoStory,
                largeHouse,
                new Vector2(14.5f, 11.5f),
                "House_Large_01"
            );
            replaced++;

            Transform outer = generated.Find("03_Surrounding_Houses");
            if (outer == null)
            {
                throw new InvalidOperationException(
                    "Falta el grupo de viviendas periféricas."
                );
            }

            List<Transform> houses = new List<Transform>();
            for (int i = 0; i < outer.childCount; i++)
            {
                Transform child = outer.GetChild(i);
                if (child.name.StartsWith(
                    "Outer_House_",
                    StringComparison.Ordinal
                ))
                {
                    houses.Add(child);
                }
            }

            houses.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < houses.Count; i++)
            {
                bool useLarge = i % 3 == 1;
                ReplaceHouseModel(
                    houses[i],
                    useLarge ? largeHouse : smallHouse,
                    useLarge
                        ? new Vector2(12f, 9.8f)
                        : new Vector2(10.5f, 8.3f),
                    useLarge ? "House_Large_01" : "House_Small_01"
                );
                replaced++;
            }

            return replaced;
        }

        private static void ReplaceHouseModel(
            Transform house,
            GameObject model,
            Vector2 footprint,
            string instanceName
        )
        {
            ClearChildren(house);
            GameObject instance = InstantiatePrefab(model, house);
            instance.name = instanceName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            Bounds sourceBounds = CalculateRendererBounds(instance);
            float scale = Mathf.Min(
                footprint.x / Mathf.Max(0.01f, sourceBounds.size.x),
                footprint.y / Mathf.Max(0.01f, sourceBounds.size.z)
            );
            instance.transform.localScale = Vector3.one * scale;
            AlignBottomToY(instance, house.position.y);
            AddHouseCompoundColliders(
                house,
                footprint,
                CalculateRendererBounds(instance).size.y
            );
            SetStaticRecursively(house.gameObject);
        }

        private static void BuildBladeLigerExhibit(
            Scene scene,
            Transform generated
        )
        {
            Transform previous = generated.Find("09_Blade_Liger_Exhibit");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject groupObject = new GameObject(
                "09_Blade_Liger_Exhibit"
            );
            groupObject.transform.SetParent(generated, false);
            Transform group = groupObject.transform;

            Terrain terrain = FindInScene<Terrain>(scene).FirstOrDefault();
            Vector3 center = new Vector3(0f, 0f, 8f);
            center.y = terrain != null
                ? terrain.SampleHeight(center) +
                  terrain.transform.position.y
                : 0f;

            Material podiumMaterial = GetOrCreatePodiumMaterial();
            CreatePodiumPrimitive(
                group,
                "Podium_Base",
                PrimitiveType.Cylinder,
                center + Vector3.up * 0.25f,
                new Vector3(9f, 0.25f, 9f),
                podiumMaterial
            );
            CreatePodiumPrimitive(
                group,
                "Podium_Upper",
                PrimitiveType.Cylinder,
                center + Vector3.up * 0.67f,
                new Vector3(7.6f, 0.18f, 7.6f),
                podiumMaterial
            );

            GameObject blade = scene.GetRootGameObjects().FirstOrDefault(
                root => root.name == "Blade_Liger"
            );
            if (blade == null)
            {
                throw new InvalidOperationException(
                    "No se encontró Blade_Liger en Echo Valley."
                );
            }

            blade.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            Bounds bladeBounds = CalculateRendererBounds(blade);
            Vector3 desired = new Vector3(
                center.x,
                center.y + 0.86f,
                center.z
            );
            Vector3 anchor = new Vector3(
                bladeBounds.center.x,
                bladeBounds.min.y,
                bladeBounds.center.z
            );
            blade.transform.position += desired - anchor;

            CreateExhibitPlaque(group, center, podiumMaterial);
            CreateExhibitLights(group, center);
            SetStaticRecursively(groupObject);
        }

        private static void CreateExhibitPlaque(
            Transform parent,
            Vector3 center,
            Material material
        )
        {
            GameObject plaque = CreatePodiumPrimitive(
                parent,
                "Blade_Liger_Plaque",
                PrimitiveType.Cube,
                center + new Vector3(0f, 1.25f, -8.9f),
                new Vector3(4.8f, 1.15f, 0.25f),
                material
            );
            plaque.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            GameObject label = new GameObject("Label_Blade_Liger");
            label.transform.SetParent(parent, true);
            label.transform.position =
                center + new Vector3(0f, 1.28f, -9.05f);
            label.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = "BLADE LIGER";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = 0.08f;
            text.color = new Color(0.72f, 0.9f, 1f);
        }

        private static void CreateExhibitLights(
            Transform parent,
            Vector3 center
        )
        {
            Vector3[] offsets =
            {
                new Vector3(-6.5f, 5.5f, -5.8f),
                new Vector3(6.5f, 5.5f, -5.8f),
                new Vector3(-6.5f, 5.5f, 5.8f),
                new Vector3(6.5f, 5.5f, 5.8f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject lightObject = new GameObject(
                    $"Exhibit_Spot_{i + 1:00}"
                );
                lightObject.transform.SetParent(parent, true);
                lightObject.transform.position = center + offsets[i];
                lightObject.transform.rotation = Quaternion.LookRotation(
                    center + Vector3.up * 3.5f -
                    lightObject.transform.position
                );
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = new Color(0.58f, 0.78f, 1f);
                light.intensity = 2400f;
                light.range = 24f;
                light.spotAngle = 46f;
                light.shadows = LightShadows.Soft;
            }
        }

        private static GameObject CreatePodiumPrimitive(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material
        )
        {
            GameObject result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent, true);
            result.transform.position = position;
            result.transform.localScale = scale;
            Renderer renderer = result.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return result;
        }

        private static Material GetOrCreatePodiumMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                PodiumMaterialPath
            );
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = "M_EchoValley_BladePodium"
            };
            material.color = new Color(0.1f, 0.14f, 0.18f);
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0.55f);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.48f);
            }
            AssetDatabase.CreateAsset(material, PodiumMaterialPath);
            return material;
        }

        private static int BuildStaticLoot(Scene scene)
        {
            GameObject lootRoot = scene.GetRootGameObjects().FirstOrDefault(
                root => root.name == "Loot"
            );
            if (lootRoot == null)
            {
                throw new InvalidOperationException(
                    "Echo Valley no contiene la raíz de loot."
                );
            }

            ClearChildren(lootRoot.transform);
            GameObject authoredRoot = new GameObject(
                "EchoValley_StaticLoot"
            );
            authoredRoot.transform.SetParent(lootRoot.transform, false);

            GameObject pickupPrefab = LoadRequired<GameObject>(
                LootPickupPrefabPath
            );
            Dictionary<string, InventoryItemDefinition> items =
                LoadLootItems();

            InventoryItemDefinition[] low = ResolveItems(
                items,
                "Item_PistolAmmo", "Item_SMGAmmo", "Item_SGAmmo",
                "Item_Bandage", "Item_MP7", "Item_Thompson",
                "Item_DesertEagle", "Item_M870", "Item_Backpack1",
                "Item_Helmet1", "Item_Vest1"
            );
            InventoryItemDefinition[] medium = ResolveItems(
                items,
                "Item_RifleAmmo", "Item_SMGAmmo", "Item_SGAmmo",
                "Item_Medkit", "Item_M4A1", "Item_AKM",
                "Item_M1887", "Item_M14EBR", "Item_Backpack2",
                "Item_Helmet2", "Item_Vest2", "Item_RedDot",
                "Item_FragGrenade"
            );
            InventoryItemDefinition[] high = ResolveItems(
                items,
                "Item_SRAmmo", "Item_RifleAmmo", "Item_AWM",
                "Item_M14EBR", "Item_M4A1", "Item_AKM",
                "Item_Backpack3", "Item_Helmet3", "Item_Vest3",
                "Item_Medkit"
            );

            Dictionary<string, string> signatureItems =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["LootHigh_Apartment_Ground"] = "Item_M14EBR",
                    ["LootHigh_Apartment_F2"] = "Item_AKM",
                    ["WarehouseLoot_2_A"] = "Item_M870",
                    ["WarehouseLoot_3_A"] = "Item_Thompson"
                };

            List<EchoValleySpawnMarker> markers =
                FindInScene<EchoValleySpawnMarker>(scene)
                    .Where(marker =>
                        marker.SpawnType == EchoValleySpawnType.LootLow ||
                        marker.SpawnType == EchoValleySpawnType.LootMedium ||
                        marker.SpawnType == EchoValleySpawnType.LootHigh
                    )
                    .OrderBy(marker => marker.name)
                    .ToList();

            int created = 0;
            foreach (EchoValleySpawnMarker marker in markers)
            {
                InventoryItemDefinition[] pool;
                int count;
                switch (marker.SpawnType)
                {
                    case EchoValleySpawnType.LootHigh:
                        pool = high;
                        count = 4;
                        break;
                    case EchoValleySpawnType.LootMedium:
                        pool = medium;
                        count = 3;
                        break;
                    default:
                        pool = low;
                        count = 2;
                        break;
                }

                for (int i = 0; i < count; i++)
                {
                    InventoryItemDefinition item;
                    if (i == 0 &&
                        signatureItems.TryGetValue(
                            marker.name,
                            out string signature
                        ))
                    {
                        item = items[signature];
                    }
                    else
                    {
                        int index = Math.Abs(
                            StableHash(marker.name + "_" + i)
                        ) % pool.Length;
                        item = pool[index];
                    }

                    float angle =
                        (i / (float)count) * Mathf.PI * 2f +
                        (StableHash(marker.name) % 31) * Mathf.Deg2Rad;
                    float distance = i == 0
                        ? 0f
                        : Mathf.Max(0.72f, marker.Radius * 0.9f);
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * distance,
                        0f,
                        Mathf.Sin(angle) * distance
                    );
                    CreateAuthoredPickup(
                        pickupPrefab,
                        authoredRoot.transform,
                        item,
                        marker.transform.position + offset,
                        created
                    );
                    created++;
                }
            }

            return created;
        }

        private static void CreateAuthoredPickup(
            GameObject pickupPrefab,
            Transform parent,
            InventoryItemDefinition item,
            Vector3 position,
            int index
        )
        {
            GameObject pickupObject = InstantiatePrefab(
                pickupPrefab,
                parent
            );
            pickupObject.name =
                $"Loot_{index + 1:000}_{item.displayName}";
            pickupObject.transform.position = position;
            pickupObject.transform.rotation = Quaternion.Euler(
                0f,
                StableHash(pickupObject.name) % 360,
                0f
            );

            LootPickup pickup = pickupObject.GetComponent<LootPickup>();
            if (pickup == null)
            {
                throw new InvalidOperationException(
                    $"{LootPickupPrefabPath} no contiene LootPickup."
                );
            }

            GameObject visualRootObject = new GameObject("VisualRoot");
            visualRootObject.transform.SetParent(
                pickupObject.transform,
                false
            );
            GameObject visual = InstantiatePrefab(
                item.worldModel,
                visualRootObject.transform
            );
            visual.name = $"Visual_{item.displayName}";
            visual.transform.localPosition = item.worldOffset;
            visual.transform.localRotation = Quaternion.Euler(
                item.worldEulerAngles
            );
            visual.transform.localScale = item.worldScale;
            DisableVisualPhysics(visual);

            int amount = item.itemType == ItemType.Ammo
                ? 30
                : item.itemType == ItemType.Healing
                    ? 2
                    : 1;
            pickup.ConfigureAuthored(
                item,
                amount,
                visualRootObject.transform,
                visual
            );
            EditorUtility.SetDirty(pickup);
        }

        private static Dictionary<string, InventoryItemDefinition>
            LoadLootItems()
        {
            Dictionary<string, InventoryItemDefinition> result =
                new Dictionary<string, InventoryItemDefinition>(
                    StringComparer.Ordinal
                );
            string[] guids = AssetDatabase.FindAssets(
                "t:InventoryItemDefinition",
                new[] { "Assets/_Game/Data" }
            );
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                InventoryItemDefinition item =
                    AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(
                        path
                    );
                if (item != null)
                {
                    result[item.name] = item;
                }
            }

            return result;
        }

        private static InventoryItemDefinition[] ResolveItems(
            IReadOnlyDictionary<string, InventoryItemDefinition> items,
            params string[] names
        )
        {
            List<InventoryItemDefinition> result =
                new List<InventoryItemDefinition>();
            foreach (string name in names)
            {
                if (!items.TryGetValue(name, out InventoryItemDefinition item))
                {
                    throw new InvalidOperationException(
                        $"Falta el item de loot {name}."
                    );
                }
                if (item.worldModel == null)
                {
                    throw new InvalidOperationException(
                        $"{name} no tiene worldModel asignado."
                    );
                }
                result.Add(item);
            }

            return result.ToArray();
        }

        private static void ValidateBuiltWorld(
            Scene scene,
            int trees,
            int houses,
            int loot
        )
        {
            if (trees != 72)
            {
                throw new InvalidOperationException(
                    $"Se esperaban 72 árboles y se reemplazaron {trees}."
                );
            }
            ValidateTreeConfiguration(scene);
            if (houses != 9)
            {
                throw new InvalidOperationException(
                    $"Se esperaban 9 casas y se reemplazaron {houses}."
                );
            }
            if (loot < 50)
            {
                throw new InvalidOperationException(
                    $"La distribución de loot es insuficiente: {loot}."
                );
            }

            string[] requiredWeapons =
            {
                "Item_AKM", "Item_M14EBR", "Item_M870", "Item_Thompson"
            };
            HashSet<string> present = new HashSet<string>(
                FindInScene<LootPickup>(scene)
                    .Where(pickup => pickup.Item != null)
                    .Select(pickup => pickup.Item.name),
                StringComparer.Ordinal
            );
            foreach (string weapon in requiredWeapons)
            {
                if (!present.Contains(weapon))
                {
                    throw new InvalidOperationException(
                        $"El loot no contiene el arma nueva {weapon}."
                    );
                }
            }
        }

        private static void ValidateTreeConfiguration(Scene scene)
        {
            List<Transform> treeInstances = FindInScene<Transform>(scene)
                .Where(tree => tree.name == "Oak_Tree_01")
                .ToList();
            if (treeInstances.Count != 72)
            {
                throw new InvalidOperationException(
                    $"Se esperaban 72 instancias Oak_Tree_01 y se " +
                    $"encontraron {treeInstances.Count}."
                );
            }

            foreach (Transform tree in treeInstances)
            {
                bool validPosition =
                    (tree.localPosition - TreeLocalPosition).sqrMagnitude < 0.0001f;
                bool validRotation =
                    Quaternion.Angle(tree.localRotation, TreeLocalRotation) < 0.01f;
                bool validScale =
                    (tree.localScale - TreeLocalScale).sqrMagnitude < 0.0001f;
                bool validStatic =
                    GameObjectUtility.GetStaticEditorFlags(tree.gameObject) ==
                    TreeStaticFlags;

                if (!validPosition || !validRotation ||
                    !validScale || !validStatic)
                {
                    throw new InvalidOperationException(
                        $"{tree.name} no tiene la configuración autoral " +
                        "requerida para Echo Valley."
                    );
                }
            }
        }

        private static T LoadRequired<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"No se encontró el recurso requerido: {path}"
                );
            }

            return asset;
        }

        private static GameObject InstantiatePrefab(
            GameObject prefab,
            Transform parent
        )
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                parent
            ) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"No se pudo instanciar el prefab/modelo {prefab.name}."
                );
            }

            return instance;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(
                    parent.GetChild(i).gameObject
                );
            }
        }

        private static void RemoveComponents<T>(GameObject gameObject)
            where T : Component
        {
            foreach (T component in gameObject.GetComponents<T>())
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static Bounds CalculateRendererBounds(GameObject gameObject)
        {
            Renderer[] renderers =
                gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{gameObject.name} no contiene renderers."
                );
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void ValidateTriangleBudget(
            GameObject model,
            ulong maximumTriangles
        )
        {
            ulong triangles = 0;
            foreach (MeshFilter filter in
                     model.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                for (int subMesh = 0;
                     subMesh < mesh.subMeshCount;
                     subMesh++)
                {
                    if (mesh.GetTopology(subMesh) == MeshTopology.Triangles)
                    {
                        triangles += mesh.GetIndexCount(subMesh) / 3;
                    }
                }
            }

            if (triangles > maximumTriangles)
            {
                throw new InvalidOperationException(
                    $"{model.name} contiene {triangles:N0} triángulos. " +
                    $"El máximo permitido para Echo Valley es " +
                    $"{maximumTriangles:N0}. Optimiza el FBX antes de usarlo."
                );
            }
        }

        private static void AlignBottomToY(
            GameObject gameObject,
            float targetY
        )
        {
            Bounds bounds = CalculateRendererBounds(gameObject);
            gameObject.transform.position +=
                Vector3.up * (targetY - bounds.min.y);
        }

        private static void AddHouseCompoundColliders(
            Transform house,
            Vector2 footprint,
            float height
        )
        {
            RemoveComponents<Collider>(house.gameObject);

            const float wallThickness = 0.35f;
            const float floorThickness = 0.2f;
            float safeHeight = Mathf.Max(2.8f, height);
            float doorwayWidth = Mathf.Min(2.2f, footprint.x * 0.28f);
            float frontSegmentWidth =
                Mathf.Max(0.5f, (footprint.x - doorwayWidth) * 0.5f);
            float frontOffset = (doorwayWidth + frontSegmentWidth) * 0.5f;

            Transform colliders = new GameObject("House_Colliders").transform;
            colliders.SetParent(house, false);

            CreateBoxCollider(
                colliders,
                "Floor",
                new Vector3(0f, -floorThickness * 0.5f, 0f),
                new Vector3(footprint.x, floorThickness, footprint.y)
            );
            CreateBoxCollider(
                colliders,
                "Wall_Back",
                new Vector3(0f, safeHeight * 0.5f, -footprint.y * 0.5f),
                new Vector3(footprint.x, safeHeight, wallThickness)
            );
            CreateBoxCollider(
                colliders,
                "Wall_Left",
                new Vector3(-footprint.x * 0.5f, safeHeight * 0.5f, 0f),
                new Vector3(wallThickness, safeHeight, footprint.y)
            );
            CreateBoxCollider(
                colliders,
                "Wall_Right",
                new Vector3(footprint.x * 0.5f, safeHeight * 0.5f, 0f),
                new Vector3(wallThickness, safeHeight, footprint.y)
            );
            CreateBoxCollider(
                colliders,
                "Wall_Front_Left",
                new Vector3(-frontOffset, safeHeight * 0.5f, footprint.y * 0.5f),
                new Vector3(frontSegmentWidth, safeHeight, wallThickness)
            );
            CreateBoxCollider(
                colliders,
                "Wall_Front_Right",
                new Vector3(frontOffset, safeHeight * 0.5f, footprint.y * 0.5f),
                new Vector3(frontSegmentWidth, safeHeight, wallThickness)
            );
        }

        private static void CreateBoxCollider(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size
        )
        {
            GameObject colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private static void SetStaticRecursively(GameObject root)
        {
            StaticEditorFlags flags =
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.ReflectionProbeStatic;
            foreach (Transform transform in
                     root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(
                    transform.gameObject,
                    flags
                );
            }
        }

        private static void SetTreeStaticRecursively(GameObject root)
        {
            foreach (Transform transform in
                     root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(
                    transform.gameObject,
                    TreeStaticFlags
                );
            }
        }

        private static void DisableVisualPhysics(GameObject visual)
        {
            foreach (Collider collider in
                     visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
            foreach (Rigidbody body in
                     visual.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash & 0x7fffffff;
            }
        }

        [MenuItem(
            "Rules Of Survival/Tools/Scenes/Auditar mundo Echo Valley"
        )]
        public static void AuditBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single
            );

            List<GameObject> objects = GetSceneObjects(scene);
            Dictionary<string, int> meshes = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase
            );
            List<string> candidates = new List<string>();

            foreach (GameObject gameObject in objects)
            {
                MeshFilter filter = gameObject.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    string meshName = filter.sharedMesh.name;
                    meshes.TryGetValue(meshName, out int count);
                    meshes[meshName] = count + 1;
                }

                if (IsWorldCandidate(gameObject.name))
                {
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        gameObject
                    );
                    candidates.Add(
                        $"{GetPath(gameObject.transform)} | " +
                        $"pos={Format(gameObject.transform.position)} | " +
                        $"rotY={gameObject.transform.eulerAngles.y:0.##} | " +
                        $"scale={Format(gameObject.transform.lossyScale)} | " +
                        $"mesh={(filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "-")} | " +
                        $"prefab={(string.IsNullOrEmpty(prefabPath) ? "-" : prefabPath)}"
                    );
                }
            }

            List<string> terrainInfo = new List<string>();
            foreach (Terrain terrain in FindInScene<Terrain>(scene))
            {
                TerrainData data = terrain.terrainData;
                terrainInfo.Add(
                    $"{GetPath(terrain.transform)} | " +
                    $"pos={Format(terrain.transform.position)} | " +
                    $"size={(data != null ? Format(data.size) : "-")} | " +
                    $"trees={(data != null ? data.treeInstanceCount : 0)} | " +
                    $"prototypes={(data != null ? data.treePrototypes.Length : 0)}"
                );
            }

            List<string> lootInfo = new List<string>();
            foreach (LootSpawner spawner in FindInScene<LootSpawner>(scene))
            {
                SerializedObject serialized = new SerializedObject(spawner);
                lootInfo.Add(
                    $"{GetPath(spawner.transform)} | " +
                    $"pos={Format(spawner.transform.position)} | " +
                    $"count={serialized.FindProperty("spawnCount")?.intValue ?? 0} | " +
                    $"radius={serialized.FindProperty("radius")?.floatValue ?? 0f:0.##}"
                );
            }

            List<LootPickup> authoredPickups =
                FindInScene<LootPickup>(scene).ToList();

            string bladeInfo = DescribeSceneObject(
                scene.GetRootGameObjects().FirstOrDefault(
                    root => root.name == "Blade_Liger"
                )
            );

            Debug.Log(
                "[EchoValleyWorldAudit] RESUMEN\n" +
                $"Objetos={objects.Count} | Terrains={terrainInfo.Count} | " +
                $"LootSpawners={lootInfo.Count} | " +
                $"LootPickups={authoredPickups.Count}\n" +
                "Raíces:\n- " + string.Join(
                    "\n- ",
                    scene.GetRootGameObjects().Select(root => root.name)
                ) +
                "\nTerreno:\n- " + string.Join("\n- ", terrainInfo) +
                "\nModelos del proyecto:\n- " + string.Join(
                    "\n- ",
                    ModelPaths.Select(DescribeAsset)
                ) +
                "\nBlade Liger:\n- " + bladeInfo +
                "\nMallas más usadas:\n- " + string.Join(
                    "\n- ",
                    meshes.OrderByDescending(pair => pair.Value)
                        .ThenBy(pair => pair.Key)
                        .Take(80)
                        .Select(pair => $"{pair.Key}: {pair.Value}")
                ) +
                "\nCandidatos de vegetación/edificios:\n- " +
                string.Join("\n- ", candidates.Take(600)) +
                "\nLoot:\n- " + string.Join("\n- ", lootInfo)
            );
        }

        private static string DescribeSceneObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "NO ENCONTRADO";
            }

            Renderer[] renderers =
                gameObject.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            string prefabPath =
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                    gameObject
                );
            return $"{gameObject.name} | " +
                   $"pos={Format(gameObject.transform.position)} | " +
                   $"renderers={renderers.Length} | " +
                   $"bounds={(hasBounds ? Format(bounds.size) : "-")} | " +
                   $"prefab={(string.IsNullOrEmpty(prefabPath) ? "-" : prefabPath)}";
        }

        private static string DescribeAsset(string path)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                return $"{path} | NO ENCONTRADO";
            }

            Renderer[] renderers = asset.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return $"{path} | renderers={renderers.Length} | " +
                   $"bounds={(hasBounds ? Format(bounds.size) : "-")}";
        }

        private static List<GameObject> GetSceneObjects(Scene scene)
        {
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                result.AddRange(transforms.Select(item => item.gameObject));
            }

            return result;
        }

        private static IEnumerable<T> FindInScene<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    yield return component;
                }
            }
        }

        private static bool IsWorldCandidate(string name)
        {
            string value = name.ToLowerInvariant();
            return value.Contains("tree") || value.Contains("arbol") ||
                   value.Contains("house") || value.Contains("casa") ||
                   value.Contains("building") || value.Contains("edificio") ||
                   value.Contains("oak") || value.Contains("pine") ||
                   value.Contains("vegetation") || value.Contains("village") ||
                   value.Contains("home");
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private static string Format(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.##},{1:0.##},{2:0.##})",
                value.x,
                value.y,
                value.z
            );
        }
    }
}
#endif
