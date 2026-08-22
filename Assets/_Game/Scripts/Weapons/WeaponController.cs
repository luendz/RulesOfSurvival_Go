using System;
using System.Collections;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Inventory;
using UnityEngine;

namespace ROS.Game.Weapons
{
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private WeaponDefinition definition;

        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private WeaponEquipmentController equipment;
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private WeaponMount weaponMount;
        [SerializeField] private Transform muzzle;
        [SerializeField] private UnityEngine.Camera aimCamera;
        [SerializeField] private WeaponEffects weaponEffects;
        [SerializeField] private WeaponRecoil weaponRecoil;

        [Header("Hit Detection")]
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private bool useBallisticTrajectory = true;
        [Range(4, 64)]
        [SerializeField] private int ballisticSegments = 18;

        [Header("Ammo")]
        [SerializeField] private int reserveAmmo = 120;

        [Header("Debug Shot")]
        [SerializeField] private bool drawShotDebug = true;
        [SerializeField] private float debugShotDuration = 0.15f;
        [SerializeField] private float debugImpactSize = 0.08f;

        [Header("Runtime Debug")]
        [SerializeField] private int debugAmmoInMagazine;
        [SerializeField] private int debugReserveAmmo;
        [SerializeField] private bool debugIsReloading;
        [SerializeField] private Vector3 debugLastHitPoint;
        [SerializeField] private Vector3 debugLastHitNormal;
        [SerializeField] private float debugCurrentSpread;
        [SerializeField] private float debugSpreadBloom;
        [SerializeField] private int debugLastShotProjectiles;
        [SerializeField] private int debugLastShotImpacts;
        [SerializeField] private int debugLastShotCharacterImpacts;
        [SerializeField] private float debugLastTravelTime;
        [SerializeField] private float debugLastEstimatedDrop;

        private InventoryComponent _ammoInventory;
        private InventoryItemDefinition _ammoItemDef;

        private float _nextShotTime;
        private bool _triggerLatch;
        private int _burstShotsRemaining;
        private Coroutine _reloadRoutine;
        private float _spreadBloom;
        private float _lastShotTime = -999f;
        private bool _usesExternalShotDirection;
        private Vector3 _externalShotDirection;

        public WeaponDefinition Definition => definition;
        public float DamageScale { get; set; } = 1f;
        public int AmmoInMagazine { get; private set; }
        public int ReserveAmmo => _ammoInventory != null && _ammoItemDef != null
            ? _ammoInventory.GetAmount(_ammoItemDef)
            : reserveAmmo;
        public bool IsReloading { get; private set; }
        public bool IsBurstFiring => _burstShotsRemaining > 0;
        public WeaponFireMode CurrentFireMode { get; private set; }
        public float ActiveReloadDuration { get; private set; }
        public float CurrentSpread => ResolveCurrentSpread();
        public int LastShotProjectileCount { get; private set; }
        public int LastShotImpactCount { get; private set; }
        public int LastShotCharacterImpactCount { get; private set; }

        public event Action AmmoChanged;
        public event Action Fired;
        public event Action<WeaponFireMode> FireModeChanged;
        public event Action<DamageResult> HitConfirmed;

        public void ConfigureDefinition(
            WeaponDefinition newDefinition,
            int startingReserveAmmo = -1)
        {
            definition = newDefinition;
            reserveAmmo = Mathf.Max(
                0,
                startingReserveAmmo >= 0
                    ? startingReserveAmmo
                    : definition != null
                        ? definition.startingReserveAmmo
                        : 0
            );

            AmmoInMagazine = definition != null ? definition.magazineSize : 0;
            CurrentFireMode = definition != null
                ? definition.GetInitialFireMode()
                : WeaponFireMode.Single;

            EnsureReferences();
            ApplyDefinitionToComponents();
            UpdateDebugValues();
            AmmoChanged?.Invoke();
            FireModeChanged?.Invoke(CurrentFireMode);
        }

