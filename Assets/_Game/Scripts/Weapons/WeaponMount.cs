using UnityEngine;

namespace ROS.Game.Weapons
{
    public enum WeaponMountPoint
    {
        RightHand,
        Back01,
        Back02,
        Hip
    }

    /// <summary>
    /// Offsets por arma y referencias físicas normalizadas para disparo, apuntado e IK.
    /// Las referencias vacías se descubren automáticamente dentro del prefab del arma.
    ///
    /// Convención de espalda del personaje:
    /// Back01 = lado derecho.
    /// Back02 = lado izquierdo.
    /// </summary>
    public sealed class WeaponMount : MonoBehaviour
    {
        [Header("Right Hand")]
        [SerializeField] private Vector3 handLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 handLocalEulerAngles = Vector3.zero;

        [Header("Back 01 - Right Side")]
        [SerializeField] private Vector3 back01LocalPosition =
            new Vector3(0.01f, 0.08f, -0.036f);
        [SerializeField] private Vector3 back01LocalEulerAngles =
            new Vector3(-180f, -180f, 50f);

        [Header("Back 02 - Left Side")]
        [SerializeField] private Vector3 back02LocalPosition =
            new Vector3(-0.03f, 0.133f, -0.035f);
        [SerializeField] private Vector3 back02LocalEulerAngles =
            new Vector3(-180f, -180f, 120f);

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

        public Transform MuzzlePoint => Resolve(ref muzzlePoint, "MuzzlePoint", transform, transform);
        public Transform AimPoint => Resolve(ref aimPoint, "AimPoint", transform, MuzzlePoint);
        public Transform StockPoint => Resolve(ref stockPoint, "StockPoint", transform, null);
        public Transform RightHandGrip => Resolve(ref rightHandGrip, "RightHandGrip", transform, transform);
        public Transform LeftHandIKTarget => Resolve(ref leftHandIKTarget, "LeftHandIK", transform, null);
        public Transform ShellEjectionPoint => Resolve(ref shellEjectionPoint, "ShellEjectionPoint", transform, MuzzlePoint);

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

        public void Apply(WeaponMountPoint mountPoint)
        {
            switch (mountPoint)
            {
                case WeaponMountPoint.RightHand:
                    ApplyLocalTransform(handLocalPosition, handLocalEulerAngles);
                    break;
                case WeaponMountPoint.Back01:
                    ApplyLocalTransform(back01LocalPosition, back01LocalEulerAngles);
                    break;
                case WeaponMountPoint.Back02:
                    ApplyLocalTransform(back02LocalPosition, back02LocalEulerAngles);
                    break;
                case WeaponMountPoint.Hip:
                    ApplyLocalTransform(hipLocalPosition, hipLocalEulerAngles);
                    break;
            }
        }

        public bool HasCompleteFiringSetup()
        {
            return FindChildRecursive(transform, "MuzzlePoint") != null &&
                   FindChildRecursive(transform, "AimPoint") != null;
        }

        private void ApplyLocalTransform(Vector3 localPosition, Vector3 localEulerAngles)
        {
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEulerAngles);
        }

        private static Transform Resolve(
            ref Transform field,
            string childName,
            Transform searchRoot,
            Transform fallback)
        {
            if (field == null)
                field = FindChildRecursive(searchRoot, childName);

            return field != null ? field : fallback;
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
