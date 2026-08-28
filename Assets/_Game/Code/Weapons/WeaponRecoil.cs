using ROS.Game.Character;
using ROS.Game.Input;
using UnityEngine;

namespace ROS.Game.Weapons
{
    public sealed class WeaponRecoil : MonoBehaviour
    {
        [Header("Camera Recoil")]
        [SerializeField] private float verticalRecoil = 1.2f;
        [SerializeField] private float horizontalRecoil = 0.35f;

        [Header("Recovery")]
        [SerializeField] private float returnSpeed = 8f;
        [SerializeField] private float snappiness = 14f;

        [Header("Weapon Kick")]
        [SerializeField] private Transform visualRecoilTransform;
        [SerializeField] private float kickBackDistance = 0.035f;
        [SerializeField] private float kickReturnSpeed = 18f;

        [Header("Runtime")]
        [SerializeField] private Vector2 debugCurrentRecoil;
        [SerializeField] private float debugCurrentMultiplier = 1f;
        [SerializeField] private int debugPatternIndex;

        private WeaponDefinition _definition;
        private PlayerInputReader _input;
        private PlayerMotor _motor;
        private Vector2 _targetRecoil;
        private Vector2 _currentRecoil;
        private float _kickTarget;
        private float _kickCurrent;
        private float _lastShotTime = -999f;
        private int _patternIndex;
        private int _sustainedShotCount;
        private Vector3 _visualBaseLocalPosition;
        private bool _visualBaseCaptured;

        public Vector2 CurrentRecoil => _currentRecoil;
        public float CurrentKickBack => _kickCurrent;

        public void ConfigureDefinition(WeaponDefinition definition)
        {
            _definition = definition;

            if (definition == null)
            {
                return;
            }

            verticalRecoil = definition.verticalRecoil;
            horizontalRecoil = definition.horizontalRecoil;
            returnSpeed = definition.recoilReturnSpeed;
            snappiness = definition.recoilSnappiness;
            kickBackDistance = definition.recoilKickBack;

        }

        public void AddRecoil()
        {
            if (_definition != null &&
                Time.time - _lastShotTime > _definition.recoilPatternResetDelay)
            {
                _patternIndex = 0;
                _sustainedShotCount = 0;
            }

            float stateMultiplier = ResolveStateMultiplier();
            float growthMultiplier = ResolveGrowthMultiplier();
            float multiplier = stateMultiplier * growthMultiplier;

            Vector2 recoilStep = ResolvePatternStep();
            recoilStep.x *= horizontalRecoil;
            recoilStep.y *= verticalRecoil;
            recoilStep *= multiplier;

            _targetRecoil += recoilStep;
            _kickTarget += Mathf.Max(0f, kickBackDistance) * multiplier;

            _patternIndex++;
            _sustainedShotCount++;
            _lastShotTime = Time.time;
            debugCurrentMultiplier = multiplier;
            debugPatternIndex = _patternIndex;
        }

        private void Awake()
        {
            ResolveOwnerContext();
            CacheVisualBasePosition();
        }

        private void Update()
        {
            if (_definition != null &&
                Time.time - _lastShotTime > _definition.recoilPatternResetDelay)
            {
                _patternIndex = 0;
                _sustainedShotCount = 0;
            }

            _targetRecoil = Vector2.Lerp(
                _targetRecoil,
                Vector2.zero,
                returnSpeed * Time.deltaTime
            );

            _currentRecoil = Vector2.Lerp(
                _currentRecoil,
                _targetRecoil,
                snappiness * Time.deltaTime
            );

            _kickTarget = Mathf.MoveTowards(
                _kickTarget,
                0f,
                Mathf.Max(0.01f, kickReturnSpeed) * Time.deltaTime
            );

            _kickCurrent = Mathf.Lerp(
                _kickCurrent,
                _kickTarget,
                Mathf.Max(0.01f, snappiness) * Time.deltaTime
            );

            ApplyVisualKick();
            debugCurrentRecoil = _currentRecoil;
        }

        private void OnDisable()
        {
            _targetRecoil = Vector2.zero;
            _currentRecoil = Vector2.zero;
            _kickTarget = 0f;
            _kickCurrent = 0f;
            _patternIndex = 0;
            _sustainedShotCount = 0;

            if (visualRecoilTransform != null && _visualBaseCaptured)
            {
                visualRecoilTransform.localPosition = _visualBaseLocalPosition;
            }
        }

        private void ResolveOwnerContext()
        {
            if (_input == null)
                _input = GetComponentInParent<PlayerInputReader>();

            if (_motor == null)
                _motor = GetComponentInParent<PlayerMotor>();
        }

        private void CacheVisualBasePosition()
        {
            if (visualRecoilTransform != null && !_visualBaseCaptured)
            {
                _visualBaseLocalPosition = visualRecoilTransform.localPosition;
                _visualBaseCaptured = true;
            }
        }

        private float ResolveStateMultiplier()
        {
            if (_definition == null)
                return 1f;

            bool aiming = _input != null && _input.AimHeld;
            bool crouching = _motor != null && _motor.IsCrouching;
            bool moving = _motor != null && _motor.MoveInput.sqrMagnitude > 0.04f;
            bool airborne = _motor != null && !_motor.IsGrounded;

            return _definition.GetRecoilStateMultiplier(
                aiming,
                crouching,
                moving,
                airborne
            );
        }

        private float ResolveGrowthMultiplier()
        {
            if (_definition == null)
                return 1f;

            float growth = 1f +
                Mathf.Max(0f, _definition.recoilGrowthPerShot) *
                _sustainedShotCount;

            return Mathf.Min(
                growth,
                Mathf.Max(1f, _definition.maxRecoilGrowthMultiplier)
            );
        }

        private Vector2 ResolvePatternStep()
        {
            if (_definition != null &&
                _definition.recoilPattern != null &&
                _definition.recoilPattern.Length > 0)
            {
                Vector2 step = _definition.recoilPattern[
                    _patternIndex % _definition.recoilPattern.Length
                ];

                return new Vector2(step.x, Mathf.Max(0f, step.y));
            }

            return new Vector2(
                Random.Range(-1f, 1f),
                1f
            );
        }

        private void ApplyVisualKick()
        {
            if (visualRecoilTransform == null || !_visualBaseCaptured)
                return;

            visualRecoilTransform.localPosition =
                _visualBaseLocalPosition +
                Vector3.back * _kickCurrent;
        }

    }
}
