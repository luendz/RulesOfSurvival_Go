using ROS.Game.Animation;
using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Combat;
using ROS.Game.Gameplay;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Parachute;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.UI
{
    /// <summary>
    /// Conecta todos los componentes del HUD con las referencias de escena y del jugador local.
    /// Colocar en la escena con los campos de escena asignados en el Inspector.
    /// BindLocalPlayer() debe llamarse cuando el jugador local este disponible.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class HUDSessionConnector : MonoBehaviour
    {
        public static HUDSessionConnector Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private BattleRoyaleManager battleRoyale;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private MatchStartController matchStart;

        [Header("HUD Components")]
        [SerializeField] private RulesOfSurvivalHUD hud;
        [SerializeField] private BattleRoyaleStartMenu startMenu;
        [SerializeField] private KillFeedPresenter killFeed;
        [SerializeField] private DamageDirectionIndicator damageIndicator;
        [SerializeField] private QuickConsumePresenter quickConsume;
        [SerializeField] private PlayerWeaponSlotsHudPresenter weaponSlots;
        [SerializeField] private RulesOfSurvivalHUDNearbyLootPresenter nearbyLoot;
        [SerializeField] private CombatFeedbackPresenter combatFeedback;
        [SerializeField] private GestureWheelUI gestureWheel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[HUDSessionConnector] Hay mas de una instancia. Se destruye la extra.", this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            hud?.BindScene(worldCamera, minimapCamera, battleRoyale);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BindLocalPlayer(GameObject playerRoot)
        {
            if (playerRoot == null)
            {
                Debug.LogError("[HUDSessionConnector] playerRoot es null.", this);
                return;
            }

            var health = playerRoot.GetComponent<Health>();
            var equipment = playerRoot.GetComponent<WeaponEquipmentController>();
            var interactor = playerRoot.GetComponent<PlayerInteractor>();
            var inventory = playerRoot.GetComponent<InventoryComponent>();
            var lootEquip = playerRoot.GetComponent<PlayerLootEquipment>();
            var auxSlots = playerRoot.GetComponent<PlayerAuxiliaryWeaponSlots>();
            var consumable = playerRoot.GetComponent<ConsumableController>();
            var input = playerRoot.GetComponent<PlayerInputReader>();
            var playerCamera = playerRoot.GetComponentInChildren<ThirdPersonCamera>();
            var gesture = playerRoot.GetComponentInChildren<PlayerGestureController>();

            hud?.BindPlayer(health, equipment, interactor);
            killFeed?.Bind(battleRoyale, health);
            damageIndicator?.Bind(health, playerRoot.transform);
            quickConsume?.Bind(inventory, consumable);
            weaponSlots?.BindPlayer(equipment, lootEquip, auxSlots, inventory);
            nearbyLoot?.BindPlayer(input, interactor, inventory, lootEquip);
            combatFeedback?.Bind(health, playerRoot.transform);
            gestureWheel?.BindPlayer(input, gesture);

            if (startMenu != null)
                startMenu.Configure(matchStart, input, playerCamera);
        }
    }
}
