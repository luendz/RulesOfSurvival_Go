using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Gameplay;
using ROS.Game.Input;
using ROS.Game.Loot;
using ROS.Game.UI;
using ROS.Game.Weapons;
using ROS.Game.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Parachute
{
    public static class BattleRoyaleMatchStartBootstrap
    {
        public static readonly Vector3 RouteStart =
            new Vector3(-90f, 105f, -35f);
        public static readonly Vector3 RouteEnd =
            new Vector3(90f, 105f, 35f);

        private const string BattleRoyaleScene = "07_BattleRoyaleTest";
        private const string ParachuteResource =
            "Parachute/PF_ParachuteVisual";
        private const string AirplaneResource =
            "Parachute/PF_AirplaneStart";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Initialize()
        {
            if (SceneManager.GetActiveScene().name != BattleRoyaleScene ||
                Object.FindFirstObjectByType<BattleRoyaleStartMenu>() != null)
            {
                return;
            }

            DemoBootstrap demo =
                Object.FindFirstObjectByType<DemoBootstrap>();
            if (demo != null)
            {
                demo.SetBeginOnStart(false);
            }

            PlayerInputReader input =
                Object.FindFirstObjectByType<PlayerInputReader>();
            BattleRoyaleManager manager =
                Object.FindFirstObjectByType<BattleRoyaleManager>();
            ThirdPersonCamera playerCamera =
                Object.FindFirstObjectByType<ThirdPersonCamera>();

            if (input == null || manager == null)
            {
                Debug.LogError(
                    "Battle Royale requiere jugador y BattleRoyaleManager " +
                    "para mostrar el menú de inicio."
                );
                return;
            }

            GameObject parachutePrefab =
                Resources.Load<GameObject>(ParachuteResource);
            GameObject airplanePrefab =
                Resources.Load<GameObject>(AirplaneResource);

            if (parachutePrefab == null || airplanePrefab == null)
            {
                Debug.LogError(
                    "No se encontraron los prefabs de avión y paracaídas."
                );
                return;
            }

            ParachuteController parachute =
                input.GetComponent<ParachuteController>();
            if (parachute == null)
            {
                parachute = input.gameObject.AddComponent<ParachuteController>();
            }

            GameObject parachuteVisual = Object.Instantiate(
                parachutePrefab,
                input.transform
            );
            parachuteVisual.name = "BattleRoyaleParachuteVisual";
            parachuteVisual.transform.localPosition =
                new Vector3(0f, 3.2f, 0f);
            parachuteVisual.transform.localRotation = Quaternion.identity;
            if (parachuteVisual.transform.childCount > 0)
            {
                parachuteVisual.transform.GetChild(0).localRotation =
                    Quaternion.Euler(
                        ParachuteController.ModelEulerAngles
                    );
            }

            parachute.ConfigureVisual(parachuteVisual);

            GameObject airplaneObject = Object.Instantiate(airplanePrefab);
            airplaneObject.name = "Airplane_BattleRoyale";
            if (airplaneObject.transform.childCount > 0)
            {
                airplaneObject.transform.GetChild(0).localRotation =
                    Quaternion.Euler(AirplaneController.ModelEulerAngles);
            }

            AirplaneController airplane =
                airplaneObject.GetComponent<AirplaneController>();
            if (airplane == null)
            {
                airplane = airplaneObject.AddComponent<AirplaneController>();
            }

            if (airplaneObject.GetComponent<AirplaneFlightEffects>() == null)
            {
                airplaneObject.AddComponent<AirplaneFlightEffects>();
            }

            airplane.PrepareRoute(RouteStart, RouteEnd);

            GameObject flowObject = new GameObject(
                "BattleRoyaleMatchStart"
            );
            MatchStartController sequence =
                flowObject.AddComponent<MatchStartController>();
            sequence.Configure(
                manager,
                airplane,
                parachute,
                input,
                RouteStart,
                RouteEnd,
                0f,
                28f
            );

            BattleRoyaleBotDirector botDirector =
                flowObject.AddComponent<BattleRoyaleBotDirector>();
            botDirector.Configure(
                input.gameObject,
                airplane,
                manager,
                parachutePrefab,
                sequence,
                BattleRoyaleBotDirector.DefaultBotCount
            );

            MatchStartHud hud = flowObject.AddComponent<MatchStartHud>();
            hud.Configure(sequence, parachute);

            input.gameObject.AddComponent<DamageNumberSpawner>();

            // Conectar munición del inventario a las armas
            input.gameObject.AddComponent<WeaponAmmoConnector>();

            // Sistema de consumibles (vendaje/botiquín con tecla F)
            input.gameObject.AddComponent<ConsumableController>();

            // HUD: kill feed, indicador de dirección de daño, estado del equipo
            Health localHealth = input.GetComponent<Health>();
            ProtectiveEquipment protection = input.GetComponent<ProtectiveEquipment>();

            // LootRuntimeSetup también agrega PlayerLootEquipment pero el orden
            // de RuntimeInitializeOnLoadMethod no está garantizado; nos aseguramos aquí.
            PlayerLootEquipment lootEquipment = input.GetComponent<PlayerLootEquipment>()
                ?? input.gameObject.AddComponent<PlayerLootEquipment>();

            KillFeedPresenter killFeed = flowObject.AddComponent<KillFeedPresenter>();
            killFeed.Bind(manager, localHealth);

            DamageDirectionIndicator damageDir =
                flowObject.AddComponent<DamageDirectionIndicator>();
            damageDir.Bind(localHealth, input.transform);

            EquipmentStatusPresenter equipStatus =
                flowObject.AddComponent<EquipmentStatusPresenter>();
            equipStatus.Bind(lootEquipment, protection);

            // Minimapa
            MinimapSystem minimap = flowObject.AddComponent<MinimapSystem>();
            minimap.Bind(
                input.transform,
                manager.SafeZone,
                flowObject.GetComponent<BattleRoyaleBotDirector>()
            );

            // Brujula (usa el yaw de la camara para reflejar free-look)
            CompassUI compass = flowObject.AddComponent<CompassUI>();
            compass.Bind(input.transform, playerCamera);

            DeathLootPanelPresenter deathLootPanel =
                new GameObject("DeathLootPanelPresenter")
                    .AddComponent<DeathLootPanelPresenter>();
            deathLootPanel.Bind(input.gameObject);

            BattleRoyaleStartMenu menu =
                flowObject.AddComponent<BattleRoyaleStartMenu>();
            menu.Configure(sequence, input, playerCamera);
        }
    }
}
