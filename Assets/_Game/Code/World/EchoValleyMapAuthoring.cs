using System;
using System.Collections.Generic;
using UnityEngine;

namespace ROS.Game.World
{
    public enum EchoValleySpawnType
    {
        Player,
        LootLow,
        LootMedium,
        LootHigh,
        Bicycle,
        RotationExit
    }

    public sealed class EchoValleySpawnMarker : MonoBehaviour
    {
        [SerializeField] private EchoValleySpawnType spawnType;
        [Min(0.1f)]
        [SerializeField] private float radius = 0.75f;

        public EchoValleySpawnType SpawnType => spawnType;
        public float Radius => radius;

        public void Configure(EchoValleySpawnType type, float markerRadius)
        {
            spawnType = type;
            radius = Mathf.Max(0.1f, markerRadius);
        }

        private void OnDrawGizmos()
        {
            switch (spawnType)
            {
                case EchoValleySpawnType.Player:
                    Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.9f);
                    break;
                case EchoValleySpawnType.LootHigh:
                    Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.9f);
                    break;
                case EchoValleySpawnType.LootMedium:
                    Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.9f);
                    break;
                case EchoValleySpawnType.Bicycle:
                    Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
                    break;
                case EchoValleySpawnType.RotationExit:
                    Gizmos.color = new Color(0.8f, 0.3f, 1f, 0.9f);
                    break;
                default:
                    Gizmos.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
                    break;
            }

            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }

    /// <summary>
    /// Reconstrucción modular de Echo Valley inspirada en el layout clásico de
    /// Rules of Survival: edificio principal de 3 pisos, casa de 2 pisos,
    /// cuatro almacenes, bicicletas, viviendas periféricas y valle cerrado.
    /// La geometría se genera con primitivas para poder sustituirla más adelante
    /// por prefabs artísticos sin cambiar posiciones, colisiones ni spawns.
    /// </summary>
    public sealed class EchoValleyMapAuthoring : MonoBehaviour
    {
        private const string GeneratedRootName = "EchoValley_Generated";

        [Header("Generation")]
        [SerializeField] private int vegetationSeed = 1987;

        [Header("Scale")]
        [Min(300f)]
        [SerializeField] private float mapWidth = 540f;
        [Min(260f)]
        [SerializeField] private float mapDepth = 470f;
        [Range(35f, 100f)]
        [SerializeField] private float ridgeHeight = 68f;

        private bool _isGenerating;

        [ContextMenu("Rebuild Echo Valley")]
        public void Rebuild()
        {
            ClearGenerated();
            BuildIfMissing();
        }

        [ContextMenu("Clear Echo Valley")]
        public void ClearGenerated()
        {
            Transform existing = transform.Find(GeneratedRootName);
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        private void BuildIfMissing()
        {
            if (_isGenerating || transform.Find(GeneratedRootName) != null)
            {
                return;
            }

            _isGenerating = true;
            try
            {
                EchoValleyBuilder builder = new EchoValleyBuilder(
                    transform,
                    mapWidth,
                    mapDepth,
                    ridgeHeight,
                    vegetationSeed,
                    false
                );
                builder.Build();
            }
            finally
            {
                _isGenerating = false;
            }
        }

        private sealed class EchoValleyBuilder
        {
            private readonly Transform _owner;
            private readonly float _mapWidth;
            private readonly float _mapDepth;
            private readonly float _ridgeHeight;
            private readonly int _seed;
            private readonly bool _preview;

            private Transform _root;
            private Terrain _terrain;
            private Palette _palette;

            private sealed class Palette
            {
                public Material Terrain;
                public Material Dirt;
                public Material Concrete;
                public Material Asphalt;
                public Material PlasterWarm;
                public Material PlasterCool;
                public Material Brick;
                public Material MetalBlue;
                public Material MetalGreen;
                public Material MetalRust;
                public Material RoofDark;
                public Material Wood;
                public Material Glass;
                public Material Foliage;
                public Material Trunk;
                public Material Rock;
                public Material Marking;
            }

            public EchoValleyBuilder(
                Transform owner,
                float mapWidth,
                float mapDepth,
                float ridgeHeight,
                int seed,
                bool preview
            )
            {
                _owner = owner;
                _mapWidth = mapWidth;
                _mapDepth = mapDepth;
                _ridgeHeight = ridgeHeight;
                _seed = seed;
                _preview = preview;
            }

            public void Build()
            {
                GameObject rootObject = new GameObject(GeneratedRootName);
                PrepareGeneratedObject(rootObject);
                rootObject.transform.SetParent(_owner, false);
                _root = rootObject.transform;

                _palette = CreatePalette();

                CreateTerrain();
                CreateValleyTracks();
                CreateCoreCompound();
                CreateOuterHouses();
                CreateFencesAndCover();
                CreateBicycles();
                CreateSpawnMarkers();
                CreateVegetationAndRocks();
                CreateUtilityDetails();
            }

            private Palette CreatePalette()
            {
                return new Palette
                {
                    Terrain = CreateMaterial("EV_Terrain", new Color(0.31f, 0.34f, 0.23f), 0f, 0.05f),
                    Dirt = CreateMaterial("EV_Dirt", new Color(0.34f, 0.29f, 0.21f), 0f, 0.02f),
                    Concrete = CreateMaterial("EV_Concrete", new Color(0.48f, 0.47f, 0.43f), 0f, 0.12f),
                    Asphalt = CreateMaterial("EV_Asphalt", new Color(0.16f, 0.17f, 0.16f), 0f, 0.08f),
                    PlasterWarm = CreateMaterial("EV_PlasterWarm", new Color(0.62f, 0.54f, 0.40f), 0f, 0.08f),
                    PlasterCool = CreateMaterial("EV_PlasterCool", new Color(0.45f, 0.50f, 0.48f), 0f, 0.08f),
                    Brick = CreateMaterial("EV_Brick", new Color(0.42f, 0.25f, 0.18f), 0f, 0.06f),
                    MetalBlue = CreateMaterial("EV_MetalBlue", new Color(0.23f, 0.34f, 0.38f), 0.18f, 0.25f),
                    MetalGreen = CreateMaterial("EV_MetalGreen", new Color(0.29f, 0.36f, 0.27f), 0.18f, 0.22f),
                    MetalRust = CreateMaterial("EV_MetalRust", new Color(0.39f, 0.25f, 0.17f), 0.12f, 0.16f),
                    RoofDark = CreateMaterial("EV_RoofDark", new Color(0.20f, 0.20f, 0.18f), 0.04f, 0.12f),
                    Wood = CreateMaterial("EV_Wood", new Color(0.34f, 0.23f, 0.14f), 0f, 0.1f),
                    Glass = CreateMaterial("EV_Glass", new Color(0.15f, 0.25f, 0.29f), 0.1f, 0.55f),
                    Foliage = CreateMaterial("EV_Foliage", new Color(0.22f, 0.34f, 0.16f), 0f, 0.05f),
                    Trunk = CreateMaterial("EV_Trunk", new Color(0.24f, 0.16f, 0.09f), 0f, 0.05f),
                    Rock = CreateMaterial("EV_Rock", new Color(0.34f, 0.34f, 0.31f), 0f, 0.06f),
                    Marking = CreateMaterial("EV_Marking", new Color(0.73f, 0.69f, 0.51f), 0f, 0.1f)
                };
            }

            private Material CreateMaterial(
                string name,
                Color color,
                float metallic,
                float smoothness
            )
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader)
                {
                    name = name,
                    hideFlags = _preview
                        ? HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                        : HideFlags.None
                };

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }

                if (material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", metallic);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", smoothness);
                }

                return material;
            }

            private void CreateTerrain()
            {
                TerrainData data = new TerrainData
                {
                    name = "EchoValley_TerrainData",
                    heightmapResolution = 257,
                    size = new Vector3(_mapWidth, _ridgeHeight, _mapDepth),
                    hideFlags = _preview
                        ? HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                        : HideFlags.None
                };

                int resolution = data.heightmapResolution;
                float[,] heights = new float[resolution, resolution];

                for (int z = 0; z < resolution; z++)
                {
                    float nz = (z / (float)(resolution - 1) - 0.5f) * 2f;
                    for (int x = 0; x < resolution; x++)
                    {
                        float nx = (x / (float)(resolution - 1) - 0.5f) * 2f;
                        float distance = Mathf.Sqrt(nx * nx + nz * nz);
                        float edge = Mathf.Clamp01((distance - 0.36f) / 0.72f);
                        float sideRidges = Mathf.Pow(Mathf.Abs(nx), 1.7f) * 0.24f;
                        float northSouth = Mathf.Pow(Mathf.Abs(nz), 2.1f) * 0.08f;
                        float noise = (
                            Mathf.PerlinNoise((nx + 2f) * 2.25f, (nz + 2f) * 2.25f) - 0.5f
                        ) * 0.065f;
                        float valley = 0.012f + edge * edge * 0.50f + sideRidges + northSouth + noise;

                        float centerMask = Mathf.Clamp01((distance - 0.24f) / 0.22f);
                        valley = Mathf.Lerp(0.018f + noise * 0.20f, valley, centerMask);
                        heights[z, x] = Mathf.Clamp01(valley);
                    }
                }

                data.SetHeights(0, 0, heights);

                TerrainLayer layer = new TerrainLayer
                {
                    name = "EchoValley_GroundLayer",
                    diffuseTexture = CreateSolidTexture(new Color(0.31f, 0.34f, 0.23f)),
                    tileSize = new Vector2(18f, 18f),
                    hideFlags = _preview
                        ? HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                        : HideFlags.None
                };
                data.terrainLayers = new[] { layer };

                GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
                terrainObject.name = "Terrain_ValleyBasin";
                PrepareGeneratedObject(terrainObject);
                terrainObject.transform.SetParent(_root, true);
                terrainObject.transform.position = new Vector3(
                    -_mapWidth * 0.5f,
                    -1.5f,
                    -_mapDepth * 0.5f
                );

                _terrain = terrainObject.GetComponent<Terrain>();
                _terrain.drawInstanced = true;
                _terrain.basemapDistance = 750f;
            }

            private Texture2D CreateSolidTexture(Color color)
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "EchoValley_GroundColor",
                    wrapMode = TextureWrapMode.Repeat,
                    hideFlags = _preview
                        ? HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                        : HideFlags.None
                };
                Color[] colors = { color, color, color, color };
                texture.SetPixels(colors);
                texture.Apply();
                return texture;
            }

            private float SurfaceY(float x, float z)
            {
                if (_terrain == null)
                {
                    return 0f;
                }

                return _terrain.SampleHeight(new Vector3(x, 0f, z)) +
                    _terrain.transform.position.y;
            }

            private void CreateValleyTracks()
            {
                Transform roads = NewGroup("01_Tracks_And_Yards");

                CreateTrack(
                    roads,
                    "South_Access_Track",
                    new[]
                    {
                        new Vector3(-8f, 0f, -218f),
                        new Vector3(-18f, 0f, -160f),
                        new Vector3(-10f, 0f, -105f),
                        new Vector3(0f, 0f, -58f),
                        new Vector3(5f, 0f, -20f)
                    },
                    7.5f
                );

                CreateTrack(
                    roads,
                    "North_Escape_Track",
                    new[]
                    {
                        new Vector3(5f, 0f, -20f),
                        new Vector3(16f, 0f, 34f),
                        new Vector3(8f, 0f, 92f),
                        new Vector3(-2f, 0f, 155f),
                        new Vector3(8f, 0f, 220f)
                    },
                    6.8f
                );

                CreateTrack(
                    roads,
                    "West_Service_Track",
                    new[]
                    {
                        new Vector3(-132f, 0f, 82f),
                        new Vector3(-86f, 0f, 58f),
                        new Vector3(-46f, 0f, 31f),
                        new Vector3(-18f, 0f, 5f)
                    },
                    5.8f
                );

                CreateTrack(
                    roads,
                    "East_Service_Track",
                    new[]
                    {
                        new Vector3(28f, 0f, 10f),
                        new Vector3(74f, 0f, 3f),
                        new Vector3(116f, 0f, 20f),
                        new Vector3(150f, 0f, 48f)
                    },
                    5.5f
                );

                CreateLot(roads, "Apartment_Courtyard", new Vector3(21f, 0f, 31f), new Vector2(48f, 45f));
                CreateLot(roads, "Warehouse_Yard", new Vector3(-17f, 0f, -43f), new Vector2(120f, 54f));
            }

            private void CreateTrack(
                Transform parent,
                string name,
                IReadOnlyList<Vector3> points,
                float width
            )
            {
                Transform track = NewGroup(name, parent);
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector3 a = points[i];
                    Vector3 b = points[i + 1];
                    Vector3 flatDirection = new Vector3(b.x - a.x, 0f, b.z - a.z);
                    float length = flatDirection.magnitude;
                    Vector3 middle = (a + b) * 0.5f;
                    middle.y = (SurfaceY(a.x, a.z) + SurfaceY(b.x, b.z)) * 0.5f + 0.08f;
                    Quaternion rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                    CreateCubeWorld(
                        track,
                        "Track_Segment_" + i,
                        middle,
                        new Vector3(width, 0.12f, length + 1.2f),
                        rotation,
                        _palette.Dirt,
                        false
                    );
                }
            }

            private void CreateLot(
                Transform parent,
                string name,
                Vector3 center,
                Vector2 size
            )
            {
                center.y = SurfaceY(center.x, center.z) + 0.09f;
                CreateCubeWorld(
                    parent,
                    name,
                    center,
                    new Vector3(size.x, 0.12f, size.y),
                    Quaternion.identity,
                    _palette.Dirt,
                    false
                );
            }

            private void CreateCoreCompound()
            {
                Transform core = NewGroup("02_Core_Compound");

                CreateApartmentBuilding(
                    core,
                    "Main_3F_Apartment",
                    new Vector3(23f, 0f, 34f),
                    8f
                );
                CreateTwoStoryHouse(
                    core,
                    "Two_Story_House",
                    new Vector3(-27f, 0f, 22f),
                    -7f
                );

                CreateWarehouse(core, "Warehouse_01_West", new Vector3(-62f, 0f, -42f), 4f, _palette.MetalBlue);
                CreateWarehouse(core, "Warehouse_02_CenterWest", new Vector3(-23f, 0f, -52f), -5f, _palette.MetalGreen);
                CreateWarehouse(core, "Warehouse_03_CenterEast", new Vector3(23f, 0f, -51f), 5f, _palette.MetalRust);
                CreateWarehouse(core, "Warehouse_04_East", new Vector3(66f, 0f, -31f), 78f, _palette.MetalBlue);
            }

            private void CreateApartmentBuilding(
                Transform parent,
                string name,
                Vector3 worldPosition,
                float yaw
            )
            {
                worldPosition.y = SurfaceY(worldPosition.x, worldPosition.z);
                Transform building = NewPlacedGroup(name, parent, worldPosition, yaw);

                const float width = 19f;
                const float depth = 16f;
                const float floorHeight = 3.2f;
                const float wall = 0.28f;

                CreateFoundation(building, width + 1.4f, depth + 1.4f);

                for (int floor = 0; floor < 3; floor++)
                {
                    float baseY = floor * floorHeight;
                    CreateFloorWithStairwell(building, baseY + 0.12f, width, depth, floor > 0);

                    Material wallMaterial = floor == 0 ? _palette.PlasterWarm : _palette.PlasterCool;
                    CreateCubeLocal(building, "BackWall_F" + floor, new Vector3(0f, baseY + 1.65f, depth * 0.5f), new Vector3(width, 3.25f, wall), Quaternion.identity, wallMaterial, true);
                    CreateCubeLocal(building, "LeftWall_F" + floor, new Vector3(-width * 0.5f, baseY + 1.65f, 0f), new Vector3(wall, 3.25f, depth), Quaternion.identity, wallMaterial, true);
                    CreateCubeLocal(building, "RightWall_F" + floor, new Vector3(width * 0.5f, baseY + 1.65f, 0f), new Vector3(wall, 3.25f, depth), Quaternion.identity, wallMaterial, true);

                    if (floor == 0)
                    {
                        CreateCubeLocal(building, "FrontWallL_F0", new Vector3(-5.7f, baseY + 1.65f, -depth * 0.5f), new Vector3(7.6f, 3.25f, wall), Quaternion.identity, wallMaterial, true);
                        CreateCubeLocal(building, "FrontWallR_F0", new Vector3(5.7f, baseY + 1.65f, -depth * 0.5f), new Vector3(7.6f, 3.25f, wall), Quaternion.identity, wallMaterial, true);
                        CreateCubeLocal(building, "DoorLintel_F0", new Vector3(0f, baseY + 2.75f, -depth * 0.5f), new Vector3(3.8f, 1.05f, wall), Quaternion.identity, wallMaterial, true);
                    }
                    else
                    {
                        CreateCubeLocal(building, "FrontWall_F" + floor, new Vector3(0f, baseY + 1.65f, -depth * 0.5f), new Vector3(width, 3.25f, wall), Quaternion.identity, wallMaterial, true);
                    }

                    CreateCubeLocal(building, "InteriorWallA_F" + floor, new Vector3(-2.6f, baseY + 1.55f, 1.1f), new Vector3(wall, 2.95f, 8.2f), Quaternion.identity, _palette.PlasterCool, true);
                    CreateCubeLocal(building, "InteriorWallB_F" + floor, new Vector3(3.6f, baseY + 1.55f, 2.4f), new Vector3(6.5f, 2.95f, wall), Quaternion.identity, _palette.PlasterCool, true);

                    AddApartmentWindows(building, floor, baseY, width, depth);
                }

                CreateStairFlight(building, new Vector3(5.9f, 0.25f, 3.9f), floorHeight, false, "Stairs_F0_F1");
                CreateStairFlight(building, new Vector3(5.9f, floorHeight + 0.25f, 3.9f), floorHeight, true, "Stairs_F1_F2");

                float roofY = floorHeight * 3f + 0.18f;
                CreateCubeLocal(building, "Flat_Roof", new Vector3(0f, roofY, 0f), new Vector3(width + 0.5f, 0.34f, depth + 0.5f), Quaternion.identity, _palette.RoofDark, true);
                CreateParapet(building, width, depth, roofY + 0.65f);

                CreateCubeLocal(building, "Roof_Access_Block", new Vector3(5.6f, roofY + 1.45f, 3.5f), new Vector3(4.2f, 2.5f, 4.4f), Quaternion.identity, _palette.PlasterCool, true);
                CreateDoorVisual(building, new Vector3(5.6f, roofY + 1.25f, 1.25f), Quaternion.identity);
            }

            private void CreateFloorWithStairwell(
                Transform building,
                float y,
                float width,
                float depth,
                bool opening
            )
            {
                if (!opening)
                {
                    CreateCubeLocal(building, "Floor_Ground", new Vector3(0f, y, 0f), new Vector3(width, 0.24f, depth), Quaternion.identity, _palette.Concrete, true);
                    return;
                }

                const float holeMinX = 4.25f;
                const float holeMaxX = 7.65f;
                const float holeMinZ = 0.8f;
                const float holeMaxZ = 7.2f;

                CreateCubeLocal(building, "Floor_Left_" + y, new Vector3((holeMinX - width * 0.5f) * 0.5f, y, 0f), new Vector3(holeMinX + width * 0.5f, 0.24f, depth), Quaternion.identity, _palette.Concrete, true);
                CreateCubeLocal(building, "Floor_Right_" + y, new Vector3((holeMaxX + width * 0.5f) * 0.5f, y, 0f), new Vector3(width * 0.5f - holeMaxX, 0.24f, depth), Quaternion.identity, _palette.Concrete, true);
                CreateCubeLocal(building, "Floor_HoleFront_" + y, new Vector3((holeMinX + holeMaxX) * 0.5f, y, (-depth * 0.5f + holeMinZ) * 0.5f), new Vector3(holeMaxX - holeMinX, 0.24f, holeMinZ + depth * 0.5f), Quaternion.identity, _palette.Concrete, true);
                CreateCubeLocal(building, "Floor_HoleBack_" + y, new Vector3((holeMinX + holeMaxX) * 0.5f, y, (depth * 0.5f + holeMaxZ) * 0.5f), new Vector3(holeMaxX - holeMinX, 0.24f, depth * 0.5f - holeMaxZ), Quaternion.identity, _palette.Concrete, true);
            }

            private void AddApartmentWindows(
                Transform building,
                int floor,
                float baseY,
                float width,
                float depth
            )
            {
                float y = baseY + 1.82f;
                float[] xPositions = { -6.4f, -2.2f, 2.2f, 6.4f };
                for (int i = 0; i < xPositions.Length; i++)
                {
                    if (floor == 0 && Mathf.Abs(xPositions[i]) < 2.3f)
                    {
                        continue;
                    }

                    CreateWindow(building, new Vector3(xPositions[i], y, -depth * 0.5f - 0.16f), new Vector3(2.15f, 1.25f, 0.08f), Quaternion.identity);
                    CreateWindow(building, new Vector3(xPositions[i], y, depth * 0.5f + 0.16f), new Vector3(2.15f, 1.25f, 0.08f), Quaternion.identity);
                }

                float[] zPositions = { -4.7f, 0f, 4.7f };
                for (int i = 0; i < zPositions.Length; i++)
                {
                    CreateWindow(building, new Vector3(-width * 0.5f - 0.16f, y, zPositions[i]), new Vector3(0.08f, 1.25f, 2.15f), Quaternion.identity);
                    CreateWindow(building, new Vector3(width * 0.5f + 0.16f, y, zPositions[i]), new Vector3(0.08f, 1.25f, 2.15f), Quaternion.identity);
                }
            }

            private void CreateTwoStoryHouse(
                Transform parent,
                string name,
                Vector3 worldPosition,
                float yaw
            )
            {
                worldPosition.y = SurfaceY(worldPosition.x, worldPosition.z);
                Transform house = NewPlacedGroup(name, parent, worldPosition, yaw);
                const float width = 14f;
                const float depth = 11.5f;
                const float floorHeight = 3.1f;
                const float wall = 0.25f;

                CreateFoundation(house, width + 1.2f, depth + 1.2f);
                for (int floor = 0; floor < 2; floor++)
                {
                    float baseY = floor * floorHeight;
                    CreateCubeLocal(house, "Floor_" + floor, new Vector3(0f, baseY + 0.12f, 0f), new Vector3(width, 0.24f, depth), Quaternion.identity, _palette.Concrete, true);
                    CreateCubeLocal(house, "Back_" + floor, new Vector3(0f, baseY + 1.58f, depth * 0.5f), new Vector3(width, 3.05f, wall), Quaternion.identity, _palette.PlasterWarm, true);
                    CreateCubeLocal(house, "SideL_" + floor, new Vector3(-width * 0.5f, baseY + 1.58f, 0f), new Vector3(wall, 3.05f, depth), Quaternion.identity, _palette.PlasterWarm, true);
                    CreateCubeLocal(house, "SideR_" + floor, new Vector3(width * 0.5f, baseY + 1.58f, 0f), new Vector3(wall, 3.05f, depth), Quaternion.identity, _palette.PlasterWarm, true);

                    if (floor == 0)
                    {
                        CreateCubeLocal(house, "FrontL_0", new Vector3(-4.6f, baseY + 1.58f, -depth * 0.5f), new Vector3(4.8f, 3.05f, wall), Quaternion.identity, _palette.PlasterWarm, true);
                        CreateCubeLocal(house, "FrontR_0", new Vector3(3.7f, baseY + 1.58f, -depth * 0.5f), new Vector3(6.6f, 3.05f, wall), Quaternion.identity, _palette.PlasterWarm, true);
                        CreateCubeLocal(house, "FrontLintel_0", new Vector3(-0.85f, baseY + 2.65f, -depth * 0.5f), new Vector3(2.7f, 0.9f, wall), Quaternion.identity, _palette.PlasterWarm, true);
                    }
                    else
                    {
                        CreateCubeLocal(house, "Front_1", new Vector3(0f, baseY + 1.58f, -depth * 0.5f), new Vector3(width, 3.05f, wall), Quaternion.identity, _palette.PlasterWarm, true);
                    }

                    CreateWindow(house, new Vector3(-4.2f, baseY + 1.75f, -depth * 0.5f - 0.14f), new Vector3(2.1f, 1.2f, 0.07f), Quaternion.identity);
                    CreateWindow(house, new Vector3(3.8f, baseY + 1.75f, -depth * 0.5f - 0.14f), new Vector3(2.1f, 1.2f, 0.07f), Quaternion.identity);
                    CreateWindow(house, new Vector3(-4.2f, baseY + 1.75f, depth * 0.5f + 0.14f), new Vector3(2.1f, 1.2f, 0.07f), Quaternion.identity);
                    CreateWindow(house, new Vector3(3.8f, baseY + 1.75f, depth * 0.5f + 0.14f), new Vector3(2.1f, 1.2f, 0.07f), Quaternion.identity);
                }

                CreateStairFlight(house, new Vector3(4.6f, 0.25f, 2.2f), floorHeight, false, "Interior_Stairs");
                float roofBase = floorHeight * 2f + 0.25f;
                CreateCubeLocal(house, "Roof_Left", new Vector3(-3.45f, roofBase + 0.7f, 0f), new Vector3(7.6f, 0.28f, depth + 0.8f), Quaternion.Euler(0f, 0f, 18f), _palette.RoofDark, true);
                CreateCubeLocal(house, "Roof_Right", new Vector3(3.45f, roofBase + 0.7f, 0f), new Vector3(7.6f, 0.28f, depth + 0.8f), Quaternion.Euler(0f, 0f, -18f), _palette.RoofDark, true);
            }

            private void CreateWarehouse(
                Transform parent,
                string name,
                Vector3 worldPosition,
                float yaw,
                Material material
            )
            {
                worldPosition.y = SurfaceY(worldPosition.x, worldPosition.z);
                Transform warehouse = NewPlacedGroup(name, parent, worldPosition, yaw);
                const float width = 21f;
                const float depth = 12f;
                const float height = 4.4f;
                const float wall = 0.24f;

                CreateFoundation(warehouse, width + 1.4f, depth + 1.4f);
                CreateCubeLocal(warehouse, "Floor", new Vector3(0f, 0.14f, 0f), new Vector3(width, 0.28f, depth), Quaternion.identity, _palette.Concrete, true);
                CreateCubeLocal(warehouse, "Back", new Vector3(0f, height * 0.5f, depth * 0.5f), new Vector3(width, height, wall), Quaternion.identity, material, true);
                CreateCubeLocal(warehouse, "SideL", new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(wall, height, depth), Quaternion.identity, material, true);
                CreateCubeLocal(warehouse, "SideR", new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(wall, height, depth), Quaternion.identity, material, true);

                CreateCubeLocal(warehouse, "FrontL", new Vector3(-6.75f, height * 0.5f, -depth * 0.5f), new Vector3(7.5f, height, wall), Quaternion.identity, material, true);
                CreateCubeLocal(warehouse, "FrontR", new Vector3(6.75f, height * 0.5f, -depth * 0.5f), new Vector3(7.5f, height, wall), Quaternion.identity, material, true);
                CreateCubeLocal(warehouse, "FrontLintel", new Vector3(0f, 3.9f, -depth * 0.5f), new Vector3(6f, 1f, wall), Quaternion.identity, material, true);

                CreateCubeLocal(warehouse, "RoofL", new Vector3(-5.2f, height + 0.65f, 0f), new Vector3(11.2f, 0.28f, depth + 0.8f), Quaternion.Euler(0f, 0f, 14f), _palette.RoofDark, true);
                CreateCubeLocal(warehouse, "RoofR", new Vector3(5.2f, height + 0.65f, 0f), new Vector3(11.2f, 0.28f, depth + 0.8f), Quaternion.Euler(0f, 0f, -14f), _palette.RoofDark, true);

                CreateCrateStack(warehouse, new Vector3(-6.8f, 0.7f, 2.9f), 3);
                CreateCrateStack(warehouse, new Vector3(4.8f, 0.7f, 2.2f), 2);
                CreateCrateStack(warehouse, new Vector3(7.2f, 0.7f, -2.7f), 2);
            }

            private void CreateOuterHouses()
            {
                Transform houses = NewGroup("03_Surrounding_Houses");
                Vector3[] positions =
                {
                    new Vector3(-101f, 0f, 76f),
                    new Vector3(-73f, 0f, 111f),
                    new Vector3(-21f, 0f, 125f),
                    new Vector3(71f, 0f, 108f),
                    new Vector3(103f, 0f, 69f),
                    new Vector3(116f, 0f, -82f),
                    new Vector3(-105f, 0f, -91f),
                    new Vector3(-57f, 0f, -126f)
                };
                float[] yaws = { 14f, -26f, 7f, 31f, -12f, 67f, -54f, 9f };

                for (int i = 0; i < positions.Length; i++)
                {
                    CreateSingleStoryHouse(
                        houses,
                        "Outer_House_" + (i + 1).ToString("00"),
                        positions[i],
                        yaws[i],
                        i % 2 == 0 ? _palette.PlasterWarm : _palette.PlasterCool
                    );
                }
            }

            private void CreateSingleStoryHouse(
                Transform parent,
                string name,
                Vector3 worldPosition,
                float yaw,
                Material wallMaterial
            )
            {
                worldPosition.y = SurfaceY(worldPosition.x, worldPosition.z);
                Transform house = NewPlacedGroup(name, parent, worldPosition, yaw);
                const float width = 10.5f;
                const float depth = 8.5f;
                const float height = 3f;
                const float wall = 0.22f;

                CreateFoundation(house, width + 1f, depth + 1f);
                CreateCubeLocal(house, "Floor", new Vector3(0f, 0.12f, 0f), new Vector3(width, 0.24f, depth), Quaternion.identity, _palette.Concrete, true);
                CreateCubeLocal(house, "Back", new Vector3(0f, 1.55f, depth * 0.5f), new Vector3(width, height, wall), Quaternion.identity, wallMaterial, true);
                CreateCubeLocal(house, "SideL", new Vector3(-width * 0.5f, 1.55f, 0f), new Vector3(wall, height, depth), Quaternion.identity, wallMaterial, true);
                CreateCubeLocal(house, "SideR", new Vector3(width * 0.5f, 1.55f, 0f), new Vector3(wall, height, depth), Quaternion.identity, wallMaterial, true);
                CreateCubeLocal(house, "FrontL", new Vector3(-3.3f, 1.55f, -depth * 0.5f), new Vector3(3.9f, height, wall), Quaternion.identity, wallMaterial, true);
                CreateCubeLocal(house, "FrontR", new Vector3(3.5f, 1.55f, -depth * 0.5f), new Vector3(3.5f, height, wall), Quaternion.identity, wallMaterial, true);
                CreateCubeLocal(house, "Lintel", new Vector3(0.2f, 2.65f, -depth * 0.5f), new Vector3(2.9f, 0.8f, wall), Quaternion.identity, wallMaterial, true);

                CreateWindow(house, new Vector3(-3f, 1.65f, depth * 0.5f + 0.13f), new Vector3(1.8f, 1.15f, 0.06f), Quaternion.identity);
                CreateWindow(house, new Vector3(2.8f, 1.65f, depth * 0.5f + 0.13f), new Vector3(1.8f, 1.15f, 0.06f), Quaternion.identity);

                CreateCubeLocal(house, "RoofL", new Vector3(-2.6f, 3.55f, 0f), new Vector3(5.9f, 0.24f, depth + 0.65f), Quaternion.Euler(0f, 0f, 20f), _palette.RoofDark, true);
                CreateCubeLocal(house, "RoofR", new Vector3(2.6f, 3.55f, 0f), new Vector3(5.9f, 0.24f, depth + 0.65f), Quaternion.Euler(0f, 0f, -20f), _palette.RoofDark, true);

                CreateCrateStack(house, new Vector3(2.9f, 0.55f, 1.9f), 1);
            }

            private void CreateFencesAndCover()
            {
                Transform cover = NewGroup("04_Cover_And_Fences");

                CreateFenceLine(cover, new Vector3(-85f, 0f, 5f), new Vector3(-48f, 0f, 7f), 2.2f);
                CreateFenceLine(cover, new Vector3(79f, 0f, 23f), new Vector3(110f, 0f, 35f), 2.2f);
                CreateFenceLine(cover, new Vector3(-41f, 0f, 61f), new Vector3(-7f, 0f, 65f), 2.1f);

                Vector3[] barriers =
                {
                    new Vector3(-16f, 0f, -15f),
                    new Vector3(4f, 0f, -8f),
                    new Vector3(46f, 0f, -7f),
                    new Vector3(-49f, 0f, -15f),
                    new Vector3(57f, 0f, 44f)
                };

                for (int i = 0; i < barriers.Length; i++)
                {
                    Vector3 p = barriers[i];
                    p.y = SurfaceY(p.x, p.z) + 0.55f;
                    CreateCubeWorld(
                        cover,
                        "Concrete_Barrier_" + i,
                        p,
                        new Vector3(4.6f, 1.1f, 0.55f),
                        Quaternion.Euler(0f, i * 17f - 20f, 0f),
                        _palette.Concrete,
                        true
                    );
                }

                CreateCrateClusterWorld(cover, new Vector3(-40f, 0f, -7f), 5);
                CreateCrateClusterWorld(cover, new Vector3(45f, 0f, 8f), 4);
                CreateBarrelCluster(cover, new Vector3(80f, 0f, -43f), 5);
                CreateBarrelCluster(cover, new Vector3(-76f, 0f, -57f), 4);
            }

            private void CreateFenceLine(
                Transform parent,
                Vector3 start,
                Vector3 end,
                float spacing
            )
            {
                Vector3 direction = end - start;
                direction.y = 0f;
                float length = direction.magnitude;
                int count = Mathf.Max(2, Mathf.CeilToInt(length / spacing));
                direction.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, direction);

                for (int i = 0; i <= count; i++)
                {
                    float t = i / (float)count;
                    Vector3 p = Vector3.Lerp(start, end, t);
                    p.y = SurfaceY(p.x, p.z);
                    CreateCubeWorld(parent, "Fence_Post_" + i, p + Vector3.up * 0.8f, new Vector3(0.12f, 1.6f, 0.12f), Quaternion.identity, _palette.Wood, true);
                }

                for (int rail = 0; rail < 2; rail++)
                {
                    Vector3 middle = (start + end) * 0.5f;
                    middle.y = (SurfaceY(start.x, start.z) + SurfaceY(end.x, end.z)) * 0.5f + 0.55f + rail * 0.55f;
                    Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                    CreateCubeWorld(parent, "Fence_Rail_" + rail, middle + right * 0.02f, new Vector3(0.1f, 0.12f, length), rotation, _palette.Wood, true);
                }
            }

            private void CreateBicycles()
            {
                Transform bikes = NewGroup("05_Bicycles");
                Vector3[] bikePositions =
                {
                    new Vector3(10f, 0f, 17f),
                    new Vector3(13f, 0f, 16f),
                    new Vector3(16f, 0f, 17.5f)
                };

                for (int i = 0; i < bikePositions.Length; i++)
                {
                    Vector3 p = bikePositions[i];
                    p.y = SurfaceY(p.x, p.z) + 0.65f;
                    CreateBicycle(bikes, "Bicycle_" + (i + 1), p, 96f + i * 4f);
                    CreateMarker(
                        bikes,
                        "BicycleSpawn_" + (i + 1),
                        new Vector3(p.x, p.y - 0.65f, p.z),
                        EchoValleySpawnType.Bicycle,
                        1.2f
                    );
                }
            }

            private void CreateBicycle(
                Transform parent,
                string name,
                Vector3 worldPosition,
                float yaw
            )
            {
                Transform bike = NewPlacedGroup(name, parent, worldPosition, yaw);
                Material frameMaterial = _palette.MetalRust;

                CreateCylinderLocal(bike, "Wheel_Front", new Vector3(0.95f, 0f, 0f), new Vector3(0.62f, 0.06f, 0.62f), Quaternion.Euler(90f, 0f, 0f), _palette.RoofDark, true);
                CreateCylinderLocal(bike, "Wheel_Rear", new Vector3(-0.95f, 0f, 0f), new Vector3(0.62f, 0.06f, 0.62f), Quaternion.Euler(90f, 0f, 0f), _palette.RoofDark, true);

                CreateBeamLocal(bike, "Frame_A", new Vector3(-0.7f, 0.1f, 0f), new Vector3(0f, 0.75f, 0f), 0.05f, frameMaterial);
                CreateBeamLocal(bike, "Frame_B", new Vector3(0f, 0.75f, 0f), new Vector3(0.72f, 0.12f, 0f), 0.05f, frameMaterial);
                CreateBeamLocal(bike, "Frame_C", new Vector3(-0.7f, 0.1f, 0f), new Vector3(0.72f, 0.12f, 0f), 0.05f, frameMaterial);
                CreateBeamLocal(bike, "Fork", new Vector3(0.72f, 0.12f, 0f), new Vector3(0.83f, 0.92f, 0f), 0.04f, frameMaterial);
                CreateBeamLocal(bike, "Handle", new Vector3(0.65f, 0.92f, -0.25f), new Vector3(0.65f, 0.92f, 0.25f), 0.035f, _palette.RoofDark);
                CreateCubeLocal(bike, "Seat", new Vector3(-0.18f, 0.86f, 0f), new Vector3(0.35f, 0.09f, 0.22f), Quaternion.identity, _palette.RoofDark, false);
            }

            private void CreateSpawnMarkers()
            {
                Transform markers = NewGroup("06_Gameplay_Markers");

                Vector3[] playerSpawns =
                {
                    new Vector3(-6f, 0f, -91f),
                    new Vector3(-91f, 0f, -34f),
                    new Vector3(-68f, 0f, 54f),
                    new Vector3(-27f, 0f, 82f),
                    new Vector3(36f, 0f, 88f),
                    new Vector3(89f, 0f, 42f),
                    new Vector3(91f, 0f, -37f),
                    new Vector3(44f, 0f, -84f),
                    new Vector3(-44f, 0f, -89f),
                    new Vector3(5f, 0f, 112f),
                    new Vector3(-113f, 0f, 17f),
                    new Vector3(116f, 0f, 2f)
                };

                for (int i = 0; i < playerSpawns.Length; i++)
                {
                    Vector3 p = playerSpawns[i];
                    p.y = SurfaceY(p.x, p.z) + 0.1f;
                    CreateMarker(markers, "PlayerSpawn_" + (i + 1).ToString("00"), p, EchoValleySpawnType.Player, 1.3f);
                }

                AddLootMarker(markers, "LootHigh_Apartment_Ground", 23f, 34f, 0.6f, EchoValleySpawnType.LootHigh);
                AddLootMarker(markers, "LootHigh_Apartment_F2", 20f, 36f, 6.7f, EchoValleySpawnType.LootHigh);
                AddLootMarker(markers, "LootHigh_Apartment_Roof", 18f, 30f, 10.3f, EchoValleySpawnType.LootHigh);
                AddLootMarker(markers, "LootMedium_TwoStory_01", -27f, 22f, 0.6f, EchoValleySpawnType.LootMedium);
                AddLootMarker(markers, "LootMedium_TwoStory_02", -24f, 24f, 3.7f, EchoValleySpawnType.LootMedium);

                Vector3[] warehouseLoot =
                {
                    new Vector3(-62f, 0f, -42f),
                    new Vector3(-23f, 0f, -52f),
                    new Vector3(23f, 0f, -51f),
                    new Vector3(66f, 0f, -31f)
                };
                for (int i = 0; i < warehouseLoot.Length; i++)
                {
                    Vector3 p = warehouseLoot[i];
                    p.y = SurfaceY(p.x, p.z) + 0.55f;
                    CreateMarker(markers, "WarehouseLoot_" + (i + 1) + "_A", p + new Vector3(-4f, 0f, 2f), EchoValleySpawnType.LootMedium, 0.75f);
                    CreateMarker(markers, "WarehouseLoot_" + (i + 1) + "_B", p + new Vector3(4f, 0f, -1f), EchoValleySpawnType.LootLow, 0.75f);
                }

                Vector3[] lowLoot =
                {
                    new Vector3(-101f, 0f, 76f),
                    new Vector3(-73f, 0f, 111f),
                    new Vector3(-21f, 0f, 125f),
                    new Vector3(71f, 0f, 108f),
                    new Vector3(103f, 0f, 69f),
                    new Vector3(116f, 0f, -82f),
                    new Vector3(-105f, 0f, -91f),
                    new Vector3(-57f, 0f, -126f)
                };
                for (int i = 0; i < lowLoot.Length; i++)
                {
                    Vector3 p = lowLoot[i];
                    p.y = SurfaceY(p.x, p.z) + 0.55f;
                    CreateMarker(markers, "OuterHouseLoot_" + (i + 1), p, EchoValleySpawnType.LootLow, 0.7f);
                }

                Vector3[] exits =
                {
                    new Vector3(-8f, 0f, -214f),
                    new Vector3(7f, 0f, 214f),
                    new Vector3(-145f, 0f, 89f),
                    new Vector3(154f, 0f, 51f)
                };
                for (int i = 0; i < exits.Length; i++)
                {
                    Vector3 p = exits[i];
                    p.y = SurfaceY(p.x, p.z) + 0.2f;
                    CreateMarker(markers, "RotationExit_" + (i + 1), p, EchoValleySpawnType.RotationExit, 3f);
                }
            }

            private void AddLootMarker(
                Transform parent,
                string name,
                float x,
                float z,
                float localHeight,
                EchoValleySpawnType type
            )
            {
                Vector3 p = new Vector3(x, SurfaceY(x, z) + localHeight, z);
                CreateMarker(parent, name, p, type, 0.8f);
            }

            private void CreateMarker(
                Transform parent,
                string name,
                Vector3 worldPosition,
                EchoValleySpawnType type,
                float radius
            )
            {
                GameObject marker = new GameObject(name);
                PrepareGeneratedObject(marker);
                marker.transform.SetParent(parent, true);
                marker.transform.position = worldPosition;
                EchoValleySpawnMarker component = marker.AddComponent<EchoValleySpawnMarker>();
                component.Configure(type, radius);
            }

            private void CreateVegetationAndRocks()
            {
                Transform nature = NewGroup("07_Nature");
                System.Random random = new System.Random(_seed);

                int treesCreated = 0;
                int attempts = 0;
                while (treesCreated < 72 && attempts < 500)
                {
                    attempts++;
                    float x = Mathf.Lerp(-_mapWidth * 0.44f, _mapWidth * 0.44f, (float)random.NextDouble());
                    float z = Mathf.Lerp(-_mapDepth * 0.44f, _mapDepth * 0.44f, (float)random.NextDouble());
                    float centerDistance = new Vector2(x / 135f, z / 125f).magnitude;
                    if (centerDistance < 0.85f || IsNearCoreStructure(x, z))
                    {
                        continue;
                    }

                    float y = SurfaceY(x, z);
                    float scale = Mathf.Lerp(0.8f, 1.35f, (float)random.NextDouble());
                    CreateTree(nature, "Tree_" + treesCreated.ToString("00"), new Vector3(x, y, z), scale);
                    treesCreated++;
                }

                for (int i = 0; i < 46; i++)
                {
                    float x = Mathf.Lerp(-_mapWidth * 0.46f, _mapWidth * 0.46f, (float)random.NextDouble());
                    float z = Mathf.Lerp(-_mapDepth * 0.46f, _mapDepth * 0.46f, (float)random.NextDouble());
                    if (new Vector2(x / 115f, z / 105f).magnitude < 0.9f)
                    {
                        continue;
                    }

                    float y = SurfaceY(x, z) + 0.35f;
                    Vector3 scale = new Vector3(
                        Mathf.Lerp(0.7f, 2.3f, (float)random.NextDouble()),
                        Mathf.Lerp(0.5f, 1.6f, (float)random.NextDouble()),
                        Mathf.Lerp(0.7f, 2.5f, (float)random.NextDouble())
                    );
                    CreateSphereWorld(nature, "Rock_" + i.ToString("00"), new Vector3(x, y, z), scale, Quaternion.Euler(0f, (float)random.NextDouble() * 180f, 0f), _palette.Rock, true);
                }
            }

            private bool IsNearCoreStructure(float x, float z)
            {
                Vector2 p = new Vector2(x, z);
                Vector2[] centers =
                {
                    new Vector2(23f, 34f),
                    new Vector2(-27f, 22f),
                    new Vector2(-62f, -42f),
                    new Vector2(-23f, -52f),
                    new Vector2(23f, -51f),
                    new Vector2(66f, -31f)
                };
                for (int i = 0; i < centers.Length; i++)
                {
                    if (Vector2.Distance(p, centers[i]) < 24f)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void CreateTree(
                Transform parent,
                string name,
                Vector3 position,
                float scale
            )
            {
                Transform tree = NewPlacedGroup(name, parent, position, 0f);
                CreateCylinderLocal(tree, "Trunk", new Vector3(0f, 2.1f * scale, 0f), new Vector3(0.28f * scale, 2.1f * scale, 0.28f * scale), Quaternion.identity, _palette.Trunk, true);
                GameObject crownA = CreateSphereLocal(tree, "Crown_A", new Vector3(0f, 5.2f * scale, 0f), new Vector3(2.5f, 2.2f, 2.5f) * scale, Quaternion.identity, _palette.Foliage, false);
                GameObject crownB = CreateSphereLocal(tree, "Crown_B", new Vector3(0.9f * scale, 4.8f * scale, 0.3f * scale), new Vector3(1.8f, 1.7f, 1.8f) * scale, Quaternion.identity, _palette.Foliage, false);
                DisableCollider(crownA);
                DisableCollider(crownB);
            }

            private void CreateUtilityDetails()
            {
                Transform detail = NewGroup("08_Utility_Details");

                Vector3[] polePositions =
                {
                    new Vector3(-16f, 0f, -106f),
                    new Vector3(-12f, 0f, -69f),
                    new Vector3(-4f, 0f, -26f),
                    new Vector3(13f, 0f, 17f),
                    new Vector3(13f, 0f, 62f),
                    new Vector3(7f, 0f, 105f)
                };

                for (int i = 0; i < polePositions.Length; i++)
                {
                    Vector3 p = polePositions[i];
                    p.y = SurfaceY(p.x, p.z);
                    Transform pole = NewPlacedGroup("UtilityPole_" + (i + 1), detail, p, 0f);
                    CreateCylinderLocal(pole, "Pole", new Vector3(0f, 4.2f, 0f), new Vector3(0.16f, 4.2f, 0.16f), Quaternion.identity, _palette.Wood, true);
                    CreateCubeLocal(pole, "Crossbar", new Vector3(0f, 7.8f, 0f), new Vector3(3f, 0.12f, 0.14f), Quaternion.identity, _palette.Wood, true);
                }

                CreateSign(detail, "EchoValley_Sign_South", new Vector3(-16f, 0f, -142f), -6f);
                CreateSign(detail, "EchoValley_Sign_North", new Vector3(2f, 0f, 143f), 174f);
            }

            private void CreateSign(
                Transform parent,
                string name,
                Vector3 position,
                float yaw
            )
            {
                position.y = SurfaceY(position.x, position.z);
                Transform sign = NewPlacedGroup(name, parent, position, yaw);
                CreateCubeLocal(sign, "PostL", new Vector3(-1.6f, 1.1f, 0f), new Vector3(0.16f, 2.2f, 0.16f), Quaternion.identity, _palette.Wood, true);
                CreateCubeLocal(sign, "PostR", new Vector3(1.6f, 1.1f, 0f), new Vector3(0.16f, 2.2f, 0.16f), Quaternion.identity, _palette.Wood, true);
                CreateCubeLocal(sign, "Board", new Vector3(0f, 2.25f, 0f), new Vector3(4.5f, 1.3f, 0.18f), Quaternion.identity, _palette.MetalGreen, true);
            }

            private void CreateFoundation(Transform parent, float width, float depth)
            {
                CreateCubeLocal(parent, "Foundation", new Vector3(0f, -0.13f, 0f), new Vector3(width, 0.26f, depth), Quaternion.identity, _palette.Concrete, true);
            }

            private void CreateParapet(Transform parent, float width, float depth, float y)
            {
                CreateCubeLocal(parent, "Parapet_Front", new Vector3(0f, y, -depth * 0.5f), new Vector3(width, 1f, 0.22f), Quaternion.identity, _palette.Concrete, true);
                CreateCubeLocal(parent, "Parapet_Back", new Vector3(0f, y, depth * 0.5f), new Vector3(width, 1f, 0.22f), Quaternion.identity, _palette.Concrete, true);
                CreateCubeLocal(parent, "Parapet_Left", new Vector3(-width * 0.5f, y, 0f), new Vector3(0.22f, 1f, depth), Quaternion.identity, _palette.Concrete, true);
                CreateCubeLocal(parent, "Parapet_Right", new Vector3(width * 0.5f, y, 0f), new Vector3(0.22f, 1f, depth), Quaternion.identity, _palette.Concrete, true);
            }

            private void CreateStairFlight(
                Transform parent,
                Vector3 start,
                float rise,
                bool reverse,
                string name
            )
            {
                Transform stairs = NewGroup(name, parent);
                const int steps = 11;
                float stepHeight = rise / steps;
                float stepDepth = 5.5f / steps;
                for (int i = 0; i < steps; i++)
                {
                    int index = reverse ? steps - 1 - i : i;
                    Vector3 p = start + new Vector3(0f, stepHeight * (i + 0.5f), -stepDepth * index);
                    CreateCubeLocal(stairs, "Step_" + i.ToString("00"), p, new Vector3(2.4f, stepHeight, stepDepth + 0.04f), Quaternion.identity, _palette.Concrete, true);
                }
            }

            private void CreateWindow(
                Transform parent,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation
            )
            {
                GameObject window = CreateCubeLocal(parent, "Window", localPosition, localScale, localRotation, _palette.Glass, false);
                DisableCollider(window);
            }

            private void CreateDoorVisual(
                Transform parent,
                Vector3 localPosition,
                Quaternion localRotation
            )
            {
                GameObject door = CreateCubeLocal(parent, "Door", localPosition, new Vector3(1.7f, 2.25f, 0.12f), localRotation, _palette.Wood, false);
                DisableCollider(door);
            }

            private void CreateCrateStack(Transform parent, Vector3 localPosition, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 p = localPosition + new Vector3((i % 2) * 1.15f, (i / 2) * 1.1f, (i % 3) * 0.35f);
                    CreateCubeLocal(parent, "Crate", p, new Vector3(1.05f, 1.05f, 1.05f), Quaternion.Euler(0f, i * 13f, 0f), _palette.Wood, true);
                }
            }

            private void CreateCrateClusterWorld(Transform parent, Vector3 position, int count)
            {
                float baseY = SurfaceY(position.x, position.z);
                Transform group = NewPlacedGroup("Crate_Cluster", parent, new Vector3(position.x, baseY, position.z), 0f);
                CreateCrateStack(group, new Vector3(0f, 0.55f, 0f), count);
            }

            private void CreateBarrelCluster(Transform parent, Vector3 position, int count)
            {
                float baseY = SurfaceY(position.x, position.z);
                Transform group = NewPlacedGroup("Barrel_Cluster", parent, new Vector3(position.x, baseY, position.z), 0f);
                for (int i = 0; i < count; i++)
                {
                    float x = (i % 3) * 0.75f;
                    float z = (i / 3) * 0.75f;
                    CreateCylinderLocal(group, "Barrel_" + i, new Vector3(x, 0.55f, z), new Vector3(0.34f, 0.55f, 0.34f), Quaternion.identity, i % 2 == 0 ? _palette.MetalBlue : _palette.MetalRust, true);
                }
            }

            private Transform NewGroup(string name, Transform parent = null)
            {
                GameObject gameObject = new GameObject(name);
                PrepareGeneratedObject(gameObject);
                gameObject.transform.SetParent(parent != null ? parent : _root, false);
                return gameObject.transform;
            }

            private Transform NewPlacedGroup(
                string name,
                Transform parent,
                Vector3 worldPosition,
                float yaw
            )
            {
                GameObject gameObject = new GameObject(name);
                PrepareGeneratedObject(gameObject);
                gameObject.transform.SetParent(parent, true);
                gameObject.transform.position = worldPosition;
                gameObject.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                return gameObject.transform;
            }

            private GameObject CreateCubeLocal(
                Transform parent,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                Material material,
                bool collider
            )
            {
                return CreatePrimitiveLocal(PrimitiveType.Cube, parent, name, localPosition, localScale, localRotation, material, collider);
            }

            private GameObject CreateCubeWorld(
                Transform parent,
                string name,
                Vector3 worldPosition,
                Vector3 worldScale,
                Quaternion worldRotation,
                Material material,
                bool collider
            )
            {
                GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gameObject.name = name;
                PrepareGeneratedObject(gameObject);
                gameObject.transform.SetParent(parent, true);
                gameObject.transform.position = worldPosition;
                gameObject.transform.rotation = worldRotation;
                gameObject.transform.localScale = worldScale;
                ApplyMaterial(gameObject, material);
                if (!collider)
                {
                    DisableCollider(gameObject);
                }
                return gameObject;
            }

            private GameObject CreateSphereLocal(
                Transform parent,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                Material material,
                bool collider
            )
            {
                return CreatePrimitiveLocal(PrimitiveType.Sphere, parent, name, localPosition, localScale, localRotation, material, collider);
            }

            private void CreateSphereWorld(
                Transform parent,
                string name,
                Vector3 worldPosition,
                Vector3 worldScale,
                Quaternion worldRotation,
                Material material,
                bool collider
            )
            {
                GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gameObject.name = name;
                PrepareGeneratedObject(gameObject);
                gameObject.transform.SetParent(parent, true);
                gameObject.transform.position = worldPosition;
                gameObject.transform.rotation = worldRotation;
                gameObject.transform.localScale = worldScale;
                ApplyMaterial(gameObject, material);
                if (!collider)
                {
                    DisableCollider(gameObject);
                }
            }

            private GameObject CreateCylinderLocal(
                Transform parent,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                Material material,
                bool collider
            )
            {
                return CreatePrimitiveLocal(PrimitiveType.Cylinder, parent, name, localPosition, localScale, localRotation, material, collider);
            }

            private GameObject CreatePrimitiveLocal(
                PrimitiveType type,
                Transform parent,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                Material material,
                bool collider
            )
            {
                GameObject gameObject = GameObject.CreatePrimitive(type);
                gameObject.name = name;
                PrepareGeneratedObject(gameObject);
                gameObject.transform.SetParent(parent, false);
                gameObject.transform.localPosition = localPosition;
                gameObject.transform.localRotation = localRotation;
                gameObject.transform.localScale = localScale;
                ApplyMaterial(gameObject, material);
                if (!collider)
                {
                    DisableCollider(gameObject);
                }
                return gameObject;
            }

            private void CreateBeamLocal(
                Transform parent,
                string name,
                Vector3 from,
                Vector3 to,
                float radius,
                Material material
            )
            {
                Vector3 direction = to - from;
                float length = direction.magnitude;
                if (length <= 0.001f)
                {
                    return;
                }

                Vector3 middle = (from + to) * 0.5f;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                CreateCylinderLocal(parent, name, middle, new Vector3(radius, length * 0.5f, radius), rotation, material, false);
            }

            private void ApplyMaterial(GameObject gameObject, Material material)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                }
            }

            private void DisableCollider(GameObject gameObject)
            {
                Collider collider = gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }

            private void PrepareGeneratedObject(GameObject gameObject)
            {
                gameObject.isStatic = true;
                if (_preview)
                {
                    gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                }
            }
        }
    }
}
