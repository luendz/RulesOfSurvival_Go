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

        [Header("Battle Royale References")]
        [SerializeField] private GameObject sourcePlayer;
        [SerializeField] private AirplaneController airplane;
        [SerializeField] private BattleRoyaleManager matchManager;
        [SerializeField] private GameObject parachutePrefab;
        [SerializeField] private MatchStartController sequence;

        [Header("Bots")]
        [Min(0)]
        [SerializeField] private int botCount = DefaultBotCount;

        private readonly List<BattleRoyaleBotController> _bots =
            new List<BattleRoyaleBotController>();

        private bool _spawned;

        public IReadOnlyList<BattleRoyaleBotController> Bots => _bots;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            // En Editor First las referencias quedan serializadas en la escena.
            // Los bots NO se crean aquí: esperan a que el jugador pulse
            // INICIAR PARTIDA BR y MatchStartController emita SequenceStarted.
            Subscribe();
        }

        public void Configure(
            GameObject source,
            AirplaneController flight,
            BattleRoyaleManager manager,
            GameObject parachute,
            MatchStartController startSequence,
            int count = DefaultBotCount
        )
        {
            Unsubscribe();
            sourcePlayer = source;
            airplane = flight;
            matchManager = manager;
            parachutePrefab = parachute;
            sequence = startSequence;
            botCount = Mathf.Max(0, count);
            Subscribe();
        }

        private void Subscribe()
        {
            if (sequence == null)
                return;

            sequence.SequenceStarted -= HandleBattleRoyaleStarted;
            sequence.SequenceStarted += HandleBattleRoyaleStarted;
        }

        private void Unsubscribe()
        {
            if (sequence != null)
                sequence.SequenceStarted -= HandleBattleRoyaleStarted;
        }

        private void HandleBattleRoyaleStarted()
        {
            if (!ValidateReferences())
                return;

            if (!_spawned)
            {
                SpawnBots();
                _spawned = true;
            }

            PrepareBotsForFlight();
        }

        private bool ValidateReferences()
        {
            if (sourcePlayer != null &&
                airplane != null &&
                matchManager != null &&
                sequence != null)
            {
                return true;
            }

            Debug.LogError(
                "[BattleRoyaleBotDirector] No se pudieron activar los bots: " +
                "faltan Source Player, Airplane, BattleRoyaleManager o MatchStartController.",
                this
            );
            return false;
        }

        private void SpawnBots()
        {
            _bots.Clear();

            int safeCount = Mathf.Max(0, botCount);
            for (int i = 0; i < safeCount; i++)
            {
                GameObject botObject = Instantiate(sourcePlayer);
                botObject.name = $"Bot_BattleRoyale_{i + 1:00}";
                ConfigureBotOnlyComponents(botObject);

                ParachuteController parachute = EnsureParachute(botObject);
                if (parachute == null)
                {
                    Destroy(botObject);
                    continue;
                }

                TeamComponent team = botObject.GetComponent<TeamComponent>();
                if (team == null)
                    team = botObject.AddComponent<TeamComponent>();

                team.Assign(i + 1, 0);

                BattleRoyaleBotController bot =
                    botObject.GetComponent<BattleRoyaleBotController>();
                if (bot == null)
                    bot = botObject.AddComponent<BattleRoyaleBotController>();

                bot.Configure(
                    i,
                    airplane,
                    matchManager,
                    CalculateLandingTarget(i, safeCount),
                    CalculateJumpProgress(i),
                    matchManager != null ? matchManager.SafeZone : null
                );

                Health health = botObject.GetComponent<Health>();
                if (health != null)
                {
                    health.OverrideMaxHealth(200f);
                    matchManager.RegisterPlayer(health);
                }

                BotHealthBar.Attach(botObject);
                ApplyBotDamageScale(botObject, 0.20f);

                if (botObject.GetComponent<CharacterDeathDissolver>() == null)
                    botObject.AddComponent<CharacterDeathDissolver>();

                PlayerEliminationController elimination =
                    botObject.GetComponent<PlayerEliminationController>();
                if (elimination == null)
                    elimination = botObject.AddComponent<PlayerEliminationController>();
                elimination.Bind(matchManager);

                InventoryComponent inventory =
                    botObject.GetComponent<InventoryComponent>();
                FillBotInventory(inventory, i);

                _bots.Add(bot);
            }

            PlayerDamageRuntimeSetup.ConfigureExistingPlayers();

            Debug.Log(
                $"[BattleRoyaleBotDirector] Bots BR activados: {_bots.Count}/{safeCount}.",
                this
            );
        }

        private ParachuteController EnsureParachute(GameObject botObject)
        {
            ParachuteController parachute =
                botObject.GetComponent<ParachuteController>();
            if (parachute == null)
                parachute = botObject.AddComponent<ParachuteController>();

            Transform visual = botObject.transform.Find(
                "BattleRoyaleParachuteVisual"
            );

            if (visual == null && parachutePrefab != null)
            {
                GameObject visualObject = Instantiate(
                    parachutePrefab,
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
                input.EnableExternalControl();

            PlayerInteractor interactor =
                botObject.GetComponent<PlayerInteractor>();
            if (interactor != null)
                interactor.enabled = false;

            DisableAll<CombatFeedbackPresenter>(botObject);
            DisableAll<NearbyLootPresenter>(botObject);
            DisableAll<DamageDebugControls>(botObject);
            DisableAll<VitalsDebugTester>(botObject);
            DisableAll<UnityEngine.Camera>(botObject);
            DisableAll<AudioListener>(botObject);
        }

        private void PrepareBotsForFlight()
        {
            if (airplane == null)
                return;

            for (int i = 0; i < _bots.Count; i++)
            {
                BattleRoyaleBotController bot = _bots[i];
                if (bot == null)
                    continue;

                int column = i % 5;
                int row = i / 5;
                Vector3 passengerOffset = new Vector3(
                    (column - 2f) * 0.58f,
                    0f,
                    (row - 1.5f) * 0.68f
                );

                bot.PrepareForFlight(
                    airplane.PassengerAnchor,
                    passengerOffset
                );
            }
        }

        private static Vector3 CalculateLandingTarget(int index, int count)
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
                    components[i].enabled = false;
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

        private static void FillBotInventory(
            InventoryComponent inventory,
            int botIndex
        )
        {
            if (inventory == null)
                return;

            InventoryItemDefinition[] allDefinitions =
                Resources.FindObjectsOfTypeAll<InventoryItemDefinition>();

            System.Random rng = new System.Random(7919 * botIndex + 1337);

            ItemType[] wanted =
            {
                ItemType.Ammo,
                ItemType.Healing,
                ItemType.Armor,
                ItemType.Helmet,
            };

            foreach (ItemType target in wanted)
            {
                foreach (InventoryItemDefinition definition in allDefinitions)
                {
                    if (definition == null || definition.itemType != target)
                        continue;

                    int amount = target == ItemType.Ammo
                        ? rng.Next(20, 61)
                        : 1;

                    inventory.Add(definition, amount);
                    break;
                }
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
