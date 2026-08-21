using System.Collections.Generic;
using ROS.Game.BattleRoyale;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Parachute;
using ROS.Game.Teams;
using ROS.Game.UI;
using ROS.Game.Weapons;
using ROS.Game.World;
using UnityEngine;

namespace ROS.Game.AI
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleBotDirector : MonoBehaviour
    {
        public const int DefaultBotCount = 10;

        private readonly List<BattleRoyaleBotController> _bots =
            new List<BattleRoyaleBotController>();

        private GameObject _sourcePlayer;
        private AirplaneController _airplane;
        private BattleRoyaleManager _matchManager;
        private GameObject _parachutePrefab;
        private MatchStartController _sequence;
        private int _botCount;

        public IReadOnlyList<BattleRoyaleBotController> Bots => _bots;

        public void Configure(
            GameObject sourcePlayer,
            AirplaneController airplane,
            BattleRoyaleManager matchManager,
            GameObject parachutePrefab,
            MatchStartController sequence,
            int botCount = DefaultBotCount
        )
        {
            _sourcePlayer = sourcePlayer;
            _airplane = airplane;
            _matchManager = matchManager;
            _parachutePrefab = parachutePrefab;
            _sequence = sequence;
            _botCount = Mathf.Max(0, botCount);

            if (_sourcePlayer == null ||
                _airplane == null ||
                _matchManager == null ||
                _sequence == null)
            {
                Debug.LogError(
                    "No se pudieron crear los bots: faltan referencias.",
                    this
                );
                return;
            }

            SpawnBots();
            PrepareBotsForFlight();

            _sequence.SequenceStarted -= PrepareBotsForFlight;
            _sequence.SequenceStarted += PrepareBotsForFlight;
        }

        private void SpawnBots()
        {
            for (int i = 0; i < _botCount; i++)
            {
                GameObject botObject = Instantiate(_sourcePlayer);
                botObject.name = $"Bot_BattleRoyale_{i + 1:00}";
                ConfigureBotOnlyComponents(botObject);

                ParachuteController parachute =
                    EnsureParachute(botObject);
                if (parachute == null)
                {
                    Destroy(botObject);
                    continue;
                }

                TeamComponent team =
                    botObject.GetComponent<TeamComponent>();
                if (team == null)
                {
                    team = botObject.AddComponent<TeamComponent>();
                }
                team.Assign(i + 1, 0);

                BattleRoyaleBotController bot =
                    botObject.AddComponent<BattleRoyaleBotController>();
                bot.Configure(
                    i,
                    _airplane,
                    _matchManager,
                    CalculateLandingTarget(i, _botCount),
                    CalculateJumpProgress(i),
                    _matchManager != null ? _matchManager.SafeZone : null
                );

                Health health = botObject.GetComponent<Health>();
                if (health != null)
                {
                    health.OverrideMaxHealth(200f);
                    _matchManager.RegisterPlayer(health);
                }

                BotHealthBar.Attach(botObject);
                ApplyBotDamageScale(botObject, 0.20f);
                botObject.AddComponent<CharacterDeathDissolver>();

                // Loot al morir
                PlayerEliminationController elim =
                    botObject.GetComponent<PlayerEliminationController>()
                    ?? botObject.AddComponent<PlayerEliminationController>();
                elim.Bind(_matchManager);

                InventoryComponent inv = botObject.GetComponent<InventoryComponent>();
                FillBotInventory(inv, i);

                _bots.Add(bot);
            }

            PlayerDamageRuntimeSetup.ConfigureExistingPlayers();
        }

        private ParachuteController EnsureParachute(GameObject botObject)
        {
            ParachuteController parachute =
                botObject.GetComponent<ParachuteController>();
            if (parachute == null)
            {
                parachute = botObject.AddComponent<ParachuteController>();
            }

            Transform visual = botObject.transform.Find(
                "BattleRoyaleParachuteVisual"
            );
            if (visual == null && _parachutePrefab != null)
            {
                GameObject visualObject = Instantiate(
                    _parachutePrefab,
                    botObject.transform
                );
                visualObject.name = "BattleRoyaleParachuteVisual";
                visualObject.transform.localPosition =
                    new Vector3(0f, 3.2f, 0f);
                visualObject.transform.localRotation = Quaternion.identity;
                visual = visualObject.transform;
            }

            if (visual != null)
            {
                if (visual.childCount > 0)
                {
                    visual.GetChild(0).localRotation = Quaternion.Euler(
                        ParachuteController.ModelEulerAngles
                    );
                }

                parachute.ConfigureVisual(visual.gameObject);
            }

            return parachute;
        }

        private void ConfigureBotOnlyComponents(GameObject botObject)
        {
            PlayerInputReader input =
                botObject.GetComponent<PlayerInputReader>();
            if (input != null)
            {
                input.EnableExternalControl();
            }

            PlayerInteractor interactor =
                botObject.GetComponent<PlayerInteractor>();
            if (interactor != null)
            {
                interactor.enabled = false;
            }

            DisableAll<CombatFeedbackPresenter>(botObject);
            DisableAll<NearbyLootPresenter>(botObject);
            DisableAll<DamageDebugControls>(botObject);
            DisableAll<VitalsDebugTester>(botObject);
            DisableAll<UnityEngine.Camera>(botObject);
            DisableAll<AudioListener>(botObject);
        }

        private void PrepareBotsForFlight()
        {
            if (_airplane == null)
            {
                return;
            }

            for (int i = 0; i < _bots.Count; i++)
            {
                BattleRoyaleBotController bot = _bots[i];
                if (bot == null)
                {
                    continue;
                }

                int column = i % 5;
                int row = i / 5;
                Vector3 passengerOffset = new Vector3(
                    (column - 2f) * 0.58f,
                    0f,
                    (row - 1.5f) * 0.68f
                );
                bot.PrepareForFlight(
                    _airplane.PassengerAnchor,
                    passengerOffset
                );
            }
        }

        private static Vector3 CalculateLandingTarget(
            int index,
            int count
        )
        {
            float safeCount = Mathf.Max(1, count);
            float angle = index / safeCount * Mathf.PI * 2f +
                          (index % 4) * 0.11f;
            float radiusDistribution = Mathf.Repeat(
                index * 0.61803398875f,
                1f
            );
            float radius = Mathf.Lerp(20f, 76f, radiusDistribution);
            return new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
        }

        private static float CalculateJumpProgress(int index)
        {
            float shuffled = Mathf.Repeat(
                index * 0.61803398875f,
                1f
            );
            return Mathf.Lerp(0.1f, 0.82f, shuffled);
        }

        private static void DisableAll<T>(GameObject root)
            where T : Behaviour
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    components[i].enabled = false;
                }
            }
        }

        private static void ApplyBotDamageScale(GameObject botObject, float scale)
        {
            WeaponController[] weapons =
                botObject.GetComponentsInChildren<WeaponController>(true);
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                    weapons[i].DamageScale = scale;
            }
        }

        private static void FillBotInventory(InventoryComponent inv, int botIndex)
        {
            if (inv == null) return;

            InventoryItemDefinition[] allDefs =
                Resources.FindObjectsOfTypeAll<InventoryItemDefinition>();

            System.Random rng = new System.Random(7919 * botIndex + 1337);

            // Un ítem de cada categoría relevante
            ItemType[] wanted = new ItemType[]
            {
                ItemType.Ammo,
                ItemType.Healing,
                ItemType.Armor,
                ItemType.Helmet,
            };

            foreach (ItemType target in wanted)
            {
                foreach (InventoryItemDefinition def in allDefs)
                {
                    if (def == null || def.itemType != target) continue;

                    int amount = target == ItemType.Ammo
                        ? rng.Next(20, 61)
                        : 1;

                    inv.Add(def, amount);
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            if (_sequence != null)
            {
                _sequence.SequenceStarted -= PrepareBotsForFlight;
            }
        }
    }
}
