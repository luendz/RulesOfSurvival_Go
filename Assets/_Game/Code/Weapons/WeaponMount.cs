using UnityEngine;

namespace ROS.Game.Weapons
{
    public enum WeaponMountPoint
    {
        RightHand,
        Back01,
        Back02,
        Hip,
        BackMelee
    }

    /// <summary>
    /// Offsets por arma y referencias físicas normalizadas para disparo, apuntado e IK.
    ///
    /// Los offsets de espalda corresponden exclusivamente al hijo Visual_*.
    /// Weapon_Back_01 / Weapon_Back_02 y el root lógico del arma (Arma_*/PF_Weapon_*)
    /// permanecen neutrales en el socket.
    /// </summary>
    public sealed class WeaponMount : MonoBehaviour
    {
        [Header("Right Hand")]
        [SerializeField] private Vector3 handLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 handLocalEulerAngles = Vector3.zero;

        [Header("Back 01 - Visual_* / Right Side")]
        [SerializeField] private Vector3 back01LocalPosition =
            new Vector3(0.01f, 0.08f, -0.036f);
        [SerializeField] private Vector3 back01LocalEulerAngles =
            new Vector3(-180f, -180f, 50f);

        [Header("Back 02 - Visual_* / Left Side")]
        [SerializeField] private Vector3 back02LocalPosition =
            new Vector3(-0.03f, 0.133f, -0.035f);
        [SerializeField] private Vector3 back02LocalEulerAngles =
            new Vector3(-180f, -180f, 120f);

        [SerializeField] private Vector3 backVisualScale =
            new Vector3(40f, 40f, 40f);

        [Header("Back Melee - Visual_*")]
        [SerializeField] private Vector3 backMeleeLocalPosition =
            new Vector3(0f, 0.05f, -0.03f);
        [SerializeField] private Vector3 backMeleeLocalEulerAngles =
            new Vector3(90f, 0f, 0f);

        [Header("Hip")]
        [SerializeField] private Vector3 hipLocalPosition = new Vector3(0.18f, -0.05f, 0f);
        [SerializeField] private Vector3 hipLocalEulerAngles = Vector3.zero;

        [Header("Weapon Physical Points")]
        [Tooltip("Boca real del cañón. Fallback: hijo llamado MuzzlePoint.")]
        [SerializeField] private Transform muzzlePoint;
        [Tooltip("Eje visual/mecánico de mira. Fallback: hijo llamado AimPoint.")]
        [SerializeField] private Transform aimPoint;
        [Tooltip("Punto de contacto de la culata con el hombro. Fallback: StockPoint.")]
        [SerializeField] private Transform stockPoint;
        [Tooltip("Agarre de la mano derecha. Fallback: RightHandGrip.")]
        [SerializeField] private Transform rightHandGrip;
        [Tooltip("Agarre/IK de la mano izquierda. Fallback: LeftHandIK.")]
        [SerializeField] private Transform leftHandIKTarget;
        [Tooltip("Expulsión de casquillos. Fallback: ShellEjectionPoint.")]
        [SerializeField] private Transform shellEjectionPoint;
        [Tooltip("Raíz visual editable del prefab del arma.")]
        [SerializeField] private Transform visualRoot;

        private bool _visualDefaultCached;
        private Vector3 _visualDefaultPosition;
        private Quaternion _visualDefaultRotation;
        private Vector3 _visualDefaultScale;

        public Transform MuzzlePoint => muzzlePoint != null ? muzzlePoint : transform;
        public Transform AimPoint => aimPoint != null ? aimPoint : MuzzlePoint;
        public Transform StockPoint => stockPoint;
        public Transform RightHandGrip => rightHandGrip != null ? rightHandGrip : transform;
        public Transform LeftHandIKTarget => leftHandIKTarget;
        public Transform ShellEjectionPoint => shellEjectionPoint != null
            ? shellEjectionPoint
            : MuzzlePoint;

        public Vector3 ShotOrigin => MuzzlePoint != null ? MuzzlePoint.position : transform.position;
        public Vector3 MechanicalForward
        {
            get
            {
                Transform muzzle = MuzzlePoint;
                Transform aim = AimPoint;

                if (aim != null && muzzle != null && aim != muzzle)
                {
                    Vector3 delta = aim.position - muzzle.position;
                    if (delta.sqrMagnitude > 0.0001f)
                        return delta.normalized;
                }

                return muzzle != null ? muzzle.forward : transform.forward;
            }
        }

        private void Awake()
        {
            CacheVisualDefault();
        }

        public void Apply(WeaponMountPoint mountPoint)
        {
            CacheVisualDefault();

            switch (mountPoint)
            {
                case WeaponMountPoint.RightHand:
                    RestoreVisualDefault();
                    ApplyRootLocalTransform(handLocalPosition, handLocalEulerAngles);
                    break;

                case WeaponMountPoint.Back01:
                    ApplyBackVisual(back01LocalPosition, back01LocalEulerAngles);
                    break;

                case WeaponMountPoint.Back02:
                    ApplyBackVisual(back02LocalPosition, back02LocalEulerAngles);
                    break;

                case WeaponMountPoint.BackMelee:
                    ApplyBackVisual(backMeleeLocalPosition, backMeleeLocalEulerAngles);
                    break;

                case WeaponMountPoint.Hip:
                    RestoreVisualDefault();
                    ApplyRootLocalTransform(hipLocalPosition, hipLocalEulerAngles);
                    break;
            }
        }

        public bool HasCompleteFiringSetup()
        {
            return muzzlePoint != null;
        }

        private void ApplyBackVisual(Vector3 localPosition, Vector3 localEulerAngles)
        {
            // Root lógico del arma neutral dentro de Weapon_Back_01 / Weapon_Back_02.
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            Transform visual = ResolveVisualRoot();
            if (visual == null)
                return;

            visual.localPosition = localPosition;
            visual.localRotation = Quaternion.Euler(localEulerAngles);
            visual.localScale = backVisualScale;
        }

        private void ApplyRootLocalTransform(Vector3 localPosition, Vector3 localEulerAngles)
        {
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEulerAngles);
            transform.localScale = Vector3.one;
        }

        private void CacheVisualDefault()
        {
            if (_visualDefaultCached)
                return;

            Transform visual = ResolveVisualRoot();
            if (visual == null)
                return;

            _visualDefaultPosition = visual.localPosition;
            _visualDefaultRotation = visual.localRotation;
            _visualDefaultScale = visual.localScale;
            _visualDefaultCached = true;
        }

        private void RestoreVisualDefault()
        {
            if (!_visualDefaultCached)
                CacheVisualDefault();

            Transform visual = ResolveVisualRoot();
            if (visual == null || !_visualDefaultCached)
                return;

            visual.localPosition = _visualDefaultPosition;
            visual.localRotation = _visualDefaultRotation;
            visual.localScale = _visualDefaultScale;
        }

        private Transform ResolveVisualRoot()
        {
            return visualRoot;
        }
    }
}
