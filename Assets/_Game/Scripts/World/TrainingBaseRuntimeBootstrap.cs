using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.World
{
    /// <summary>
    /// Construye la geometria de Training Base al cargar 09_TrainingBase.
    /// La composicion sigue las referencias del Training Base clasico de Ghillie Island:
    /// edificio azul de dos plantas con terraza, campo abierto, lineas de tiro,
    /// blancos, muros/pilas de neumaticos, barreras bajas y colinas arboladas.
    /// </summary>
    public static class TrainingBaseRuntimeBootstrap
    {
        public const string SceneName = "09_TrainingBase";
        public const string GeneratedRootName = "__TrainingBase_Environment";
        public static readonly Vector3 PlayerSpawn = new Vector3(-4f, 1.1f, -56f);

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            BuildForActiveScene(true);
        }

        public static GameObject BuildForActiveScene(bool rebuild = false)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != SceneName)
            {
                return null;
            }

            GameObject existing = GameObject.Find(GeneratedRootName);
            if (existing != null)
            {
                if (!rebuild)
                {
                    return existing;
                }

                if (Application.isPlaying)
                    Object.DestroyImmediate(existing);
                else
                    Object.DestroyImmediate(existing);
            }

            Materials.Clear();
            DisableCombatTestGeometry(scene);

            GameObject root = new GameObject(GeneratedRootName);
            BuildGround(root.transform);
            BuildBlueTerraceHouse(root.transform);
            BuildFiringDeck(root.transform);
            BuildShootingLanes(root.transform);
            BuildTireZones(root.transform);
            BuildPerimeter(root.transform);
            BuildServiceArea(root.transform);
            BuildLandscape(root.transform);
            EnsureSun(root.transform);
            MovePlayerToSpawn(scene);
            return root;
        }

        private static void DisableCombatTestGeometry(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go == null || go.name == GeneratedRootName || go.name == "Player_Prototype")
                    continue;

                if (go.name.StartsWith("DamageTarget_") ||
                    go.name == "Ground" ||
                    go.name == "Floor" ||
                    go.name == "TestGround")
                {
                    go.SetActive(false);
                }
            }
        }

        private static void MovePlayerToSpawn(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go != null && go.name == "Player_Prototype")
                {
                    go.transform.SetPositionAndRotation(PlayerSpawn, Quaternion.identity);
                    return;
                }
            }
        }

        private static void BuildGround(Transform parent)
        {
            Transform g = Group("01_Terreno", parent);
            Box("Campo_Principal", g, new Vector3(0f, -0.55f, 10f), new Vector3(190f, 1f, 180f), "grass");
            Box("Explanada_Tierra", g, new Vector3(0f, 0.01f, 24f), new Vector3(92f, 0.12f, 116f), "dirt");
            Box("Camino_Entrada", g, new Vector3(-28f, 0.08f, -49f), new Vector3(17f, 0.14f, 48f), "road");
            Box("Patio_Edificio", g, new Vector3(-18f, 0.09f, -31f), new Vector3(50f, 0.14f, 31f), "dirtLight");

            for (int i = 0; i < 6; i++)
            {
                float x = -30f + i * 12f;
                Box("Linea_Tiro_" + (i + 1), g, new Vector3(x, 0.16f, 28f), new Vector3(1.1f, 0.05f, 91f), "lane");
            }
        }

        private static void BuildBlueTerraceHouse(Transform parent)
        {
            Transform b = Group("02_Edificio_Azul_Terraza", parent);
            Vector3 c = new Vector3(-28f, 0f, -26f);

            Box("Base", b, c + new Vector3(0f, 0.18f, 0f), new Vector3(24f, 0.35f, 16f), "concrete");
            BuildFloorWalls(b, c, 0.35f, "blue");
            BuildFloorWalls(b, c, 4.55f, "blue");
            Box("Losa_Intermedia", b, c + new Vector3(0f, 4.3f, 0f), new Vector3(24f, 0.3f, 16f), "concrete");
            Box("Techo_Plano", b, c + new Vector3(-3.8f, 8.75f, 0f), new Vector3(16.2f, 0.35f, 16.4f), "roof");

            // Terraza abierta del segundo piso hacia el campo de tiro.
            Box("Terraza", b, c + new Vector3(8.6f, 8.2f, 0f), new Vector3(7.3f, 0.28f, 16f), "concrete");
            Rail(b, c + new Vector3(12f, 9f, 0f), new Vector3(0.18f, 1.55f, 16f));
            Rail(b, c + new Vector3(8.4f, 9f, -7.8f), new Vector3(7.3f, 1.55f, 0.18f));
            Rail(b, c + new Vector3(8.4f, 9f, 7.8f), new Vector3(7.3f, 1.55f, 0.18f));

            // Escalera exterior y descanso, rasgo util para subir a la terraza.
            for (int i = 0; i < 11; i++)
            {
                Box("Escalon_" + i, b,
                    c + new Vector3(12.8f, 0.28f + i * 0.36f, -6.3f + i * 0.52f),
                    new Vector3(2.3f, 0.28f, 0.75f), "concrete");
            }
            Box("Descanso_Escalera", b, c + new Vector3(12.8f, 4.35f, 0.5f), new Vector3(2.5f, 0.28f, 3f), "concrete");

            // Marcos oscuros para puertas/ventanas visibles desde la explanada.
            for (int floor = 0; floor < 2; floor++)
            {
                float y = 2.15f + floor * 4.2f;
                for (int i = 0; i < 3; i++)
                {
                    float x = c.x - 7.2f + i * 6.5f;
                    Box("Ventana_Frontal_" + floor + "_" + i, b, new Vector3(x, y, c.z + 8.06f), new Vector3(2.4f, 1.7f, 0.12f), "glass");
                }
            }
            Box("Puerta_Principal", b, new Vector3(c.x + 7.4f, 1.45f, c.z + 8.08f), new Vector3(2.4f, 2.9f, 0.14f), "door");
        }

        private static void BuildFloorWalls(Transform parent, Vector3 center, float yBase, string mat)
        {
            float y = yBase + 2f;
            Box("Muro_Oeste_" + yBase, parent, center + new Vector3(-11.85f, y, 0f), new Vector3(0.3f, 4f, 16f), mat);
            Box("Muro_Este_" + yBase, parent, center + new Vector3(11.85f, y, 0f), new Vector3(0.3f, 4f, 16f), mat);
            Box("Muro_Norte_" + yBase, parent, center + new Vector3(0f, y, -7.85f), new Vector3(24f, 4f, 0.3f), mat);
            // Frente segmentado para que las puertas/ventanas se lean como huecos.
            Box("Frente_Izq_" + yBase, parent, center + new Vector3(-9.8f, y, 7.85f), new Vector3(4.1f, 4f, 0.3f), mat);
            Box("Frente_Centro_" + yBase, parent, center + new Vector3(0f, y, 7.85f), new Vector3(10.7f, 4f, 0.3f), mat);
            Box("Frente_Der_" + yBase, parent, center + new Vector3(9.9f, y, 7.85f), new Vector3(3.8f, 4f, 0.3f), mat);
        }

        private static void BuildFiringDeck(Transform parent)
        {
            Transform d = Group("03_Plataforma_De_Tiro", parent);
            Box("Tarima", d, new Vector3(0f, 0.42f, -14f), new Vector3(55f, 0.65f, 9f), "wood");
            for (int i = 0; i < 6; i++)
            {
                float x = -25f + i * 10f;
                Box("Puesto_" + (i + 1), d, new Vector3(x, 1.05f, -10.3f), new Vector3(8.2f, 0.95f, 0.35f), "woodDark");
                Box("Mesa_" + (i + 1), d, new Vector3(x, 1.25f, -13.2f), new Vector3(3.2f, 0.15f, 1.25f), "wood");
            }
            for (int i = 0; i < 8; i++)
                Box("Poste_" + i, d, new Vector3(-27f + i * 7.7f, 2.8f, -17.6f), new Vector3(0.28f, 5f, 0.28f), "woodDark");
        }

        private static void BuildShootingLanes(Transform parent)
        {
            Transform r = Group("04_Campo_De_Tiro", parent);
            float[] distances = { 8f, 26f, 48f, 72f };
            for (int row = 0; row < distances.Length; row++)
            {
                float z = distances[row] + 4f;
                int count = row < 2 ? 6 : 5;
                for (int i = 0; i < count; i++)
                {
                    float x = -25f + i * (50f / Mathf.Max(1, count - 1));
                    CreateTarget(r, "Blanco_" + (int)distances[row] + "m_" + (i + 1), new Vector3(x, 0f, z), row);
                }
            }

            // Barreras bajas azules y parapetos presentes alrededor del campo.
            for (int i = 0; i < 5; i++)
            {
                Box("Barrera_Azul_Izq_" + i, r, new Vector3(-40f, 0.75f, -2f + i * 18f), new Vector3(8f, 1.35f, 0.7f), "blueBarrier");
                Box("Barrera_Azul_Der_" + i, r, new Vector3(40f, 0.75f, -2f + i * 18f), new Vector3(8f, 1.35f, 0.7f), "blueBarrier");
            }
            Box("Muro_Fondo", r, new Vector3(0f, 1.15f, 89f), new Vector3(82f, 2.1f, 1.2f), "concreteDark");
        }

        private static void CreateTarget(Transform parent, string name, Vector3 pos, int row)
        {
            Transform t = Group(name, parent);
            Box("Poste", t, pos + new Vector3(0f, 1.05f, 0f), new Vector3(0.16f, 2.1f, 0.16f), "metal");
            Box("Cuerpo", t, pos + new Vector3(0f, 2.15f, 0f), new Vector3(0.8f + row * 0.05f, 1.15f, 0.14f), "target");
            Sphere("Cabeza", t, pos + new Vector3(0f, 3.0f, 0f), new Vector3(0.48f, 0.48f, 0.25f), "target");
            Box("Base", t, pos + new Vector3(0f, 0.12f, 0f), new Vector3(1.15f, 0.2f, 0.75f), "metal");
        }

        private static void BuildTireZones(Transform parent)
        {
            Transform t = Group("05_Neumaticos_Y_Obstaculos", parent);
            // Gran muro del fondo.
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 18; col++)
                Tire(t, new Vector3(-34f + col * 4f, 1f + row * 1.7f, 84f), Quaternion.Euler(90f, 0f, 0f));

            // Pilas laterales y zig-zag de entrenamiento.
            for (int pile = 0; pile < 8; pile++)
            {
                float x = pile % 2 == 0 ? -34f : 34f;
                float z = 4f + pile * 9f;
                for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    Tire(t, new Vector3(x + (col - 1) * 1.7f, 0.9f + row * 1.5f, z), Quaternion.Euler(90f, 0f, 0f));
            }

            // Neumaticos en el suelo para la pista fisica.
            for (int i = 0; i < 12; i++)
                Tire(t, new Vector3(23f + (i % 3) * 2.2f, 0.28f, -5f + (i / 3) * 2.4f), Quaternion.identity);
        }

        private static void Tire(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = "Neumatico";
            tire.transform.SetParent(parent, false);
            tire.transform.position = pos;
            tire.transform.rotation = rot;
            tire.transform.localScale = new Vector3(1.25f, 0.38f, 1.25f);
            SetMaterial(tire, "tire");

            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Hueco";
            hub.transform.SetParent(tire.transform, false);
            hub.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            hub.transform.localScale = new Vector3(0.52f, 1.02f, 0.52f);
            SetMaterial(hub, "tireHole");
            Collider c = hub.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c);
        }

        private static void BuildPerimeter(Transform parent)
        {
            Transform p = Group("06_Perimetro", parent);
            for (int i = 0; i <= 18; i++)
            {
                float z = -66f + i * 9f;
                FencePost(p, new Vector3(-48f, 1.5f, z));
                FencePost(p, new Vector3(48f, 1.5f, z));
            }
            for (int i = 0; i <= 10; i++)
            {
                float x = -45f + i * 9f;
                FencePost(p, new Vector3(x, 1.5f, 98f));
            }
            // Acceso frontal con garitas sencillas.
            Box("Garita_Izq", p, new Vector3(-9f, 1.65f, -64f), new Vector3(4f, 3.3f, 4f), "blue");
            Box("Garita_Der", p, new Vector3(9f, 1.65f, -64f), new Vector3(4f, 3.3f, 4f), "blue");
            Box("Porton_Izq", p, new Vector3(-3.5f, 1.5f, -64f), new Vector3(6.5f, 3f, 0.18f), "metal");
            Box("Porton_Der", p, new Vector3(3.5f, 1.5f, -64f), new Vector3(6.5f, 3f, 0.18f), "metal");
        }

        private static void FencePost(Transform p, Vector3 pos)
        {
            Box("Poste_Cerco", p, pos, new Vector3(0.18f, 3f, 0.18f), "metal");
            Box("Travesano_Sup", p, pos + new Vector3(0f, 1.15f, 0f), new Vector3(0.12f, 0.12f, 8.8f), "metal");
            Box("Travesano_Med", p, pos + new Vector3(0f, 0f, 0f), new Vector3(0.1f, 0.1f, 8.8f), "metal");
        }

        private static void BuildServiceArea(Transform parent)
        {
            Transform s = Group("07_Area_De_Servicio", parent);
            Box("Cobertizo", s, new Vector3(30f, 2.1f, -31f), new Vector3(15f, 4.2f, 10f), "sheet");
            Box("Techo_Cobertizo", s, new Vector3(30f, 4.35f, -31f), new Vector3(16f, 0.3f, 11f), "roof");
            for (int i = 0; i < 7; i++)
                Box("Caja_" + i, s, new Vector3(24f + (i % 4) * 2.2f, 0.6f, -22f + (i / 4) * 2.2f), new Vector3(1.7f, 1.2f, 1.7f), "crate");
            Box("Contenedor_1", s, new Vector3(36f, 1.35f, -11f), new Vector3(11f, 2.7f, 3.2f), "container");
            Box("Contenedor_2", s, new Vector3(34f, 1.35f, -5f), new Vector3(11f, 2.7f, 3.2f), "containerBlue");
        }

        private static void BuildLandscape(Transform parent)
        {
            Transform l = Group("08_Colinas_Y_Vegetacion", parent);
            Vector3[] hills =
            {
                new Vector3(-72f,-3f,55f), new Vector3(70f,-4f,62f), new Vector3(-66f,-4f,-34f),
                new Vector3(66f,-3f,-28f), new Vector3(0f,-7f,123f)
            };
            Vector3[] scales =
            {
                new Vector3(42f,16f,52f), new Vector3(48f,18f,55f), new Vector3(38f,14f,40f),
                new Vector3(42f,15f,46f), new Vector3(95f,24f,45f)
            };
            for (int i = 0; i < hills.Length; i++)
                Sphere("Colina_" + i, l, hills[i], scales[i], "hill");

            for (int i = 0; i < 46; i++)
            {
                float angle = i * 2.399963f;
                float radius = 55f + (i % 7) * 5.5f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * radius, 0f, 22f + Mathf.Sin(angle) * radius);
                if (Mathf.Abs(p.x) < 48f && p.z > -67f && p.z < 99f) continue;
                Tree(l, p, 0.8f + (i % 5) * 0.12f);
            }
        }

        private static void Tree(Transform parent, Vector3 pos, float scale)
        {
            Cylinder("Tronco", parent, pos + Vector3.up * 2.1f * scale, new Vector3(0.55f * scale, 2.1f * scale, 0.55f * scale), "trunk");
            Sphere("Copa", parent, pos + Vector3.up * 5.2f * scale, new Vector3(4.2f * scale, 5.2f * scale, 4.2f * scale), "foliage");
        }

        private static void EnsureSun(Transform parent)
        {
            if (Object.FindFirstObjectByType<Light>() != null) return;
            GameObject go = new GameObject("Sol_TrainingBase");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
        }

        private static Transform Group(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, string material)
        {
            return Primitive(PrimitiveType.Cube, name, parent, pos, scale, Quaternion.identity, material);
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 pos, Vector3 scale, string material)
        {
            return Primitive(PrimitiveType.Sphere, name, parent, pos, scale, Quaternion.identity, material);
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, string material)
        {
            return Primitive(PrimitiveType.Cylinder, name, parent, pos, scale, Quaternion.identity, material);
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, string material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            SetMaterial(go, material);
            return go;
        }

        private static void Rail(Transform parent, Vector3 pos, Vector3 scale)
        {
            Box("Baranda", parent, pos, scale, "metal");
        }

        private static void SetMaterial(GameObject go, string key)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = GetMaterial(key);
        }

        private static Material GetMaterial(string key)
        {
            if (Materials.TryGetValue(key, out Material cached) && cached != null) return cached;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material m = new Material(shader) { name = "TB_" + key };
            Color color = Color.gray;
            float smooth = 0.18f;
            float metallic = 0f;
            switch (key)
            {
                case "grass": color = new Color(0.24f,0.39f,0.16f); break;
                case "hill": color = new Color(0.21f,0.35f,0.13f); break;
                case "dirt": color = new Color(0.43f,0.34f,0.22f); break;
                case "dirtLight": color = new Color(0.49f,0.40f,0.28f); break;
                case "road": color = new Color(0.29f,0.29f,0.27f); break;
                case "lane": color = new Color(0.68f,0.62f,0.48f); break;
                case "blue": color = new Color(0.19f,0.48f,0.66f); break;
                case "blueBarrier": color = new Color(0.17f,0.42f,0.59f); break;
                case "containerBlue": color = new Color(0.16f,0.35f,0.49f); break;
                case "container": color = new Color(0.35f,0.39f,0.31f); break;
                case "concrete": color = new Color(0.55f,0.55f,0.50f); break;
                case "concreteDark": color = new Color(0.35f,0.36f,0.34f); break;
                case "roof": color = new Color(0.46f,0.18f,0.12f); break;
                case "wood": color = new Color(0.38f,0.25f,0.14f); break;
                case "woodDark": color = new Color(0.24f,0.15f,0.08f); break;
                case "metal": color = new Color(0.33f,0.35f,0.35f); metallic = 0.65f; smooth = 0.4f; break;
                case "sheet": color = new Color(0.47f,0.49f,0.45f); metallic = 0.35f; break;
                case "target": color = new Color(0.87f,0.84f,0.70f); break;
                case "tire": color = new Color(0.035f,0.035f,0.032f); smooth = 0.28f; break;
                case "tireHole": color = new Color(0.09f,0.09f,0.085f); break;
                case "glass": color = new Color(0.09f,0.16f,0.19f); smooth = 0.75f; break;
                case "door": color = new Color(0.16f,0.13f,0.10f); break;
                case "crate": color = new Color(0.39f,0.27f,0.15f); break;
                case "trunk": color = new Color(0.28f,0.18f,0.10f); break;
                case "foliage": color = new Color(0.16f,0.31f,0.10f); break;
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            Materials[key] = m;
            return m;
        }
    }
}
