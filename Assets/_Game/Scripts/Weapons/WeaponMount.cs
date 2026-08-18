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
    /// Per-weapon local offsets used when the weapon is moved between character sockets.
    /// Also exposes optional weapon-specific IK targets so the player rig can adapt to
    /// whichever weapon is currently equipped.
    /// </summary>
    public sealed class WeaponMount : MonoBehaviour
    {
        [Header("Right Hand")]
        [SerializeField] private Vector3 handLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 handLocalEulerAngles = Vector3.zero;

        [Header("Back 01")]
        [SerializeField] private Vector3 back01LocalPosition = new Vector3(0.18f, 0.05f, -0.12f);
        [SerializeField] private Vector3 back01LocalEulerAngles = new Vector3(0f, 0f, 35f);

        [Header("Back 02")]
        [SerializeField] private Vector3 back02LocalPosition = new Vector3(-0.18f, 0.05f, -0.12f);
        [SerializeField] private Vector3 back02LocalEulerAngles = new Vector3(0f, 0f, -35f);

        [Header("Hip")]
        [SerializeField] private Vector3 hipLocalPosition = new Vector3(0.18f, -0.05f, 0f);
        [SerializeField] private Vector3 hipLocalEulerAngles = Vector3.zero;

        [Header("IK Targets")]
        [Tooltip("Optional. If empty, a child named LeftHandIK is discovered automatically.")]
        [SerializeField] private Transform leftHandIKTarget;

        public Transform LeftHandIKTarget
        {
            get
            {
                if (leftHandIKTarget == null)
                    leftHandIKTarget = FindChildRecursive(transform, "LeftHandIK");

                return leftHandIKTarget;
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

        private void ApplyLocalTransform(Vector3 localPosition, Vector3 localEulerAngles)
        {
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEulerAngles);
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
