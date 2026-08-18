using ROS.Game.Input;
using ROS.Game.Weapons;
using UnityEngine;

namespace ROS.Game.CameraSystem
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WeaponRecoil weaponRecoil;
        [SerializeField] private WeaponEquipmentController equipment;

        [Header("Position")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.55f, 0f, 0f);

        [Header("Normal Camera")]
        [SerializeField] private float normalDistance = 3.7f;
        [SerializeField] private float normalShoulderX = 0.55f;
        [SerializeField] private float normalFov = 60f;

        [Header("Aim Camera")]
        [SerializeField] private float aimDistance = 2.2f;
        [SerializeField] private float aimShoulderX = 0.35f;
        [SerializeField] private float aimFov = 48f;
        [SerializeField] private float aimTransitionSpeed = 10f;

        [Header("Shoulder Switch")]
        [SerializeField] private float rightShoulderX = 0.55f;
        [SerializeField] private float leftShoulderX = -0.55f;
        [SerializeField] private float shoulderSwitchSpeed = 8f;

        [Header("Look")]
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float gamepadSensitivity = 120f;
        [SerializeField] private float minPitch = -45f;
        [SerializeField] private float maxPitch = 75f;

        [Header("Recoil")]
        [SerializeField] private float recoilMultiplier = 1f;

        [Header("Smoothing")]
        [SerializeField] private float rotationSmoothTime = 12f;
        [SerializeField] private float positionSmoothTime = 18f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.2f;

        private float _yaw;
        private float _pitch = 12f;
        private bool _leftShoulder;
        private float _currentShoulderX;
        private float _currentDistance;
        private float _currentAimShoulderX;
        private Quaternion _currentRotation;
        private Vector3 _currentPosition;
        private UnityEngine.Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();

            if (target == null)
            {
                PlayerInputReader foundInput = FindFirstObjectByType<PlayerInputReader>();
                if (foundInput != null)
                {
                    input = foundInput;
                    target = foundInput.transform;
                }
            }
            else if (input == null)
            {
                input = target.GetComponent<PlayerInputReader>();
            }

            if (target != null)
                _yaw = target.eulerAngles.y;

            _currentRotation = transform.rotation;
            _currentPosition = transform.position;
            _leftShoulder = false;
            _currentShoulderX = rightShoulderX;
            _currentDistance = normalDistance;
            _currentAimShoulderX = normalShoulderX;

            FindEquipment();
            FindWeaponRecoil();

            if (_camera != null)
                _camera.fieldOfView = normalFov;
        }

        public void Bind(Transform newTarget, PlayerInputReader newInput)
        {
            target = newTarget;
            input = newInput;
            _yaw = target.eulerAngles.y;
            _camera = GetComponent<UnityEngine.Camera>();
            _currentRotation = transform.rotation;
            _currentPosition = transform.position;
            _leftShoulder = false;
            _currentShoulderX = rightShoulderX;
            _currentDistance = normalDistance;
            _currentAimShoulderX = normalShoulderX;

            FindEquipment();
            FindWeaponRecoil();

            if (_camera != null)
                _camera.fieldOfView = normalFov;
        }

        private void LateUpdate()
        {
            if (target == null || input == null)
                return;

            HandleShoulderSwitch();
            HandleAim();
            HandleLook();
            UpdateCameraTransform();
        }

        private void HandleShoulderSwitch()
        {
            if (input.ShoulderSwitchPressed)
                _leftShoulder = !_leftShoulder;

            float targetShoulderX = _leftShoulder ? leftShoulderX : rightShoulderX;
            _currentShoulderX = Mathf.Lerp(
                _currentShoulderX,
                targetShoulderX,
                shoulderSwitchSpeed * Time.deltaTime
            );
        }

        private void HandleAim()
        {
            bool isAiming = IsAimActive();

            float targetDistance = isAiming ? aimDistance : normalDistance;
            float targetAimShoulderX = isAiming ? aimShoulderX : normalShoulderX;

            _currentDistance = Mathf.Lerp(
                _currentDistance,
                targetDistance,
                aimTransitionSpeed * Time.deltaTime
            );

            _currentAimShoulderX = Mathf.Lerp(
                _currentAimShoulderX,
                targetAimShoulderX,
                aimTransitionSpeed * Time.deltaTime
            );

            if (_camera != null)
            {
                float targetFov = isAiming ? aimFov : normalFov;
                _camera.fieldOfView = Mathf.Lerp(
                    _camera.fieldOfView,
                    targetFov,
                    aimTransitionSpeed * Time.deltaTime
                );
            }
        }

        private void HandleLook()
        {
            Vector2 look = input.Look;
            float multiplier = input.LookFromMouse
                ? sensitivity
                : gamepadSensitivity * Time.deltaTime;

            _yaw += look.x * multiplier;
            _pitch -= look.y * multiplier;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        private void UpdateCameraTransform()
        {
            float recoilYaw = 0f;
            float recoilPitch = 0f;

            WeaponRecoil activeRecoil = GetActiveWeaponRecoil();
            if (activeRecoil != null)
            {
                Vector2 recoil = activeRecoil.CurrentRecoil * recoilMultiplier;
                recoilYaw = recoil.x;
                recoilPitch = -recoil.y;
            }

            float finalYaw = _yaw + recoilYaw;
            float finalPitch = Mathf.Clamp(_pitch + recoilPitch, minPitch, maxPitch);

            Quaternion targetRotation = Quaternion.Euler(finalPitch, finalYaw, 0f);
            Vector3 pivot = target.position + pivotOffset;

            float shoulderSign = _leftShoulder ? -1f : 1f;
            float effectiveShoulderX = _currentShoulderX;

            if (IsAimActive())
                effectiveShoulderX = shoulderSign * _currentAimShoulderX;

            Vector3 currentShoulderOffset = new Vector3(
                effectiveShoulderX,
                shoulderOffset.y,
                shoulderOffset.z
            );

            Vector3 desired =
                pivot
                + targetRotation * currentShoulderOffset
                - targetRotation * Vector3.forward * _currentDistance;

            desired = ResolveCollision(pivot, desired);

            _currentRotation = Quaternion.Slerp(
                _currentRotation,
                targetRotation,
                rotationSmoothTime * Time.deltaTime
            );

            _currentPosition = Vector3.Lerp(
                _currentPosition,
                desired,
                positionSmoothTime * Time.deltaTime
            );

            transform.SetPositionAndRotation(_currentPosition, _currentRotation);
        }

        private void FindEquipment()
        {
            if (equipment != null || target == null)
                return;

            equipment = target.GetComponent<WeaponEquipmentController>();
        }

        private void FindWeaponRecoil()
        {
            if (weaponRecoil != null || target == null)
                return;

            weaponRecoil = target.GetComponentInChildren<WeaponRecoil>(true);
        }

        private bool IsAimActive()
        {
            if (input == null)
                return false;

            if (equipment != null)
                return equipment.CombatState == ROS.Game.Core.PlayerCombatState.Aiming;

            return input.AimHeld;
        }

        private WeaponRecoil GetActiveWeaponRecoil()
        {
            if (equipment != null && equipment.EquippedWeapon != null)
            {
                WeaponRecoil active = equipment.EquippedWeapon.GetComponent<WeaponRecoil>();
                if (active != null)
                    return active;
            }

            return weaponRecoil;
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            Vector3 direction = desired - pivot;
            float length = direction.magnitude;

            if (length <= 0.001f)
                return desired;

            if (Physics.SphereCast(
                    pivot,
                    collisionRadius,
                    direction.normalized,
                    out RaycastHit hit,
                    length,
                    collisionMask,
                    QueryTriggerInteraction.Ignore
                ))
            {
                return pivot + direction.normalized *
                    Mathf.Max(0.2f, hit.distance - collisionRadius);
            }

            return desired;
        }
    }
}
