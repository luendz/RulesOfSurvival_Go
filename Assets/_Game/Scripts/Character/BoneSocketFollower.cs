using UnityEngine;

namespace ROS.Game.Character
{
    public sealed class BoneSocketFollower : MonoBehaviour
    {
        [Header("Target Bone")]
        [SerializeField] private Transform targetBone;

        [Header("Local Offset")]
        [SerializeField] private Vector3 positionOffset;
        [SerializeField] private Vector3 rotationOffset;

        [Header("Follow")]
        [SerializeField] private bool followPosition = true;
        [SerializeField] private bool followRotation = true;

        public Transform TargetBone => targetBone;

        public void Bind(Transform bone)
        {
            targetBone = bone;
            SnapToBone();
        }

        public void SnapToBone()
        {
            if (targetBone == null)
                return;

            if (followPosition)
            {
                transform.position =
                    targetBone.TransformPoint(positionOffset);
            }

            if (followRotation)
            {
                transform.rotation =
                    targetBone.rotation *
                    Quaternion.Euler(rotationOffset);
            }
        }

        private void LateUpdate()
        {
            SnapToBone();
        }
    }
}