using System;
using ROS.Game.BattleRoyale;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Inventory;
using ROS.Game.Loot;
using ROS.Game.Parachute;
using ROS.Game.Weapons;
using ROS.Game.World;
using UnityEngine;

namespace ROS.Game.AI
{
    [DefaultExecutionOrder(-80)]
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleBotController : MonoBehaviour
    {
        public enum BotState
        {
            Plane,
            AirDrop,
            SeekWeapon,
            SeekLoot,
            MoveToSafeZone,
            Combat,
            Roam,
            Dead
        }

        private const float PerceptionDistance = 28f;
        private const float ShootingDistance = 22f;
        private const float PreferredCombatDistance = 11f;
        private const float MapRoamRadius = 78f;
        private const float MinShootInterval = 0.55f;
        private const float MaxShootInterval = 1.35f;
        private const float ArmedLootSearchDistance = 34f;
        private const float UnarmedLootSearchDistance = 72f;
        private const float LootPickupDistance = 1.35f;
        private const float LootTargetAbandonDistance = 95f;
        private const float BotWeaponDamageScale = 0.20f;

        [Header("Runtime AI Debug")]
        [SerializeField] private BotState state = BotState.Plane;
        [SerializeField] private string targetName = string.Empty;
        [SerializeField] private string lootTargetName = string.Empty;

        private PlayerInputReader _input;
        private PlayerMotor _motor;
        private ParachuteController _parachute;
        private WeaponEquipmentController _equipment;
        private PlayerLootEquipment _lootEquipment;
        private InventoryComponent _inventory;
        private Health _health;
        private AirplaneController _airplane;
        private BattleRoyaleManager _matchManager;
        private SafeZoneController _safeZone;
        private Transform _controlFrame;
        private Health _target;
        private LootPickup _lootTarget;
        private Vector3 _landingTarget;
        private Vector3 _roamTarget;
        private float _jumpProgress;
        private float _nextThinkTime;
        private float _nextShootTime;
        private float _strafePhase;
        private bool _jumped;
        private System.Random _random;

        public int BotIndex { get; private set; }
        public Vector3 LandingTarget => _landingTarget;
        public BotState State => state;

        public static bool IsBot(Component component)
        {
            return component != null &&
                   component.GetComponent<BattleRoyaleBotController>() != null;
        }

        public static PlayerInputReader FindLocalPlayerInput()
        {
            PlayerInputReader[] inputs = FindObjectsByType<PlayerInputReader>(
                FindObjectsSortMode.None
            );
            for (int i = 0; i < inputs.Length; i++)
            {
                if (!IsBot(inputs[i]))
                    return inputs[i];
            }

            return null;
        }

        public static Health FindLocalPlayerHealth()
        {
            PlayerInputReader input = FindLocalPlayerInput();
            return input != null ? input.GetComponent<Health>() : null;
        }

        public void Configure(
            int botIndex,
            AirplaneController airplane,
            BattleRoyaleManager matchManager,
            Vector3 landingTarget,
            float jumpProgress,
            SafeZoneController safeZone = null
        )
        {
            BotIndex = botIndex;
            _airplane = airplane;
            _matchManager = matchManager;
            _safeZone = safeZone;
            _landingTarget = landingTarget;
            _roamTarget = landingTarget;
            _jumpProgress = Mathf.Clamp01(jumpProgress);
            _random = new System.Random(5107 + botIndex * 7919);
            _strafePhase = NextFloat(0f, Mathf.PI * 2f);

            EnsureReferences();
            _input.EnableExternalControl();
            _input.ApplyExternalControl(Vector2.zero, false, false);

            if (_motor != null)
            {
                GameObject frame = new GameObject("AIControlFrame");
                frame.transform.SetParent(transform, false);
                _controlFrame = frame.transform;
                _motor.SetCamera(_controlFrame);
            }
        }

        public void PrepareForFlight(
            Transform passengerAnchor,
            Vector3 passengerOffset
        )
        {
            EnsureReferences();
            _jumped = false;
            _target = null;
            _lootTarget = null;
            state = BotState.Plane;
            _parachute.PrepareForPlane();
            transform.SetParent(passengerAnchor, false);
            transform.localPosition = passengerOffset;
            transform.localRotation = Quaternion.identity;
            _input.ApplyExternalControl(Vector2.zero, false, false);
        }

        private void Update()
        {
            EnsureReferences();

            if (_health == null || !_health.IsAlive)
            {
                state = BotState.Dead;
                StopControl();
                enabled = false;
                return;
            }

            if (!_jumped)
            {
                state = BotState.Plane;
                UpdatePlanePhase();
                UpdateDebugNames();
                return;
            }

            if (_parachute != null && _parachute.IsAirbornePhase)
            {
                state = BotState.AirDrop;
                UpdateAirDrop();
                UpdateDebugNames();
                return;
            }

            if (_parachute == null ||
                _parachute.State != AirDropState.Landed)
            {
                StopControl();
                return;
            }

            UpdateGroundAutonomy();
            UpdateDebugNames();
        }

        private void UpdatePlanePhase()
        {
            StopControl();

            if (_airplane == null ||
                !_airplane.IsFlying ||
                _airplane.Progress < _jumpProgress)
            {
                return;
            }

            _jumped = true;
            transform.SetParent(null, true);

            float side = BotIndex % 2 == 0 ? -1f : 1f;
            transform.position +=
                _airplane.transform.right * side * 1.4f +
                -_airplane.transform.forward * 2.2f +
                Vector3.down * 1.2f;

            _parachute.BeginAirDrop(_airplane.Velocity);
        }

        private void UpdateAirDrop()
        {
            Vector3 direction = PlanarDirectionTo(_landingTarget);
            FaceDirection(direction, 145f);
            _input.ApplyExternalControl(
                new Vector2(0f, 1f),
                false,
                false
            );
        }

        private void UpdateGroundAutonomy()
        {
            bool gameplayActive =
                _matchManager != null &&
                (_matchManager.State == MatchState.Playing ||
                 _matchManager.State == MatchState.FinalCircle);

            if (Time.time >= _nextThinkTime)
            {
                _nextThinkTime = Time.time + NextFloat(0.28f, 0.48f);
                Think(gameplayActive);
            }

            // La zona tiene prioridad absoluta cuando el bot está fuera.
            if (_safeZone != null && _safeZone.IsOutside(transform.position))
            {
                state = BotState.MoveToSafeZone;
                MoveToward(GetSafeZoneDestination(), true);
                return;
            }

            // Un bot desarmado no persigue enemigos: primero busca un arma.
            if (!HasUsableWeapon())
            {
                state = BotState.SeekWeapon;
                if (UpdateLootMovement())
                    return;

                MoveToward(_roamTarget, true);
                return;
            }

            // Si ya tiene arma, recoge loot valioso antes de vagar sin objetivo.
            if (_lootTarget != null && IsLootTargetStillValid(_lootTarget))
            {
                // Ante un enemigo cercano, el combate gana sobre loot secundario.
                if (!(gameplayActive && _target != null && _target.IsAlive))
                {
                    state = BotState.SeekLoot;
                    if (UpdateLootMovement())
                        return;
                }
            }

            if (gameplayActive &&
                _target != null &&
                _target.IsAlive)
            {
                state = BotState.Combat;
                UpdateCombat(_target);
                return;
            }

            state = BotState.Roam;
            if (PlanarDistanceTo(_roamTarget) < 3.5f)
                _roamTarget = RandomMapPoint();

            MoveToward(_roamTarget, true);
        }

        private void Think(bool gameplayActive)
        {
            bool hasWeapon = HasUsableWeapon();

            if (_safeZone != null && _safeZone.IsOutside(transform.position))
            {
                _target = null;
                return;
            }

            _target = gameplayActive && hasWeapon
                ? FindNearestEnemy()
                : null;

            if (_lootTarget == null || !IsLootTargetStillValid(_lootTarget))
                _lootTarget = FindBestLootTarget(hasWeapon);

            // Si está desarmado y no encontró loot en el radio actual,
            // cambia de zona de exploración para continuar buscando.
            if (!hasWeapon && _lootTarget == null &&
                PlanarDistanceTo(_roamTarget) < 5f)
            {
                _roamTarget = RandomMapPoint();
            }
        }

        private bool UpdateLootMovement()
        {
            if (_lootTarget == null || !IsLootTargetStillValid(_lootTarget))
            {
                _lootTarget = FindBestLootTarget(HasUsableWeapon());
                if (_lootTarget == null)
                    return false;
            }

            float distance = PlanarDistanceTo(_lootTarget.transform.position);
            if (distance > LootPickupDistance)
            {
                MoveToward(_lootTarget.transform.position, true);
                return true;
            }

            StopControl();

            LootPickup collected = _lootTarget;
            bool success = collected.TryCollect(gameObject);
            _lootTarget = null;

            if (success)
            {
                ApplyDamageScaleToWeapons();
                EnsureBestWeaponEquipped();
                _nextThinkTime = 0f;
            }

            return true;
        }

        private LootPickup FindBestLootTarget(bool hasWeapon)
        {
            LootPickup[] pickups = FindObjectsByType<LootPickup>(
                FindObjectsSortMode.None
            );

            LootPickup best = null;
            float bestScore = float.MinValue;
            float maxDistance = hasWeapon
                ? ArmedLootSearchDistance
                : UnarmedLootSearchDistance;

            for (int i = 0; i < pickups.Length; i++)
            {
                LootPickup pickup = pickups[i];
                if (pickup == null || pickup.IsConsumed || pickup.Item == null)
                    continue;

                float distance = PlanarDistanceTo(pickup.transform.position);
                if (distance > maxDistance)
                    continue;

                if (!pickup.CanInteract(gameObject))
                    continue;

                float score = ScoreLoot(pickup.Item, distance, hasWeapon);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = pickup;
            }

            return best;
        }

        private float ScoreLoot(
            InventoryItemDefinition item,
            float distance,
            bool hasWeapon
        )
        {
            float score = 1000f - distance * 8f;

            switch (item.itemType)
            {
                case ItemType.Weapon:
                    if (!hasWeapon)
                    {
                        score += IsPrimaryWeapon(item) ? 12000f : 9000f;
                    }
                    else if (IsPrimaryWeapon(item) &&
                             (_equipment == null || !_equipment.HasWeaponInSlot(2)))
                    {
                        score += 5200f;
                    }
                    else if (item.weaponDefinition != null &&
                             item.weaponDefinition.family == WeaponFamily.Pistol &&
                             (_equipment == null || !_equipment.HasWeaponInSlot(3)))
                    {
                        score += 3200f;
                    }
                    else
                    {
                        score += 550f;
                    }
                    break;

                case ItemType.Ammo:
                    if (HasWeaponUsingAmmo(item.ammoType))
                    {
                        int amount = GetAmmoAmount(item.ammoType);
                        score += amount < 45 ? 6500f : 2600f;
                    }
                    else
                    {
                        score += hasWeapon ? 250f : 1200f;
                    }
                    break;

                case ItemType.Armor:
                case ItemType.Helmet:
                case ItemType.Backpack:
                    score += 2800f;
                    break;

                case ItemType.Healing:
                    float healthRatio = _health != null && _health.MaxHealth > 0f
                        ? _health.CurrentHealth / _health.MaxHealth
                        : 1f;
                    score += healthRatio < 0.65f ? 4200f : 1500f;
                    break;

                case ItemType.Throwable:
                    score += 900f;
                    break;

                case ItemType.Attachment:
                    score += 650f;
                    break;

                default:
                    score += 150f;
                    break;
            }

            return score;
        }

        private static bool IsPrimaryWeapon(InventoryItemDefinition item)
        {
            if (item == null || item.weaponDefinition == null)
                return false;

            WeaponFamily family = item.weaponDefinition.family;
            return family != WeaponFamily.Pistol &&
                   family != WeaponFamily.Melee;
        }

        private bool HasWeaponUsingAmmo(AmmoType ammoType)
        {
            if (_equipment == null || ammoType == AmmoType.None)
                return false;

            for (int slot = 1; slot <= 3; slot++)
            {
                WeaponController weapon = _equipment.GetWeaponForSlot(slot);
                if (weapon != null && weapon.Definition != null &&
                    weapon.Definition.ammoType == ammoType)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetAmmoAmount(AmmoType ammoType)
        {
            if (_inventory == null || ammoType == AmmoType.None)
                return 0;

            int total = 0;
            foreach (InventoryStack stack in _inventory.Stacks)
            {
                if (stack != null && stack.item != null &&
                    stack.item.itemType == ItemType.Ammo &&
                    stack.item.ammoType == ammoType)
                {
                    total += Mathf.Max(0, stack.amount);
                }
            }

            return total;
        }

        private bool IsLootTargetStillValid(LootPickup pickup)
        {
            if (pickup == null || pickup.IsConsumed || pickup.Item == null)
                return false;

            if (PlanarDistanceTo(pickup.transform.position) > LootTargetAbandonDistance)
                return false;

            return pickup.CanInteract(gameObject);
        }

        private void UpdateCombat(Health target)
        {
            WeaponController weapon = ResolveWeapon();
            if (weapon == null)
            {
                _target = null;
                _lootTarget = FindBestLootTarget(false);
                return;
            }

            Vector3 aimPoint = target.transform.position + Vector3.up * 1.25f;
            Vector3 direction = PlanarDirectionTo(aimPoint);
            float distance = Vector3.Distance(transform.position, aimPoint);
            SetControlFrame(direction);

            bool hasLineOfSight =
                distance <= ShootingDistance &&
                HasLineOfSight(target, aimPoint);

            float strafe = Mathf.Sin(
                Time.time * 1.4f + _strafePhase
            ) * 0.72f;
            float forward = distance > PreferredCombatDistance
                ? 0.72f
                : distance < 7f
                    ? -0.45f
                    : 0.08f;

            _input.ApplyExternalControl(
                new Vector2(strafe, forward),
                false,
                false
            );

            if (!hasLineOfSight || Time.time < _nextShootTime)
                return;

            Vector3 dispersion = new Vector3(
                NextFloat(-0.42f, 0.42f),
                NextFloat(-0.28f, 0.32f),
                NextFloat(-0.42f, 0.42f)
            );

            if (weapon.TryFireAt(aimPoint + dispersion))
            {
                _nextShootTime = Time.time +
                    NextFloat(MinShootInterval, MaxShootInterval);
            }
        }

        private void EnsureBestWeaponEquipped()
        {
            if (_equipment == null)
                return;

            if (_equipment.EquippedWeapon != null)
                return;

            for (int slot = 1; slot <= 3; slot++)
            {
                if (_equipment.HasWeaponInSlot(slot))
                {
                    _equipment.EquipSlot(slot);
                    return;
                }
            }
        }

        private bool HasUsableWeapon()
        {
            if (_equipment == null)
                return false;

            for (int slot = 1; slot <= 3; slot++)
            {
                WeaponController weapon = _equipment.GetWeaponForSlot(slot);
                if (weapon != null && weapon.Definition != null &&
                    (weapon.AmmoInMagazine > 0 || weapon.ReserveAmmo > 0))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyDamageScaleToWeapons()
        {
            WeaponController[] weapons =
                GetComponentsInChildren<WeaponController>(true);

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                    weapons[i].DamageScale = BotWeaponDamageScale;
            }
        }

        private void MoveToward(Vector3 destination, bool sprint)
        {
            Vector3 direction = PlanarDirectionTo(destination);
            SetControlFrame(direction);
            _input.ApplyExternalControl(
                new Vector2(0f, 1f),
                sprint,
                false
            );
        }

        private Health FindNearestEnemy()
        {
            Health[] candidates = FindObjectsByType<Health>(
                FindObjectsSortMode.None
            );
            Health nearest = null;
            float nearestSqr = PerceptionDistance * PerceptionDistance;

            for (int i = 0; i < candidates.Length; i++)
            {
                Health candidate = candidates[i];
                if (candidate == null ||
                    candidate == _health ||
                    !candidate.IsAlive ||
                    candidate.GetComponent<PlayerMotor>() == null)
                {
                    continue;
                }

                float sqr = (candidate.transform.position -
                             transform.position).sqrMagnitude;
                if (sqr >= nearestSqr)
                    continue;

                Vector3 aimPoint =
                    candidate.transform.position + Vector3.up * 1.25f;
                if (!HasLineOfSight(candidate, aimPoint))
                    continue;

                nearestSqr = sqr;
                nearest = candidate;
            }

            return nearest;
        }

        private bool HasLineOfSight(Health target, Vector3 targetPoint)
        {
            Vector3 origin = transform.position + Vector3.up * 1.35f;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
                return true;

            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction / distance,
                distance,
                ~0,
                QueryTriggerInteraction.Collide
            );
            Array.Sort(hits, (left, right) =>
                left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null ||
                    collider.transform.root == transform.root)
                {
                    continue;
                }

                Health hitHealth =
                    collider.GetComponentInParent<Health>();
                if (hitHealth != null)
                    return hitHealth == target;

                if (!collider.isTrigger)
                    return false;
            }

            return true;
        }

        private WeaponController ResolveWeapon()
        {
            EnsureBestWeaponEquipped();
            return _equipment != null ? _equipment.EquippedWeapon : null;
        }

        private Vector3 GetSafeZoneDestination()
        {
            if (_safeZone == null)
                return _roamTarget;

            Vector3 center = _safeZone.Center;
            float radius = Mathf.Max(4f, _safeZone.Radius * 0.55f);
            Vector2 offset = new Vector2(
                NextFloat(-1f, 1f),
                NextFloat(-1f, 1f)
            );
            if (offset.sqrMagnitude > 1f)
                offset.Normalize();
            offset *= radius * NextFloat(0.1f, 0.75f);

            return new Vector3(
                center.x + offset.x,
                center.y,
                center.z + offset.y
            );
        }

        private Vector3 RandomMapPoint()
        {
            float angle = NextFloat(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(NextFloat(0.08f, 1f)) *
                           MapRoamRadius;
            return new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
        }

        private Vector3 PlanarDirectionTo(Vector3 destination)
        {
            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : transform.forward;
        }

        private float PlanarDistanceTo(Vector3 destination)
        {
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private void FaceDirection(Vector3 direction, float degreesPerSecond)
        {
            if (direction.sqrMagnitude <= 0.001f)
                return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                degreesPerSecond * Time.deltaTime
            );
        }

        private void SetControlFrame(Vector3 direction)
        {
            if (_controlFrame == null ||
                direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            _controlFrame.rotation = Quaternion.LookRotation(
                direction,
                Vector3.up
            );
        }

        private void StopControl()
        {
            if (_input != null)
            {
                _input.ApplyExternalControl(
                    Vector2.zero,
                    false,
                    false
                );
            }
        }

        private void UpdateDebugNames()
        {
            targetName = _target != null ? _target.gameObject.name : string.Empty;
            lootTargetName = _lootTarget != null
                ? _lootTarget.Item != null
                    ? _lootTarget.Item.displayName
                    : _lootTarget.gameObject.name
                : string.Empty;
        }

        private float NextFloat(float minimum, float maximum)
        {
            if (_random == null)
                _random = new System.Random(5107 + BotIndex * 7919);

            return Mathf.Lerp(
                minimum,
                maximum,
                (float)_random.NextDouble()
            );
        }

        private void EnsureReferences()
        {
            if (_input == null)
                _input = GetComponent<PlayerInputReader>();

            if (_motor == null)
                _motor = GetComponent<PlayerMotor>();

            if (_parachute == null)
                _parachute = GetComponent<ParachuteController>();

            if (_equipment == null)
                _equipment = GetComponent<WeaponEquipmentController>();

            if (_lootEquipment == null)
                _lootEquipment = GetComponent<PlayerLootEquipment>();

            if (_inventory == null)
                _inventory = GetComponent<InventoryComponent>();

            if (_health == null)
                _health = GetComponent<Health>();
        }
    }
}
