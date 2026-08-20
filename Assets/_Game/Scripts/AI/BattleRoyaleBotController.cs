using System;
using ROS.Game.BattleRoyale;
using ROS.Game.Character;
using ROS.Game.Combat;
using ROS.Game.Core;
using ROS.Game.Input;
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
        private const float PerceptionDistance = 20f;
        private const float ShootingDistance   = 10f;
        private const float PreferredCombatDistance = 8f;
        private const float MapRoamRadius = 78f;
        private const float MinShootInterval = 1.2f;
        private const float MaxShootInterval = 2.2f;

        private PlayerInputReader _input;
        private PlayerMotor _motor;
        private ParachuteController _parachute;
        private WeaponEquipmentController _equipment;
        private Health _health;
        private AirplaneController _airplane;
        private BattleRoyaleManager _matchManager;
        private Transform _controlFrame;
        private Health _target;
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
                {
                    return inputs[i];
                }
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
            float jumpProgress
        )
        {
            BotIndex = botIndex;
            _airplane = airplane;
            _matchManager = matchManager;
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
                StopControl();
                enabled = false;
                return;
            }

            if (!_jumped)
            {
                UpdatePlanePhase();
                return;
            }

            if (_parachute != null && _parachute.IsAirbornePhase)
            {
                UpdateAirDrop();
                return;
            }

            if (_parachute == null ||
                _parachute.State != AirDropState.Landed)
            {
                StopControl();
                return;
            }

            UpdateGroundAutonomy();
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
            if (Time.time >= _nextThinkTime)
            {
                _nextThinkTime = Time.time + NextFloat(0.35f, 0.62f);
                _target = FindNearestEnemy();

                if (_target == null &&
                    PlanarDistanceTo(_roamTarget) < 3.5f)
                {
                    _roamTarget = RandomMapPoint();
                }
            }

            bool gameplayActive =
                _matchManager != null &&
                (_matchManager.State == MatchState.Playing ||
                 _matchManager.State == MatchState.FinalCircle);

            if (gameplayActive &&
                _target != null &&
                _target.IsAlive)
            {
                UpdateCombat(_target);
                return;
            }

            MoveToward(_roamTarget, true);
        }

        private void UpdateCombat(Health target)
        {
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
                : distance < 9f
                    ? -0.45f
                    : 0.08f;

            _input.ApplyExternalControl(
                new Vector2(strafe, forward),
                false,
                hasLineOfSight
            );

            if (!hasLineOfSight)
            {
                return;
            }

            if (Time.time < _nextShootTime)
            {
                return;
            }

            WeaponController weapon = ResolveWeapon();
            if (weapon != null)
            {
                Vector3 dispersion = new Vector3(
                    NextFloat(-0.50f, 0.50f),
                    NextFloat(-0.35f, 0.40f),
                    NextFloat(-0.50f, 0.50f)
                );
                weapon.TryFireAt(aimPoint + dispersion);
                _nextShootTime = Time.time +
                    NextFloat(MinShootInterval, MaxShootInterval);
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
                {
                    continue;
                }

                // Solo detectar si hay línea de visión directa (no a través de muros)
                Vector3 aimPoint =
                    candidate.transform.position + Vector3.up * 1.25f;
                if (!HasLineOfSight(candidate, aimPoint))
                {
                    continue;
                }

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
            {
                return true;
            }

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
                {
                    return hitHealth == target;
                }

                if (!collider.isTrigger)
                {
                    return false;
                }
            }

            return true;
        }

        private WeaponController ResolveWeapon()
        {
            if (_equipment == null)
            {
                return null;
            }

            if (_equipment.EquippedWeapon == null &&
                _equipment.HasWeaponInSlot(1))
            {
                _equipment.EquipSlot(1);
            }

            return _equipment.EquippedWeapon;
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
            {
                return;
            }

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

        private float NextFloat(float minimum, float maximum)
        {
            if (_random == null)
            {
                _random = new System.Random(5107 + BotIndex * 7919);
            }

            return Mathf.Lerp(
                minimum,
                maximum,
                (float)_random.NextDouble()
            );
        }

        private void EnsureReferences()
        {
            if (_input == null)
            {
                _input = GetComponent<PlayerInputReader>();
            }

            if (_motor == null)
            {
                _motor = GetComponent<PlayerMotor>();
            }

            if (_parachute == null)
            {
                _parachute = GetComponent<ParachuteController>();
            }

            if (_equipment == null)
            {
                _equipment = GetComponent<WeaponEquipmentController>();
            }

            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
        }
    }
}
