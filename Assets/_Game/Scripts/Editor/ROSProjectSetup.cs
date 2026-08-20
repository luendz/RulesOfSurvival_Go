#if UNITY_EDITOR
using System.IO;
using ROS.Game.Animation;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Parachute;
using ROS.Game.UI;
using ROS.Game.Vehicles;
using ROS.Game.Weapons;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Editor
{
    public static class ROSProjectSetup
    {
        private const string Root = "Assets/_Game";
        private const string Data = Root + "/Data";
        private const string Animations = Root + "/Animations";
        private const string Prefabs = Root + "/Prefabs";
        private const string Scenes = Root + "/Scenes";

        [MenuItem("ROS Battle Royale/01 - Crear proyecto demo completo")]
        public static void CreateDemoProject()
        {
            EnsureFolders();
            GetOrCreateAnimatorController();
            var rifle = GetOrCreateRifle();
            var ammo = GetOrCreateItem("Item_556Ammo", "ammo_556", "Munición 5.56", ItemType.Ammo, 60, 0.02f);
            GetOrCreateItem("Item_Bandage", "heal_bandage", "Vendaje", ItemType.Healing, 10, 0.1f);
            GetOrCreateItem("Item_Backpack1", "backpack_1", "Mochila Nivel 1", ItemType.Backpack, 1, 1f);

            var playerPrefab = GetOrCreatePlayerPrefab(rifle);
            var lootPrefab = GetOrCreateLootPrefab();
            GetOrCreateVehiclePrefab();

            CreateCharacterTest(playerPrefab);
            CreateCombatTest(playerPrefab);
            CreateVehicleTest(playerPrefab);
            CreateParachuteTest(playerPrefab);
            CreateBattleRoyaleTest(playerPrefab, lootPrefab, ammo);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Scenes + "/07_BattleRoyaleTest.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("ROS Battle Royale", "Base creada. Abre Assets/_Game/Scenes/07_BattleRoyaleTest.unity y presiona Play.", "OK");
        }

        [MenuItem("ROS Battle Royale/02 - Abrir escena Battle Royale")]
        public static void OpenBattleRoyaleScene()
        {
            string path = Scenes + "/07_BattleRoyaleTest.unity";
            if (File.Exists(path)) EditorSceneManager.OpenScene(path);
            else EditorUtility.DisplayDialog("ROS Battle Royale", "Primero ejecuta: ROS Battle Royale > 01 - Crear proyecto demo completo", "OK");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Data);
            Directory.CreateDirectory(Animations);
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory(Scenes);
        }

        private static AnimatorController GetOrCreateAnimatorController()
        {
            string path = Animations + "/AC_Player_Prototype.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var layers = controller.layers;
            layers[0].name = "Locomotion";
            controller.layers = layers;
            controller.AddLayer("WeaponUpperBody");
            controller.AddLayer("Actions");
            controller.AddLayer("Overrides");

            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Prone", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Aim", AnimatorControllerParameterType.Bool);

            foreach (var layer in controller.layers)
                layer.stateMachine.AddState("PLACEHOLDER_" + layer.name);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static WeaponDefinition GetOrCreateRifle()
        {
            string path = Data + "/Weapon_PrototypeRifle.asset";
            var asset = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<WeaponDefinition>();
            asset.weaponId = "prototype_rifle";
            asset.displayName = "Prototype Rifle";
            asset.damage = 28f;
            asset.shotsPerSecond = 9f;
            asset.magazineSize = 30;
            asset.reloadTime = 2.2f;
            asset.emptyReloadTime = 2.7f;
            asset.supportsSingle = true;
            asset.supportsBurst = true;
            asset.supportsAuto = true;
            asset.range = 250f;
            asset.hipSpreadDegrees = 1.35f;
            asset.adsSpreadDegrees = 0.28f;
            asset.walkSpreadMultiplier = 1.25f;
            asset.runSpreadMultiplier = 1.65f;
            asset.sprintSpreadMultiplier = 2.4f;
            asset.crouchSpreadMultiplier = 0.72f;
            asset.airborneSpreadMultiplier = 2.75f;
            asset.spreadBloomPerShot = 0.10f;
            asset.maxSpreadBloom = 1.2f;
            asset.spreadRecoveryPerSecond = 3.5f;
            asset.spreadDegrees = 0.7f;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static InventoryItemDefinition GetOrCreateItem(string file, string id, string name, ItemType type, int stack, float weight)
        {
            string path = $"{Data}/{file}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<InventoryItemDefinition>();
            asset.itemId = id;
            asset.displayName = name;
            asset.itemType = type;
            asset.maxStack = stack;
            asset.weight = weight;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static GameObject GetOrCreatePlayerPrefab(WeaponDefinition rifle)
        {
            string path = Prefabs + "/Player_Prototype.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject("Player_Prototype");
            root.tag = "Player";
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.32f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            root.AddComponent<PlayerInputReader>();
            root.AddComponent<Health>();
            root.AddComponent<InventoryComponent>();
            root.AddComponent<PlayerInteractor>();

            var visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(root.transform, false);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "PlaceholderCharacter_REPLACE_ME";
            visual.transform.SetParent(visualRoot, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.65f, 0.9f, 0.65f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visualRoot.gameObject.AddComponent<PlayerVisualAdapter>();

            var motor = root.AddComponent<PlayerMotor>();
            SetObjectReference(motor, "visualRoot", visualRoot);
            var animatorDriver = visualRoot.gameObject.AddComponent<PlayerAnimatorDriver>();
            SetObjectReference(animatorDriver, "motor", motor);
            SetObjectReference(animatorDriver, "input", root.GetComponent<PlayerInputReader>());

            var weaponRoot = new GameObject("WeaponRoot").transform;
            weaponRoot.SetParent(root.transform, false);
            var weapon = new GameObject("PrototypeRifle");
            weapon.transform.SetParent(weaponRoot, false);
            weapon.transform.localPosition = new Vector3(0.35f, 1.2f, 0.35f);
            var wc = weapon.AddComponent<WeaponController>();
            SetObjectReference(wc, "definition", rifle);
            SetObjectReference(wc, "input", root.GetComponent<PlayerInputReader>());

            var gunVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunVisual.name = "PlaceholderRifle_REPLACE_ME";
            gunVisual.transform.SetParent(weapon.transform, false);
            gunVisual.transform.localScale = new Vector3(0.08f, 0.08f, 0.65f);
            Object.DestroyImmediate(gunVisual.GetComponent<Collider>());
            var muzzle = new GameObject("MuzzlePoint").transform;
            muzzle.SetParent(weapon.transform, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.7f);
            SetObjectReference(wc, "muzzle", muzzle);

            root.AddComponent<WeaponEquipmentController>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject GetOrCreateLootPrefab()
        {
            string path = Prefabs + "/LootPickup_Prototype.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LootPickup_Prototype";
            go.transform.localScale = Vector3.one * 0.35f;
            var pickup = go.AddComponent<LootPickup>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject GetOrCreateVehiclePrefab()
        {
            string path = Prefabs + "/Vehicle_Prototype.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = "Vehicle_Prototype";
            vehicle.transform.localScale = new Vector3(1.8f, 0.7f, 3.6f);
            var rb = vehicle.AddComponent<Rigidbody>();
            rb.mass = 1200f;
            var controller = vehicle.AddComponent<SimpleVehicleController>();

            var seat = new GameObject("DriverSeat").transform;
            seat.SetParent(vehicle.transform, false);
            seat.localPosition = new Vector3(-0.35f, 0.7f, 0f);
            var exit = new GameObject("ExitPoint").transform;
            exit.SetParent(vehicle.transform, false);
            exit.localPosition = new Vector3(1.5f, 0.5f, 0f);
            var seatComp = vehicle.AddComponent<VehicleSeat>();
            SetObjectReference(seatComp, "seatPoint", seat);
            SetObjectReference(seatComp, "exitPoint", exit);
            SetObjectReference(seatComp, "vehicle", controller);

            var prefab = PrefabUtility.SaveAsPrefabAsset(vehicle, path);
            Object.DestroyImmediate(vehicle);
            return prefab;
        }

        private static void CreateCharacterTest(GameObject playerPrefab)
        {
            var scene = NewBaseScene("03_CharacterTest", out _);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.zero;
            CreateCamera(player);
            Save(scene, "03_CharacterTest");
        }

        private static void CreateCombatTest(GameObject playerPrefab)
        {
            var scene = NewBaseScene("04_CombatTest", out _);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            CreateCamera(player);
            for (int i = 0; i < 6; i++)
            {
                var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = $"DamageTarget_{i + 1}";
                target.transform.position = new Vector3((i - 3) * 2.5f, 1f, 15f + (i % 2) * 5f);
                target.transform.localScale = new Vector3(1.2f, 2f, 1.2f);
                target.AddComponent<Health>();
            }
            Save(scene, "04_CombatTest");
        }

        private static void CreateVehicleTest(GameObject playerPrefab)
        {
            var scene = NewBaseScene("05_VehicleTest", out _);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            CreateCamera(player);
            var vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Vehicle_Prototype.prefab");
            var vehicle = (GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab);
            vehicle.transform.position = new Vector3(4f, 1f, 5f);
            Save(scene, "05_VehicleTest");
        }

        private static void CreateParachuteTest(GameObject playerPrefab)
        {
            var scene = NewBaseScene("06_ParachuteTest", out _);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 100f, 0f);
            CreateCamera(player);
            var parachuteVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            parachuteVisual.name = "PlaceholderParachute_REPLACE_ME";
            parachuteVisual.transform.SetParent(player.transform, false);
            parachuteVisual.transform.localPosition = new Vector3(0f, 3f, 0f);
            parachuteVisual.transform.localScale = new Vector3(4f, 0.35f, 2f);
            Object.DestroyImmediate(parachuteVisual.GetComponent<Collider>());
            var parachute = player.AddComponent<ParachuteController>();
            SetObjectReference(parachute, "parachuteVisual", parachuteVisual);
            var starter = player.AddComponent<ParachuteDemoStarter>();
            SetObjectReference(starter, "parachute", parachute);
            Save(scene, "06_ParachuteTest");
        }

        private static void CreateBattleRoyaleTest(GameObject playerPrefab, GameObject lootPrefab, InventoryItemDefinition ammo)
        {
            var scene = NewBaseScene("07_BattleRoyaleTest", out var ground);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.zero;
            CreateCamera(player);

            var zoneObject = new GameObject("SafeZone");
            var zone = zoneObject.AddComponent<SafeZoneController>();
            var matchObject = new GameObject("BattleRoyaleManager");
            var match = matchObject.AddComponent<BattleRoyaleManager>();
            SetObjectReference(match, "safeZone", zone);
            var bootstrap = matchObject.AddComponent<DemoBootstrap>();
            SetObjectReference(bootstrap, "matchManager", match);
            new GameObject("PrototypeHUD_REPLACE_LATER").AddComponent<HudPresenter>();

            for (int i = 0; i < 14; i++)
            {
                Vector2 p = Random.insideUnitCircle * 35f;
                var loot = (GameObject)PrefabUtility.InstantiatePrefab(lootPrefab);
                loot.transform.position = new Vector3(p.x, 0.25f, p.y);
                loot.GetComponent<LootPickup>().Configure(ammo, Random.Range(15, 61));
            }

            for (int i = 0; i < 20; i++)
            {
                var cover = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                cover.name = "EnvironmentPlaceholder_REPLACE_ME";
                Vector2 p = Random.insideUnitCircle * 70f;
                cover.transform.position = new Vector3(p.x, 1f, p.y);
                cover.transform.localScale = new Vector3(Random.Range(1f, 4f), Random.Range(1f, 4f), Random.Range(1f, 4f));
            }

            Save(scene, "07_BattleRoyaleTest");
        }

        private static Scene NewBaseScene(string name, out GameObject ground)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            return scene;
        }

        private static UnityEngine.Camera CreateCamera(GameObject player)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<UnityEngine.Camera>();
            cameraGo.AddComponent<AudioListener>();
            var thirdPerson = cameraGo.AddComponent<ThirdPersonCamera>();
            var input = player.GetComponent<PlayerInputReader>();
            thirdPerson.Bind(player.transform, input);
            player.GetComponent<PlayerMotor>().SetCamera(cameraGo.transform);

            var weapon = player.GetComponentInChildren<WeaponController>();
            if (weapon != null) SetObjectReference(weapon, "aimCamera", camera);
            return camera;
        }

        private static void Save(Scene scene, string name)
        {
            EditorSceneManager.SaveScene(scene, $"{Scenes}/{name}.unity");
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

}
#endif
