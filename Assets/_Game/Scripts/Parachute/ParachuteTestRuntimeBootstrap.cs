using ROS.Game.BattleRoyale;
using ROS.Game.Input;
using ROS.Game.UI;
using ROS.Game.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.Parachute
{
    public static class ParachuteTestRuntimeBootstrap
    {
        private const string TestSceneName = "06_ParachuteTest";
        private const string ParachuteResource =
            "Parachute/PF_ParachuteVisual";
        private const string AirplaneResource =
            "Parachute/PF_AirplaneStart";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Initialize()
        {
            if (SceneManager.GetActiveScene().name != TestSceneName)
            {
                return;
            }

            ParachuteController parachute =
                Object.FindFirstObjectByType<ParachuteController>();

            if (parachute == null)
            {
                Debug.LogError(
                    "06_ParachuteTest no contiene un jugador con ParachuteController."
                );
                return;
            }

            DisableLegacyStarter(parachute);

            GameObject parachutePrefab =
                Resources.Load<GameObject>(ParachuteResource);
            GameObject airplanePrefab =
                Resources.Load<GameObject>(AirplaneResource);

            if (parachutePrefab == null || airplanePrefab == null)
            {
                Debug.LogError(
                    "Faltan los prefabs del inicio de partida. " +
                    "Ejecuta ROS Battle Royale/Build Parachute Match Start."
                );
                return;
            }

            DisablePlaceholder(parachute.transform);
            GameObject parachuteVisual = Object.Instantiate(
                parachutePrefab,
                parachute.transform
            );
            parachuteVisual.name = "ParachuteVisual";
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
            airplaneObject.name = "Airplane_MatchStart";
            AirplaneController airplane =
                airplaneObject.GetComponent<AirplaneController>();

            if (airplane == null)
            {
                airplane = airplaneObject.AddComponent<AirplaneController>();
            }

            BattleRoyaleManager manager =
                Object.FindFirstObjectByType<BattleRoyaleManager>();

            if (manager == null)
            {
                manager = new GameObject("BattleRoyaleManager")
                    .AddComponent<BattleRoyaleManager>();
            }

            PlayerInputReader input =
                parachute.GetComponent<PlayerInputReader>();
            float flightHeight = Mathf.Max(
                100f,
                parachute.transform.position.y
            );
            Vector3 routeStart =
                new Vector3(-90f, flightHeight, -35f);
            Vector3 routeEnd =
                new Vector3(90f, flightHeight, 35f);

            MatchStartController sequence =
                new GameObject("MatchStartController")
                    .AddComponent<MatchStartController>();

            sequence.Configure(
                manager,
                airplane,
                parachute,
                input,
                routeStart,
                routeEnd,
                3f,
                24f
            );

            MatchStartHud hud =
                sequence.gameObject.AddComponent<MatchStartHud>();
            hud.Configure(sequence, parachute);
            sequence.BeginSequence();
        }

        private static void DisableLegacyStarter(
            ParachuteController parachute
        )
        {
            ParachuteDemoStarter starter =
                parachute.GetComponent<ParachuteDemoStarter>();

            if (starter != null)
            {
                starter.enabled = false;
            }
        }

        private static void DisablePlaceholder(Transform player)
        {
            Transform placeholder = player.Find(
                "PlaceholderParachute_REPLACE_ME"
            );

            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(false);
            }
        }
    }
}
