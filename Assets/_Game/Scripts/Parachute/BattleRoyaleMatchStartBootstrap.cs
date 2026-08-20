using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.UI;
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
                3f,
                28f
            );

            MatchStartHud hud = flowObject.AddComponent<MatchStartHud>();
            hud.Configure(sequence, parachute);

            BattleRoyaleStartMenu menu =
                flowObject.AddComponent<BattleRoyaleStartMenu>();
            menu.Configure(sequence, input, playerCamera);
        }
    }
}