        public void SetAmmoSource(
            InventoryComponent inventory,
            InventoryItemDefinition ammoDef)
        {
            bool firstRealConnection = _ammoItemDef == null && ammoDef != null;

            _ammoInventory = inventory;
            _ammoItemDef = ammoDef;

            if (firstRealConnection && reserveAmmo > 0 && inventory != null)
            {
                inventory.Add(ammoDef, reserveAmmo);
                reserveAmmo = 0;
            }

            AmmoChanged?.Invoke();
        }

        public void DisableForElimination()
        {
            if (_reloadRoutine != null)
            {
                StopCoroutine(_reloadRoutine);
                _reloadRoutine = null;
            }

            IsReloading = false;
            ActiveReloadDuration = 0f;
            _triggerLatch = false;
            _burstShotsRemaining = 0;
            enabled = false;
            UpdateDebugValues();
        }

        private void Awake()
        {
            EnsureReferences();

            AmmoInMagazine = definition != null ? definition.magazineSize : 0;
            CurrentFireMode = definition != null
                ? definition.GetInitialFireMode()
                : WeaponFireMode.Single;

            ApplyDefinitionToComponents();
            UpdateDebugValues();
        }

        private void OnEnable()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (input == null)
                input = GetComponentInParent<PlayerInputReader>();

            if (aimController == null)
                aimController = GetComponentInParent<PlayerAimController>();

            if (motor == null)
                motor = GetComponentInParent<PlayerMotor>();

            if (equipment == null)
                equipment = GetComponentInParent<WeaponEquipmentController>();

            if (weaponMount == null)
                weaponMount = GetComponent<WeaponMount>();

            if (aimCamera == null)
                aimCamera = UnityEngine.Camera.main;

            if (muzzle == null && weaponMount != null)
                muzzle = weaponMount.MuzzlePoint;

            if (muzzle == null)
                muzzle = FindChildRecursive(transform, "MuzzlePoint");

            if (weaponEffects == null)
                weaponEffects = GetComponent<WeaponEffects>();

            if (weaponRecoil == null)
                weaponRecoil = GetComponent<WeaponRecoil>();

