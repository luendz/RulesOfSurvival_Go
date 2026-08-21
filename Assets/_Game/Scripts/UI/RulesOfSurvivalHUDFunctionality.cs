using System.Collections.Generic;
using ROS.Game.AI;
using ROS.Game.BattleRoyale;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.UI
{
    /// <summary>
    /// Puente funcional entre el HUD reconstruido de Rules of Survival y los
    /// sistemas reales del prototipo. Se ejecuta después de RulesOfSurvivalHUD
    /// y antes del pulido final para que la caja de muerte pueda reutilizar el
    /// mismo panel amarillo sin perder su comportamiento especializado.
    /// </summary>
    [DefaultExecutionOrder(800)]
    public sealed class RulesOfSurvivalHUDFunctionality : MonoBehaviour
    {
        private const string SceneName = "07_BattleRoyaleTest";
        private const float WeaponPanelWidth = 205f;
        private const float HealthBarWidth = 250f;

        private static readonly Color Yellow =
            new Color(0.96f, 0.86f, 0.02f, 0.96f);

        private static readonly Color ActionIdle =
            new Color(0.04f, 0.05f, 0.06f, 0.78f);

        private PlayerInputReader _input;
        private PlayerMotor _motor;
        private Health _health;
        private PlayerInteractor _interactor;
        private WeaponEquipmentController _equipment;
        private BattleRoyaleManager _battleRoyale;
        private SafeZoneController _safeZone;
        private Camera _worldCamera;

        private Transform _hudRoot;
        private RectTransform _vitalsRoot;
        private RectTransform _weaponsRoot;
        private RectTransform _actionsRoot;
        private RectTransform _lootPanel;

        private Text _killText;
        private Text _leftText;
        private Text _distanceText;
        private Text _playerNameText;
        private Text _healthValueText;
        private Text _zoneText;
        private Text _interactionText;
        private Image _healthFill;
        private Image _armorFill;

        private readonly WeaponSlotBinding[] _weaponSlots =
            new WeaponSlotBinding[3];

        private readonly List<Text> _lootRows =
            new List<Text>(7);

        private Image _fireAction;
        private Image _aimAction;
        private Image _interactAction;
        private Image _crouchAction;
        private Image _proneAction;

        private Camera _minimapCamera;
        private Image _minimapArrow;

        private float _nextResolveTime;
        private BattleRoyaleManager _registeredManager;
        private Health _registeredHealth;

        private sealed class WeaponSlotBinding
        {
            public Image Background;
            public Text Slot;
            public Text Name;
            public Text Ammo;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RulesOfSurvivalHUDFunctionality>() != null)
            {
                return;
            }

            new GameObject("ROS_HUD_Functionality")
                .AddComponent<RulesOfSurvivalHUDFunctionality>();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextResolveTime)
            {
                _nextResolveTime = Time.unscaledTime + 0.25f;
                ResolveGameplayReferences();
                ResolveHudReferences();
                EnsureCoreHudVisible();
                EnsureLocalPlayerRegistered();
            }

            UpdateVitals();
            UpdateWeapons();
            UpdateBattleRoyale();
            UpdateInteraction();
            UpdateActions();
            UpdateMinimap();
        }

        private void ResolveGameplayReferences()
        {
            if (!IsValidLocalInput(_input))
            {
                _input = FindLocalPlayerInput();
                _motor = null;
                _health = null;
                _interactor = null;
                _equipment = null;
                _registeredManager = null;
                _registeredHealth = null;
            }

            if (_input != null)
            {
                GameObject player = _input.gameObject;

                if (_motor == null)
                {
                    _motor = player.GetComponent<PlayerMotor>();
                }

                if (_health == null)
                {
                    _health = player.GetComponent<Health>();
                }

                if (_interactor == null)
                {
                    _interactor = player.GetComponent<PlayerInteractor>();
                }

                if (_equipment == null)
                {
                    _equipment = player.GetComponent<WeaponEquipmentController>();
                }
            }

            if (!IsSceneComponent(_battleRoyale))
            {
                _battleRoyale = FindSceneComponent<BattleRoyaleManager>();
                _registeredManager = null;
            }

            SafeZoneController managerZone =
                _battleRoyale != null
                    ? _battleRoyale.SafeZone
                    : null;

            if (managerZone != null)
            {
                _safeZone = managerZone;
            }
            else if (!IsSceneComponent(_safeZone))
            {
                _safeZone = FindSceneComponent<SafeZoneController>();
            }

            if (_worldCamera == null ||
                !_worldCamera.gameObject.scene.IsValid())
            {
                _worldCamera = Camera.main;
            }
        }

        private void ResolveHudReferences()
        {
            GameObject rootObject = GameObject.Find("ROS_HUD_Runtime");
            if (rootObject == null)
            {
                _hudRoot = null;
                return;
            }

            if (_hudRoot != rootObject.transform)
            {
                _hudRoot = rootObject.transform;
                ClearHudCache();
            }

            _vitalsRoot ??= FindRect("Canvas/Vitals");
            _weaponsRoot ??= FindRect("Canvas/Weapons");
            _actionsRoot ??= FindRect("Canvas/Actions");
            _lootPanel ??= FindRect("Canvas/NearbyLoot");

            _killText ??= FindText("Canvas/TopRightStats/KillText");
            _leftText ??= FindText("Canvas/TopRightStats/LeftText");
            _distanceText ??= FindText(
                "Canvas/TopRightStats/DistancePanel/DistanceText"
            );
            _playerNameText ??= FindText("Canvas/Vitals/PlayerName");
            _healthValueText ??= FindText("Canvas/Vitals/HealthValue");
            _zoneText ??= FindText("Canvas/ZoneBanner");
            _interactionText ??= FindText("Canvas/InteractionHint");
            _healthFill ??= FindImage("Canvas/Vitals/HealthBack/HealthFill");
            _armorFill ??= FindImage("Canvas/Vitals/ArmorBack/ArmorFill");

            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                if (_weaponSlots[i] != null)
                {
                    continue;
                }

                int slot = i + 1;
                string path = $"Canvas/Weapons/WeaponSlot_{slot}";
                RectTransform slotRoot = FindRect(path);
                if (slotRoot == null)
                {
                    continue;
                }

                _weaponSlots[i] = new WeaponSlotBinding
                {
                    Background = slotRoot.GetComponent<Image>(),
                    Slot = FindText(path + "/Slot"),
                    Name = FindText(path + "/WeaponName"),
                    Ammo = FindText(path + "/Ammo")
                };
            }

            if (_lootRows.Count == 0)
            {
                for (int i = 0; i < 7; i++)
                {
                    Text row = FindText($"Canvas/NearbyLoot/LootRow_{i}");
                    if (row != null)
                    {
                        _lootRows.Add(row);
                    }
                }
            }

            _fireAction ??= FindImage("Canvas/Actions/Fire");
            _aimAction ??= FindImage("Canvas/Actions/Aim");
            _interactAction ??= FindImage("Canvas/Actions/Interact");
            _crouchAction ??= FindImage("Canvas/Actions/Crouch");
            _proneAction ??= FindImage("Canvas/Actions/Prone");

            if (_minimapCamera == null)
            {
                GameObject minimapCameraObject =
                    GameObject.Find("ROS_MinimapCamera");
                if (minimapCameraObject != null)
                {
                    _minimapCamera =
                        minimapCameraObject.GetComponent<Camera>();
                }
            }

            _minimapArrow ??= FindImage("Canvas/MinimapFrame/PlayerArrow");
        }

        private void ClearHudCache()
        {
            _vitalsRoot = null;
            _weaponsRoot = null;
            _actionsRoot = null;
            _lootPanel = null;
            _killText = null;
            _leftText = null;
            _distanceText = null;
            _playerNameText = null;
            _healthValueText = null;
            _zoneText = null;
            _interactionText = null;
            _healthFill = null;
            _armorFill = null;
            _fireAction = null;
            _aimAction = null;
            _interactAction = null;
            _crouchAction = null;
            _proneAction = null;
            _minimapArrow = null;
            _minimapCamera = null;
            _lootRows.Clear();

            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                _weaponSlots[i] = null;
            }
        }

        private void EnsureCoreHudVisible()
        {
            SetActive(_vitalsRoot, true);
            SetActive(_weaponsRoot, true);
            SetActive(_actionsRoot, true);

            RectTransform compass = FindRect("Canvas/CompassStrip");
            RectTransform stats = FindRect("Canvas/TopRightStats");
            RectTransform minimap = FindRect("Canvas/MinimapFrame");

            SetActive(compass, true);
            SetActive(stats, true);
            SetActive(minimap, true);

            Canvas canvas =
                _hudRoot != null
                    ? _hudRoot.GetComponentInChildren<Canvas>(true)
                    : null;

            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        private void EnsureLocalPlayerRegistered()
        {
            if (_battleRoyale == null || _health == null)
            {
                return;
            }

            if (_registeredManager == _battleRoyale &&
                _registeredHealth == _health)
            {
                return;
            }

            // RegisterPlayer es idempotente. Esto corrige además el conteo LEFT:
            // el bootstrap de bots registra a los bots, pero no siempre al local.
            _battleRoyale.RegisterPlayer(_health);
            _registeredManager = _battleRoyale;
            _registeredHealth = _health;
        }

        private void UpdateVitals()
        {
            if (_health == null)
            {
                return;
            }

            SetActive(_vitalsRoot, true);

            float health01 =
                _health.MaxHealth > 0f
                    ? Mathf.Clamp01(
                        _health.CurrentHealth / _health.MaxHealth
                    )
                    : 0f;

            float armor01 =
                _health.MaxArmor > 0f
                    ? Mathf.Clamp01(
                        _health.CurrentArmor / _health.MaxArmor
                    )
                    : 0f;

            SetWidth(_healthFill, HealthBarWidth * health01);
            SetWidth(_armorFill, HealthBarWidth * armor01);

            if (_healthValueText != null)
            {
                _healthValueText.text =
                    Mathf.CeilToInt(_health.CurrentHealth).ToString();
            }

            if (_playerNameText != null)
            {
                _playerNameText.text =
                    _health.gameObject.name
                        .Replace("_Prototype", string.Empty)
                        .Replace("Player_", string.Empty)
                        .ToUpperInvariant();
            }
        }

        private void UpdateWeapons()
        {
            if (_equipment == null)
            {
                return;
            }

            SetActive(_weaponsRoot, true);

            WeaponController[] weapons =
            {
                _equipment.PrimarySlot1,
                _equipment.PrimarySlot2,
                _equipment.SidearmSlot
            };

            for (int i = 0; i < weapons.Length; i++)
            {
                UpdateWeaponSlot(
                    _weaponSlots[i],
                    i + 1,
                    weapons[i]
                );
            }
        }

        private void UpdateWeaponSlot(
            WeaponSlotBinding binding,
            int slotNumber,
            WeaponController weapon
        )
        {
            if (binding == null)
            {
                return;
            }

            bool selected =
                weapon != null &&
                _equipment.EquippedSlot == slotNumber &&
                _equipment.EquippedWeapon == weapon;

            if (binding.Background != null)
            {
                binding.Background.color = selected
                    ? new Color(0.11f, 0.10f, 0.02f, 0.96f)
                    : new Color(0.025f, 0.035f, 0.045f, 0.84f);
            }

            if (binding.Slot != null)
            {
                binding.Slot.text = slotNumber.ToString();
                binding.Slot.color = selected ? Yellow : Color.white;
            }

            if (binding.Name == null || binding.Ammo == null)
            {
                return;
            }

            if (weapon == null)
            {
                binding.Name.text = "—";
                binding.Name.color = new Color(1f, 1f, 1f, 0.4f);
                binding.Ammo.text = string.Empty;
                return;
            }

            binding.Name.color = Color.white;
            binding.Name.text = weapon.Definition != null
                ? weapon.Definition.displayName.ToUpperInvariant()
                : weapon.name.ToUpperInvariant();
            binding.Ammo.text =
                $"{weapon.AmmoInMagazine}/{weapon.ReserveAmmo}";
        }

        private void UpdateBattleRoyale()
        {
            int alive = ResolveAliveCount();
            int kills =
                _battleRoyale != null && _health != null
                    ? _battleRoyale.GetKillCount(_health)
                    : 0;

            if (_killText != null)
            {
                _killText.text = $"{kills} KILL";
            }

            if (_leftText != null)
            {
                _leftText.text = alive > 0
                    ? $"{alive} LEFT"
                    : "0 LEFT";
            }

            if (_safeZone != null && _health != null)
            {
                Vector3 player = _health.transform.position;
                Vector3 center = _safeZone.Center;
                player.y = 0f;
                center.y = 0f;

                float fromCenter = Vector3.Distance(player, center);
                float outsideDistance =
                    Mathf.Max(0f, fromCenter - _safeZone.Radius);

                if (_distanceText != null)
                {
                    _distanceText.text = outsideDistance > 0.01f
                        ? $"ZONE\n{outsideDistance:0}m"
                        : $"SAFE\n{_safeZone.Radius:0}m";
                }

                if (_zoneText != null)
                {
                    if (_safeZone.CurrentPhase < 0)
                    {
                        _zoneText.text = string.Empty;
                    }
                    else
                    {
                        int seconds = Mathf.Max(
                            0,
                            Mathf.CeilToInt(
                                _safeZone.PhaseTimeRemaining
                            )
                        );

                        _zoneText.text = _safeZone.IsShrinking
                            ? $"SAFE ZONE CLOSING  {seconds}s"
                            : $"SAFE ZONE SHRINKS IN  {seconds}s";
                    }
                }
            }
            else
            {
                if (_distanceText != null)
                {
                    _distanceText.text = "ZONE\n--";
                }

                if (_zoneText != null)
                {
                    _zoneText.text = string.Empty;
                }
            }
        }

        private int ResolveAliveCount()
        {
            if (_battleRoyale != null && _battleRoyale.AliveCount > 0)
            {
                return _battleRoyale.AliveCount;
            }

            int alive = 0;

            if (_health != null && _health.IsAlive)
            {
                alive++;
            }

            BattleRoyaleBotController[] bots =
                Resources.FindObjectsOfTypeAll<BattleRoyaleBotController>();

            Scene activeScene = SceneManager.GetActiveScene();

            for (int i = 0; i < bots.Length; i++)
            {
                BattleRoyaleBotController bot = bots[i];
                if (bot == null ||
                    bot.gameObject.scene != activeScene)
                {
                    continue;
                }

                Health botHealth = bot.GetComponent<Health>();
                if (botHealth != null && botHealth.IsAlive)
                {
                    alive++;
                }
            }

            return alive;
        }

        private void UpdateInteraction()
        {
            if (_interactor == null || _lootPanel == null)
            {
                return;
            }

            IReadOnlyList<IInteractable> nearby = _interactor.Nearby;
            bool hasNearby = nearby != null && nearby.Count > 0;

            _lootPanel.gameObject.SetActive(hasNearby);

            for (int i = 0; i < _lootRows.Count; i++)
            {
                Text row = _lootRows[i];
                if (row == null)
                {
                    continue;
                }

                if (!hasNearby || i >= nearby.Count || nearby[i] == null)
                {
                    row.text = string.Empty;
                    continue;
                }

                IInteractable candidate = nearby[i];
                bool current = ReferenceEquals(
                    candidate,
                    _interactor.Current
                );

                row.text =
                    (current ? "▶ " : "  ") +
                    candidate.InteractionLabel;
                row.color = current
                    ? new Color(0.13f, 0.07f, 0.18f, 1f)
                    : Color.black;
            }

            if (_interactionText != null)
            {
                IInteractable current = _interactor.Current;
                _interactionText.text =
                    current != null && current.CanInteract(_input.gameObject)
                        ? $"[F] {current.InteractionLabel}"
                        : string.Empty;
            }
        }

        private void UpdateActions()
        {
            if (_input == null)
            {
                return;
            }

            SetActive(_actionsRoot, true);

            SetActionState(_fireAction, _input.FireHeld);
            SetActionState(_aimAction, _input.AimHeld);
            SetActionState(
                _interactAction,
                _interactor != null && _interactor.Current != null
            );
            SetActionState(
                _crouchAction,
                _motor != null && _motor.IsCrouching
            );
            SetActionState(
                _proneAction,
                _motor != null && _motor.IsProne
            );
        }

        private void UpdateMinimap()
        {
            if (_health == null)
            {
                return;
            }

            if (_minimapCamera != null)
            {
                Vector3 position = _health.transform.position;
                _minimapCamera.transform.position =
                    position + Vector3.up * 180f;
                _minimapCamera.transform.rotation =
                    Quaternion.Euler(90f, 0f, 0f);
            }

            if (_minimapArrow != null)
            {
                float heading =
                    _worldCamera != null
                        ? _worldCamera.transform.eulerAngles.y
                        : _health.transform.eulerAngles.y;

                _minimapArrow.rectTransform.localEulerAngles =
                    new Vector3(0f, 0f, -heading);
            }
        }

        private static PlayerInputReader FindLocalPlayerInput()
        {
            PlayerInputReader[] inputs =
                Resources.FindObjectsOfTypeAll<PlayerInputReader>();

            Scene activeScene = SceneManager.GetActiveScene();
            PlayerInputReader fallback = null;

            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInputReader candidate = inputs[i];
                if (!IsValidLocalInput(candidate) ||
                    candidate.gameObject.scene != activeScene)
                {
                    continue;
                }

                if (candidate.gameObject.name == "Player_Prototype" ||
                    candidate.gameObject.name.StartsWith("Player_"))
                {
                    return candidate;
                }

                if (fallback == null &&
                    !candidate.gameObject.name.StartsWith("Bot_"))
                {
                    fallback = candidate;
                }
            }

            return fallback;
        }

        private static bool IsValidLocalInput(PlayerInputReader input)
        {
            return input != null &&
                   input.gameObject.scene.IsValid() &&
                   !input.UsesExternalControl;
        }

        private static T FindSceneComponent<T>() where T : Component
        {
            T[] components = Resources.FindObjectsOfTypeAll<T>();
            Scene activeScene = SceneManager.GetActiveScene();
            T fallback = null;

            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null ||
                    component.gameObject.scene != activeScene)
                {
                    continue;
                }

                if (component.gameObject.activeInHierarchy)
                {
                    return component;
                }

                fallback ??= component;
            }

            return fallback;
        }

        private static bool IsSceneComponent(Component component)
        {
            return component != null &&
                   component.gameObject.scene.IsValid() &&
                   component.gameObject.scene ==
                   SceneManager.GetActiveScene();
        }

        private RectTransform FindRect(string path)
        {
            if (_hudRoot == null)
            {
                return null;
            }

            Transform found = _hudRoot.Find(path);
            return found as RectTransform;
        }

        private Text FindText(string path)
        {
            RectTransform rect = FindRect(path);
            return rect != null ? rect.GetComponent<Text>() : null;
        }

        private Image FindImage(string path)
        {
            RectTransform rect = FindRect(path);
            return rect != null ? rect.GetComponent<Image>() : null;
        }

        private static void SetActive(RectTransform rect, bool active)
        {
            if (rect != null && rect.gameObject.activeSelf != active)
            {
                rect.gameObject.SetActive(active);
            }
        }

        private static void SetWidth(Image image, float width)
        {
            if (image == null)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            Vector2 size = rect.sizeDelta;
            size.x = Mathf.Max(0f, width);
            rect.sizeDelta = size;
        }

        private static void SetActionState(Image image, bool active)
        {
            if (image == null)
            {
                return;
            }

            image.color = active ? Yellow : ActionIdle;
        }
    }
}
