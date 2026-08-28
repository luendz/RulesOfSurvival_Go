using ROS.Game.Core;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ROS.Game.Weapons
{
    /// <summary>
    /// Drives the left-hand IK only while the currently equipped weapon is aiming.
    /// Normal locomotion, crouch, sprint, jump, fall and hip-fire remain fully
    /// controlled by their animation clips so the arm is not over-constrained.
    /// </summary>
    public sealed class WeaponLeftHandIKController : MonoBehaviour
    {
        [Header("IK")]
        [SerializeField] private TwoBoneIKConstraint leftHandConstraint;
        [SerializeField] private Transform ikProxy;

        [Header("Equipment")]
        [SerializeField] private WeaponEquipmentController equipment;

        [Header("Aim IK")]
        [Range(0f, 1f)]
        [SerializeField] private float aimingWeight = 1f;
        [SerializeField] private float blendInSpeed = 10f;
        [SerializeField] private float blendOutSpeed = 12f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugWeaponEquipped;
        [SerializeField] private string debugActiveWeapon;
        [SerializeField] private bool debugHasLeftHandTarget;
        [SerializeField] private PlayerCombatState debugCombatState;
        [SerializeField] private float debugTargetWeight;
        [SerializeField] private float debugCurrentWeight;

        private WeaponController _activeWeapon;
        private Transform _activeTarget;

        private void Awake()
        {
            ResolveReferences();

            if (leftHandConstraint != null)
                leftHandConstraint.weight = 0f;

            RefreshActiveWeapon();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (equipment != null)
            {
                equipment.WeaponEquipped += OnWeaponEquipped;
                equipment.WeaponHolstered += OnWeaponHolstered;
            }
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.WeaponEquipped -= OnWeaponEquipped;
                equipment.WeaponHolstered -= OnWeaponHolstered;
            }

            ForceReleaseIk();
        }

        private void LateUpdate()
        {
            ResolveReferences();

            if (equipment != null && equipment.EquippedWeapon != _activeWeapon)
                RefreshActiveWeapon();

            bool hasValidTarget = _activeWeapon != null && _activeTarget != null;

            if (hasValidTarget)
                FollowActiveTarget();

            PlayerCombatState combatState = equipment != null
                ? equipment.CombatState
                : PlayerCombatState.Unarmed;

            bool shouldUseIk =
                hasValidTarget &&
                combatState == PlayerCombatState.Aiming;

            // Si ya no existe arma equipada no hacemos blend-out: soltamos la
            // mano inmediatamente. Esto evita que durante unos frames una mano
            // siga intentando alcanzar el socket de un arma ya guardada.
            if (_activeWeapon == null)
                ForceReleaseIk();
            else
                UpdateWeight(shouldUseIk ? aimingWeight : 0f);

            debugWeaponEquipped = _activeWeapon != null;
            debugActiveWeapon = _activeWeapon != null ? _activeWeapon.name : string.Empty;
            debugHasLeftHandTarget = _activeTarget != null;
            debugCombatState = combatState;
            debugTargetWeight = shouldUseIk ? aimingWeight : 0f;
            debugCurrentWeight = leftHandConstraint != null ? leftHandConstraint.weight : 0f;
        }

        private void ResolveReferences()
        {
            if (equipment == null)
                equipment = GetComponentInParent<WeaponEquipmentController>();
        }

        private void OnWeaponEquipped(WeaponController weapon, int slot)
        {
            SetActiveWeapon(weapon);
        }

        private void OnWeaponHolstered(WeaponController weapon, int slot)
        {
            SetActiveWeapon(null);
        }

        private void RefreshActiveWeapon()
        {
            SetActiveWeapon(equipment != null ? equipment.EquippedWeapon : null);
        }

        private void SetActiveWeapon(WeaponController weapon)
        {
            _activeWeapon = weapon;
            _activeTarget = ResolveLeftHandTarget(weapon);

            if (_activeWeapon == null)
            {
                ForceReleaseIk();
                return;
            }

            if (_activeTarget != null && ikProxy != null)
            {
                ikProxy.SetPositionAndRotation(
                    _activeTarget.position,
                    _activeTarget.rotation
                );
            }
        }

        private static Transform ResolveLeftHandTarget(WeaponController weapon)
        {
            if (weapon == null)
                return null;

            WeaponMount mount = weapon.GetComponent<WeaponMount>();
            if (mount != null && mount.LeftHandIKTarget != null)
                return mount.LeftHandIKTarget;

            Transform[] children = weapon.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child.name == "LeftHandIK")
                    return child;
            }

            return null;
        }

        private void FollowActiveTarget()
        {
            if (ikProxy == null || _activeTarget == null)
                return;

            ikProxy.SetPositionAndRotation(
                _activeTarget.position,
                _activeTarget.rotation
            );
        }

        private void ForceReleaseIk()
        {
            if (leftHandConstraint != null)
                leftHandConstraint.weight = 0f;
        }

        private void UpdateWeight(float targetWeight)
        {
            if (leftHandConstraint == null)
                return;

            float speed = targetWeight > leftHandConstraint.weight
                ? blendInSpeed
                : blendOutSpeed;

            leftHandConstraint.weight = Mathf.MoveTowards(
                leftHandConstraint.weight,
                targetWeight,
                speed * Time.deltaTime
            );
        }
    }
}