            if (weaponEffects != null)
                weaponEffects.EnsureRuntimeSetup();
        }

        private void ApplyDefinitionToComponents()
        {
            if (weaponRecoil != null)
                weaponRecoil.ConfigureDefinition(definition);

            if (weaponEffects != null)
                weaponEffects.ConfigureDefinition(definition);
        }

        private void Update()
        {
            EnsureReferences();

            if (definition == null || input == null)
            {
                UpdateDebugValues();
                return;
            }

            if (equipment != null && equipment.EquippedWeapon != this)
            {
                _triggerLatch = false;
                _burstShotsRemaining = 0;
                RecoverSpreadBloom();
                UpdateDebugValues();
                return;
            }

            if (input.FireModePressed)
                CycleFireMode();

            if (input.ReloadPressed)
                TryReload();

            RecoverSpreadBloom();
            HandleFire();
            UpdateDebugValues();
        }

        private void HandleFire()
        {
            if (IsReloading)
                return;

            bool wantsFire = input.FireHeld;

            if (CurrentFireMode == WeaponFireMode.Burst)
            {
                HandleBurstFire(wantsFire);
                return;
            }

            if (!wantsFire)
            {
                _triggerLatch = false;
                return;
            }

            if (CurrentFireMode == WeaponFireMode.Single && _triggerLatch)
                return;

            if (Time.time < _nextShotTime)
                return;

            if (AmmoInMagazine <= 0)
            {
                TryReload();
                return;
            }

            FireOneShot();
            _triggerLatch = CurrentFireMode == WeaponFireMode.Single;
        }

        private void HandleBurstFire(bool wantsFire)
        {
            if (!wantsFire)
            {
                _triggerLatch = false;
            }
            else if (!_triggerLatch && _burstShotsRemaining <= 0)
            {
                _triggerLatch = true;
                _burstShotsRemaining = Mathf.Min(
                    Mathf.Max(1, definition.burstCount),
                    AmmoInMagazine
                );
            }

            if (_burstShotsRemaining <= 0 || Time.time < _nextShotTime)
            {
                if (AmmoInMagazine <= 0 && wantsFire)
                    TryReload();

                return;
            }

            FireOneShot();
            _burstShotsRemaining--;
        }

        public void CycleFireMode()
        {
            if (definition == null || IsReloading)
                return;

            SetFireMode(definition.GetNextFireMode(CurrentFireMode));
        }

        public bool SetFireMode(WeaponFireMode mode)
        {
            if (definition == null || !definition.SupportsFireMode(mode))
                return false;

            CurrentFireMode = mode;
            _triggerLatch = false;
            _burstShotsRemaining = 0;
            FireModeChanged?.Invoke(CurrentFireMode);
            return true;
        }

        public bool TryFireAt(Vector3 worldPoint)
        {
            EnsureReferences();

            if (!isActiveAndEnabled ||
                definition == null ||
                IsReloading ||
                Time.time < _nextShotTime ||
                (equipment != null && equipment.EquippedWeapon != this))
            {
                return false;
            }

            if (AmmoInMagazine <= 0)
            {
                TryReload();
                return false;
            }

            Vector3 origin = GetShotOrigin();
            Vector3 direction = worldPoint - origin;
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            _usesExternalShotDirection = true;
            _externalShotDirection = ShouldUseBallistics()
                ? WeaponBallistics.ApplyDropToAimDirection(
                    origin,
                    worldPoint,
                    definition.muzzleVelocity,
                    definition.gravityScale
                )
                : direction.normalized;

            try
            {
                FireOneShot();
            }
            finally
            {
                _usesExternalShotDirection = false;
            }

            return true;
        }

        private void FireOneShot()
        {
            AmmoInMagazine--;
            _nextShotTime = Time.time + 1f / definition.shotsPerSecond;

            Vector3 origin = GetShotOrigin();
            Vector3 baseDirection = GetShotDirection(origin);
            float currentSpread = ResolveCurrentSpread();
            int projectileCount = definition.GetProjectileCount();

            LastShotProjectileCount = projectileCount;
            LastShotImpactCount = 0;
            LastShotCharacterImpactCount = 0;

            for (int projectileIndex = 0; projectileIndex < projectileCount; projectileIndex++)
            {
                Vector3 direction = ApplySpread(baseDirection, currentSpread);

                ProcessHit(
                    origin,
                    direction,
                    out bool hasHit,
                    out Vector3 hitPoint,
                    out Vector3 hitNormal,
                    out bool hitCharacter
                );

                if (hasHit)
                    LastShotImpactCount++;

                if (hitCharacter)
                    LastShotCharacterImpactCount++;

                debugLastHitPoint = hitPoint;
                debugLastHitNormal = hitNormal;

                float traveledDistance = Vector3.Distance(origin, hitPoint);
                debugLastTravelTime = WeaponBallistics.EstimateTravelTime(
                    traveledDistance,
                    definition.muzzleVelocity
                );
                debugLastEstimatedDrop = WeaponBallistics.EstimateDrop(
                    traveledDistance,
                    definition.muzzleVelocity,
                    definition.gravityScale
                );

                if (drawShotDebug)
                    DrawShotDebug(origin, hitPoint, hitNormal, hasHit);

                if (weaponEffects != null)
                {
                    weaponEffects.PlayShot(
                        hitPoint,
                        hitNormal,
                        hasHit,
                        hitCharacter,
                        projectileIndex == 0
                    );
                }
            }

            if (weaponRecoil != null)
                weaponRecoil.AddRecoil();

            _spreadBloom = Mathf.Min(
                definition.maxSpreadBloom,
                _spreadBloom + definition.spreadBloomPerShot
            );
            _lastShotTime = Time.time;

            UpdateDebugValues();
            AmmoChanged?.Invoke();
            Fired?.Invoke();
        }

        private Vector3 GetShotOrigin()
        {
            if (weaponMount != null)
                return weaponMount.ShotOrigin;

            if (muzzle != null)
                return muzzle.position;

            if (aimCamera != null)
                return aimCamera.transform.position;

            return transform.position;
        }

        private Vector3 GetShotDirection(Vector3 origin)
        {
            if (_usesExternalShotDirection)
                return _externalShotDirection;

            if (aimController != null)
                return aimController.GetDirectionFrom(origin);

            if (aimCamera != null)
                return aimCamera.transform.forward;

            if (weaponMount != null)
                return weaponMount.MechanicalForward;

            if (muzzle != null)
                return muzzle.forward;

            return transform.forward;
        }

        private float ResolveCurrentSpread()
        {
            if (definition == null)
                return 0f;

            float spread = input != null && input.AimHeld
                ? definition.adsSpreadDegrees
                : definition.hipSpreadDegrees;

            if (motor != null)
            {
                if (!motor.IsGrounded)
                {
                    spread *= definition.airborneSpreadMultiplier;
                }
                else if (motor.IsCrouching)
                {
                    spread *= definition.crouchSpreadMultiplier;
                }
                else
                {
                    float moveAmount = motor.MoveInput.magnitude;

                    if (input != null && input.SprintHeld && motor.MoveInput.y > 0.25f)
                        spread *= definition.sprintSpreadMultiplier;
                    else if (moveAmount > 0.65f)
                        spread *= definition.runSpreadMultiplier;
                    else if (moveAmount > 0.05f)
                        spread *= definition.walkSpreadMultiplier;
                }
            }

            spread += _spreadBloom;
            return Mathf.Max(0f, spread);
        }

        private void RecoverSpreadBloom()
        {
            if (definition == null || _spreadBloom <= 0f)
                return;

            if (Time.time <= _lastShotTime)
                return;

            _spreadBloom = Mathf.MoveTowards(
                _spreadBloom,
                0f,
                definition.spreadRecoveryPerSecond * Time.deltaTime
            );
        }

        private Vector3 ApplySpread(Vector3 direction, float spreadDegrees)
        {
            if (spreadDegrees <= 0f)
                return direction.normalized;

            float spreadRadius = Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            Vector2 spread = UnityEngine.Random.insideUnitCircle * spreadRadius;

            Vector3 orientationUp = Mathf.Abs(
                Vector3.Dot(direction.normalized, Vector3.up)
            ) > 0.98f
                ? Vector3.forward
                : Vector3.up;

            Quaternion orientation = Quaternion.LookRotation(direction, orientationUp);
            Vector3 right = orientation * Vector3.right;
            Vector3 up = orientation * Vector3.up;

            return (direction + right * spread.x + up * spread.y).normalized;
        }

        private bool ShouldUseBallistics()
        {
            return useBallisticTrajectory &&
                   definition != null &&
                   definition.gravityScale > 0f &&
                   definition.muzzleVelocity > 1f;
        }

        private void ProcessHit(
            Vector3 origin,
            Vector3 direction,
            out bool hasHit,
            out Vector3 hitPoint,
            out Vector3 hitNormal,
            out bool hitCharacter)
        {
            if (ShouldUseBallistics())
            {
                ProcessBallisticHit(
                    origin,
                    direction,
                    out hasHit,
                    out hitPoint,
                    out hitNormal,
                    out hitCharacter
                );
                return;
            }

            ProcessLinearHit(
                origin,
                direction,
                out hasHit,
                out hitPoint,
                out hitNormal,
                out hitCharacter
            );
        }

        private void ProcessLinearHit(
            Vector3 origin,
            Vector3 direction,
            out bool hasHit,
            out Vector3 hitPoint,
            out Vector3 hitNormal,
            out bool hitCharacter)
        {
            hitPoint = origin + direction * definition.range;
            hitNormal = -direction;

            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                definition.range,
                hitMask,
                QueryTriggerInteraction.Collide
            );

            hasHit = TryResolveHits(
                hits,
                direction,
                out hitPoint,
                out hitNormal,
                out hitCharacter
            );
        }

        private void ProcessBallisticHit(
            Vector3 origin,
            Vector3 direction,
            out bool hasHit,
            out Vector3 hitPoint,
            out Vector3 hitNormal,
            out bool hitCharacter)
        {
            hasHit = false;
            hitCharacter = false;
            hitPoint = origin;
            hitNormal = -direction;

            float velocity = Mathf.Max(1f, definition.muzzleVelocity);
            float maxTime = definition.range / velocity;
            int segments = Mathf.Clamp(ballisticSegments, 4, 64);
            Vector3 initialVelocity = direction.normalized * velocity;
            Vector3 previous = origin;

            for (int i = 1; i <= segments; i++)
            {
                float t = maxTime * i / segments;
                Vector3 current = WeaponBallistics.EvaluatePosition(
                    origin,
                    initialVelocity,
                    definition.gravityScale,
                    t
                );

                Vector3 segment = current - previous;
                float segmentLength = segment.magnitude;
                if (segmentLength <= 0.0001f)
                    continue;

                Vector3 segmentDirection = segment / segmentLength;

                if (drawShotDebug)
                {
                    Debug.DrawLine(
                        previous,
                        current,
                        Color.cyan,
                        debugShotDuration
                    );
                }

                RaycastHit[] hits = Physics.RaycastAll(
                    previous,
                    segmentDirection,
                    segmentLength,
                    hitMask,
                    QueryTriggerInteraction.Collide
                );

                if (TryResolveHits(
                    hits,
                    segmentDirection,
                    out hitPoint,
                    out hitNormal,
                    out hitCharacter))
                {
                    hasHit = true;
                    return;
                }

                previous = current;
                hitPoint = current;
                hitNormal = -segmentDirection;
            }
        }

        private bool TryResolveHits(
            RaycastHit[] hits,
            Vector3 direction,
            out Vector3 hitPoint,
            out Vector3 hitNormal,
            out bool hitCharacter)
        {
            hitPoint = Vector3.zero;
            hitNormal = -direction;
            hitCharacter = false;

            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool hasCharacterFallback = false;
            RaycastHit characterFallback = default;
            Health fallbackHealth = null;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.collider.transform.root == transform.root)
                    continue;

                DamageHitbox hitbox = hit.collider.GetComponent<DamageHitbox>();
                Health targetHealth = hitbox != null
                    ? hitbox.Owner
                    : hit.collider.GetComponentInParent<Health>();

                if (targetHealth != null)
                {
                    if (hitbox != null)
                    {
                        ApplyFirearmDamage(targetHealth, hitbox.HitZone, hit, direction);
                        ApplyImpactForce(hit, direction);
                        hitCharacter = true;
                        hitPoint = hit.point;
                        hitNormal = hit.normal;
                        return true;
                    }

                    if (!hasCharacterFallback)
                    {
                        hasCharacterFallback = true;
                        characterFallback = hit;
                        fallbackHealth = targetHealth;
                    }

                    continue;
                }

                if (hit.collider.isTrigger)
                    continue;

                if (hasCharacterFallback)
                {
                    ApplyFirearmDamage(
                        fallbackHealth,
                        HitZone.Torso,
                        characterFallback,
                        direction
                    );
                    ApplyImpactForce(characterFallback, direction);
                    hitCharacter = true;
                    hitPoint = characterFallback.point;
                    hitNormal = characterFallback.normal;
                    return true;
                }

                hitPoint = hit.point;
                hitNormal = hit.normal;
                ApplyImpactForce(hit, direction);

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.ApplyDamage(
                        new DamageInfo(
                            definition.damage * DamageScale,
                            hit.point,
                            direction,
                            gameObject,
                            DamageType.Firearm,
                            HitZone.Torso
                        )
                    );
                }

                return true;
            }

            if (hasCharacterFallback)
            {
                ApplyFirearmDamage(
                    fallbackHealth,
                    HitZone.Torso,
                    characterFallback,
                    direction
                );
                ApplyImpactForce(characterFallback, direction);
                hitCharacter = true;
                hitPoint = characterFallback.point;
                hitNormal = characterFallback.normal;
                return true;
            }

            return false;
        }

        private void ApplyFirearmDamage(
            Health target,
            HitZone hitZone,
            RaycastHit hit,
            Vector3 direction)
        {
            if (target == null)
                return;

            bool wasAlive = target.IsAlive;

            target.ApplyDamage(
                new DamageInfo(
                    definition.damage * DamageScale,
                    hit.point,
                    direction,
                    gameObject,
                    DamageType.Firearm,
                    hitZone
                )
            );

            if (wasAlive)
                HitConfirmed?.Invoke(target.LastDamageResult);
        }

        private void ApplyImpactForce(RaycastHit hit, Vector3 direction)
        {
            if (definition == null || definition.impactForce <= 0f)
                return;

            Rigidbody body = hit.rigidbody;
            if (body == null || body.isKinematic)
                return;

            body.AddForceAtPosition(
                direction.normalized * definition.impactForce,
                hit.point,
                ForceMode.Impulse
            );
        }

        private void DrawShotDebug(
            Vector3 origin,
            Vector3 hitPoint,
            Vector3 hitNormal,
            bool hasHit)
        {
            Color shotColor = hasHit ? Color.green : Color.red;
            Debug.DrawLine(origin, hitPoint, shotColor, debugShotDuration);

            if (!hasHit)
                return;

            Vector3 right = Vector3.right * debugImpactSize;
            Vector3 up = Vector3.up * debugImpactSize;
            Vector3 forward = Vector3.forward * debugImpactSize;

            Debug.DrawLine(hitPoint - right, hitPoint + right, Color.yellow, debugShotDuration);
            Debug.DrawLine(hitPoint - up, hitPoint + up, Color.yellow, debugShotDuration);
            Debug.DrawLine(hitPoint - forward, hitPoint + forward, Color.yellow, debugShotDuration);
            Debug.DrawRay(hitPoint, hitNormal * 0.35f, Color.cyan, debugShotDuration);
        }

        public void TryReload()
        {
            if (equipment != null && equipment.EquippedWeapon != this)
                return;

            if (IsReloading ||
                definition == null ||
                AmmoInMagazine >= definition.magazineSize ||
                ReserveAmmo <= 0)
            {
                return;
            }

            if (_reloadRoutine != null)
                StopCoroutine(_reloadRoutine);

            _burstShotsRemaining = 0;
            _reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            ActiveReloadDuration = definition.GetReloadDuration(AmmoInMagazine <= 0);
            UpdateDebugValues();

            yield return new WaitForSeconds(ActiveReloadDuration);

            int needed = definition.magazineSize - AmmoInMagazine;
            int available = ReserveAmmo;
            int amount = Mathf.Min(needed, available);

            AmmoInMagazine += amount;

            if (_ammoInventory != null && _ammoItemDef != null)
                _ammoInventory.Remove(_ammoItemDef, amount);
            else
                reserveAmmo -= amount;

            IsReloading = false;
            ActiveReloadDuration = 0f;
            _reloadRoutine = null;
            UpdateDebugValues();
            AmmoChanged?.Invoke();
        }

        private void UpdateDebugValues()
        {
            debugAmmoInMagazine = AmmoInMagazine;
            debugReserveAmmo = ReserveAmmo;
            debugIsReloading = IsReloading;
            debugCurrentSpread = definition != null ? ResolveCurrentSpread() : 0f;
            debugSpreadBloom = _spreadBloom;
            debugLastShotProjectiles = LastShotProjectileCount;
            debugLastShotImpacts = LastShotImpactCount;
            debugLastShotCharacterImpacts = LastShotCharacterImpactCount;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
