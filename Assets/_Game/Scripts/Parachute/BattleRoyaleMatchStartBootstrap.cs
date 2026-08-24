using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Gameplay;
using ROS.Game.Input;
using ROS.Game.Inventory;
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
        private const string AirplaneResource =
            "Parachute/PF_AirplaneStart";
        private const string ParachuteResource =
            "Parachute/PF_ParachuteVisual";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Initialize()
        {
            if (SceneManager.GetActiveScene().name != BattleRoyaleScene)
                return;

            if (Object.FindFirstObjectByType<MatchStartController>() != null)
                return;

            DemoBootstrap demo =
                Object.FindFirstObjectByType<DemoBootstrap>();
            if (demo != null)
                demo.SetBeginOnStart(false);

            PlayerInputReader input =
                Object.FindFirstObjectByType<PlayerInputReader>();
            BattleRoyaleManager manager =
                Object.FindFirstObjectByType<BattleRoyaleManager>();
            ThirdPersonCamera playerCamera =
                Object.FindFirstObjectByType<ThirdPersonCamera>();

            if (input == null || manager == null)
            {
                Debug.LogError(
                    "Battle Royale requiere jugador y BattleRoyaleManager."
                );
                return;
            }

            // EDITOR FIRST: el jugador principal debe llegar completo desde la escena.
            ParachuteController parachute =
                input.GetComponent<ParachuteController>();
            if (parachute == null)
            {
                Debug.LogError(
                    "[Editor First] Falta ParachuteController fisico en el jugador. " +
                    "No se agregara en runtime."
                );
                return;
            }

            Transform parachuteVisualTransform =
                input.transform.Find("BattleRoyaleParachuteVisual");
            if (parachuteVisualTransform == null)
            {
                Debug.LogError(
                    "[Editor First] Falta BattleRoyaleParachuteVisual fisico en el jugador. " +
                    "No se instanciara en runtime."
                );
                return;
            }

            parachute.ConfigureVisual(parachuteVisualTransform.gameObject);

            // El avion y el flujo BR siguen siendo dinamicos por ahora; se revisaran despues.
            GameObject airplanePrefab =
                Resources.Load<GameObject>(AirplaneResource);
            if (airplanePrefab == null)
            {
                Debug.LogError(
                    "No se encontro el prefab de avion Battle Royale."
                );
                return;
            }

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
                airplane = airplaneObject.AddComponent<AirplaneController>();

            if (airplaneObject.GetComponent<AirplaneFlightEffects>() == null)
                airplaneObject.AddComponent<AirplaneFlightEffects>();

            airplane.PrepareRoute(RouteStart, RouteEnd);

            GameObject flowObject = new GameObject("BattleRoyaleMatchStart");
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

            GameObject parachutePrefab =
                Resources.Load<GameObject>(ParachuteResource);

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

            // EDITOR FIRST: todos los componentes del jugador deben existir ya.
            RequireExisting<DamageNumberSpawner>(input.gameObject);
            RequireExisting<WeaponAmmoConnector>(input.gameObject);
            ConsumableController consumable =
                RequireExisting<ConsumableController>(input.gameObject);
            PlayerLootEquipment lootEquipment =
                RequireExisting<PlayerLootEquipment>(input.gameObject);

            Health localHealth = input.GetComponent<Health>();
            ProtectiveEquipment protection =
                input.GetComponent<ProtectiveEquipment>();
            WeaponEquipmentController weaponEquip =
                input.GetComponent<WeaponEquipmentController>();
            InventoryComponent inventory =
                input.GetComponent<InventoryComponent>();

            // EDITOR FIRST: el HUD solo enlaza componentes fisicos existentes.
            MatchStartHud matchHud =
                Object.FindFirstObjectByType<MatchStartHud>();
            if (matchHud != null)
                matchHud.Configure(sequence, parachute);

            KillFeedPresenter killFeed =
                Object.FindFirstObjectByType<KillFeedPresenter>();
            if (killFeed != null)
                killFeed.Bind(manager, localHealth);

            DamageDirectionIndicator damageDir =
                Object.FindFirstObjectByType<DamageDirectionIndicator>();
            if (damageDir != null)
                damageDir.Bind(localHealth, input.transform);

            EquipmentStatusPresenter equipStatus =
                Object.FindFirstObjectByType<EquipmentStatusPresenter>();
            if (equipStatus != null)
                equipStatus.Bind(lootEquipment, protection);

            MinimapSystem minimap =
                Object.FindFirstObjectByType<MinimapSystem>();
            if (minimap != null)
            {
                minimap.Bind(
                    input.transform,
                    manager.SafeZone,
                    botDirector
                );
            }

            CompassUI compass =
                Object.FindFirstObjectByType<CompassUI>();
            if (compass != null)
                compass.Bind(input.transform, playerCamera);

            WeaponSlotsPresenter weaponSlots =
                Object.FindFirstObjectByType<WeaponSlotsPresenter>();
            if (weaponSlots != null)
                weaponSlots.Bind(weaponEquip, lootEquipment);

            QuickConsumePresenter quickConsume =
                Object.FindFirstObjectByType<QuickConsumePresenter>();
            if (quickConsume != null)
                quickConsume.Bind(inventory, consumable);

            DeathLootPanelPresenter deathLootPanel =
                Object.FindFirstObjectByType<DeathLootPanelPresenter>();
            if (deathLootPanel != null)
                deathLootPanel.Bind(input.gameObject);

            BattleRoyaleStartMenu menu =
                Object.FindFirstObjectByType<BattleRoyaleStartMenu>();
            if (menu != null)
            {
                menu.Configure(sequence, input, playerCamera);
            }
            else
            {
                Debug.LogWarning(
                    "[Editor First] No existe BattleRoyaleStartMenu fisico en 07."
                );
            }
        }

        private static T RequireExisting<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning(
                    "[Editor First] Falta " + typeof(T).Name +
                    " fisico en " + target.name +
                    ". No se agregara en runtime."
                );
            }
            return component;
        }
    }
}
